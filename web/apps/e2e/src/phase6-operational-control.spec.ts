import { test, expect } from '@playwright/test';

test.describe('ZainX Workforce — Phase 6 Operational Control (Reporting, Admin, Integrations, Notifications, Audit) E2E', () => {

  test('Flow A: Navigation to Phase 6 Modules and Header Notification Bell', async ({ page }) => {
    await page.goto('http://localhost:4200/');

    // Verify Navigation links
    const reportsLink = page.locator('[data-testid="nav-reports-link"]');
    await expect(reportsLink).toBeVisible();

    const adminLink = page.locator('[data-testid="nav-administration-link"]');
    await expect(adminLink).toBeVisible();

    // Verify Notification Bell in Header
    const notifBell = page.locator('[data-testid="notification-bell-btn"]');
    await expect(notifBell).toBeVisible();
  });

  test('Flow B: In-App Notification Center Drawer & Mark-Read Interaction', async ({ page }) => {
    await page.goto('http://localhost:4200/');

    const notifBell = page.locator('[data-testid="notification-bell-btn"]');
    await notifBell.click();

    // Verify Dropdown opens
    const dropdown = page.locator('[data-testid="notification-dropdown"]');
    await expect(dropdown).toBeVisible();

    // Verify filter buttons
    const unreadBtn = dropdown.locator('button:has-text("Unread"), button:has-text("غير المقروءة")');
    if (await unreadBtn.isVisible()) {
      await unreadBtn.click();
    }

    // Mark all as read if present
    const markAllBtn = page.locator('[data-testid="mark-all-read-btn"]');
    if (await markAllBtn.isVisible()) {
      await markAllBtn.click();
    }
  });

  test('Flow C: Enterprise Reporting Catalog, Operational Grid & CSV Export Trigger', async ({ page }) => {
    await page.goto('http://localhost:4200/reports');

    await expect(page.locator('[data-testid="reports-workspace"]')).toBeVisible();

    // Check Headcount report card
    const headcountCard = page.locator('[data-testid="report-card-HEADCOUNT_SUMMARY"]');
    if (await headcountCard.isVisible()) {
      await headcountCard.click();
    }

    // Verify Export Button trigger
    const exportBtn = page.locator('[data-testid="export-report-btn"]');
    await expect(exportBtn).toBeVisible();
    await exportBtn.click();

    // The development sandbox is an explicit admin context, while a restricted
    // identity must receive a governed 403. In either case the UI must surface
    // an explicit outcome instead of silently dropping the export request.
    const outcomeBanner = page.locator('[data-testid="report-error-banner"], [data-testid="export-notice-banner"]').first();
    await expect(outcomeBanner).toBeVisible({ timeout: 5000 });
    const deniedBanner = page.locator('[data-testid="report-error-banner"]');
    if (await deniedBanner.isVisible()) {
      await expect(deniedBanner).toContainText(/Permission Denied|غير مصرح/);
    } else {
      await expect(page.locator('[data-testid="export-notice-banner"]')).toContainText(/Export completed|تم إنشاء/);
    }

    // Open Save View Modal
    const saveViewBtn = page.locator('[data-testid="save-view-btn"]');
    if (await saveViewBtn.isVisible()) {
      await saveViewBtn.click();
      await page.fill('[data-testid="view-name-input"]', 'E2E Test Report View');
      const confirmSaveBtn = page.locator('[data-testid="confirm-save-view-btn"]');
      if (await confirmSaveBtn.isVisible()) {
        await confirmSaveBtn.click();
      }
    }
  });

  test('Flow D: Platform Administration, RBAC Matrix, Settings, Retention & Webhooks', async ({ page }) => {
    await page.goto('http://localhost:4200/administration');

    await expect(page.locator('[data-testid="administration-workspace"]')).toBeVisible();

    // 1. Roles & Permissions
    const rolesTab = page.locator('[data-testid="admin-tab-roles"]');
    await rolesTab.click();
    await expect(page.locator('[data-testid="create-role-btn"]')).toBeVisible();

    // 2. Platform Settings
    const settingsTab = page.locator('[data-testid="admin-tab-settings"]');
    await settingsTab.click();
    await expect(page.locator('th:has-text("Category"), th:has-text("الفئة")').first()).toBeVisible();

    // 3. Retention Policies
    const retentionTab = page.locator('[data-testid="admin-tab-retention"]');
    await retentionTab.click();
    await expect(page.locator('th:has-text("Retention Period"), th:has-text("فترة الحفظ")').first()).toBeVisible();

    // 4. Integrations & Deliveries
    const integrationsTab = page.locator('[data-testid="admin-tab-integrations"]');
    await integrationsTab.click();
    await expect(page.locator('[data-testid="create-connector-btn"]')).toBeVisible();

    // 5. Immutable Audit Trail
    const auditTab = page.locator('[data-testid="admin-tab-audit"]');
    await auditTab.click();
    await expect(page.locator('th:has-text("Timestamp"), th:has-text("الوقت")').first()).toBeVisible();
  });

  test('Flow E: Arabic RTL Layout & Localization across Phase 6 Surfaces', async ({ page }) => {
    await page.goto('http://localhost:4200/reports');

    const langBtn = page.locator('[data-testid="lang-switch-btn"]');
    if (await langBtn.isVisible()) {
      await langBtn.click();
      const dir = await page.locator('div[dir]').first().getAttribute('dir');
      expect(dir).toBe('rtl');
    }
  });

});
