import { defineConfig } from 'vite';

export default defineConfig({
  build: {
    outDir: 'dist-electron/main',
    emptyOutDir: true,
    sourcemap: true,
    lib: {
      entry: 'electron/main.ts',
      formats: ['cjs'],
      fileName: () => 'main.cjs',
    },
    rollupOptions: {
      external: ['electron', 'node:fs', 'node:os', 'node:path', 'node:url', 'node:crypto', 'node:events', 'node:child_process'],
    },
  },
});
