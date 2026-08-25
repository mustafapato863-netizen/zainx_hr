import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor, act } from '@testing-library/react';
import React from 'react';
import { axe } from 'vitest-axe';
import * as matchers from 'vitest-axe/matchers';
import { AiWorkspace } from '../AiWorkspace';
import { AiQuickLauncher } from '../AiQuickLauncher';

expect.extend(matchers);

// Mock react-i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: {
      language: 'en',
      dir: () => 'ltr',
      changeLanguage: () => Promise.resolve()
    }
  })
}));

// Mock global fetch
global.fetch = vi.fn((url: string) => {
  if (url.endsWith('/conversations')) {
    return Promise.resolve({
      ok: true,
      json: () => Promise.resolve([
        {
          id: '11111111-1111-1111-1111-111111111111',
          title: 'May Payroll Analysis',
          createdAtUtc: new Date().toISOString(),
          updatedAtUtc: new Date().toISOString(),
          messageCount: 2
        }
      ])
    } as Response);
  }
  if (url.endsWith('/tools')) {
    return Promise.resolve({
      ok: true,
      json: () => Promise.resolve([
        {
          toolCode: 'policy.search_company_policy',
          descriptionEn: 'Search company policies with temporal versioning',
          descriptionAr: 'البحث في لوائح وسياسات الشركة',
          requiredPermission: 'core.platform',
          dataClassification: 'Internal',
          isReadOnly: true
        }
      ])
    } as Response);
  }
  if (url.endsWith('/actions') || url.endsWith('/proposals')) {
    return Promise.resolve({
      ok: true,
      json: () => Promise.resolve([])
    } as Response);
  }
  return Promise.resolve({
    ok: true,
    json: () => Promise.resolve({})
  } as Response);
});

describe('ZainX Workforce — Phase 7A AI Accessibility & Component Unit Tests (WCAG AA)', () => {

  it('AiWorkspace renders and passes Axe WCAG AA audit with 0 violations', async () => {
    let container: HTMLElement;
    await act(async () => {
      const rendered = render(<AiWorkspace />);
      container = rendered.container;
    });

    await waitFor(() => {
      expect(screen.getByTestId('ai-workspace-container')).toBeDefined();
      expect(screen.getByTestId('input-ai-prompt')).toBeDefined();
      expect(screen.getByTestId('btn-submit-prompt')).toBeDefined();
    });

    const results = await axe(container!);
    expect(results).toHaveNoViolations();
  });

  it('AiQuickLauncher renders trigger button and passes Axe WCAG AA audit', async () => {
    let container: HTMLElement;
    await act(async () => {
      const rendered = render(<AiQuickLauncher />);
      container = rendered.container;
    });
    
    expect(screen.getByTestId('ai-quick-launcher-trigger')).toBeDefined();

    const results = await axe(container!);
    expect(results).toHaveNoViolations();
  });

});

