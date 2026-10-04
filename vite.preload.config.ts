import { defineConfig } from 'vite';

export default defineConfig({
  build: {
    outDir: 'dist-electron/preload',
    emptyOutDir: true,
    sourcemap: true,
    lib: {
      entry: 'electron/preload.ts',
      formats: ['cjs'],
      fileName: () => 'preload.cjs',
    },
    rollupOptions: {
      external: ['electron'],
    },
  },
});
