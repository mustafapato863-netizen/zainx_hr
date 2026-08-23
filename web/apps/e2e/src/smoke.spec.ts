import { test, expect } from '@playwright/test';

test.describe('ZainX Workforce Health Smoke', () => {
  test('should load application shell and display healthy status from backend API', async ({ page }) => {
    await page.goto('http://127.0.0.1:4200');
    await expect(page.locator('h1')).toContainText('ZainX Workforce');
    await expect(page.getByTestId('health-status')).toContainText('Healthy', { timeout: 10000 });
  });
});
