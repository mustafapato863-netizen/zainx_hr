import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  root: import.meta.dirname,
  cacheDir: '../../node_modules/.vite/apps/design-system-docs',
  plugins: [react()],
  server: {
    port: 4400,
    host: 'localhost',
  },
  build: {
    outDir: '../../dist/apps/design-system-docs',
    emptyOutDir: true,
    reportCompressedSize: true,
  },
});
