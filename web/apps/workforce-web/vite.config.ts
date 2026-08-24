/// <reference types='vitest' />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  root: import.meta.dirname,
  cacheDir: '../../node_modules/.vite/apps/workforce-web',
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    include: ['src/**/*.{test,spec}.{js,mjs,cjs,ts,mts,cts,jsx,tsx}'],
  },
  server: {
    port: 4200,
    host: '127.0.0.1',
    proxy: {
      '/health': {
        target: 'http://127.0.0.1:5041',
        changeOrigin: true,
      },
      '/api': {
        target: 'http://127.0.0.1:5041',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: '../../dist/apps/workforce-web',
    emptyOutDir: true,
    reportCompressedSize: true,
    commonjsOptions: {
      transformMixedEsModules: true,
    },
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (
            id.includes('ag-grid-community') ||
            id.includes('ag-grid-enterprise') ||
            id.includes('ag-grid-react') ||
            id.includes('ZainXDataGrid')
          ) {
            return 'vendor-aggrid';
          }
          if (id.includes('packages/people/src/components/EmployeeDirectory')) {
            return 'people-directory';
          }
          if (
            id.includes('packages/people/src/components/EmployeeProfile') ||
            id.includes('packages/people/src/components/ChangeAssignmentModal')
          ) {
            return 'people-workspace';
          }
          if (id.includes('packages/attendance')) {
            return 'module-attendance';
          }
          if (id.includes('packages/leave')) {
            return 'module-leave';
          }
          if (id.includes('packages/approvals')) {
            return 'module-approvals';
          }
        },
      },
    },
  },
});
