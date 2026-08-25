import React, { FormEvent, useMemo, useState } from 'react';
import { createRoute } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import {
  CreatePositionRequest,
  CreateCostCenterRequest,
  CostCenterDto,
  LegalEntityDto,
  LocationDto,
  OrganizationUnitDto,
  PositionDto,
  useGetApiV1OrganizationLocations,
  useGetApiV1OrganizationCostCenters,
  useGetApiV1OrganizationPositions,
  useGetApiV1OrganizationUnits,
  useGetApiV1TenancyLegalEntities,
  usePostApiV1OrganizationPositions,
  usePostApiV1OrganizationCostCenters,
} from '@zainx/contracts';
import { Icon, PageHeader } from '@zainx/design-system';
import { Route as rootRoute } from './__root';

type OrganizationTab = 'legalEntities' | 'units' | 'positions' | 'costCenters' | 'locations';

export const organizationRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/organization',
  component: OrganizationPage,
});

function OrganizationPage() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === 'ar';
  const [activeTab, setActiveTab] = useState<OrganizationTab>('units');
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isCostCenterCreateOpen, setIsCostCenterCreateOpen] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [costCenterFormError, setCostCenterFormError] = useState<string | null>(null);
  const [form, setForm] = useState<CreatePositionRequest>({
    organizationUnitId: '',
    jobCode: '',
    titleEn: '',
    titleAr: '',
    grade: '',
  });
  const [costCenterForm, setCostCenterForm] = useState<CreateCostCenterRequest>({ code: '', nameEn: '', nameAr: '' });

  const units = useGetApiV1OrganizationUnits();
  const positions = useGetApiV1OrganizationPositions();
  const locations = useGetApiV1OrganizationLocations();
  const costCenters = useGetApiV1OrganizationCostCenters();
  const legalEntities = useGetApiV1TenancyLegalEntities();
  const createPosition = usePostApiV1OrganizationPositions();
  const createCostCenter = usePostApiV1OrganizationCostCenters();

  const unitById = useMemo(
    () => new Map((units.data ?? []).map((unit) => [unit.id, isAr ? unit.nameAr : unit.nameEn])),
    [isAr, units.data],
  );

  const labels = {
    title: isAr ? 'الهيكل التنظيمي' : 'Organization',
    subtitle: isAr
      ? 'مرجع موثوق للوحدات والمناصب والمواقع ضمن الكيان القانوني الحالي.'
      : 'Authoritative organization master data for the active legal-entity context.',
    units: isAr ? 'الوحدات التنظيمية' : 'Organization units',
    positions: isAr ? 'المناصب' : 'Positions',
    locations: isAr ? 'المواقع' : 'Locations',
    legalEntities: isAr ? 'الكيانات القانونية' : 'Legal entities',
    costCenters: isAr ? 'مراكز التكلفة' : 'Cost centers',
  };

  const handleCreatePosition = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);

    const data: CreatePositionRequest = {
      organizationUnitId: form.organizationUnitId.trim(),
      jobCode: form.jobCode.trim(),
      titleEn: form.titleEn.trim(),
      titleAr: form.titleAr.trim(),
      grade: form.grade?.trim() || null,
    };

    if (!data.organizationUnitId || !data.jobCode || !data.titleEn || !data.titleAr) {
      setFormError(isAr ? 'أكمل الحقول المطلوبة قبل الحفظ.' : 'Complete all required fields before saving.');
      return;
    }

    try {
      await createPosition.mutateAsync({ data });
      await positions.refetch();
      setForm({ organizationUnitId: '', jobCode: '', titleEn: '', titleAr: '', grade: '' });
      setIsCreateOpen(false);
    } catch {
      setFormError(isAr ? 'تعذر حفظ المنصب. راجع الصلاحيات وبيانات الكيان.' : 'The position could not be saved. Review permissions and entity data.');
    }
  };

  const handleCreateCostCenter = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setCostCenterFormError(null);
    const data = {
      code: costCenterForm.code.trim(),
      nameEn: costCenterForm.nameEn.trim(),
      nameAr: costCenterForm.nameAr.trim(),
    };
    if (!data.code || !data.nameEn || !data.nameAr) {
      setCostCenterFormError(isAr ? 'أكمل الحقول المطلوبة قبل الحفظ.' : 'Complete all required fields before saving.');
      return;
    }
    try {
      await createCostCenter.mutateAsync({ data });
      await costCenters.refetch();
      setCostCenterForm({ code: '', nameEn: '', nameAr: '' });
      setIsCostCenterCreateOpen(false);
    } catch {
      setCostCenterFormError(isAr ? 'تعذر حفظ مركز التكلفة. راجع الصلاحيات والكيان الحالي.' : 'The cost center could not be saved. Review permissions and the active entity.');
    }
  };

  const activeQuery = activeTab === 'legalEntities' ? legalEntities : activeTab === 'units' ? units : activeTab === 'positions' ? positions : activeTab === 'costCenters' ? costCenters : locations;
  const hasActiveError = activeQuery.isError;

  return (
    <main className="mx-auto w-full max-w-[1440px]">
      <PageHeader
        title={labels.title}
        subtitle={labels.subtitle}
        badge={<span className="rounded-full bg-primary-subtle px-2.5 py-1 text-xs font-semibold text-primary-subtle-text">{isAr ? 'بيانات أساسية' : 'Master data'}</span>}
        actions={
          activeTab === 'costCenters' ? (
            <button type="button" onClick={() => { setCostCenterFormError(null); setIsCostCenterCreateOpen(true); }} className="inline-flex min-h-10 items-center gap-2 rounded-md bg-primary px-4 text-sm font-semibold text-white shadow-xs transition hover:bg-primary-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-border-focus"><Icon name="plus" size="sm" aria-hidden="true" />{isAr ? 'إضافة مركز تكلفة' : 'Add cost center'}</button>
          ) : activeTab === 'positions' ? (
          <button
            type="button"
            onClick={() => { setFormError(null); setIsCreateOpen(true); }}
            className="inline-flex min-h-10 items-center gap-2 rounded-md bg-primary px-4 text-sm font-semibold text-white shadow-xs transition hover:bg-primary-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-border-focus"
          >
            <Icon name="plus" size="sm" aria-hidden="true" />
            {isAr ? 'إضافة منصب' : 'Add position'}
          </button>
          ) : null
        }
      />

      <div className="mb-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <SummaryTile label={labels.legalEntities} value={legalEntities.isLoading ? '—' : String(legalEntities.data?.length ?? 0)} icon="building" />
        <SummaryTile label={labels.units} value={units.isLoading ? '—' : String(units.data?.length ?? 0)} icon="building" />
        <SummaryTile label={labels.positions} value={positions.isLoading ? '—' : String(positions.data?.length ?? 0)} icon="briefcase" />
        <SummaryTile label={labels.costCenters} value={costCenters.isLoading ? '—' : String(costCenters.data?.length ?? 0)} icon="table" />
        <SummaryTile label={labels.locations} value={locations.isLoading ? '—' : String(locations.data?.length ?? 0)} icon="table" />
      </div>

      <div className="rounded-xl border border-border-default bg-surface shadow-xs">
        <div className="flex flex-wrap items-center gap-1 border-b border-border-default p-2" role="tablist" aria-label={isAr ? 'بيانات المؤسسة' : 'Organization data'}>
          {(['legalEntities', 'units', 'positions', 'costCenters', 'locations'] as OrganizationTab[]).map((tab) => (
            <button
              key={tab}
              type="button"
              role="tab"
              aria-selected={activeTab === tab}
              onClick={() => setActiveTab(tab)}
              className={activeTab === tab
                ? 'rounded-md bg-primary-subtle px-3 py-2 text-sm font-semibold text-primary-subtle-text'
                : 'rounded-md px-3 py-2 text-sm font-medium text-text-secondary hover:bg-surface-subtle hover:text-text-primary'}
            >
              {labels[tab]}
            </button>
          ))}
        </div>

        {hasActiveError ? (
          <StatePanel icon="alert-circle" title={isAr ? 'تعذر تحميل البيانات' : 'Data could not be loaded'} body={isAr ? 'تحقق من الاتصال والصلاحيات ثم أعد المحاولة.' : 'Check the connection and permissions, then try again.'} action={activeQuery.refetch} actionLabel={isAr ? 'إعادة المحاولة' : 'Retry'} />
        ) : activeQuery.isLoading ? (
          <div className="space-y-3 p-5" aria-label={isAr ? 'جار التحميل' : 'Loading organization data'}>
            <div className="h-10 animate-pulse rounded-md bg-surface-subtle" />
            <div className="h-10 animate-pulse rounded-md bg-surface-subtle" />
            <div className="h-10 animate-pulse rounded-md bg-surface-subtle" />
          </div>
        ) : activeTab === 'legalEntities' ? (
          <LegalEntitiesTable legalEntities={legalEntities.data ?? []} isAr={isAr} />
        ) : activeTab === 'units' ? (
          <UnitsTable units={units.data ?? []} isAr={isAr} />
        ) : activeTab === 'positions' ? (
          <PositionsTable positions={positions.data ?? []} unitById={unitById} isAr={isAr} />
        ) : activeTab === 'costCenters' ? (
          <CostCentersTable costCenters={costCenters.data ?? []} isAr={isAr} />
        ) : (
          <LocationsTable locations={locations.data ?? []} isAr={isAr} />
        )}
      </div>

      {isCreateOpen && (
        <div className="fixed inset-0 z-50 grid place-items-center bg-surface-overlay p-4" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setIsCreateOpen(false); }}>
          <form onSubmit={handleCreatePosition} className="w-full max-w-xl rounded-xl border border-border-default bg-surface p-5 shadow-overlay" role="dialog" aria-modal="true" aria-labelledby="create-position-title">
            <div className="mb-5 flex items-start justify-between gap-4">
              <div>
                <p className="zainx-eyebrow">{isAr ? 'بيانات أساسية' : 'Master data'}</p>
                <h2 id="create-position-title" className="mt-1 text-xl font-semibold text-text-primary">{isAr ? 'إضافة منصب' : 'Create position'}</h2>
              </div>
              <button type="button" onClick={() => setIsCreateOpen(false)} className="rounded-md p-2 text-text-secondary hover:bg-surface-subtle hover:text-text-primary" aria-label={isAr ? 'إغلاق' : 'Close'}><Icon name="x" size="sm" aria-hidden="true" /></button>
            </div>

            {formError && <div role="alert" className="mb-4 rounded-md border border-danger/30 bg-danger-subtle px-3 py-2 text-sm text-danger-subtle-text">{formError}</div>}

            <div className="grid gap-4 sm:grid-cols-2">
              <Field label={isAr ? 'الوحدة التنظيمية' : 'Organization unit'} required>
                <select required value={form.organizationUnitId} onChange={(event) => setForm({ ...form, organizationUnitId: event.target.value })} className="min-h-10 w-full rounded-md border border-border-default bg-surface-input px-3 text-sm text-text-primary focus-visible:outline-2 focus-visible:outline-border-focus">
                  <option value="">{isAr ? 'اختر وحدة' : 'Select a unit'}</option>
                  {(units.data ?? []).filter((unit) => unit.isActive !== false).map((unit) => <option key={unit.id} value={unit.id}>{isAr ? unit.nameAr : unit.nameEn}</option>)}
                </select>
              </Field>
              <Field label={isAr ? 'رمز المنصب' : 'Job code'} required><input required value={form.jobCode} onChange={(event) => setForm({ ...form, jobCode: event.target.value })} className="min-h-10 w-full rounded-md border border-border-default bg-surface-input px-3 text-sm text-text-primary focus-visible:outline-2 focus-visible:outline-border-focus" /></Field>
              <Field label={isAr ? 'المسمى بالإنجليزية' : 'English title'} required><input required value={form.titleEn} onChange={(event) => setForm({ ...form, titleEn: event.target.value })} className="min-h-10 w-full rounded-md border border-border-default bg-surface-input px-3 text-sm text-text-primary focus-visible:outline-2 focus-visible:outline-border-focus" /></Field>
              <Field label={isAr ? 'المسمى بالعربية' : 'Arabic title'} required><input required dir="rtl" value={form.titleAr} onChange={(event) => setForm({ ...form, titleAr: event.target.value })} className="min-h-10 w-full rounded-md border border-border-default bg-surface-input px-3 text-sm text-text-primary focus-visible:outline-2 focus-visible:outline-border-focus" /></Field>
              <Field label={isAr ? 'الدرجة (اختياري)' : 'Grade (optional)'}><input value={form.grade ?? ''} onChange={(event) => setForm({ ...form, grade: event.target.value })} className="min-h-10 w-full rounded-md border border-border-default bg-surface-input px-3 text-sm text-text-primary focus-visible:outline-2 focus-visible:outline-border-focus" /></Field>
            </div>

            <div className="mt-6 flex flex-wrap justify-end gap-2 border-t border-border-default pt-4">
              <button type="button" onClick={() => setIsCreateOpen(false)} className="min-h-10 rounded-md border border-border-default px-4 text-sm font-semibold text-text-secondary hover:bg-surface-subtle">{isAr ? 'إلغاء' : 'Cancel'}</button>
              <button type="submit" disabled={createPosition.isPending || units.isLoading} className="min-h-10 rounded-md bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{createPosition.isPending ? (isAr ? 'جار الحفظ…' : 'Saving…') : (isAr ? 'حفظ المنصب' : 'Save position')}</button>
            </div>
          </form>
        </div>
      )}

      {isCostCenterCreateOpen && (
        <div className="fixed inset-0 z-50 grid place-items-center bg-surface-overlay p-4" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setIsCostCenterCreateOpen(false); }}>
          <form onSubmit={handleCreateCostCenter} className="w-full max-w-xl rounded-xl border border-border-default bg-surface p-5 shadow-overlay" role="dialog" aria-modal="true" aria-labelledby="create-cost-center-title">
            <div className="mb-5 flex items-start justify-between gap-4"><div><p className="zainx-eyebrow">{isAr ? 'بيانات أساسية' : 'Master data'}</p><h2 id="create-cost-center-title" className="mt-1 text-xl font-semibold text-text-primary">{isAr ? 'إضافة مركز تكلفة' : 'Create cost center'}</h2></div><button type="button" onClick={() => setIsCostCenterCreateOpen(false)} className="rounded-md p-2 text-text-secondary hover:bg-surface-subtle hover:text-text-primary" aria-label={isAr ? 'إغلاق' : 'Close'}><Icon name="x" size="sm" aria-hidden="true" /></button></div>
            {costCenterFormError && <div role="alert" className="mb-4 rounded-md border border-danger/30 bg-danger-subtle px-3 py-2 text-sm text-danger-subtle-text">{costCenterFormError}</div>}
            <div className="grid gap-4 sm:grid-cols-2"><Field label={isAr ? 'رمز مركز التكلفة' : 'Cost center code'} required><input required value={costCenterForm.code} onChange={(event) => setCostCenterForm({ ...costCenterForm, code: event.target.value })} className="min-h-10 w-full rounded-md border border-border-default bg-surface-input px-3 text-sm text-text-primary focus-visible:outline-2 focus-visible:outline-border-focus" /></Field><Field label={isAr ? 'الاسم بالإنجليزية' : 'English name'} required><input required value={costCenterForm.nameEn} onChange={(event) => setCostCenterForm({ ...costCenterForm, nameEn: event.target.value })} className="min-h-10 w-full rounded-md border border-border-default bg-surface-input px-3 text-sm text-text-primary focus-visible:outline-2 focus-visible:outline-border-focus" /></Field><Field label={isAr ? 'الاسم بالعربية' : 'Arabic name'} required><input required dir="rtl" value={costCenterForm.nameAr} onChange={(event) => setCostCenterForm({ ...costCenterForm, nameAr: event.target.value })} className="min-h-10 w-full rounded-md border border-border-default bg-surface-input px-3 text-sm text-text-primary focus-visible:outline-2 focus-visible:outline-border-focus" /></Field></div>
            <div className="mt-6 flex flex-wrap justify-end gap-2 border-t border-border-default pt-4"><button type="button" onClick={() => setIsCostCenterCreateOpen(false)} className="min-h-10 rounded-md border border-border-default px-4 text-sm font-semibold text-text-secondary hover:bg-surface-subtle">{isAr ? 'إلغاء' : 'Cancel'}</button><button type="submit" disabled={createCostCenter.isPending} className="min-h-10 rounded-md bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{createCostCenter.isPending ? (isAr ? 'جار الحفظ…' : 'Saving…') : (isAr ? 'حفظ مركز التكلفة' : 'Save cost center')}</button></div>
          </form>
        </div>
      )}
    </main>
  );
}

