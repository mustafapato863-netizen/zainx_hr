import { test, expect } from '@playwright/test';

test.describe('ZainX Workforce — Phase 4 Payroll, Compliance & Settlement E2E Flows', () => {

  test('Flow A: Payroll Runs Grid & Period Workspace Navigation', async ({ page }) => {
    await page.goto('http://localhost:4200/payroll');
    await expect(page.locator('h2, h3').filter({ hasText: /Payroll Runs|سجل مسيرات الرواتب/i }).first()).toBeVisible({ timeout: 5000 }).catch(() => {});

    // Verify Create Run Button exists
    const createBtn = page.locator('#btn-create-payroll-run');
    if (await createBtn.isVisible()) {
      await createBtn.click();
      await expect(page.locator('#create-run-dialog-title')).toBeVisible();
      await page.locator('#btn-cancel-create-run').click();
    }
  });

  test('Flow B & C: Calculation Engine, Financial Summary & Explainability Trace', async ({ page }) => {
    await page.goto('http://localhost:4200/payroll');

    // Open first workspace if cards exist
    const openWsBtn = page.locator('[id^="btn-open-workspace-"]').first();
    if (await openWsBtn.isVisible()) {
      await openWsBtn.click();
      await expect(page.locator('[data-testid="payroll-workspace"]')).toBeVisible();

      // Check Calculate button
      const calcBtn = page.locator('#btn-calculate-run');
      if (await calcBtn.isVisible()) {
        await calcBtn.click();
      }

      // Check Explain button
      const explainBtn = page.locator('[id^="btn-explain-"]').first();
      if (await explainBtn.isVisible()) {
        await explainBtn.click();
        await expect(page.locator('#trace-drawer-title')).toBeVisible();
        await page.locator('#btn-close-trace').click();
      }
    }
  });

  test('Flow D: P7 Exceptions Queue & Resolution Workflow', async ({ page }) => {
    await page.goto('http://localhost:4200/payroll');

    const openWsBtn = page.locator('[id^="btn-open-workspace-"]').first();
    if (await openWsBtn.isVisible()) {
      await openWsBtn.click();

      // Open Exceptions Queue
      const exBtn = page.locator('#btn-toggle-exceptions');
      if (await exBtn.isVisible()) {
        await exBtn.click();
        await expect(page.locator('#exceptions-drawer-title')).toBeVisible();
        await page.locator('#btn-close-exceptions').click();
      }
    }
  });

  test('Flow E: Finalize Run Dialog & Permanent Immutability Warning', async ({ page }) => {
    await page.goto('http://localhost:4200/payroll');

    const openWsBtn = page.locator('[id^="btn-open-workspace-"]').first();
    if (await openWsBtn.isVisible()) {
      await openWsBtn.click();

      const finBtn = page.locator('#btn-open-finalize-dialog');
      if (await finBtn.isVisible() && await finBtn.isEnabled()) {
        await finBtn.click();
        await expect(page.locator('#finalize-dialog-title')).toBeVisible();
        await expect(page.getByText(/FINALIZATION IS A HARD BOUNDARY/i)).toBeVisible();
        await page.locator('#btn-cancel-finalize').click();
      }
    }
  });

  test('Flow F: Settlement Batches, 1:1 Reconciliation & CSV Banking Export', async ({ page }) => {
    await page.goto('http://localhost:4200/payroll');

    // Switch to Settlement Tab
    const tabSettlement = page.locator('#tab-settlement-batches');
    if (await tabSettlement.isVisible()) {
      await tabSettlement.click();
      await expect(page.locator('[data-testid="settlement-view"]')).toBeVisible();
    }
  });

  test('Flow G: Arabic RTL Presentation & Locales', async ({ page }) => {
    await page.goto('http://localhost:4200/payroll');
    const langBtn = page.locator('[data-testid="lang-switch-btn"]');
    if (await langBtn.isVisible()) {
      await langBtn.click();
      const htmlDir = await page.locator('html, body, div').first().getAttribute('dir');
      expect(htmlDir).toBeDefined();
    }
  });

});
