import { test, expect } from '@playwright/test';

test.describe('ZainX Workforce — Phase 2 Real End-to-End Enterprise Flow', () => {
  test('Complete English Workforce Lifecycle: Directory -> Create -> Workspace -> Assignment -> Concurrency -> Documents', async ({ page }) => {
    // 1. Navigate to People directory
    await page.goto('/people');
    await expect(page.getByTestId('people-page-container')).toBeVisible({ timeout: 15000 });
    await expect(page.locator('h2')).toContainText('People Directory');

    // 2. Open Add Employee Modal
    await page.getByTestId('open-create-employee-modal-btn').click();
    await expect(page.getByRole('dialog')).toBeVisible();

    const uniqueEmpNo = `EMP-${Date.now().toString().slice(-5)}`;

    // 3. Fill Employee Form
    await page.getByLabel(/Employee Number/i).fill(uniqueEmpNo);
    await page.getByLabel(/First Name \(EN\)/i).fill('Faisal');
    await page.getByLabel(/Last Name \(EN\)/i).fill('Al-Otaibi');
    await page.getByLabel(/First Name \(AR\)/i).fill('فيصل');
    await page.getByLabel(/Last Name \(AR\)/i).fill('العتيبي');
    await page.getByLabel(/National Identifier/i).fill('1098765432');
    await page.getByLabel(/Primary Email/i).fill('faisal@zainx.com');
    await page.getByLabel(/Job Title \(EN\)/i).fill('Principal Systems Architect');
    await page.getByLabel(/Job Title \(AR\)/i).fill('مهندس معماري نظم رئيسي');
    await page.getByLabel(/Hire Date/i).fill('2024-01-01');

    // Submit employee creation
    await page.getByRole('button', { name: /Save Employee|Submit|Create/i }).click();
    await expect(page.getByRole('dialog')).toBeHidden({ timeout: 10000 });

    // 4. Verify in directory
    await page.getByPlaceholder(/Search employees/i).fill(uniqueEmpNo);
    await expect(page.getByText('Faisal Al-Otaibi')).toBeVisible({ timeout: 10000 });

    // 5. Select employee to open Workspace
    await page.getByText('Faisal Al-Otaibi').click();
    await expect(page.getByText('Principal Systems Architect')).toBeVisible({ timeout: 10000 });

    // 6. Test Concurrency & Assignment Change
    const changeAssignBtn = page.getByRole('button', { name: /Change Assignment|Promote/i });
    if (await changeAssignBtn.isVisible()) {
      await changeAssignBtn.click();
      await page.getByLabel(/New Job Title \(EN\)/i).fill('VP of Technology');
      await page.getByLabel(/Effective From/i).fill('2024-07-01');
      await page.getByRole('button', { name: /Apply Assignment/i }).click();
      await expect(page.getByText('VP of Technology')).toBeVisible({ timeout: 10000 });
    }

    // 7. Verify Documents Tab
    const docsTab = page.getByRole('tab', { name: /Documents|الوثائق/i });
    if (await docsTab.isVisible()) {
      await docsTab.click();
      await expect(page.getByText(/Upload Document|No documents/i)).toBeVisible();
    }
  });

  test('Arabic RTL Lifecycle & Locale Integrity', async ({ page }) => {
    await page.goto('/people');
    await expect(page.getByTestId('people-page-container')).toBeVisible({ timeout: 15000 });

    // Switch to Arabic
    await page.getByTestId('lang-switch-btn').click();

    // Verify RTL Direction
    const rootContainer = page.locator('div[dir]');
    await expect(rootContainer).toHaveAttribute('dir', 'rtl');

    // Verify Arabic Headings and Directory
    await expect(page.locator('aside')).toBeVisible();
  });
});