function SummaryTile({ label, value, icon }: { label: string; value: string; icon: 'building' | 'briefcase' | 'table' }) {
  return <div className="rounded-xl border border-border-default bg-surface p-4 shadow-xs"><div className="flex items-center justify-between gap-4"><span className="text-sm text-text-secondary">{label}</span><Icon name={icon} size="sm" className="text-primary" aria-hidden="true" /></div><p className="mt-3 text-2xl font-semibold tracking-tight text-text-primary">{value}</p></div>;
}

function StatePanel({ icon, title, body, action, actionLabel }: { icon: 'alert-circle' | 'table'; title: string; body: string; action?: () => void; actionLabel?: string }) {
  return <div className="grid place-items-center px-6 py-16 text-center"><Icon name={icon} size="lg" className="mb-3 text-text-tertiary" aria-hidden="true" /><h2 className="text-base font-semibold text-text-primary">{title}</h2><p className="mt-1 max-w-md text-sm text-text-secondary">{body}</p>{action && actionLabel && <button type="button" onClick={() => void action()} className="mt-5 rounded-md border border-border-default px-3 py-2 text-sm font-semibold text-text-primary hover:bg-surface-subtle">{actionLabel}</button>}</div>;
}

function EmptyTable({ message }: { message: string }) {
  return <div className="grid place-items-center px-6 py-16 text-center"><Icon name="table" size="lg" className="mb-3 text-text-tertiary" aria-hidden="true" /><p className="text-sm font-semibold text-text-primary">{message}</p></div>;
}

