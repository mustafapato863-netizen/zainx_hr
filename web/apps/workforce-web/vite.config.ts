/// <reference types='vitest' />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  root: import.meta.dirname,
  cacheDir: '../../node_modules/.vite/apps/workforce-web',
  plugins: [react()],
  resolve: {
    alias: [
      { find: /^@zainx\/platform\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/platform/src/$1') },
      { find: '@zainx/platform', replacement: path.resolve(import.meta.dirname, '../../packages/platform/src/index.ts') },
      { find: /^@zainx\/design-system\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/design-system/src/$1') },
      { find: '@zainx/design-system', replacement: path.resolve(import.meta.dirname, '../../packages/design-system/src/index.ts') },
      { find: /^@zainx\/contracts\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/contracts/src/$1') },
      { find: '@zainx/contracts', replacement: path.resolve(import.meta.dirname, '../../packages/contracts/src/index.ts') },
      { find: /^@zainx\/people\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/people/src/$1') },
      { find: '@zainx/people', replacement: path.resolve(import.meta.dirname, '../../packages/people/src/index.ts') },
      { find: /^@zainx\/attendance\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/attendance/src/$1') },
      { find: '@zainx/attendance', replacement: path.resolve(import.meta.dirname, '../../packages/attendance/src/index.ts') },
      { find: /^@zainx\/leave\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/leave/src/$1') },
      { find: '@zainx/leave', replacement: path.resolve(import.meta.dirname, '../../packages/leave/src/index.ts') },
      { find: /^@zainx\/payroll\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/payroll/src/$1') },
      { find: '@zainx/payroll', replacement: path.resolve(import.meta.dirname, '../../packages/payroll/src/index.ts') },
      { find: /^@zainx\/recruitment\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/recruitment/src/$1') },
      { find: '@zainx/recruitment', replacement: path.resolve(import.meta.dirname, '../../packages/recruitment/src/index.ts') },
      { find: /^@zainx\/approvals\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/approvals/src/$1') },
      { find: '@zainx/approvals', replacement: path.resolve(import.meta.dirname, '../../packages/approvals/src/index.ts') },
      { find: /^@zainx\/reports\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/reports/src/$1') },
      { find: '@zainx/reports', replacement: path.resolve(import.meta.dirname, '../../packages/reports/src/index.ts') },
      { find: /^@zainx\/administration\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/administration/src/$1') },
      { find: '@zainx/administration', replacement: path.resolve(import.meta.dirname, '../../packages/administration/src/index.ts') },
      { find: /^@zainx\/ai\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/ai/src/$1') },
      { find: '@zainx/ai', replacement: path.resolve(import.meta.dirname, '../../packages/ai/src/index.ts') },
    ],
  },
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
          if (id.includes('packages/payroll')) {
            return 'module-payroll';
          }
          if (id.includes('packages/recruitment')) {
            return 'module-recruitment';
          }
        },
      },
    },
  },
});
