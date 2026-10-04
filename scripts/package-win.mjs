import { spawn } from 'node:child_process';
import { cp, copyFile, mkdir, readFile, rm, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const outputDirectory = path.resolve(root, 'artifacts', 'electron-dist');
const backendDirectory = path.resolve(root, 'artifacts', 'backend', 'win-x64');
const browserRuntimeDirectory = path.resolve(root, 'artifacts', 'browser-runtime');
const packageJson = JSON.parse(await readFile(path.join(root, 'package.json'), 'utf8'));

function assertInsideWorkspace(target) {
  const relative = path.relative(root, target);
  if (!relative || relative === '..' || relative.startsWith(`..${path.sep}`) || path.isAbsolute(relative)) {
    throw new Error(`Refusing to alter a path outside the generated workspace output: ${target}`);
  }
}

function run(command, args, label, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { cwd: options.cwd ?? root, env: options.env ?? process.env, stdio: 'inherit', windowsHide: true });
    child.once('error', reject);
    child.once('exit', (code, signal) => {
      if (code === 0) resolve();
      else reject(new Error(`${label} failed (code=${code ?? 'null'}, signal=${signal ?? 'none'}).`));
    });
  });
}

const npmCli = process.env.npm_execpath;
if (!npmCli) throw new Error('Run this script through `npm run package:win` so npm supplies its CLI path.');
await run(process.execPath, [npmCli, 'run', 'typecheck'], 'TypeScript validation');
await run(process.execPath, [npmCli, 'run', 'build'], 'Electron and renderer build');

assertInsideWorkspace(backendDirectory);
await rm(backendDirectory, { recursive: true, force: true });
await mkdir(path.dirname(backendDirectory), { recursive: true });
await run('dotnet', [
  'publish', 'src/Automator.Backend/Automator.Backend.csproj',
  '--configuration', 'Release', '--runtime', 'win-x64', '--self-contained', 'true',
  '--output', backendDirectory,
  '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true',
], 'Self-contained Windows x64 backend publish');

assertInsideWorkspace(browserRuntimeDirectory);
if (process.arch !== 'x64' || Number(process.versions.node.split('.')[0]) < 20)
  throw new Error('Windows packaging requires a 64-bit Node.js 20 or newer runtime for Browser Automation.');
await rm(browserRuntimeDirectory, { recursive: true, force: true });
const browserPackagesDirectory = path.join(browserRuntimeDirectory, 'packages');
await mkdir(browserPackagesDirectory, { recursive: true });
await copyFile(process.execPath, path.join(browserRuntimeDirectory, 'node.exe'));
// Keep the runtime packages outside a directory named node_modules. electron-builder
// filters development node_modules out of extraResources, even when explicitly staged.
await cp(path.join(root, 'node_modules', 'playwright'), path.join(browserPackagesDirectory, 'playwright'), { recursive: true });
await cp(path.join(root, 'node_modules', 'playwright-core'), path.join(browserPackagesDirectory, 'playwright-core'), { recursive: true });
await copyFile(path.join(root, 'src', 'Automator.Infrastructure', 'Automation', 'browser-worker.cjs'),
  path.join(browserRuntimeDirectory, 'browser-worker.cjs'));
await run(path.join(browserRuntimeDirectory, 'node.exe'), [
  '--eval',
  "const playwright = require('./playwright'); const core = require('playwright-core'); if (!playwright.chromium || !core.chromium) process.exit(1);",
], 'Staged Playwright runtime validation', {
  cwd: browserPackagesDirectory,
  env: { ...process.env, NODE_PATH: browserPackagesDirectory },
});

assertInsideWorkspace(outputDirectory);
await rm(outputDirectory, { recursive: true, force: true });
await mkdir(outputDirectory, { recursive: true });
await run(process.execPath, [path.join(root, 'node_modules', 'electron-builder', 'cli.js'), '--win', '--x64'], 'Windows x64 packaging');

const stableDirectory = path.join(outputDirectory, 'win-unpacked');
const stableExecutable = path.join(stableDirectory, 'Automator.exe');
const asar = path.join(stableDirectory, 'resources', 'app.asar');
const backend = path.join(stableDirectory, 'resources', 'backend', 'Automator.Backend.exe');
const browserNode = path.join(stableDirectory, 'resources', 'browser-runtime', 'node.exe');
const browserWorker = path.join(stableDirectory, 'resources', 'browser-runtime', 'browser-worker.cjs');
const playwrightPackage = path.join(stableDirectory, 'resources', 'browser-runtime', 'packages', 'playwright', 'package.json');
const playwrightCorePackage = path.join(stableDirectory, 'resources', 'browser-runtime', 'packages', 'playwright-core', 'package.json');
await Promise.all([stat(stableExecutable), stat(asar), stat(backend), stat(browserNode), stat(browserWorker), stat(playwrightPackage), stat(playwrightCorePackage)]);
const portable = path.join(outputDirectory, `Automator-${packageJson.version}-x64-portable.exe`);
await stat(portable);

const manifest = {
  formatVersion: 1,
  productName: 'Automator',
  version: packageJson.version,
  buildId: `release-${packageJson.version}-${new Date().toISOString().replaceAll(/[-:.TZ]/g, '')}`,
  builtAtUtc: new Date().toISOString(),
  executable: 'Automator.exe',
  applicationArchive: 'resources/app.asar',
  backendExecutable: 'resources/backend/Automator.Backend.exe',
  backendRuntime: 'win-x64-self-contained',
  browserNodeExecutable: 'resources/browser-runtime/node.exe',
  browserWorkerScript: 'resources/browser-runtime/browser-worker.cjs',
  playwrightPackage: 'resources/browser-runtime/packages/playwright',
  playwrightCorePackage: 'resources/browser-runtime/packages/playwright-core',
  browserRuntimeNodeMajor: Number(process.versions.node.split('.')[0]),
};
await writeFile(path.join(stableDirectory, 'automator-build.json'), `${JSON.stringify(manifest, null, 2)}\n`, 'utf8');
console.log(`Stable unpacked app: ${stableExecutable}`);
console.log(`Portable package: ${portable}`);
