/// <reference types='vitest' />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'path';

export default defineConfig({
  root: import.meta.dirname,
  cacheDir: '../../node_modules/.vite/apps/workforce-web',
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: [
      { find: /^@zainx\/platform\/(.*)$/, replacement: path.resolve(import.meta.dirname, '../../packages/platform/src/$1') },
      { find: '@zainx/platform', replacement: path.resolve(import.meta.dirname, '../../packages/platform/src/index.ts') },
      { find: '@zainx/design-system/enterprise', replacement: path.resolve(import.meta.dirname, '../../packages/design-system/src/enterprise.ts') },
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
    // Route widgets are loaded by the active route. Avoid emitting preload
    // hints for every known route into the initial Home document.
    modulePreload: false,
    commonjsOptions: {
      transformMixedEsModules: true,
    },
    rolldownOptions: {
      output: {
        manualChunks(id) {
          const norm = id.replace(/\\/g, '/');

          // 1. Heavy Enterprise Engine Vendor Chunks (Strictly isolated from initial shell)
          if (
            norm.includes('ag-grid') ||
            norm.includes('ZainXDataGrid')
          ) {
            return 'vendor-aggrid';
          }
          if (
            norm.includes('echarts') ||
            norm.includes('zrender') ||
            norm.includes('ZainXChart')
          ) {
            return 'vendor-echarts';
          }
          if (
            norm.includes('fullcalendar') ||
            norm.includes('ZainXScheduler')
          ) {
            return 'vendor-fullcalendar';
          }
          if (
            norm.includes('tiptap') ||
            norm.includes('dompurify') ||
            norm.includes('prosemirror') ||
            norm.includes('ZainXRichTextEditor')
          ) {
            return 'vendor-tiptap';
          }
          if (
            norm.includes('dnd-kit') ||
            norm.includes('ZainXDnD')
          ) {
            return 'vendor-dndkit';
          }

          // 2. Feature Package Modular Chunks
          if (norm.includes('packages/people/src/components/EmployeeDirectory')) {
            return 'people-directory';
          }
          if (
            norm.includes('packages/people/src/components/EmployeeProfile') ||
            norm.includes('packages/people/src/components/ChangeAssignmentModal')
          ) {
            return 'people-workspace';
          }
          if (norm.includes('packages/people')) {
            return 'module-people';
          }
          if (norm.includes('packages/attendance')) {
            return 'module-attendance';
          }
          if (norm.includes('packages/leave')) {
            return 'module-leave';
          }
          if (norm.includes('packages/approvals')) {
            return 'module-approvals';
          }
          if (norm.includes('packages/payroll')) {
            return 'module-payroll';
          }
          if (norm.includes('packages/recruitment')) {
            return 'module-recruitment';
          }
          if (norm.includes('packages/reports')) {
            return 'module-reports';
          }
          if (norm.includes('packages/administration')) {
            return 'module-administration';
          }
          if (norm.includes('packages/ai')) {
            return 'module-ai';
          }
        },
      },
    },
  },
});
