'use strict';

const fs = require('node:fs');
const readline = require('node:readline');

const maximumTextBytes = 128 * 1024;
const maximumLinkTextBytes = 512;
const maximumLinkUrlBytes = 2048;

function playwright() {
  const moduleRoot = process.env.AUTOMATOR_PLAYWRIGHT_MODULE_ROOT;
  if (!moduleRoot || !fs.existsSync(require('node:path').join(moduleRoot, 'package.json')))
    throw new Error('Playwright module is unavailable.');
  return require(moduleRoot);
}

function status() {
  try {
    const { chromium } = playwright();
    const installed = fs.existsSync(chromium.executablePath());
    process.stdout.write(JSON.stringify({ installed, ready: installed }) + '\n');
    process.exitCode = 0;
  } catch {
    process.stdout.write(JSON.stringify({ installed: false, ready: false }) + '\n');
    process.exitCode = 0;
  }
}

function truncateUtf8(value, maximumBytes) {
  const text = String(value ?? '');
  if (Buffer.byteLength(text, 'utf8') <= maximumBytes) return text;
  let result = '';
  let used = 0;
  for (const character of text) {
    const width = Buffer.byteLength(character, 'utf8');
    if (used + width > maximumBytes) break;
    result += character;
    used += width;
  }
  return result;
}

function locatorFor(page, action) {
  switch (action.locatorKind) {
    case 'role': return page.getByRole(action.locator);
    case 'label': return page.getByLabel(action.locator);
    case 'placeholder': return page.getByPlaceholder(action.locator);
    case 'text': return page.getByText(action.locator);
    case 'testId': return page.getByTestId(action.locator);
    default: throw new Error('Unsupported locator.');
  }
}

async function readResult(page) {
  return {
    currentUrl: page.url(),
    title: null,
    text: null,
    links: [],
  };
}

async function execute(page, action) {
  const timeout = action.timeoutMilliseconds;
  if (!Number.isInteger(timeout) || timeout < 250 || timeout > 30000) throw new Error('Invalid timeout.');
  switch (action.kind) {
    case 'navigate':
      if (typeof action.url !== 'string' || action.url.length > 4096) throw new Error('Invalid URL.');
      await page.goto(action.url, { waitUntil: 'domcontentloaded', timeout });
      break;
    case 'click':
      await locatorFor(page, action).click({ timeout });
      break;
    case 'fill':
      if (typeof action.value !== 'string' || Buffer.byteLength(action.value, 'utf8') > 32 * 1024) throw new Error('Invalid value.');
      await locatorFor(page, action).fill(action.value, { timeout });
      break;
    case 'select':
      if (typeof action.value !== 'string' || Buffer.byteLength(action.value, 'utf8') > 32 * 1024) throw new Error('Invalid value.');
      await locatorFor(page, action).selectOption(action.value, { timeout });
      break;
    case 'waitFor':
      await locatorFor(page, action).waitFor({ state: 'visible', timeout });
      break;
    case 'readTitle':
      break;
    case 'readText':
      break;
    case 'listLinks':
      break;
    default:
      throw new Error('Unsupported browser action.');
  }

  const result = await readResult(page);
  if (action.kind === 'readTitle') result.title = truncateUtf8(await page.title(), 4096);
  if (action.kind === 'readText') result.text = truncateUtf8(await locatorFor(page, action).innerText({ timeout }), maximumTextBytes);
  if (action.kind === 'listLinks') {
    const links = page.locator('a');
    const count = Math.min(await links.count(), 64);
    for (let index = 0; index < count; index += 1) {
      const link = links.nth(index);
      const href = await link.getAttribute('href', { timeout });
      if (!href) continue;
      result.links.push({ text: truncateUtf8(await link.innerText({ timeout }).catch(() => ''), maximumLinkTextBytes), url: truncateUtf8(href, maximumLinkUrlBytes) });
    }
  }
  return result;
}

async function run() {
  const { chromium } = playwright();
  const required = ['AUTOMATOR_BROWSER_PROFILE_DIR', 'AUTOMATOR_BROWSER_PROXY_SERVER',
    'AUTOMATOR_BROWSER_PROXY_USERNAME', 'AUTOMATOR_BROWSER_PROXY_PASSWORD', 'PLAYWRIGHT_BROWSERS_PATH'];
  if (required.some(key => !process.env[key])) throw new Error('Browser worker configuration is incomplete.');

  const context = await chromium.launchPersistentContext(process.env.AUTOMATOR_BROWSER_PROFILE_DIR, {
    headless: true,
    acceptDownloads: false,
    serviceWorkers: 'block',
    proxy: {
      server: process.env.AUTOMATOR_BROWSER_PROXY_SERVER,
      username: process.env.AUTOMATOR_BROWSER_PROXY_USERNAME,
      password: process.env.AUTOMATOR_BROWSER_PROXY_PASSWORD,
      bypass: '<-loopback>',
    },
    args: [
      '--proxy-bypass-list=<-loopback>',
      '--disable-quic',
      '--force-webrtc-ip-handling-policy=disable_non_proxied_udp',
      '--host-resolver-rules=MAP * ~NOTFOUND, EXCLUDE localhost, EXCLUDE 127.0.0.1',
      '--disable-background-networking',
      '--disable-component-update',
      '--disable-sync',
      '--disable-default-apps',
      '--no-first-run',
    ],
  });
  await context.route('**/*', async route => {
    let url;
    try { url = new URL(route.request().url()); }
    catch { await route.abort('blockedbyclient'); return; }
    if (url.protocol !== 'http:' && url.protocol !== 'https:') {
      await route.abort('blockedbyclient');
      return;
    }
    await route.continue();
  });
  const pages = context.pages();
  const page = pages[0] || await context.newPage();
  const input = readline.createInterface({ input: process.stdin, crlfDelay: Infinity });
  process.stdout.write(JSON.stringify({ type: 'ready' }) + '\n');
  let queue = Promise.resolve();
  input.on('line', line => {
    queue = queue.then(async () => {
      let command;
      try {
        command = JSON.parse(line);
        if (!command || typeof command.id !== 'string' || !command.action || typeof command.action !== 'object') throw new Error('Invalid command.');
        const result = await execute(page, command.action);
        process.stdout.write(JSON.stringify({ id: command.id, ok: true, result }) + '\n');
      } catch {
        process.stdout.write(JSON.stringify({ id: command?.id ?? '', ok: false, error: 'action-failed' }) + '\n');
      }
    });
  });
  input.on('close', async () => {
    await queue.catch(() => {});
    await context.close().catch(() => {});
    process.exit(0);
  });
}

if (process.argv.includes('--status')) status();
else run().catch(() => {
  process.stdout.write(JSON.stringify({ type: 'error', error: 'runtime-unavailable' }) + '\n');
  process.exitCode = 2;
});
