import { defineConfig } from 'vite';

export default defineConfig({
  build: {
    outDir: 'artifacts/test-tools/backend-process',
    emptyOutDir: true,
    lib: {
      entry: 'electron/backendProcess.ts',
      formats: ['es'],
      fileName: () => 'backendProcess.mjs',
    },
    rollupOptions: {
      external: ['node:events', 'node:child_process'],
    },
  },
});
