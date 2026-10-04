import { spawn } from 'node:child_process';
import { setTimeout as delay } from 'node:timers/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const viteScript = path.join(root, 'node_modules', 'vite', 'bin', 'vite.js');
const electronExecutable = path.join(root, 'node_modules', 'electron', 'dist', 'electron.exe');
const mainEntry = path.join(root, 'dist-electron', 'main', 'main.cjs');
const rendererUrl = 'http://127.0.0.1:5173/';
const buildId = `dev-${new Date().toISOString().replaceAll(/[-:.TZ]/g, '').slice(0, 14)}-${process.pid}`;

let vite;
let ownsVite = false;
let electron;
let stopping = false;
const stopChildren = (code = 0) => {
  if (stopping) return;
  stopping = true;
  electron?.kill();
  if (ownsVite) vite?.kill();
  process.exitCode = code;
};

process.once('SIGINT', () => stopChildren(130));
process.once('SIGTERM', () => stopChildren(143));

async function existingViteIsThisProject() {
  try {
    const response = await fetch(rendererUrl);
    if (!response.ok) return false;
    const html = await response.text();
    return html.includes('id="root"') && html.includes('/ui/main.tsx');
  } catch { return false; }
}

async function waitForVite() {
  if (await existingViteIsThisProject()) {
    console.log('Reusing the existing Automator Vite preview at http://127.0.0.1:5173/.');
    return;
  }
  vite = spawn(process.execPath, [viteScript, '--host', '127.0.0.1'], {
    cwd: root,
    stdio: 'inherit',
    windowsHide: true,
  });
  ownsVite = true;
  vite.once('exit', (code) => { if (!stopping) stopChildren(code ?? 1); });
  let ready = false;
  for (let attempt = 0; attempt < 100; attempt += 1) {
    if (vite.exitCode !== null) throw new Error(`Vite exited with code ${vite.exitCode}.`);
    if (await existingViteIsThisProject()) { ready = true; break; }
    await delay(100);
  }
  if (!ready) throw new Error('Vite did not become ready at http://127.0.0.1:5173/ within 10 seconds.');
}

try {
  await waitForVite();

  const env = { ...process.env };
  for (const name of ['AUTOMATOR_TEST_MODE', 'AUTOMATOR_TEST_TRAY', 'AUTOMATOR_BUILD_ID', 'PORTABLE_EXECUTABLE_FILE']) delete env[name];
  env.AUTOMATOR_BUILD_ID = buildId;
  env.AUTOMATOR_RENDERER_URL = rendererUrl;
  env.AUTOMATOR_TEST_MODE = '1';
  env.AUTOMATOR_TEST_TRAY = '1';
  env.AUTOMATOR_SHOW_ON_START = '1';
  env.AUTOMATOR_BROWSER_NODE_PATH = process.execPath;
  env.AUTOMATOR_BROWSER_WORKER_PATH = path.join(root, 'src', 'Automator.Infrastructure', 'Automation', 'browser-worker.cjs');
  env.AUTOMATOR_PLAYWRIGHT_MODULE_ROOT = path.join(root, 'node_modules', 'playwright');
  electron = spawn(electronExecutable, [mainEntry], {
    cwd: root,
    env,
    stdio: 'inherit',
  });
  electron.once('error', (error) => {
    console.error('Could not start the Electron desktop host:', error);
    stopChildren(1);
  });
  electron.once('exit', (code, signal) => stopChildren(code ?? (signal ? 1 : 0)));
} catch (error) {
  console.error(error instanceof Error ? error.message : String(error));
  stopChildren(1);
}
