// Preloaded into every Electron test process (node --require). Playwright's app.close(), app.evaluate() and
// page.evaluate() have no timeout, so one stuck Electron instance used to hold the whole CI job until its limit.
// This bounds those calls, says which call hung and what the instance last printed, and kills what outlives a file.
const { execFileSync } = require('node:child_process');
const { _electron: electron } = require('playwright');

// Generous against CI timings: the slowest whole test takes 22 s, so a single call or close that needs more is stuck.
const LAUNCH_TIMEOUT_MS = 30_000;
const CALL_TIMEOUT_MS = 20_000;
const CLOSE_TIMEOUT_MS = 15_000;
const OUTPUT_LINES = 40;
const live = new Set();

function withTimeout(promise, ms, description) {
  let timer;
  const timeout = new Promise((_, reject) => { timer = setTimeout(() => reject(new Error(`HANG: ${description} did not finish within ${ms} ms.`)), ms); });
  return Promise.race([promise, timeout]).finally(() => clearTimeout(timer));
}

function killTree(child) {
  if (!child?.pid) return;
  try {
    if (process.platform === 'win32') execFileSync('taskkill', ['/PID', String(child.pid), '/T', '/F'], { stdio: 'ignore', timeout: 15_000 });
    else child.kill('SIGKILL');
  } catch { /* already gone */ }
}

function describeFunction(fn) { return String(fn).replace(/\s+/g, ' ').slice(0, 160); }

function boundPage(page, pid) {
  if (!page || page.__bounded) return page;
  page.__bounded = true;
  const evaluate = page.evaluate.bind(page);
  page.evaluate = (fn, arg) => withTimeout(evaluate(fn, arg), CALL_TIMEOUT_MS, `page.evaluate in Electron pid ${pid}: ${describeFunction(fn)}`);
  return page;
}

function boundApp(app, name) {
  const child = app.process();
  const pid = child?.pid;
  const output = [];
  const capture = (kind) => (chunk) => {
    for (const line of String(chunk).split(/\r?\n/)) if (line) output.push(`${kind}: ${line}`);
    if (output.length > OUTPUT_LINES) output.splice(0, output.length - OUTPUT_LINES);
  };
  child?.stdout?.on('data', capture('stdout'));
  child?.stderr?.on('data', capture('stderr'));
  live.add(child);
  child?.once('exit', () => live.delete(child));

  const report = (message) => console.log(`${message}\nElectron pid ${pid} (${name}) last output:\n${output.join('\n') || '(none)'}`);
  const close = app.close.bind(app);
  app.close = async () => {
    try { await withTimeout(close(), CLOSE_TIMEOUT_MS, `app.close of Electron pid ${pid}`); }
    catch (error) { report(error.message); killTree(child); throw error; }
  };
  const evaluate = app.evaluate.bind(app);
  app.evaluate = (fn, arg) => withTimeout(evaluate(fn, arg), CALL_TIMEOUT_MS, `app.evaluate in Electron pid ${pid}: ${describeFunction(fn)}`)
    .catch((error) => { if (String(error.message).startsWith('HANG:')) report(error.message); throw error; });
  const firstWindow = app.firstWindow.bind(app);
  app.firstWindow = async (...args) => boundPage(await firstWindow(...args), pid);
  const windows = app.windows.bind(app);
  app.windows = () => windows().map((page) => boundPage(page, pid));
  app.on('window', (page) => boundPage(page, pid));
  return app;
}

const launch = electron.launch.bind(electron);
electron.launch = async (options) => {
  const name = options?.env?.AUTOMATOR_BUILD_ID ?? 'electron';
  return boundApp(await launch({ ...options, timeout: Math.min(options?.timeout ?? LAUNCH_TIMEOUT_MS, LAUNCH_TIMEOUT_MS) }), name);
};

process.on('exit', () => { for (const child of live) killTree(child); });
