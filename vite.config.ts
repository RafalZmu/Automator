import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

const developmentCspMeta = {
  name: 'automator-development-csp',
  apply: 'serve' as const,
  transformIndexHtml(html: string) {
    return html.replace("script-src 'self';", "script-src 'self' 'unsafe-inline';");
  },
};

export default defineConfig({
  base: './',
  plugins: [developmentCspMeta, react(), tailwindcss()],
  server: {
    host: '127.0.0.1',
    strictPort: true,
    port: 5173,
    watch: {
      ignored: [
        '**/node_modules/**',
        '**/.git/**',
        '**/artifacts/**',
        '**/dist-electron/**',
        '**/bin/**',
        '**/obj/**',
      ],
    },
  },
  build: {
    outDir: 'dist-electron/renderer',
    emptyOutDir: false,
    sourcemap: true,
  },
});
