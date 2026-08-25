import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './apps/e2e/src',
  fullyParallel: false,
  workers: 1,
  reporter: 'line',
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
  },
  webServer: {
    command: 'pnpm exec nx serve workforce-web --port=4200',
    url: 'http://localhost:4200',
    reuseExistingServer: true,
    timeout: 120000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
