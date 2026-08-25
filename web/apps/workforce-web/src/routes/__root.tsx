import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Link,
  Outlet,
  createRootRoute,
  useNavigate,
  useRouterState,
} from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { CommandPalette, type CommandItem } from '@zainx/design-system/components/CommandPalette/CommandPalette';
import { BrandMark } from '@zainx/design-system/components/BrandMark/BrandMark';
import { Icon, type IconName } from '@zainx/design-system/components/Icon/Icon';
import { QuickCreate } from '@zainx/design-system/components/QuickCreate/QuickCreate';
import { NotificationCenter } from '@zainx/platform';
import { AiQuickLauncher } from '@zainx/ai/components/AiQuickLauncher';

type NavigationItem = {
  to: '/' | '/me' | '/people' | '/organization' | '/attendance' | '/leave' | '/approvals' | '/payroll' | '/recruitment' | '/reports' | '/administration' | '/ai';
  icon: IconName;
  en: string;
  ar: string;
  section: 'Home' | 'Workforce' | 'Talent' | 'Insights' | 'System';
  testId?: string;
};

const navigation: readonly NavigationItem[] = [
  { to: '/', icon: 'grid', en: 'Home', ar: 'الرئيسية', section: 'Home' },
  { to: '/me', icon: 'user', en: 'My workspace', ar: 'مساحتي', section: 'Home', testId: 'nav-me-link' },
  { to: '/people', icon: 'users', en: 'People', ar: 'الموظفون', section: 'Workforce', testId: 'nav-people-link' },
  { to: '/organization', icon: 'building', en: 'Organization', ar: 'الهيكل التنظيمي', section: 'Workforce', testId: 'nav-organization-link' },
  { to: '/attendance', icon: 'clock', en: 'Attendance', ar: 'الحضور والانصراف', section: 'Workforce', testId: 'nav-attendance-link' },
  { to: '/leave', icon: 'calendar', en: 'Leave', ar: 'الإجازات', section: 'Workforce', testId: 'nav-leave-link' },
  { to: '/approvals', icon: 'check-circle', en: 'Approvals', ar: 'الموافقات', section: 'Workforce', testId: 'nav-approvals-link' },
  { to: '/payroll', icon: 'dollar-sign', en: 'Payroll & settlement', ar: 'الرواتب والتسويات', section: 'Workforce', testId: 'nav-payroll-link' },
  { to: '/recruitment', icon: 'briefcase', en: 'Recruitment', ar: 'التوظيف', section: 'Talent', testId: 'nav-recruitment-link' },
  { to: '/reports', icon: 'bar-chart-2', en: 'Reports & insights', ar: 'التقارير والرؤى', section: 'Insights', testId: 'nav-reports-link' },
  { to: '/administration', icon: 'settings', en: 'Administration', ar: 'الإدارة والحوكمة', section: 'System', testId: 'nav-administration-link' },
  { to: '/ai', icon: 'sparkles', en: 'Workforce AI', ar: 'المساعد الذكي', section: 'System', testId: 'nav-ai-link' },
];

const contextLabels: Record<string, { en: string; ar: string }> = {
  '/': { en: 'Workspace overview', ar: 'نظرة عامة على مساحة العمل' },
  '/me': { en: 'My workforce profile', ar: 'ملفي الوظيفي' },
  '/people': { en: 'People operations', ar: 'عمليات الموظفين' },
  '/organization': { en: 'Organization master data', ar: 'البيانات الأساسية للهيكل' },
  '/attendance': { en: 'Attendance control', ar: 'التحكم في الحضور والانصراف' },
  '/leave': { en: 'Leave operations', ar: 'عمليات الإجازات' },
  '/approvals': { en: 'Approval inbox', ar: 'صندوق الموافقات' },
  '/payroll': { en: 'Payroll & settlement', ar: 'الرواتب والتسويات' },
  '/recruitment': { en: 'Talent acquisition', ar: 'استقطاب المواهب' },
  '/reports': { en: 'Reports & insights', ar: 'التقارير والرؤى' },
  '/administration': { en: 'Administration & governance', ar: 'الإدارة والحوكمة' },
  '/ai': { en: 'Workforce AI', ar: 'المساعد الذكي' },
};

const sectionLabels: Record<NavigationItem['section'], { en: string; ar: string }> = {
  Home: { en: 'Home', ar: 'الرئيسية' },
  Workforce: { en: 'Workforce', ar: 'القوى العاملة' },
  Talent: { en: 'Talent', ar: 'المواهب' },
  Insights: { en: 'Insights', ar: 'الرؤى' },
  System: { en: 'System', ar: 'النظام' },
};

export const Route = createRootRoute({ component: RootComponent });

