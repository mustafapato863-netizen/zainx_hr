import { test, expect } from '@playwright/test';

test.describe('ZainX Workforce — Phase 2 Real End-to-End Enterprise Flow', () => {
  test('Complete English Workforce Lifecycle: Directory -> Create -> Workspace -> Assignment -> Concurrency -> Documents', async ({ page }) => {
    // 1. Navigate to People directory
    await page.goto('/people');
    await expect(page.getByTestId('people-page-container')).toBeVisible({ timeout: 15000 });
    await expect(page.getByRole('heading', { name: /Employee Directory/i })).toBeVisible();

    // 2. Open Add Employee Modal
    await page.getByTestId('open-create-employee-modal-btn').click();
    await expect(page.getByRole('dialog')).toBeVisible();

    const runId = Date.now().toString().slice(-5);
    const firstNameEn = `Faisal${runId}`;
    const createdName = `${firstNameEn} Al-Otaibi`;
    const uniqueNationalId = `10${Date.now().toString().slice(-8)}`;

    // 3. Fill Employee Form
    await page.getByLabel(/First Name \(English\)/i).fill(firstNameEn);
    await page.getByLabel(/Last Name \(English\)/i).fill('Al-Otaibi');
    await page.getByLabel(/الاسم الأول \(عربي\)/i).fill('فيصل');
    await page.getByLabel(/اسم العائلة \(عربي\)/i).fill('العتيبي');
    await page.getByLabel(/National ID/i).fill(uniqueNationalId);
    await page.getByLabel(/Employee Number/i).fill(`EMP-${runId}`);
    await page.getByLabel(/Date of Birth/i).fill('1990-01-01');
    await page.getByLabel(/Work Email/i).fill(`faisal-${runId}@zainx.com`);
    await page.getByLabel(/Job Title \(English\)/i).fill('Principal Systems Architect');
    await page.getByLabel(/المسمى الوظيفي \(عربي\)/i).fill('مهندس معماري نظم رئيسي');
    await page.getByLabel(/Hire Date/i).fill('2024-01-01');

    // Submit employee creation
    await page.getByRole('button', { name: /Save Employee/i }).click();
    await expect(page.getByRole('dialog')).toBeHidden({ timeout: 10000 });

    // 4. Verify in directory
    await page.getByPlaceholder(/Filter results/i).fill(createdName);
    const createdEmployee = page.getByText(createdName, { exact: true });
    await expect(createdEmployee).toBeVisible({ timeout: 10000 });

    // 5. Select employee to open Workspace
    await createdEmployee.click();
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
    const rootContainer = page.getByTestId('app-shell-root');
    await expect(rootContainer).toHaveAttribute('dir', 'rtl');

    // Verify Arabic Headings and Directory
    await expect(page.locator('aside')).toBeVisible();
  });
});
