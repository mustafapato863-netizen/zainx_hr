import { test, expect } from '@playwright/test';

test.describe('ZainX Workforce Shell Smoke', () => {
  test('should load the command center shell and display live operational status', async ({ page }) => {
    await page.goto('http://127.0.0.1:4200');
    await expect(page.getByTestId('home-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: /Workspace overview/i })).toBeVisible();
    await expect(page.getByText('Live', { exact: true })).toBeVisible();
  });
});
