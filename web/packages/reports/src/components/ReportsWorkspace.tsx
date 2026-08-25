import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Icon } from '@zainx/design-system';

export interface ReportDefinition {
  reportCode: string;
  nameEn: string;
  nameAr: string;
  domain: string;
  descriptionEn: string;
  descriptionAr: string;
  allowedFiltersJson: string;
  allowedColumnsJson: string;
  requiredPermissionsJson: string;
  dataClassification: string;
  supportedFormatsJson: string;
}

export interface SavedReportView {
  id: string;
  reportCode: string;
  viewName: string;
  isTenantShared: boolean;
  filtersJson: string;
}

export interface ReportExecutionResult {
  columns: string[];
  rows: Record<string, any>[];
  totalCount: number;
}

const BASELINE_CATALOG: ReportDefinition[] = [
  {
    reportCode: 'HEADCOUNT_SUMMARY',
    nameEn: 'Headcount & Demographics Summary',
    nameAr: 'ملخص القوى العاملة والتركيبة السكانية',
    domain: 'People',
    descriptionEn:
      'Enterprise overview of active headcounts, department distribution, and employment status.',
    descriptionAr: 'نظرة عامة على إجمالي الموظفين وتوزيع الأقسام وحالات التوظيف.',
    allowedFiltersJson: '["department","status"]',
    allowedColumnsJson:
      '["employeeNumber","fullNameEn","fullNameAr","nationalId","jobTitle","department","hireDate","status"]',
    requiredPermissionsJson: '["people.read"]',
    dataClassification: 'Internal',
    supportedFormatsJson: '["CSV","JSON"]',
  },
  {
    reportCode: 'ATTENDANCE_MONTHLY',
    nameEn: 'Monthly Attendance & Exception Summary',
    nameAr: 'ملخص الحضور والانصراف والاستثناءات الشهري',
    domain: 'Attendance',
    descriptionEn:
      'Aggregated monthly attendance, lateness penalties, overtime hours, and verified worked minutes.',
    descriptionAr: 'ساعات العمل الفعلية، ساعات العمل الإضافي، والتأخيرات الشهرية المعتمدة.',
    allowedFiltersJson: '["month","year","departmentId"]',
    allowedColumnsJson:
      '["employeeNumber","employeeName","scheduledDays","presentDays","lateArrivalMinutes","overtimeMinutes","unpaidAbsenceDays"]',
    requiredPermissionsJson: '["attendance.read"]',
    dataClassification: 'Internal',
    supportedFormatsJson: '["CSV","JSON"]',
  },
  {
    reportCode: 'LEAVE_UTILIZATION',
    nameEn: 'Annual & Statutory Leave Utilization',
    nameAr: 'تقرير استهلاك واستحقاق الإجازات النظامية',
    domain: 'Leave',
    descriptionEn:
      'Balance tracking, approved leaves, pending requests, and carryover accruals per legal entity.',
    descriptionAr: 'أرصدة الإجازات المستحقة، الإجازات المستهلكة، والطلبات المعلقة.',
    allowedFiltersJson: '["leaveTypeId","year"]',
    allowedColumnsJson:
      '["employeeNumber","employeeName","leaveType","entitlementDays","consumedDays","remainingBalanceDays"]',
    requiredPermissionsJson: '["leave.read"]',
    dataClassification: 'Internal',
    supportedFormatsJson: '["CSV","JSON"]',
  },
  {
    reportCode: 'PAYROLL_RECONCILIATION',
    nameEn: 'Finalized Payroll Reconciliation & Statutory',
    nameAr: 'مطابقة مسيرات الرواتب المعتمدة والاشتراكات النظامية',
    domain: 'Payroll',
    descriptionEn:
      'Strictly derived from finalized payroll snapshots: Gross pay, statutory GOSI deductions, net pay.',
    descriptionAr:
      'تقرير مشتق حصراً من مسيرات الرواتب المعتمدة: إجمالي الرواتب، استقطاعات التأمينات، صافي الرواتب.',
    allowedFiltersJson: '["periodId","costCenter"]',
    allowedColumnsJson:
      '["employeeNumber","employeeName","basicSalary","housingAllowance","transportAllowance","otherEarnings","gosiEmployee","gosiEmployer","totalDeductions","netPay"]',
    requiredPermissionsJson: '["payroll.read","payroll.result.read_sensitive"]',
    dataClassification: 'Confidential',
    supportedFormatsJson: '["CSV","JSON"]',
  },
  {
    reportCode: 'RECRUITMENT_FUNNEL',
    nameEn: 'Recruitment Pipeline & Conversion Metrics',
    nameAr: 'قمع التوظيف ومؤشرات التحويل والتعيين',
    domain: 'Recruitment',
    descriptionEn:
      'Applicant funnel metrics, stage transition durations, conversion rates, and offer acceptance ratios.',
    descriptionAr:
      'إحصائيات المتقدمين ومعدلات الانتقال بين المراحل وسرعة التعيين ونسب قبول العروض.',
    allowedFiltersJson: '["requisitionId"]',
    allowedColumnsJson:
      '["requisitionCode","requisitionTitle","appliedCount","screenedCount","interviewedCount","offeredCount","hiredCount","averageDaysToHire"]',
    requiredPermissionsJson: '["recruitment.read"]',
    dataClassification: 'Internal',
    supportedFormatsJson: '["CSV","JSON"]',
  },
  {
    reportCode: 'AUDIT_SECURITY_EVENTS',
    nameEn: 'Audit Trail & Security Access Log',
    nameAr: 'سجل التدقيق والعمليات الأمنية والحساسة',
    domain: 'Audit',
    descriptionEn:
      'Chronological audit trail of administrative changes, sensitive data access, and privilege assignments.',
    descriptionAr: 'سجل تاريخي للعمليات الإدارية، الوصول للبيانات الحساسة، ومنح الصلاحيات.',
    allowedFiltersJson: '["actionCode","entityType"]',
    allowedColumnsJson:
      '["occurredAtUtc","actorUserId","actorType","actionCode","entityType","entityId","correlationId","ipAddress"]',
    requiredPermissionsJson: '["audit.read"]',
    dataClassification: 'Restricted',
    supportedFormatsJson: '["CSV","JSON"]',
  },
];

