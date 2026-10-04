import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { test } from 'node:test';
import { createServer, loadConfigFromFile } from 'vite';

const workspace = path.resolve(import.meta.dirname, '..');

test('Vite excludes generated outputs and Tailwind scans only renderer sources', async () => {
  const loaded = await loadConfigFromFile(
    { command: 'serve', mode: 'test' },
    path.join(workspace, 'vite.config.ts'),
    workspace,
  );
  assert.ok(loaded, 'the Vite config should load');

  const ignored = loaded.config.server?.watch?.ignored;
  assert.ok(Array.isArray(ignored), 'generated directories should be explicitly ignored by the dev watcher');
  for (const directory of ['artifacts', 'dist-electron', 'bin', 'obj']) {
    assert.ok(ignored.includes(`**/${directory}/**`), `Vite should ignore generated ${directory} directories`);
  }

  const css = await readFile(path.join(workspace, 'ui', 'styles.css'), 'utf8');
  assert.match(css, /@import\s+["']tailwindcss["']\s+source\(["']\.["']\)/,
    'Tailwind source scanning should be rooted at the UI stylesheet directory');

  const runId = randomUUID();
  const artifactDirectory = path.join(workspace, 'artifacts', 'test-data', `vite-watch-${runId}`);
  const artifactProbe = path.join(artifactDirectory, 'cache-probe.js');
  const sourceProbe = path.join(workspace, 'ui', `__vite_watch_probe_${runId}.tsx`);
  let server;
  try {
    server = await createServer({
      configFile: path.join(workspace, 'vite.config.ts'),
      root: workspace,
      logLevel: 'silent',
      server: { host: '127.0.0.1', port: 0, strictPort: false },
    });
    const watchedPaths = [];
    server.watcher.on('all', (_event, filePath) => watchedPaths.push(path.resolve(filePath).toLowerCase()));
    await server.listen();
    const address = server.httpServer.address();
    assert.ok(address && typeof address === 'object');
    const cssResponse = await fetch(`http://127.0.0.1:${address.port}/ui/styles.css`);
    assert.equal(cssResponse.status, 200, 'the dev CSS pipeline should compile');
    await cssResponse.text();

    await mkdir(artifactDirectory, { recursive: true });
    await writeFile(artifactProbe, 'const cacheWrite = "generated";\n', 'utf8');
    await new Promise((resolve) => setTimeout(resolve, 750));
    assert.equal(watchedPaths.includes(artifactProbe.toLowerCase()), false,
      'writing generated Electron cache data must not trigger Vite HMR');

    await writeFile(sourceProbe, 'export const watchProbe = "source";\n', 'utf8');
    const sourceDeadline = Date.now() + 3000;
    while (Date.now() < sourceDeadline && !watchedPaths.includes(sourceProbe.toLowerCase()))
      await new Promise((resolve) => setTimeout(resolve, 50));
    assert.equal(watchedPaths.includes(sourceProbe.toLowerCase()), true,
      'UI source edits must remain watched by Vite');
  } finally {
    await server?.close();
    await rm(sourceProbe, { force: true });
    await rm(artifactDirectory, { recursive: true, force: true });
  }
});
