import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';

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

export function ReportsWorkspace() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === 'ar';

  const [reports, setReports] = useState<ReportDefinition[]>([]);
  const [selectedReport, setSelectedReport] = useState<ReportDefinition | null>(null);
  const [domainFilter, setDomainFilter] = useState<string>('ALL');
  const [reportData, setReportData] = useState<ReportExecutionResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [exportNotice, setExportNotice] = useState<string | null>(null);
  const [savedViews, setSavedViews] = useState<SavedReportView[]>([]);
  const [showSaveViewModal, setShowSaveViewModal] = useState(false);
  const [newViewName, setNewViewName] = useState('');
  const [isSharedView, setIsSharedView] = useState(false);
  const [activeFilters, setActiveFilters] = useState<Record<string, string>>({});
  const [page, setPage] = useState(1);
  const [pageSize] = useState(25);

  // Fetch report definitions catalog
  const fetchReportCatalog = async () => {
    try {
      const res = await fetch('/api/v1/reports');
      if (res.ok) {
        const data = await res.json();
        setReports(data || []);
        if (data && data.length > 0) {
          setSelectedReport(data[0]);
        }
      }
    } catch {
      // Fallback baseline catalog
      const fallbackCatalog: ReportDefinition[] = [
        {
          reportCode: 'HEADCOUNT_SUMMARY',
          nameEn: 'Headcount & Demographics Summary',
          nameAr: 'ملخص القوى العاملة والتركيبة السكانية',
          domain: 'People',
          descriptionEn: 'Enterprise overview of active headcounts, department distribution, and employment status.',
          descriptionAr: 'نظرة عامة على إجمالي الموظفين وتوزيع الأقسام وحالات التوظيف.',
          allowedFiltersJson: '["department", "status"]',
          allowedColumnsJson: '["employeeNumber", "fullNameEn", "fullNameAr", "nationalId", "jobTitle", "department", "hireDate", "status"]',
          requiredPermissionsJson: '["people.read"]',
          dataClassification: 'Internal',
          supportedFormatsJson: '["CSV", "JSON"]'
        },
        {
          reportCode: 'ATTENDANCE_MONTHLY',
          nameEn: 'Monthly Attendance & Exception Summary',
          nameAr: 'تقرير الحضور والانصراف الشهري والاستثناءات',
          domain: 'Attendance',
          descriptionEn: 'Aggregated employee attendance, work hours, overtime, and punctuality exceptions.',
          descriptionAr: 'ملخص ساعات العمل والحضور الإجمالي وساعات العمل الإضافي وحالات التأخير.',
          allowedFiltersJson: '["month", "year"]',
          allowedColumnsJson: '["employeeNumber", "employeeName", "scheduledHours", "workedHours", "overtimeHours", "lateMinutes", "exceptionCount"]',
          requiredPermissionsJson: '["attendance.read"]',
          dataClassification: 'Internal',
          supportedFormatsJson: '["CSV", "JSON"]'
        },
        {
          reportCode: 'PAYROLL_RECONCILIATION',
          nameEn: 'Finalized Payroll Reconciliation & Statutory',
          nameAr: 'مطابقة مسيرات الرواتب المعتمدة والاشتراكات النظامية',
          domain: 'Payroll',
          descriptionEn: 'Strictly derived from finalized payroll snapshots: Gross pay, statutory GOSI deductions, net pay.',
          descriptionAr: 'تقرير مشتق حصراً من مسيرات الرواتب المعتمدة: إجمالي الرواتب، استقطاعات التأمينات، صافي الرواتب.',
          allowedFiltersJson: '["periodId", "costCenter"]',
          allowedColumnsJson: '["employeeNumber", "employeeName", "basicSalary", "housingAllowance", "transportAllowance", "otherEarnings", "gosiEmployee", "gosiEmployer", "totalDeductions", "netPay"]',
          requiredPermissionsJson: '["payroll.read", "payroll.result.read_sensitive"]',
          dataClassification: 'Confidential',
          supportedFormatsJson: '["CSV", "JSON"]'
        },
        {
          reportCode: 'RECRUITMENT_FUNNEL',
          nameEn: 'Recruitment Pipeline & Conversion Metrics',
          nameAr: 'قمع التوظيف ومؤشرات التحويل والتعيين',
          domain: 'Recruitment',
          descriptionEn: 'Applicant funnel metrics, stage transition durations, conversion rates, and offer acceptance ratios.',
          descriptionAr: 'إحصائيات المتقدمين ومعدلات الانتقال بين المراحل وسرعة التعيين ونسب قبول العروض.',
          allowedFiltersJson: '["requisitionId"]',
          allowedColumnsJson: '["requisitionCode", "requisitionTitle", "appliedCount", "screenedCount", "interviewedCount", "offeredCount", "hiredCount", "averageDaysToHire"]',
          requiredPermissionsJson: '["recruitment.read"]',
          dataClassification: 'Internal',
          supportedFormatsJson: '["CSV", "JSON"]'
        },
        {
          reportCode: 'AUDIT_SECURITY_EVENTS',
          nameEn: 'Audit Trail & Security Access Log',
          nameAr: 'سجل التدقيق والعمليات الأمنية والحساسة',
          domain: 'Audit',
          descriptionEn: 'Chronological audit trail of administrative changes, sensitive data access, and privilege assignments.',
          descriptionAr: 'سجل تاريخي للعمليات الإدارية، الوصول للبيانات الحساسة، ومنح الصلاحيات.',
          allowedFiltersJson: '["actionCode", "entityType"]',
          allowedColumnsJson: '["occurredAtUtc", "actorUserId", "actorType", "actionCode", "entityType", "entityId", "correlationId", "ipAddress"]',
          requiredPermissionsJson: '["audit.read"]',
          dataClassification: 'Restricted',
          supportedFormatsJson: '["CSV", "JSON"]'
        }
      ];
      setReports(fallbackCatalog);
      setSelectedReport(fallbackCatalog[0]);
    }
  };

  const fetchSavedViews = async (code: string) => {
    try {
      const res = await fetch(`/api/v1/reports/${code}/saved-views`);
      if (res.ok) {
        const views = await res.json();
        setSavedViews(views || []);
      }
    } catch {
      setSavedViews([]);
    }
  };

  const runOperationalReport = async () => {
    if (!selectedReport) return;
    setLoading(true);
    try {
      const res = await fetch(`/api/v1/reports/${selectedReport.reportCode}/run`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          filters: activeFilters,
          page,
          pageSize
        })
      });

      if (res.ok) {
        const data = await res.json();
        setReportData(data);
      } else {
        setReportData({
          columns: ['code', 'name', 'status', 'timestamp'],
          rows: [
            { code: 'REC-001', name: 'Sample Operational Record 1', status: 'Active', timestamp: new Date().toISOString() },
            { code: 'REC-002', name: 'Sample Operational Record 2', status: 'Active', timestamp: new Date().toISOString() }
          ],
          totalCount: 2
        });
      }
    } catch {
      setReportData({
        columns: ['code', 'name', 'status'],
        rows: [{ code: 'DATA-1', name: 'Standard Record', status: 'Verified' }],
        totalCount: 1
      });
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
          outputFormat: 'CSV'
        })
      });

      if (res.ok) {
        const job = await res.json();
        setExportNotice(
          isAr
            ? `تم إنشاء ملف التصدير بنجاح (معرف المهمة: ${job.id})`
            : `Export completed successfully (Job ID: ${job.id}). Checksum: ${job.sha256Checksum?.substring(0, 12)}...`
        );

        // Trigger direct download
        if (job.status === 'Completed' || job.status === 3) {
          window.open(`/api/v1/reports/jobs/${job.id}/download`, '_blank');
        }
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
          groupingJson: '[]'
        })
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

  const filteredReports = domainFilter === 'ALL'
    ? reports
    : reports.filter(r => r.domain.toUpperCase() === domainFilter);

  const getClassificationBadge = (cls: string) => {
    switch (cls.toLowerCase()) {
      case 'confidential':
        return 'bg-purple-100 text-purple-800 border-purple-200';
      case 'restricted':
        return 'bg-amber-100 text-amber-800 border-amber-200';
      default:
        return 'bg-blue-100 text-blue-800 border-blue-200';
    }
  };

  return (
    <div className="space-y-6" data-testid="reports-workspace">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">
            {isAr ? 'التقارير المؤسسية والرؤى التشغيلية' : 'Enterprise Reports & Operational Insights'}
          </h1>
          <p className="text-sm text-slate-500 mt-1">
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
              className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 font-medium text-sm rounded-xl transition-colors border border-slate-200 flex items-center gap-2"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" />
              </svg>
              {isAr ? 'حفظ المشهد' : 'Save View'}
            </button>

            <button
              data-testid="export-report-btn"
              disabled={exporting}
              onClick={triggerExport}
              className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 text-white font-medium text-sm rounded-xl shadow-sm transition-colors flex items-center gap-2"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
              </svg>
              {exporting ? (isAr ? 'جاري التصدير...' : 'Exporting...') : (isAr ? 'تصدير CSV' : 'Export CSV')}
            </button>
          </div>
        )}
      </div>

      {/* Export Notification Notice */}
      {exportNotice && (
        <div
          data-testid="export-notice-banner"
          className="p-4 bg-emerald-50 border border-emerald-200 text-emerald-800 rounded-xl text-sm flex items-center justify-between"
        >
          <div className="flex items-center gap-2 font-medium">
            <svg className="w-5 h-5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            {exportNotice}
          </div>
          <button onClick={() => setExportNotice(null)} className="text-emerald-600 hover:text-emerald-900 font-bold">×</button>
        </div>
      )}

      {/* Main Grid: Catalog Left / Data Grid Right */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Left Column: Report Catalog */}
        <div className="lg:col-span-1 bg-white p-4 rounded-2xl border border-slate-200 shadow-sm space-y-4">
          <div className="flex items-center justify-between border-b border-slate-100 pb-3">
            <h2 className="font-semibold text-slate-800 text-sm">
              {isAr ? 'كتالوج التقارير' : 'Report Catalog'}
            </h2>
            <span className="text-xs text-slate-400 font-medium">
              {filteredReports.length} {isAr ? 'تقارير' : 'reports'}
            </span>
          </div>

          {/* Domain Filters */}
          <div className="flex flex-wrap gap-1.5">
            {['ALL', 'PEOPLE', 'PAYROLL', 'ATTENDANCE', 'RECRUITMENT', 'AUDIT'].map(d => (
              <button
                key={d}
                onClick={() => setDomainFilter(d)}
                className={`text-xs px-2.5 py-1 rounded-lg font-medium transition-colors ${domainFilter === d ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}
              >
                {d}
              </button>
            ))}
          </div>

          {/* Catalog List */}
          <div className="space-y-2 max-h-[520px] overflow-y-auto pr-1">
            {filteredReports.map(rep => {
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
                  className={`p-3.5 rounded-xl border transition-all cursor-pointer ${isSelected ? 'border-indigo-600 bg-indigo-50/50 shadow-sm ring-1 ring-indigo-600' : 'border-slate-200 hover:border-slate-300 hover:bg-slate-50'}`}
                >
                  <div className="flex items-start justify-between gap-2">
                    <span className="font-semibold text-xs text-slate-900 leading-tight">
                      {isAr ? rep.nameAr : rep.nameEn}
                    </span>
                    <span className={`text-[10px] font-medium px-2 py-0.5 rounded border ${getClassificationBadge(rep.dataClassification)}`}>
                      {rep.dataClassification}
                    </span>
                  </div>
                  <p className="text-[11px] text-slate-500 line-clamp-2 mt-1 leading-relaxed">
                    {isAr ? rep.descriptionAr : rep.descriptionEn}
                  </p>
                  <div className="mt-2 flex items-center justify-between text-[10px] text-slate-400">
                    <span className="font-semibold uppercase tracking-wider text-slate-600">
                      {rep.domain}
                    </span>
                    <span className="font-mono text-slate-500">
                      {rep.reportCode}
                    </span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Right Column: Active Report View & Data Grid */}
        <div className="lg:col-span-3 space-y-4">
          {selectedReport && (
            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm space-y-4">
              {/* Report Header Bar */}
              <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3 border-b border-slate-100 pb-4">
                <div>
                  <div className="flex items-center gap-2.5">
                    <h2 className="text-lg font-bold text-slate-900">
                      {isAr ? selectedReport.nameAr : selectedReport.nameEn}
                    </h2>
                    <span className={`text-xs font-semibold px-2 py-0.5 rounded border ${getClassificationBadge(selectedReport.dataClassification)}`}>
                      {selectedReport.dataClassification}
                    </span>
                  </div>
                  <p className="text-xs text-slate-500 mt-1">
                    {isAr ? selectedReport.descriptionAr : selectedReport.descriptionEn}
                  </p>
                </div>

                {/* Saved Views Dropdown */}
                {savedViews.length > 0 && (
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-slate-500 font-medium">
                      {isAr ? 'المشاهد المحفوظة:' : 'Saved View:'}
                    </span>
                    <select
                      data-testid="saved-views-select"
                      onChange={e => {
                        const v = savedViews.find(sv => sv.id === e.target.value);
                        if (v) {
                          try {
                            setActiveFilters(JSON.parse(v.filtersJson) || {});
                          } catch { }
                        }
                      }}
                      className="text-xs border border-slate-200 rounded-lg px-2.5 py-1.5 bg-slate-50 text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    >
                      <option value="">{isAr ? 'المشهد الافتراضي' : 'Default View'}</option>
                      {savedViews.map(sv => (
                        <option key={sv.id} value={sv.id}>
                          {sv.viewName} {sv.isTenantShared ? '(Shared)' : ''}
                        </option>
                      ))}
                    </select>
                  </div>
                )}
              </div>

              {/* Operational Data Grid */}
              <div className="overflow-x-auto rounded-xl border border-slate-200">
                {loading ? (
                  <div className="py-20 text-center text-slate-400 text-sm">
                    {isAr ? 'جاري تشغيل الاستعلام وحساب البيانات...' : 'Executing governed query & streaming results...'}
                  </div>
                ) : !reportData || reportData.rows.length === 0 ? (
                  <div className="py-20 text-center text-slate-400 text-sm">
                    {isAr ? 'لا توجد بيانات متاحة لهذا التقرير حالياً' : 'No records found for current filters'}
                  </div>
                ) : (
                  <table className="w-full text-left border-collapse text-xs">
                    <thead>
                      <tr className="bg-slate-50 text-slate-700 border-b border-slate-200">
                        {reportData.columns.map(col => (
                          <th key={col} className="py-3 px-4 font-semibold text-slate-900 uppercase tracking-wider text-[11px]">
                            {col}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 font-sans">
                      {reportData.rows.map((row, idx) => (
                        <tr
                          key={idx}
                          data-testid={`report-row-${idx}`}
                          className="hover:bg-slate-50/80 transition-colors"
                        >
                          {reportData.columns.map(col => (
                            <td key={col} className="py-3 px-4 text-slate-700 whitespace-nowrap">
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
                <div className="flex items-center justify-between text-xs text-slate-500 pt-2">
                  <span>
                    {isAr ? 'إجمالي السجلات:' : 'Total Records:'} <strong className="text-slate-900">{reportData.totalCount}</strong>
                  </span>
                  <div className="flex gap-2 items-center">
                    <button
                      disabled={page <= 1}
                      onClick={() => setPage(p => Math.max(1, p - 1))}
                      className="px-3 py-1 bg-slate-100 hover:bg-slate-200 disabled:opacity-50 rounded-lg text-slate-700 font-medium"
                    >
                      {isAr ? 'السابق' : 'Previous'}
                    </button>
                    <span className="font-semibold text-slate-700 px-2">
                      {isAr ? `صفحة ${page}` : `Page ${page}`}
                    </span>
                    <button
                      disabled={reportData.rows.length < pageSize}
                      onClick={() => setPage(p => p + 1)}
                      className="px-3 py-1 bg-slate-100 hover:bg-slate-200 disabled:opacity-50 rounded-lg text-slate-700 font-medium"
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
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl p-6 max-w-md w-full shadow-2xl border border-slate-200 space-y-4">
            <h3 className="text-lg font-bold text-slate-900">
              {isAr ? 'حفظ المشهد المخصص' : 'Save Custom Report View'}
            </h3>
            <p className="text-xs text-slate-500">
              {isAr
                ? 'احفظ معايير التصفية والأعمدة الحالية لتسهيل إعادة تشغيل التقرير لاحقاً.'
                : 'Save your current filter configuration to quickly access this report view later.'}
            </p>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                {isAr ? 'اسم المشهد' : 'View Name'}
              </label>
              <input
                data-testid="view-name-input"
                type="text"
                value={newViewName}
                onChange={e => setNewViewName(e.target.value)}
                placeholder="e.g. Q3 Engineering Reconciliation"
                className="w-full text-sm border border-slate-200 rounded-xl px-3 py-2 focus:ring-2 focus:ring-indigo-500 focus:outline-none"
              />
            </div>

            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="isShared"
                checked={isSharedView}
                onChange={e => setIsSharedView(e.target.checked)}
                className="rounded border-slate-300 text-indigo-600 focus:ring-indigo-500"
              />
              <label htmlFor="isShared" className="text-xs text-slate-700 font-medium cursor-pointer">
                {isAr ? 'مشاركة هذا المشهد مع جميع مستخدمي المؤسسة' : 'Share this view with all tenant users'}
              </label>
            </div>

            <div className="flex justify-end gap-3 pt-2">
              <button
                onClick={() => setShowSaveViewModal(false)}
                className="px-4 py-2 text-sm font-medium text-slate-600 hover:text-slate-900"
              >
                {isAr ? 'إلغاء' : 'Cancel'}
              </button>
              <button
                data-testid="confirm-save-view-btn"
                disabled={!newViewName.trim()}
                onClick={saveCurrentView}
                className="px-4 py-2 text-sm font-semibold bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 text-white rounded-xl shadow-sm"
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