function UnitsTable({ units, isAr }: { units: OrganizationUnitDto[]; isAr: boolean }) {
  if (!units.length) return <EmptyTable message={isAr ? 'لا توجد وحدات تنظيمية في هذا السياق.' : 'No organization units are available in this context.'} />;
  return <DataTable headers={[isAr ? 'الرمز' : 'Code', isAr ? 'الاسم' : 'Name', isAr ? 'النوع' : 'Type', isAr ? 'الحالة' : 'Status']} rows={units.map((unit) => [unit.code ?? '—', isAr ? unit.nameAr ?? '—' : unit.nameEn ?? '—', unit.type ?? '—', unit.isActive === false ? (isAr ? 'غير نشط' : 'Inactive') : (isAr ? 'نشط' : 'Active')])} />;
}

function LegalEntitiesTable({ legalEntities, isAr }: { legalEntities: LegalEntityDto[]; isAr: boolean }) {
  if (!legalEntities.length) return <EmptyTable message={isAr ? 'لا توجد كيانات قانونية مصرح بها في هذا السياق.' : 'No authorized legal entities are available in this context.'} />;
  return <DataTable headers={[isAr ? 'الرمز' : 'Code', isAr ? 'الاسم' : 'Name', isAr ? 'الدولة' : 'Country', isAr ? 'العملة' : 'Currency', isAr ? 'المنطقة الزمنية' : 'Timezone', isAr ? 'الحالة' : 'Status']} rows={legalEntities.map((entity) => [entity.code ?? '—', isAr ? entity.nameAr ?? '—' : entity.nameEn ?? '—', entity.countryCode ?? '—', entity.currencyCode ?? '—', entity.timezoneId ?? '—', entity.isActive === false ? (isAr ? 'غير نشط' : 'Inactive') : (isAr ? 'نشط' : 'Active')])} />;
}