function Navigation({ isAr, onNavigate }: { isAr: boolean; onNavigate?: () => void }) {
  const grouped = navigation.reduce<Record<string, NavigationItem[]>>((acc, item) => {
    (acc[item.section] ??= []).push(item);
    return acc;
  }, {});

  return (
    <nav aria-label={isAr ? 'التنقل الرئيسي' : 'Primary navigation'} data-testid="primary-navigation" className="space-y-5">
      {(Object.keys(sectionLabels) as NavigationItem['section'][]).map((section) => (
        <div key={section}>
          <div className="zainx-eyebrow mb-2 px-3 text-brand-mineral-500">
            {isAr ? sectionLabels[section].ar : sectionLabels[section].en}
          </div>
          <div className="space-y-1">
            {(grouped[section] ?? []).map((item) => (
              <Link
                key={item.to}
                to={item.to}
                activeOptions={{ exact: item.to === '/' }}
                activeProps={{ className: 'bg-brand-cyan-300/10 text-white shadow-[inset_3px_0_0_var(--color-brand-cyan-300)]' }}
                onClick={onNavigate}
                data-testid={item.testId}
                className="group flex min-h-10 items-center gap-3 rounded-md px-3 text-xs font-medium text-brand-mineral-200 transition-[background-color,color,box-shadow] duration-150 hover:bg-white/[0.06] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-brand-cyan-300"
              >
                <Icon name={item.icon} size="sm" className="text-brand-mineral-500 transition-colors group-hover:text-brand-cyan-300" aria-hidden="true" />
                <span className="truncate">{isAr ? item.ar : item.en}</span>
              </Link>
            ))}
          </div>
        </div>
      ))}
    </nav>
  );
}

function Sidebar({ isAr, mobile, onClose, onLanguageToggle }: { isAr: boolean; mobile?: boolean; onClose?: () => void; onLanguageToggle?: () => void }) {
  return (
    <aside
      className={mobile ? 'fixed inset-y-0 start-0 z-50 flex w-[min(86vw,292px)] flex-col bg-surface-sidebar text-white shadow-overlay lg:hidden' : 'hidden w-[var(--component-shell-sidebar-width)] shrink-0 flex-col bg-surface-sidebar text-white lg:flex'}
      aria-label={isAr ? 'تنقل المنصة' : 'Platform navigation'}
    >
      <div className="flex h-[var(--component-shell-topbar-height)] items-center justify-between border-b border-white/10 px-5">
        <Link to="/" onClick={onClose} className="rounded-md focus-visible:outline-brand-cyan-300">
          <BrandMark inverse />
        </Link>
        {mobile && <button type="button" onClick={onClose} className="flex h-9 w-9 items-center justify-center rounded-md text-brand-mineral-500 hover:bg-white/10 hover:text-white" aria-label={isAr ? 'إغلاق القائمة' : 'Close navigation'}><Icon name="x" size="sm" aria-hidden="true" /></button>}
      </div>
      <div className="zainx-scrollbar flex-1 overflow-y-auto px-3 py-5"><Navigation isAr={isAr} onNavigate={onClose} /></div>
      <div className="border-t border-white/10 px-5 py-4 text-[11px] text-brand-mineral-500">
        {mobile && onLanguageToggle && (
          <button type="button" onClick={onLanguageToggle} className="mb-4 flex w-full items-center justify-between rounded-md border border-white/10 px-3 py-2 text-xs font-semibold text-brand-mineral-200 hover:bg-white/10 hover:text-white" aria-label={isAr ? 'التبديل إلى الإنجليزية' : 'Switch to Arabic'}>
            <span>{isAr ? 'English' : 'العربية'}</span>
            <Icon name="chevron-right" size="xs" aria-hidden="true" />
          </button>
        )}
        <div className="flex items-center justify-between"><span>{isAr ? 'منصة تشغيلية' : 'Operational platform'}</span><span className="flex items-center gap-1.5 font-mono text-brand-cyan-300"><span className="h-1.5 w-1.5 rounded-full bg-brand-cyan-300" /><span>Live</span></span></div>
      </div>
    </aside>
  );
}

