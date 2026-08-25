import { defineConfig } from 'vitest/config';
import path from 'path';

export default defineConfig({
  resolve: {
    alias: {
      '@zainx/platform': path.resolve(__dirname, 'packages/platform/src'),
      '@zainx/design-system': path.resolve(__dirname, 'packages/design-system/src'),
      '@zainx/contracts': path.resolve(__dirname, 'packages/contracts/src'),
      '@zainx/people': path.resolve(__dirname, 'packages/people/src'),
      '@zainx/attendance': path.resolve(__dirname, 'packages/attendance/src'),
      '@zainx/leave': path.resolve(__dirname, 'packages/leave/src'),
      '@zainx/payroll': path.resolve(__dirname, 'packages/payroll/src'),
      '@zainx/recruitment': path.resolve(__dirname, 'packages/recruitment/src'),
      '@zainx/approvals': path.resolve(__dirname, 'packages/approvals/src'),
      '@zainx/reports': path.resolve(__dirname, 'packages/reports/src'),
      '@zainx/administration': path.resolve(__dirname, 'packages/administration/src'),
      '@zainx/ai': path.resolve(__dirname, 'packages/ai/src')
    }
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: [path.resolve(__dirname, 'vitest.setup.ts')],
    include: [
      'apps/**/*.{test,spec}.{ts,tsx}',
      'packages/**/*.{test,spec}.{ts,tsx}'
    ],
    exclude: ['apps/e2e/**', 'node_modules/**', '**/dist/**']
  }
});