export function ReportsWorkspace() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === 'ar';

  const [reports, setReports] = useState<ReportDefinition[]>(BASELINE_CATALOG);
  const [selectedReport, setSelectedReport] = useState<ReportDefinition | null>(
    BASELINE_CATALOG[0],
  );
  const [domainFilter, setDomainFilter] = useState<string>('ALL');
  const [reportData, setReportData] = useState<ReportExecutionResult | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [reportError, setReportError] = useState<string | null>(null);

  // Pagination & Filtering
  const [page, setPage] = useState<number>(1);
  const [pageSize] = useState<number>(25);
  const [activeFilters, setActiveFilters] = useState<Record<string, string>>({});

  // Export & View State
  const [exporting, setExporting] = useState<boolean>(false);
  const [exportNotice, setExportNotice] = useState<string | null>(null);
  const [savedViews, setSavedViews] = useState<SavedReportView[]>([]);
  const [showSaveViewModal, setShowSaveViewModal] = useState<boolean>(false);
  const [newViewName, setNewViewName] = useState<string>('');
  const [isSharedView, setIsSharedView] = useState<boolean>(false);

  const fetchReportCatalog = async () => {
    try {
      const res = await fetch('/api/v1/reports');
      if (res.ok) {
        const data = await res.json();
        if (Array.isArray(data) && data.length > 0) {
          setReports(data);
          if (!selectedReport) setSelectedReport(data[0]);
        }
      }
    } catch {
      // Fallback to baseline governed catalog
    }
  };

  const fetchSavedViews = async (reportCode: string) => {
    try {
      const res = await fetch(`/api/v1/reports/${reportCode}/saved-views`);
      if (res.ok) {
        const data = await res.json();
        setSavedViews(Array.isArray(data) ? data : []);
      }
    } catch {
      setSavedViews([]);
    }
  };

  const runOperationalReport = async () => {
    if (!selectedReport) return;
    setLoading(true);
    setReportError(null);

    try {
      const res = await fetch(`/api/v1/reports/${selectedReport.reportCode}/run`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          filters: activeFilters,
          page,
          pageSize,
        }),
      });

      if (res.ok) {
        const data = await res.json();
        setReportData(data);
      } else if (res.status === 403) {
        setReportError(
          isAr
            ? 'غير مصرح لك بتشغيل هذا التقرير المؤسسي. يلزم وجود الصلاحيات المطلوبة.'
            : 'Permission Denied: You do not possess the required permissions to run this report.',
        );
        setReportData(null);
      } else {
        setReportError(
          isAr
            ? 'فشل في تشغيل التقرير. يرجى التحقق من معايير التصفية والمحاولة مرة أخرى.'
            : 'Failed to run report. Please check filters and query criteria.',
        );
        setReportData(null);
      }
    } catch {
      setReportError(
        isAr
          ? 'تعذر الاتصال بخدمة التقارير. يرجى المحاولة لاحقاً.'
          : 'Unable to connect to the reports engine service.',
      );
      setReportData(null);
    } finally {
      setLoading(false);
    }
  };

  const triggerExport = async () => {
    if (!selectedReport) return;
    setExporting(true);
    setExportNotice(null);

    try {
      const res = await fetch(`/api/v1/reports/${selectedReport.reportCode}/export`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          filters: activeFilters,
          outputFormat: 'CSV',
        }),
      });

      if (res.ok) {
        const job = await res.json();
        setExportNotice(
          isAr
            ? `تم إنشاء ملف التصدير بنجاح (معرف المهمة: ${job.id})`
            : `Export completed successfully (Job ID: ${job.id}). Checksum: ${job.sha256Checksum?.substring(0, 12)}...`,
        );

        // Trigger direct download
        if (job.status === 'Completed' || job.status === 3) {
          window.open(`/api/v1/reports/jobs/${job.id}/download`, '_blank');
        }
      } else if (res.status === 403) {
        setReportError(
          isAr
            ? 'غير مصرح لك بتصدير هذا التقرير المؤسسي. يلزم وجود الصلاحيات المطلوبة.'
            : 'Permission Denied: You do not possess the required permission to export this report.',
        );
        setExportNotice(null);
      } else {
        setReportError(
          isAr
            ? 'فشل تصدير التقرير. يرجى المحاولة مرة أخرى.'
            : 'Report export failed. Please try again.',
        );
        setExportNotice(null);
      }
    } catch (e: any) {
      setExportNotice(isAr ? 'حدث خطأ أثناء تصدير التقرير' : `Export error: ${e.message}`);
    } finally {
      setExporting(false);
    }
  };

  const saveCurrentView = async () => {
    if (!selectedReport || !newViewName.trim()) return;
    try {
      const res = await fetch(`/api/v1/reports/${selectedReport.reportCode}/saved-views`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          viewName: newViewName.trim(),
          isTenantShared: isSharedView,
          filtersJson: JSON.stringify(activeFilters),
          columnsJson: '[]',
          sortJson: '[]',
          groupingJson: '[]',
        }),
      });

      if (res.ok) {
        setShowSaveViewModal(false);
        setNewViewName('');
        fetchSavedViews(selectedReport.reportCode);
      }
    } catch {
      setShowSaveViewModal(false);
    }
  };

  useEffect(() => {
    fetchReportCatalog();
  }, []);

  useEffect(() => {
    if (selectedReport) {
      fetchSavedViews(selectedReport.reportCode);
      runOperationalReport();
    }
  }, [selectedReport, page]);

  const filteredReports =
    domainFilter === 'ALL'
      ? reports
      : reports.filter((r) => r.domain.toUpperCase() === domainFilter);

  const getClassificationBadge = (cls: string) => {
    switch (cls.toLowerCase()) {
      case 'confidential':
        return 'bg-primary-subtle text-primary border-primary border-primary';
      case 'restricted':
        return 'bg-warning-subtle text-warning border-warning border-warning';
      default:
        return 'bg-info-subtle text-info border-info';
    }
  };

  return (
    <div className="space-y-6" data-testid="reports-workspace">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 bg-surface p-6 rounded-xl border border-border-default shadow-xs">
        <div>
          <h1 className="text-2xl font-bold text-text-primary">
            {isAr
              ? 'التقارير المؤسسية والرؤى التشغيلية'
              : 'Enterprise Reports & Operational Insights'}
          </h1>
          <p className="text-sm text-text-secondary mt-1">
            {isAr
              ? 'توليد التقارير المعتمدة، التصدير الآمن المحمي من حقن الصيغ، وحفظ المشاهد المخصصة.'
              : 'Governed read-model queries, formula-injection-safe exports, and tenant-scoped saved views.'}
          </p>
        </div>

        {selectedReport && (
          <div className="flex items-center gap-3">
            <button
              data-testid="save-view-btn"
              onClick={() => setShowSaveViewModal(true)}
              className="px-4 py-2 bg-surface-subtle hover:bg-surface text-text-primary font-medium text-sm rounded-lg transition-colors border border-border-default flex items-center gap-2"
            >
              <Icon name="columns" size="xs" />
              {isAr ? 'حفظ المشهد' : 'Save View'}
            </button>

            <button
              data-testid="export-report-btn"
              disabled={exporting}
              onClick={triggerExport}
              className="px-4 py-2 bg-primary hover:bg-primary/90 disabled:opacity-50 text-text-inverse font-medium text-sm rounded-lg shadow-xs transition-colors flex items-center gap-2"
            >
              <Icon name="download" size="xs" />
              {exporting
                ? isAr
                  ? 'جاري التصدير...'
                  : 'Exporting...'
                : isAr
                  ? 'تصدير CSV'
                  : 'Export CSV'}
            </button>
          </div>
        )}
      </div>

      {/* Export Notification Notice */}
      {exportNotice && (
        <div
          data-testid="export-notice-banner"
          className="p-4 bg-success-subtle border border-success text-success rounded-lg text-sm flex items-center justify-between"
        >
          <div className="flex items-center gap-2 font-medium">
            <Icon name="check-circle" size="sm" className="text-success" />
            {exportNotice}
          </div>
          <button
            onClick={() => setExportNotice(null)}
            className="text-success hover:text-success-hover font-bold"
          >
            ×
          </button>
        </div>
      )}

      {reportError && (
        <div
          role="alert"
          data-testid="report-error-banner"
          className="flex items-center justify-between rounded-lg border border-danger border-danger bg-danger-subtle p-4 text-sm text-danger"
        >
          <div className="flex items-center gap-2">
            <Icon name="alert-circle" size="sm" className="text-danger" />
            <span>{reportError}</span>
          </div>
          <button
            onClick={() => setReportError(null)}
            className="font-bold text-danger hover:text-danger-hover"
            aria-label="Dismiss report error"
          >
            ×
          </button>
        </div>
      )}

      {/* Main Grid: Catalog Left / Data Grid Right */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Left Column: Report Catalog */}
        <div className="lg:col-span-1 bg-surface p-4 rounded-xl border border-border-default shadow-xs space-y-4">
          <div className="flex items-center justify-between border-b border-border-subtle pb-3">
            <h2 className="font-semibold text-text-primary text-sm">
              {isAr ? 'كتالوج التقارير' : 'Report Catalog'}
            </h2>
            <span className="text-xs text-text-muted font-medium">
              {filteredReports.length} {isAr ? 'تقارير' : 'reports'}
            </span>
          </div>

          {/* Domain Filters */}
          <div className="flex flex-wrap gap-1.5">
            {['ALL', 'PEOPLE', 'PAYROLL', 'ATTENDANCE', 'RECRUITMENT', 'AUDIT'].map((d) => (
              <button
                key={d}
                onClick={() => setDomainFilter(d)}
                className={`text-xs px-2.5 py-1 rounded-lg font-medium transition-colors ${
                  domainFilter === d
                    ? 'bg-primary text-text-inverse'
                    : 'bg-surface-subtle text-text-secondary hover:bg-surface border border-border-subtle'
                }`}
              >
                {d}
              </button>
            ))}
          </div>

          {/* Catalog List */}
          <div className="space-y-2 max-h-[520px] overflow-y-auto pe-1">
            {filteredReports.length === 0 ? (
              <div className="rounded-lg border border-dashed border-border-default px-4 py-8 text-center text-xs text-text-muted">
                {isAr
                  ? 'لا توجد تعريفات تقارير متاحة حالياً.'
                  : 'No governed report definitions are available.'}
              </div>
            ) : (
              filteredReports.map((rep) => {
                const isSelected = selectedReport?.reportCode === rep.reportCode;
                return (
                  <div
                    key={rep.reportCode}
                    data-testid={`report-card-${rep.reportCode}`}
                    onClick={() => {
                      setSelectedReport(rep);
                      setActiveFilters({});
                      setPage(1);
                    }}
                    className={`p-3.5 rounded-lg border transition-all cursor-pointer ${
                      isSelected
                        ? 'border-primary bg-primary/5 shadow-xs ring-1 ring-primary'
                        : 'border-border-default hover:border-border-focus hover:bg-surface-subtle'
                    }`}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <span className="font-semibold text-xs text-text-primary leading-tight">
                        {isAr ? rep.nameAr : rep.nameEn}
                      </span>
                      <span
                        className={`text-[10px] font-medium px-2 py-0.5 rounded border ${getClassificationBadge(rep.dataClassification)}`}
                      >
                        {rep.dataClassification}
                      </span>
                    </div>
                    <p className="text-[11px] text-text-secondary line-clamp-2 mt-1 leading-relaxed">
                      {isAr ? rep.descriptionAr : rep.descriptionEn}
                    </p>
                    <div className="mt-2 flex items-center justify-between text-[10px] text-text-muted">
                      <span className="font-semibold uppercase tracking-wider text-text-secondary">
                        {rep.domain}
                      </span>
                      <span className="font-mono text-text-muted">{rep.reportCode}</span>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>

        {/* Right Column: Active Report View & Data Grid */}
        <div className="lg:col-span-3 space-y-4">
          {selectedReport && (
            <div className="bg-surface p-5 rounded-xl border border-border-default shadow-xs space-y-4">
              {/* Report Header Bar */}
              <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3 border-b border-border-subtle pb-4">
                <div>
                  <div className="flex items-center gap-2.5">
                    <h2 className="text-lg font-bold text-text-primary">
                      {isAr ? selectedReport.nameAr : selectedReport.nameEn}
                    </h2>
                    <span
                      className={`text-xs font-semibold px-2 py-0.5 rounded border ${getClassificationBadge(selectedReport.dataClassification)}`}
                    >
                      {selectedReport.dataClassification}
                    </span>
                  </div>
                  <p className="text-xs text-text-secondary mt-1">
                    {isAr ? selectedReport.descriptionAr : selectedReport.descriptionEn}
                  </p>
                </div>

                {/* Saved Views Dropdown */}
                {savedViews.length > 0 && (
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-text-secondary font-medium">
                      {isAr ? 'المشاهد المحفوظة:' : 'Saved View:'}
                    </span>
                    <select
                      id="saved-views-select"
                      aria-label={isAr ? 'المشاهد المحفوظة' : 'Saved Views'}
                      data-testid="saved-views-select"
                      onChange={(e) => {
                        const v = savedViews.find((sv) => sv.id === e.target.value);
                        if (v) {
                          try {
                            setActiveFilters(JSON.parse(v.filtersJson) || {});
                          } catch {
                            // ignore malformed saved view json
                          }
                        }
                      }}
                      className="text-xs border border-border-default rounded-lg px-2.5 py-1.5 bg-surface text-text-primary focus:outline-hidden focus:ring-2 focus:ring-primary"
                    >
                      <option key="__default__" value="">
                        {isAr ? 'المشهد الافتراضي' : 'Default View'}
                      </option>
                      {savedViews.map((sv, idx) => (
                        <option key={sv.id || `sv-${idx}`} value={sv.id}>
                          {sv.viewName} {sv.isTenantShared ? '(Shared)' : ''}
                        </option>
                      ))}
                    </select>
                  </div>
                )}
              </div>

              {/* Operational Data Grid */}
              <div className="overflow-x-auto rounded-lg border border-border-default">
                {loading ? (
                  <div className="py-20 text-center text-text-muted text-sm">
                    {isAr
                      ? 'جاري تشغيل الاستعلام وحساب البيانات...'
                      : 'Executing governed query & streaming results...'}
                  </div>
                ) : !reportData || !reportData.rows || reportData.rows.length === 0 ? (
                  <div className="py-20 text-center text-text-muted text-sm">
                    {isAr
                      ? 'لا توجد بيانات متاحة لهذا التقرير حالياً'
                      : 'No records found for current filters'}
                  </div>
                ) : (
                  <table className="w-full text-start border-collapse text-xs">
                    <thead>
                      <tr className="bg-surface-subtle text-text-secondary border-b border-border-default">
                        {(reportData.columns || []).map((col) => (
                          <th
                            key={col}
                            className="py-3 px-4 font-semibold text-text-primary uppercase tracking-wider text-[11px]"
                          >
                            {col}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border-subtle font-sans">
                      {(reportData.rows || []).map((row, idx) => (
                        <tr
                          key={idx}
                          data-testid={`report-row-${idx}`}
                          className="hover:bg-surface-subtle transition-colors"
                        >
                          {reportData.columns.map((col) => (
                            <td
                              key={col}
                              className="py-3 px-4 text-text-secondary whitespace-nowrap"
                            >
                              {row[col] !== null && row[col] !== undefined ? String(row[col]) : '—'}
                            </td>
                          ))}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>

              {/* Table Footer / Pagination */}
              {reportData && (
                <div className="flex items-center justify-between text-xs text-text-secondary pt-2">
                  <span>
                    {isAr ? 'إجمالي السجلات:' : 'Total Records:'}{' '}
                    <strong className="text-text-primary">{reportData.totalCount}</strong>
                  </span>
                  <div className="flex gap-2 items-center">
                    <button
                      disabled={page <= 1}
                      onClick={() => setPage((p) => Math.max(1, p - 1))}
                      className="px-3 py-1 bg-surface-subtle hover:bg-surface disabled:opacity-50 rounded-lg text-text-primary font-medium border border-border-default"
                    >
                      {isAr ? 'السابق' : 'Previous'}
                    </button>
                    <span className="font-semibold text-text-primary px-2">
                      {isAr ? `صفحة ${page}` : `Page ${page}`}
                    </span>
                    <button
                      disabled={(reportData?.rows?.length ?? 0) < pageSize}
                      onClick={() => setPage((p) => p + 1)}
                      className="px-3 py-1 bg-surface-subtle hover:bg-surface disabled:opacity-50 rounded-lg text-text-primary font-medium border border-border-default"
                    >
                      {isAr ? 'التالي' : 'Next'}
                    </button>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Save View Modal */}
      {showSaveViewModal && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-xs z-50 flex items-center justify-center p-4">
          <div className="bg-surface rounded-xl p-6 max-w-md w-full shadow-overlay border border-border-default space-y-4">
            <h3 className="text-lg font-bold text-text-primary">
              {isAr ? 'حفظ المشهد المخصص' : 'Save Custom Report View'}
            </h3>
            <p className="text-xs text-text-secondary">
              {isAr
                ? 'احفظ معايير التصفية والأعمدة الحالية لتسهيل إعادة تشغيل التقرير لاحقاً.'
                : 'Save your current filter configuration to quickly access this report view later.'}
            </p>

            <div>
              <label className="block text-xs font-semibold text-text-primary mb-1">
                {isAr ? 'اسم المشهد' : 'View Name'}
              </label>
              <input
                data-testid="view-name-input"
                type="text"
                value={newViewName}
                onChange={(e) => setNewViewName(e.target.value)}
                placeholder="e.g. Q3 Engineering Reconciliation"
                className="w-full text-sm border border-border-default bg-surface rounded-lg px-3 py-2 text-text-primary focus:ring-2 focus:ring-primary focus:outline-hidden"
              />
            </div>

            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="isShared"
                checked={isSharedView}
                onChange={(e) => setIsSharedView(e.target.checked)}
                className="rounded border-border-default text-primary focus:ring-primary"
              />
              <label
                htmlFor="isShared"
                className="text-xs text-text-secondary font-medium cursor-pointer"
              >
                {isAr
                  ? 'مشاركة هذا المشهد مع جميع مستخدمي المؤسسة'
                  : 'Share this view with all tenant users'}
              </label>
            </div>

            <div className="flex justify-end gap-3 pt-2">
              <button
                onClick={() => setShowSaveViewModal(false)}
                className="px-4 py-2 text-sm font-medium text-text-secondary hover:text-text-primary"
              >
                {isAr ? 'إلغاء' : 'Cancel'}
              </button>
              <button
                data-testid="confirm-save-view-btn"
                disabled={!newViewName.trim()}
                onClick={saveCurrentView}
                className="px-4 py-2 text-sm font-semibold bg-primary hover:bg-primary/90 disabled:opacity-50 text-text-inverse rounded-lg shadow-xs"
              >
                {isAr ? 'حفظ' : 'Save'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
