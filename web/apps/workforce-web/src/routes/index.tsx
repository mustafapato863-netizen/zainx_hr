import React from 'react';
import { Link, createRoute } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { Badge, Icon, PageHeader } from '@zainx/design-system';
import {
  useGetApiV1ApprovalsInbox,
  useGetApiV1OrganizationUnits,
  useGetApiV1PeopleEmployees,
} from '@zainx/contracts';
import { Route as rootRoute } from './__root';

function ReadModelValue({ value, unavailable }: { value?: number | string; unavailable: boolean }) {
  if (unavailable) return <span className="text-base text-text-tertiary">—</span>;
  if (value === undefined || value === null) return <span className="inline-block h-7 w-14 animate-pulse rounded bg-surface-subtle" aria-label="Loading" />;
  return <span className="zainx-tabular text-[2.25rem] font-semibold leading-none tracking-[-0.04em] text-text-primary">{value}</span>;
}

function Home() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === 'ar';
  const people = useGetApiV1PeopleEmployees({ page: 1, pageSize: 1 });
  const approvals = useGetApiV1ApprovalsInbox({ page: 1, pageSize: 5 });
  const organization = useGetApiV1OrganizationUnits();

  const modelsUnavailable = people.isError || approvals.isError || organization.isError;
  const approvalItems = approvals.data?.items ?? [];
  const workspaces = [
    ['people', 'users', isAr ? 'الموظفون' : 'People', isAr ? 'البيانات الرئيسية' : 'Master data'],
    ['attendance', 'clock', isAr ? 'الحضور' : 'Attendance', isAr ? 'السجل اليومي' : 'Daily control'],
    ['payroll', 'dollar-sign', isAr ? 'الرواتب' : 'Payroll', isAr ? 'الدورات والتسوية' : 'Runs & settlement'],
    ['reports', 'bar-chart-2', isAr ? 'التقارير' : 'Reports', isAr ? 'رؤى قابلة للتنفيذ' : 'Actionable insights'],
  ] as const;

  return (
    <div className="mx-auto w-full max-w-[var(--component-page-max-width)] space-y-7" data-testid="home-page">
      <PageHeader
        title={isAr ? 'نظرة عامة على مساحة العمل' : 'Workspace overview'}
        subtitle={isAr ? 'نقطة تشغيل هادئة لمتابعة الاستثناءات والمهام ذات الأثر.' : 'A calm operating view for exceptions, approvals, and the work that needs attention.'}
        badge={<Badge variant="info" size="sm" dot>{isAr ? 'بيانات تشغيلية' : 'Operational view'}</Badge>}
      />

      <section className="zainx-brand-grid relative overflow-hidden rounded-xl px-6 py-7 text-white shadow-overlay sm:px-8 lg:px-10" aria-labelledby="home-introduction">
        <div className="relative z-10 max-w-2xl">
          <p className="zainx-eyebrow text-brand-cyan-300">{isAr ? 'مركز القيادة' : 'Command center'}</p>
          <h2 id="home-introduction" className="mt-3 max-w-xl text-[clamp(1.8rem,3.8vw,3.35rem)] font-semibold leading-[1.04] tracking-[-0.05em] text-white">
            {isAr ? 'وضوح عملي يبدأ من الأشخاص.' : 'Operational clarity starts with people.'}
          </h2>
          <p className="mt-4 max-w-xl text-sm leading-6 text-brand-mineral-200 sm:text-base">
            {isAr ? 'مساحة واحدة لمراجعة القوى العاملة، فهم الاستثناءات، والوصول إلى الإجراء التالي بثقة.' : 'One focused place to understand the workforce, see exceptions, and move to the next action with confidence.'}
          </p>
        </div>
        <div aria-hidden="true" className="pointer-events-none absolute -end-8 -top-16 h-72 w-72 rounded-full border border-brand-cyan-300/25 sm:-end-2 sm:-top-20 sm:h-96 sm:w-96">
          <div className="absolute inset-10 rounded-full border border-brand-cyan-300/20" />
          <div className="absolute inset-20 rounded-full border border-brand-cyan-300/25" />
          <svg viewBox="0 0 180 180" className="absolute inset-1/4 h-1/2 w-1/2 text-brand-cyan-300" fill="none">
            <path d="M27 42h126L61 138h92" stroke="currentColor" strokeWidth="8" strokeLinecap="square" />
          </svg>
        </div>
        <div className="relative z-10 mt-7 flex flex-wrap gap-x-6 gap-y-2 text-[11px] font-medium text-brand-mineral-200">
          <span className="inline-flex items-center gap-2"><span className="h-1.5 w-1.5 rounded-full bg-brand-cyan-300" />{isAr ? 'مصادر حقيقية' : 'Truthful read models'}</span>
          <span className="inline-flex items-center gap-2"><span className="h-1.5 w-1.5 rounded-full bg-brand-sand-500" />{isAr ? 'وصول محكوم' : 'Governed access'}</span>
        </div>
      </section>

      {modelsUnavailable && (
        <div role="status" className="flex items-start gap-3 rounded-lg border border-warning/30 bg-warning-subtle px-4 py-3 text-sm text-warning-subtle-text">
          <Icon name="alert-circle" size="sm" className="mt-0.5 shrink-0" aria-hidden="true" />
          <div><p className="font-semibold">{isAr ? 'بعض مصادر البيانات غير متاحة' : 'Some operational read models are unavailable'}</p><p className="mt-0.5 text-xs opacity-85">{isAr ? 'لن يتم عرض أرقام افتراضية. تحقق من الاتصال أو افتح الوحدة المطلوبة لإعادة المحاولة.' : 'No placeholder metrics are shown. Check the service connection or open the relevant workspace to retry.'}</p></div>
        </div>
      )}

      <section aria-labelledby="overview-metrics" className="space-y-3">
        <div className="flex items-end justify-between gap-4"><div><p className="zainx-eyebrow">{isAr ? 'الإشارة التشغيلية' : 'Operational signal'}</p><h2 id="overview-metrics" className="mt-1 text-lg font-semibold tracking-tight">{isAr ? 'لقطة من النظام الحالي' : 'A live snapshot of the system'}</h2></div><span className="text-[11px] text-text-tertiary">{isAr ? 'نماذج قراءة حالية' : 'Current read models'}</span></div>
        <div className="grid gap-3 sm:grid-cols-3">
          {[
            ['users', isAr ? 'سجل الموظفين' : 'Employee directory', people.data?.totalCount, !!people.isError, '/people', isAr ? 'فتح الموظفين' : 'Open people', 'text-primary'],
            ['check-circle', isAr ? 'عناصر بانتظار الموافقة' : 'Pending approvals', approvals.data?.totalCount, !!approvals.isError, '/approvals', isAr ? 'فتح الصندوق' : 'Open inbox', 'text-warning'],
            ['building', isAr ? 'الوحدات التنظيمية' : 'Organization units', organization.data?.length, !!organization.isError, '/organization', isAr ? 'مراجعة الهيكل' : 'Review structure', 'text-info'],
          ].map(([icon, label, value, unavailable, to, action, tone]) => (
            <Link key={label as string} to={to as '/people' | '/approvals' | '/administration'} className="group border-b border-border-default bg-surface pb-4 pt-3 transition-colors hover:border-primary sm:px-3">
              <div className="flex items-start justify-between gap-3"><span className="text-xs font-medium text-text-secondary">{label}</span><span className={`${tone} opacity-80`}><Icon name={icon as 'users' | 'check-circle' | 'building'} size="sm" aria-hidden="true" /></span></div>
              <div className="mt-4"><ReadModelValue value={value as number | string | undefined} unavailable={unavailable as boolean} /></div>
              <div className="mt-4 flex items-center gap-1.5 text-xs font-semibold text-text-link">{action}<Icon name="arrow-right" size="xs" aria-hidden="true" /></div>
            </Link>
          ))}
        </div>
      </section>

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1.35fr)_minmax(320px,0.65fr)]">
        <section className="zainx-panel-rule border-y border-border-default bg-surface px-5 py-5 sm:px-6" aria-labelledby="attention-heading">
          <div className="flex items-start justify-between gap-4"><div><p className="zainx-eyebrow">{isAr ? 'قائمة الانتباه' : 'Attention queue'}</p><h2 id="attention-heading" className="mt-1 text-lg font-semibold tracking-tight">{isAr ? 'ما يحتاج انتباهك' : 'What needs your attention'}</h2><p className="mt-1 text-xs text-text-secondary">{isAr ? 'عناصر حقيقية من صندوق الموافقات الحالي.' : 'Live items from your current approval inbox.'}</p></div><Link to="/approvals" className="shrink-0 text-xs font-semibold text-text-link hover:underline">{isAr ? 'عرض الكل' : 'View all'}</Link></div>
          <div className="mt-5">
            {approvals.isLoading ? <div className="space-y-3" aria-label="Loading approvals"><div className="h-12 animate-pulse rounded-md bg-surface-subtle" /><div className="h-12 animate-pulse rounded-md bg-surface-subtle" /></div> : approvals.isError ? <div className="rounded-md border border-dashed border-border-default px-4 py-8 text-center text-sm text-text-secondary">{isAr ? 'تعذر تحميل صندوق الموافقات.' : 'The approval inbox could not be loaded.'}</div> : approvalItems.length === 0 ? <div className="rounded-md border border-dashed border-border-default px-4 py-8 text-center"><Icon name="check-circle" size="lg" className="mx-auto mb-3 text-success" aria-hidden="true" /><p className="text-sm font-semibold text-text-primary">{isAr ? 'لا توجد عناصر معلقة' : 'Nothing is waiting for action'}</p><p className="mt-1 text-xs text-text-secondary">{isAr ? 'صندوق الموافقات الحالي هادئ.' : 'The current approval inbox is clear.'}</p></div> : <div className="divide-y divide-border-subtle">{approvalItems.map((item) => <Link key={item.id} to="/approvals" className="group flex items-center justify-between gap-4 py-3 first:pt-0 last:pb-0"><div className="min-w-0"><p className="truncate text-sm font-medium text-text-primary group-hover:text-text-link">{item.title}</p><p className="mt-1 truncate text-xs text-text-secondary">{item.sourceModule} · {item.workflowType}</p></div><div className="flex shrink-0 items-center gap-3"><Badge variant="warning" size="sm" dot>{item.status}</Badge><Icon name="chevron-right" size="sm" className="text-text-tertiary" aria-hidden="true" /></div></Link>)}</div>}
          </div>
        </section>

        <section className="rounded-lg border border-border-default bg-surface-subtle p-5" aria-labelledby="workspaces-heading">
          <p className="zainx-eyebrow">{isAr ? 'الوصول السريع' : 'Quick access'}</p><h2 id="workspaces-heading" className="mt-1 text-lg font-semibold tracking-tight">{isAr ? 'مسارات العمل' : 'Workspaces'}</h2><p className="mt-1 text-xs leading-5 text-text-secondary">{isAr ? 'افتح الوحدة المناسبة عندما تحتاج إلى إجراء.' : 'Go directly to the workspace where the next action lives.'}</p>
          <div className="mt-5 grid gap-1.5">{workspaces.map(([to, icon, label, description]) => <Link key={to} to={`/${to}` as '/people' | '/attendance' | '/payroll' | '/reports'} className="group flex items-center gap-3 border-b border-border-default py-3 last:border-b-0"><span className="flex h-8 w-8 items-center justify-center rounded-md bg-surface text-primary shadow-xs"><Icon name={icon as 'users' | 'clock' | 'dollar-sign' | 'bar-chart-2'} size="sm" aria-hidden="true" /></span><span className="min-w-0"><span className="block text-xs font-semibold text-text-primary group-hover:text-text-link">{label}</span><span className="block truncate text-[11px] text-text-tertiary">{description}</span></span><Icon name="arrow-right" size="xs" className="ms-auto text-text-tertiary" aria-hidden="true" /></Link>)}</div>
        </section>
      </div>
    </div>
  );
}

export const indexRoute = createRoute({ getParentRoute: () => rootRoute, path: '/', component: Home });