function CostCentersTable({ costCenters, isAr }: { costCenters: CostCenterDto[]; isAr: boolean }) {
  if (!costCenters.length) return <EmptyTable message={isAr ? 'لا توجد مراكز تكلفة في هذا السياق.' : 'No cost centers are available in this context.'} />;
  return <DataTable headers={[isAr ? 'الرمز' : 'Code', isAr ? 'الاسم' : 'Name', isAr ? 'الحالة' : 'Status']} rows={costCenters.map((center) => [center.code ?? '—', isAr ? center.nameAr ?? '—' : center.nameEn ?? '—', center.isActive === false ? (isAr ? 'غير نشط' : 'Inactive') : (isAr ? 'نشط' : 'Active')])} />;
}

function PositionsTable({ positions, unitById, isAr }: { positions: PositionDto[]; unitById: Map<string | undefined, string | undefined>; isAr: boolean }) {
  if (!positions.length) return <EmptyTable message={isAr ? 'لا توجد مناصب مسجلة في هذا السياق.' : 'No positions are available in this context.'} />;
  return <DataTable headers={[isAr ? 'الرمز' : 'Code', isAr ? 'المسمى' : 'Title', isAr ? 'الوحدة' : 'Unit', isAr ? 'الدرجة' : 'Grade', isAr ? 'الحالة' : 'Status']} rows={positions.map((position) => [position.jobCode ?? '—', isAr ? position.titleAr ?? '—' : position.titleEn ?? '—', unitById.get(position.organizationUnitId) ?? (isAr ? 'غير متاح' : 'Unavailable'), position.grade ?? (isAr ? 'غير متاح' : 'Unavailable'), position.isActive === false ? (isAr ? 'غير نشط' : 'Inactive') : (isAr ? 'نشط' : 'Active')])} />;
}

