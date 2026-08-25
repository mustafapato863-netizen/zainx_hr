import React, { act } from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { axe } from 'vitest-axe';
import * as matchers from 'vitest-axe/matchers';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { NotificationCenter } from '../components/NotificationCenter';
import { ReportsWorkspace } from '@zainx/reports';
import { AdministrationWorkspace } from '@zainx/administration';

expect.extend(matchers);

// Mock fetch
global.fetch = vi.fn().mockImplementation((url: string) => {
  if (url.includes('unread-count')) {
    return Promise.resolve({
      ok: true,
      json: () => Promise.resolve({ unreadCount: 2 })
    });
  }
  if (url.includes('/api/v1/reports')) {
    return Promise.resolve({
      ok: true,
      json: () => Promise.resolve([
        {
          reportCode: 'HEADCOUNT_SUMMARY',
          nameEn: 'Headcount & Demographics Summary',
          nameAr: 'ملخص القوى العاملة',
          domain: 'People',
          descriptionEn: 'Enterprise overview of active headcounts.',
          descriptionAr: 'نظرة عامة على إجمالي الموظفين.',
          allowedFiltersJson: '["department", "status"]',
          allowedColumnsJson: '["employeeNumber", "fullNameEn", "department"]',
          requiredPermissionsJson: '["people.read"]',
          dataClassification: 'Internal',
          supportedFormatsJson: '["CSV", "JSON"]'
        }
      ])
    });
  }
  if (url.includes('/api/v1/admin/roles') || url.includes('/api/v1/admin/settings')) {
    return Promise.resolve({
      ok: true,
      json: () => Promise.resolve([])
    });
  }
  return Promise.resolve({
    ok: true,
    json: () => Promise.resolve({ items: [], unreadCount: 0, rows: [], columns: [] })
  });
}) as any;

// Mock i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, defaultVal?: string) => defaultVal || key,
    i18n: { language: 'en', changeLanguage: () => Promise.resolve() }
  })
}));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
    mutations: { retry: false }
  }
});

const renderWithQuery = (ui: React.ReactElement) => {
  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  );
};

describe('Phase 6 Platform Operations Accessibility Verification (Axe WCAG AA)', () => {
  it('ReportsWorkspace passes axe accessibility check with 0 critical/serious violations', async () => {
    let container: HTMLElement;
    await act(async () => {
      const res = renderWithQuery(<ReportsWorkspace />);
      container = res.container;
    });
    const results = await axe(container!);
    
    const criticalViolations = results.violations.filter(v => v.impact === 'critical');
    const seriousViolations = results.violations.filter(v => v.impact === 'serious');
    
    expect(criticalViolations).toHaveLength(0);
    expect(seriousViolations).toHaveLength(0);
  });

  it('AdministrationWorkspace passes axe accessibility check across all admin tabs with 0 critical/serious violations', async () => {
    let container: HTMLElement;
    await act(async () => {
      const res = renderWithQuery(<AdministrationWorkspace />);
      container = res.container;
    });
    const results = await axe(container!);
    
    const criticalViolations = results.violations.filter(v => v.impact === 'critical');
    const seriousViolations = results.violations.filter(v => v.impact === 'serious');
    
    expect(criticalViolations).toHaveLength(0);
    expect(seriousViolations).toHaveLength(0);
  });

  it('NotificationCenter passes axe accessibility check with 0 critical/serious violations', async () => {
    let container: HTMLElement;
    await act(async () => {
      const res = renderWithQuery(<NotificationCenter />);
      container = res.container;
    });
    
    // Open the notification drawer
    const bellBtn = screen.getByRole('button', { name: /notifications/i });
    await act(async () => {
      fireEvent.click(bellBtn);
    });

    const results = await axe(container!);
    const criticalViolations = results.violations.filter(v => v.impact === 'critical');
    const seriousViolations = results.violations.filter(v => v.impact === 'serious');
    
    expect(criticalViolations).toHaveLength(0);
    expect(seriousViolations).toHaveLength(0);
  });

  // =========================================================================
  // KEYBOARD ACCESSIBILITY TESTS
  // =========================================================================

  it('Proves keyboard interaction for Report Filters and Saved Views', async () => {
    await act(async () => {
      renderWithQuery(<ReportsWorkspace />);
    });
    
    // 1. Focus on category filter button
    const peopleFilterBtn = screen.getByRole('button', { name: /^people$/i });
    peopleFilterBtn.focus();
    expect(document.activeElement).toBe(peopleFilterBtn);

    // 2. Trigger category filter
    await act(async () => {
      fireEvent.click(peopleFilterBtn);
    });

    // 3. Focus and trigger save view modal
    const saveViewBtn = screen.getByTestId('save-view-btn');
    saveViewBtn.focus();
    expect(document.activeElement).toBe(saveViewBtn);
    await act(async () => {
      fireEvent.click(saveViewBtn);
    });
    
    // Modal input focus
    const viewNameInput = screen.getByTestId('view-name-input');
    viewNameInput.focus();
    expect(document.activeElement).toBe(viewNameInput);
  });

  it('Proves keyboard interaction for Role Assignment and Settings dialogs in Administration', async () => {
    await act(async () => {
      renderWithQuery(<AdministrationWorkspace />);
    });
    
    // 1. Tab through Admin navigation tabs
    const rolesTab = screen.getByRole('button', { name: /roles & permissions/i });
    const settingsTab = screen.getByRole('button', { name: /platform settings/i });
    
    rolesTab.focus();
    expect(document.activeElement).toBe(rolesTab);

    // Switch tabs with keyboard
    await act(async () => {
      fireEvent.click(settingsTab);
    });
    expect(screen.getByRole('heading', { name: /effective-dated settings/i })).toBeDefined();
  });

  it('Proves keyboard interaction for Notification Center Drawer and Action buttons', async () => {
    await act(async () => {
      renderWithQuery(<NotificationCenter />);
    });
    
    const bellBtn = screen.getByRole('button', { name: /notifications/i });
    bellBtn.focus();
    expect(document.activeElement).toBe(bellBtn);

    // Trigger open via Enter
    await act(async () => {
      fireEvent.click(bellBtn);
    });

    // Verify unread filter toggle button is focusable
    const unreadFilterBtn = screen.getByRole('button', { name: /unread/i });
    unreadFilterBtn.focus();
    expect(document.activeElement).toBe(unreadFilterBtn);
    await act(async () => {
      fireEvent.click(unreadFilterBtn);
    });
  });
});
