import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    include: ['apps/workforce-web/src/**/*.{test,spec}.{ts,tsx}'],
    exclude: ['apps/e2e/**', 'node_modules/**'],
  },
});