function RootComponent() {
  const { i18n } = useTranslation();
  const navigate = useNavigate();
  const pathname = useRouterState({ select: (state) => state.location.pathname });
  const isAr = i18n.language === 'ar';
  const [isNavOpen, setIsNavOpen] = useState(false);
  const mobileNavTriggerRef = useRef<HTMLButtonElement>(null);
  const [isCommandOpen, setIsCommandOpen] = useState(false);
  const closeMobileNav = useCallback(() => {
    setIsNavOpen(false);
    window.requestAnimationFrame(() => mobileNavTriggerRef.current?.focus());
  }, []);

  useEffect(() => {
    if (!isNavOpen) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') closeMobileNav();
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [closeMobileNav, isNavOpen]);

  const pageContext = contextLabels[pathname] ?? contextLabels['/'];
  const commandItems = useMemo<CommandItem[]>(() => navigation.map((item) => ({
    id: `navigate-${item.to}`,
    title: isAr ? item.ar : item.en,
    subtitle: isAr ? sectionLabels[item.section].ar : sectionLabels[item.section].en,
    icon: item.icon,
    section: isAr ? sectionLabels[item.section].ar : sectionLabels[item.section].en,
    onSelect: () => navigate({ to: item.to }),
  })), [isAr, navigate]);

  return (
    <div className="flex min-h-dvh w-full bg-canvas text-text-primary" dir={i18n.dir()} lang={isAr ? 'ar' : 'en'} data-testid="app-shell-root">
      <a href="#main-content" className="sr-only focus:not-sr-only focus:fixed focus:start-4 focus:top-4 focus:z-[60] focus:rounded-md focus:bg-primary focus:px-3 focus:py-2 focus:text-sm focus:text-white">{isAr ? 'تخطي إلى المحتوى الرئيسي' : 'Skip to main content'}</a>
      <Sidebar isAr={isAr} />
      {isNavOpen && <button type="button" className="fixed inset-0 z-40 bg-brand-ink-950/65 lg:hidden" onClick={closeMobileNav} aria-label={isAr ? 'إغلاق القائمة' : 'Close navigation'} />}
      {isNavOpen && <Sidebar isAr={isAr} mobile onClose={closeMobileNav} onLanguageToggle={() => i18n.changeLanguage(isAr ? 'en' : 'ar')} />}

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 flex min-h-[var(--component-shell-topbar-height)] items-center justify-between gap-3 border-b border-border-default bg-surface-topbar/95 px-4 backdrop-blur sm:px-6 lg:px-8">
          <div className="flex min-w-0 items-center gap-3">
            <button ref={mobileNavTriggerRef} type="button" onClick={() => setIsNavOpen(true)} className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md border border-border-default text-text-secondary hover:bg-surface-subtle lg:hidden" aria-label={isAr ? 'فتح القائمة' : 'Open navigation'}><Icon name="menu" size="sm" aria-hidden="true" /></button>
            <div className="min-w-0"><div className="zainx-eyebrow hidden sm:block">ZainX Workforce</div><div className="truncate text-sm font-semibold text-text-primary">{isAr ? pageContext.ar : pageContext.en}</div></div>
          </div>
          <div className="flex shrink-0 items-center gap-1.5 sm:gap-2">
            <button type="button" data-testid="command-palette-trigger" onClick={() => setIsCommandOpen(true)} className="hidden h-9 items-center gap-2 rounded-md border border-border-default bg-surface-subtle px-3 text-xs text-text-secondary hover:border-border-strong hover:text-text-primary md:flex" aria-label={isAr ? 'بحث وتنقل' : 'Search and navigate'}><Icon name="search" size="sm" aria-hidden="true" /><span>{isAr ? 'بحث وتنقل' : 'Search or jump'}</span><kbd className="ms-2 rounded border border-border-default bg-surface px-1.5 py-0.5 text-[10px] font-mono text-text-tertiary">⌘K</kbd></button>
            <QuickCreate buttonLabel={isAr ? 'إجراء سريع' : 'Quick create'} title={isAr ? 'بدء إجراء' : 'Start an action'} className="hidden sm:inline-flex" items={[
              { id: 'employee', label: isAr ? 'موظف جديد' : 'New employee', description: isAr ? 'فتح مساحة الموظفين' : 'Open the people workspace', icon: 'user', onAction: () => navigate({ to: '/people' }) },
              { id: 'leave', label: isAr ? 'طلب إجازة' : 'Leave request', description: isAr ? 'فتح عمليات الإجازات' : 'Open leave operations', icon: 'calendar', onAction: () => navigate({ to: '/leave' }) },
              { id: 'payroll', label: isAr ? 'مسير رواتب' : 'Payroll run', description: isAr ? 'فتح مسيرات الرواتب' : 'Open payroll runs', icon: 'dollar-sign', onAction: () => navigate({ to: '/payroll' }) },
            ]} />
            <AiQuickLauncher />
            <NotificationCenter />
            <button type="button" data-testid="lang-switch-btn" onClick={() => i18n.changeLanguage(isAr ? 'en' : 'ar')} className="hidden h-9 rounded-md border border-border-default bg-surface px-3 text-xs font-semibold text-text-secondary hover:bg-surface-subtle hover:text-text-primary sm:inline-flex sm:items-center" aria-label={isAr ? 'Switch to English' : 'التبديل إلى العربية'}>{isAr ? 'English' : 'العربية'}</button>
            <button type="button" className="flex h-9 w-9 items-center justify-center rounded-full bg-surface-sidebar text-[11px] font-bold text-white shadow-xs" aria-label={isAr ? 'حساب المستخدم' : 'User account'}>AD</button>
          </div>
        </header>
        <main id="main-content" className="zainx-scrollbar min-w-0 flex-1 overflow-auto p-4 sm:p-6 lg:p-8"><Outlet /></main>
      </div>
      <CommandPalette isOpen={isCommandOpen} onClose={() => setIsCommandOpen(false)} onOpen={() => setIsCommandOpen(true)} items={commandItems} />
    </div>
  );
}