function LocationsTable({ locations, isAr }: { locations: LocationDto[]; isAr: boolean }) {
  if (!locations.length) return <EmptyTable message={isAr ? 'لا توجد مواقع مسجلة في هذا السياق.' : 'No locations are available in this context.'} />;
  return <DataTable headers={[isAr ? 'الرمز' : 'Code', isAr ? 'الاسم' : 'Name', isAr ? 'المدينة' : 'City', isAr ? 'الدولة' : 'Country', isAr ? 'الحالة' : 'Status']} rows={locations.map((location) => [location.code ?? '—', isAr ? location.nameAr ?? '—' : location.nameEn ?? '—', location.city ?? '—', location.country ?? '—', location.isActive === false ? (isAr ? 'غير نشط' : 'Inactive') : (isAr ? 'نشط' : 'Active')])} />;
}

function DataTable({ headers, rows }: { headers: string[]; rows: string[][] }) {
  return <div className="overflow-x-auto"><table className="min-w-full divide-y divide-border-default text-start"><thead className="bg-surface-subtle"><tr>{headers.map((header) => <th key={header} scope="col" className="whitespace-nowrap px-5 py-3 text-start text-xs font-semibold uppercase tracking-[0.08em] text-text-tertiary">{header}</th>)}</tr></thead><tbody className="divide-y divide-border-subtle bg-surface">{rows.map((row, rowIndex) => <tr key={`${row[0]}-${rowIndex}`} className="hover:bg-surface-card-hover">{row.map((cell, cellIndex) => <td key={`${cell}-${cellIndex}`} className="whitespace-nowrap px-5 py-4 text-sm text-text-primary">{cell}</td>)}</tr>)}</tbody></table></div>;
}

function Field({ label, required, children }: { label: string; required?: boolean; children: React.ReactNode }) {
  return <label className="block text-sm font-medium text-text-primary"><span className="mb-1.5 block">{label}{required && <span className="ms-1 text-danger" aria-hidden="true">*</span>}</span>{children}</label>;
}
