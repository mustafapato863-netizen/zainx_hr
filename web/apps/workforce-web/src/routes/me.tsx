import React, { FormEvent, useEffect, useState } from 'react';
import { createRoute } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import {
  getApiV1SelfServiceDocumentsIdDownload,
  useGetApiV1SelfServiceAttendanceToday,
  useGetApiV1SelfServiceDocuments,
  useGetApiV1SelfServiceLeaveBalances,
  useGetApiV1SelfServiceLeaveRequests,
  useGetApiV1SelfServiceLeaveTypes,
  useGetApiV1SelfServiceProfile,
  useGetApiV1SelfServiceTeam,
  usePostApiV1SelfServiceAttendanceClock,
  usePostApiV1SelfServiceLeaveRequests,
  usePutApiV1SelfServiceProfile,
} from '@zainx/contracts';
import { Icon, PageHeader } from '@zainx/design-system';
import { Route as rootRoute } from './__root';

export const meRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/me',
  component: SelfServicePage,
});

function SelfServicePage() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === 'ar';
  const profile = useGetApiV1SelfServiceProfile();
  const selfServiceEnabled = Boolean(profile.data);
  const team = useGetApiV1SelfServiceTeam(
    { page: 1, pageSize: 20 },
    { query: { enabled: selfServiceEnabled } },
  );
  const leaveBalances = useGetApiV1SelfServiceLeaveBalances(
    { year: new Date().getFullYear() },
    { query: { enabled: selfServiceEnabled } },
  );
  const leaveRequests = useGetApiV1SelfServiceLeaveRequests(
    { page: 1, pageSize: 5 },
    { query: { enabled: selfServiceEnabled } },
  );
  const leaveTypes = useGetApiV1SelfServiceLeaveTypes({ query: { enabled: selfServiceEnabled } });
  const attendance = useGetApiV1SelfServiceAttendanceToday(
    { date: new Date().toISOString().slice(0, 10) },
    { query: { enabled: selfServiceEnabled } },
  );
  const documents = useGetApiV1SelfServiceDocuments({
    query: { enabled: selfServiceEnabled },
  });
  const updateProfile = usePutApiV1SelfServiceProfile();
  const clock = usePostApiV1SelfServiceAttendanceClock();
  const submitLeave = usePostApiV1SelfServiceLeaveRequests();
  const [primaryEmail, setPrimaryEmail] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const [operationsError, setOperationsError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const [clockSaved, setClockSaved] = useState(false);
  const [documentDownloadId, setDocumentDownloadId] = useState<string | null>(null);
  const [documentDownloadError, setDocumentDownloadError] = useState<string | null>(null);
  const [leaveTypeId, setLeaveTypeId] = useState('');
  const [leaveStartDate, setLeaveStartDate] = useState('');
  const [leaveEndDate, setLeaveEndDate] = useState('');
  const [leaveReason, setLeaveReason] = useState('');
  const [leaveSubmitError, setLeaveSubmitError] = useState<string | null>(null);
  const [leaveSubmitted, setLeaveSubmitted] = useState(false);

  useEffect(() => {
    if (!profile.data) return;
    setPrimaryEmail(profile.data.primaryEmail ?? '');
    setPhoneNumber(profile.data.phoneNumber ?? '');
  }, [profile.data]);

  useEffect(() => {
    if (!leaveTypeId && leaveTypes.data?.length) setLeaveTypeId(leaveTypes.data[0].id);
  }, [leaveTypeId, leaveTypes.data]);

  const labels = {
    title: isAr ? 'مساحتي' : 'My workspace',
    subtitle: isAr
      ? 'ملفك الوظيفي وفريقك المباشر من سجلات القوى العاملة المصرح بها.'
      : 'Your profile and direct team, projected from the authorized workforce record.',
    profile: isAr ? 'بيانات الاتصال' : 'Contact details',
    team: isAr ? 'فريقي المباشر' : 'My direct team',
    email: isAr ? 'البريد الإلكتروني' : 'Primary email',
    phone: isAr ? 'رقم الهاتف' : 'Phone number',
    save: isAr ? 'حفظ التغييرات' : 'Save changes',
    saved: isAr ? 'تم حفظ التغييرات' : 'Changes saved',
    noLinkTitle: isAr ? 'لم يتم ربط حسابك بموظف' : 'Employee identity link required',
    noLinkBody: isAr
      ? 'لم يتم ربط المستخدم الحالي بسجل توظيف في الكيان القانوني الحالي. اطلب من المسؤول إعداد الربط.'
      : 'The current user is not explicitly linked to an employment in this legal-entity context. Ask an administrator to configure the link.',
    loading: isAr ? 'جار تحميل بياناتك' : 'Loading your workforce profile',
    retry: isAr ? 'إعادة المحاولة' : 'Retry',
    emptyTeam: isAr ? 'لا يوجد أعضاء فريق مباشرون' : 'No direct team members are currently assigned.',
    teamError: isAr ? 'تعذر تحميل الفريق' : 'The team could not be loaded',
    operations: isAr ? 'العمليات اليومية' : 'Daily operations',
    attendanceToday: isAr ? 'حضور اليوم' : "Today's attendance",
    clockIn: isAr ? 'تسجيل حضور' : 'Clock in',
    clockOut: isAr ? 'تسجيل انصراف' : 'Clock out',
    noAttendance: isAr ? 'لم يتم تسجيل حركة حضور لهذا اليوم.' : 'No attendance event has been recorded today.',
    attendanceError: isAr ? 'تعذر تحميل حالة الحضور' : 'Today\'s attendance could not be loaded',
    leaveBalances: isAr ? 'أرصدة الإجازات' : 'Leave balances',
    leaveRequests: isAr ? 'آخر طلبات الإجازة' : 'Recent leave requests',
    noBalances: isAr ? 'لا توجد أرصدة إجازات متاحة لهذه السنة.' : 'No leave balances are available for this year.',
    noRequests: isAr ? 'لا توجد طلبات إجازة.' : 'No leave requests have been submitted.',
    leaveError: isAr ? 'تعذر تحميل بيانات الإجازات' : 'Leave data could not be loaded',
    available: isAr ? 'متاح' : 'Available',
    status: isAr ? 'الحالة' : 'Status',
    clockSaved: isAr ? 'تم تسجيل حركة الحضور' : 'Attendance event recorded',
    operationError: isAr ? 'تعذر تنفيذ العملية. حاول مرة أخرى.' : 'The operation could not be completed. Try again.',
    documents: isAr ? 'مستنداتي' : 'My documents',
    documentsDescription: isAr
      ? 'المستندات المرتبطة بسجلك الوظيفي في الكيان الحالي.'
      : 'Documents attached to your workforce record in the current legal entity.',
    noDocuments: isAr ? 'لا توجد مستندات مرتبطة بسجلك.' : 'No documents are attached to your record.',
    documentsError: isAr ? 'تعذر تحميل المستندات' : 'Your documents could not be loaded',
    requestLeave: isAr ? 'طلب إجازة' : 'Request leave',
    leaveType: isAr ? 'نوع الإجازة' : 'Leave type',
    startDate: isAr ? 'من' : 'Start date',
    endDate: isAr ? 'إلى' : 'End date',
    reason: isAr ? 'السبب' : 'Reason',
    submitted: isAr ? 'تم إرسال الطلب للموافقة' : 'Request submitted for manager approval',
    noLeaveTypes: isAr ? 'لا توجد أنواع إجازات مهيأة.' : 'No configured leave types are available.',
    download: isAr ? 'تنزيل' : 'Download',
    downloading: isAr ? 'جارٍ التنزيل…' : 'Downloading…',
    noExpiry: isAr ? 'بدون تاريخ انتهاء' : 'No expiry date',
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);
    setSaved(false);
    if (!profile.data) return;

    try {
      await updateProfile.mutateAsync({
        data: {
          primaryEmail: primaryEmail.trim(),
          phoneNumber: phoneNumber.trim(),
          rowVersion: profile.data.rowVersion ?? 0,
        },
      });
      await profile.refetch();
      setSaved(true);
    } catch {
      setFormError(isAr ? 'تعذر حفظ البيانات. حدّث الصفحة وحاول مرة أخرى.' : 'The profile could not be saved. Refresh and try again.');
    }
  };

  const handleClock = async (type: 1 | 2) => {
    setOperationsError(null);
    setClockSaved(false);
    try {
      await clock.mutateAsync({ data: { type, source: 3 } });
      await attendance.refetch();
      setClockSaved(true);
    } catch {
      setOperationsError(labels.operationError);
    }
  };

  const handleLeaveSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setLeaveSubmitError(null);
    setLeaveSubmitted(false);
    try {
      await submitLeave.mutateAsync({
        data: {
          leaveTypeId,
          startDate: leaveStartDate,
          endDate: leaveEndDate,
          reason: leaveReason.trim(),
        },
      });
      setLeaveSubmitted(true);
      setLeaveStartDate('');
      setLeaveEndDate('');
      setLeaveReason('');
      await Promise.all([leaveRequests.refetch(), leaveBalances.refetch()]);
    } catch (error) {
      const detail = (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      setLeaveSubmitError(detail || labels.operationError);
    }
  };

  const handleDocumentDownload = async (id: string, title: string) => {
    setDocumentDownloadError(null);
    setDocumentDownloadId(id);
    try {
      const file = await getApiV1SelfServiceDocumentsIdDownload(id);
      const url = URL.createObjectURL(file);
      const anchor = globalThis.document.createElement('a');
      anchor.href = url;
      anchor.download = title || 'workforce-document';
      globalThis.document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      globalThis.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch {
      setDocumentDownloadError(labels.documentsError);
    } finally {
      setDocumentDownloadId(null);
    }
  };

  if (profile.isLoading) {
    return <StatePanel icon="refresh" title={labels.loading} body="" />;
  }

  if (profile.isError || !profile.data) {
    return (
      <main className="mx-auto w-full max-w-[1200px]">
        <PageHeader title={labels.title} subtitle={labels.subtitle} />
        <section className="rounded-xl border border-border-default bg-surface p-6 shadow-xs" role="status">
          <div className="flex items-start gap-4">
            <span className="grid h-11 w-11 shrink-0 place-items-center rounded-full bg-warning-subtle text-warning"><Icon name="user" size="sm" aria-hidden="true" /></span>
            <div>
              <h2 className="text-lg font-semibold text-text-primary">{labels.noLinkTitle}</h2>
              <p className="mt-1 max-w-2xl text-sm leading-6 text-text-secondary">{labels.noLinkBody}</p>
              <button type="button" onClick={() => profile.refetch()} className="mt-4 inline-flex min-h-10 items-center gap-2 rounded-md border border-border-default px-4 text-sm font-semibold text-text-primary hover:bg-surface-subtle focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-border-focus">{labels.retry}</button>
            </div>
          </div>
        </section>
      </main>
    );
  }

  const fullName = isAr ? profile.data.fullNameAr : profile.data.fullNameEn;
  const teamItems = team.data?.items ?? [];
  const balanceItems = leaveBalances.data ?? [];
  const requestItems = leaveRequests.data?.items ?? [];
  const attendanceDay = attendance.data && 'status' in attendance.data ? attendance.data : null;
  const documentItems = documents.data ?? [];

  return (
    <main className="mx-auto w-full max-w-[1200px]">
      <PageHeader
        title={labels.title}
        subtitle={labels.subtitle}
        badge={<span className="rounded-full bg-primary-subtle px-2.5 py-1 text-xs font-semibold text-primary-subtle-text">{isAr ? 'الخدمة الذاتية' : 'Self-service'}</span>}
      />

      <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
        <section className="rounded-xl border border-border-default bg-surface p-5 shadow-xs" aria-labelledby="self-profile-title">
          <div className="mb-5 flex items-start gap-3">
            <span className="grid h-11 w-11 shrink-0 place-items-center rounded-full bg-primary-subtle text-primary"><Icon name="user" size="sm" aria-hidden="true" /></span>
            <div className="min-w-0"><p className="text-xs font-semibold uppercase tracking-[0.12em] text-text-tertiary">{profile.data.employeeNumber}</p><h2 id="self-profile-title" className="truncate text-lg font-semibold text-text-primary">{fullName}</h2><p className="text-sm text-text-secondary">{isAr ? profile.data.currentAssignment?.jobTitleAr : profile.data.currentAssignment?.jobTitleEn}</p></div>
          </div>
          <form onSubmit={handleSubmit} className="space-y-4">
            <label className="block text-sm font-medium text-text-primary"><span className="mb-1.5 block">{labels.email}</span><input type="email" value={primaryEmail} onChange={(event) => setPrimaryEmail(event.target.value)} className="min-h-11 w-full rounded-md border border-border-default bg-surface px-3 text-sm text-text-primary outline-none focus:border-border-focus focus:ring-2 focus:ring-primary/20" /></label>
            <label className="block text-sm font-medium text-text-primary"><span className="mb-1.5 block">{labels.phone}</span><input type="tel" value={phoneNumber} onChange={(event) => setPhoneNumber(event.target.value)} className="min-h-11 w-full rounded-md border border-border-default bg-surface px-3 text-sm text-text-primary outline-none focus:border-border-focus focus:ring-2 focus:ring-primary/20" /></label>
            {formError && <p className="text-sm text-danger" role="alert">{formError}</p>}
            {saved && <p className="text-sm text-success" role="status">{labels.saved}</p>}
            <button type="submit" disabled={updateProfile.isPending} className="inline-flex min-h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-white shadow-xs transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-border-focus">{updateProfile.isPending ? '…' : labels.save}</button>
          </form>
        </section>

        <section className="rounded-xl border border-border-default bg-surface p-5 shadow-xs" aria-labelledby="self-team-title">
          <div className="mb-5 flex items-center justify-between gap-3"><div><p className="zainx-eyebrow">{isAr ? 'المدير' : 'Manager view'}</p><h2 id="self-team-title" className="mt-1 text-lg font-semibold text-text-primary">{labels.team}</h2></div><span className="rounded-full bg-surface-subtle px-2.5 py-1 text-xs font-semibold text-text-secondary">{team.data?.totalCount ?? 0}</span></div>
          {team.isLoading ? <div className="space-y-3" aria-label={labels.loading}><div className="h-12 animate-pulse rounded-md bg-surface-subtle" /><div className="h-12 animate-pulse rounded-md bg-surface-subtle" /></div> : team.isError ? <p className="text-sm text-danger" role="alert">{labels.teamError}</p> : teamItems.length ? <div className="space-y-2">{teamItems.map((member) => <div key={member.id} className="flex items-center justify-between gap-3 rounded-lg border border-border-default p-3"><div className="min-w-0"><p className="truncate text-sm font-semibold text-text-primary">{isAr ? member.fullNameAr : member.fullNameEn}</p><p className="truncate text-xs text-text-secondary">{isAr ? member.jobTitleAr : member.jobTitleEn}</p></div><span className="shrink-0 text-xs text-text-tertiary">{member.status}</span></div>)}</div> : <p className="rounded-lg bg-surface-subtle p-4 text-sm text-text-secondary">{labels.emptyTeam}</p>}
        </section>
      </div>

      <section className="mt-5 rounded-xl border border-border-default bg-surface p-5 shadow-xs" aria-labelledby="self-operations-title">
        <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
          <div>
            <p className="zainx-eyebrow">{labels.operations}</p>
            <h2 id="self-operations-title" className="mt-1 text-lg font-semibold text-text-primary">{isAr ? 'يوم العمل والإجازات' : 'Workday and time away'}</h2>
          </div>
          <span className="text-xs text-text-tertiary">{new Date().toISOString().slice(0, 10)}</span>
        </div>

        <div className="grid gap-5 lg:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)]">
          <div className="rounded-lg border border-border-default bg-surface-subtle p-4" aria-labelledby="self-attendance-title">
            <div className="flex items-start justify-between gap-3">
              <div>
                <h3 id="self-attendance-title" className="font-semibold text-text-primary">{labels.attendanceToday}</h3>
                <p className="mt-1 text-sm text-text-secondary">{attendanceDay?.status ?? labels.noAttendance}</p>
              </div>
              <Icon name="clock" size="sm" aria-hidden="true" />
            </div>
            {attendance.isLoading ? <div className="mt-4 h-10 animate-pulse rounded-md bg-surface" aria-label={labels.loading} /> : attendance.isError ? <p className="mt-4 text-sm text-danger" role="alert">{labels.attendanceError}</p> : attendanceDay && <dl className="mt-4 grid grid-cols-2 gap-3 text-sm"><div><dt className="text-text-tertiary">{labels.status}</dt><dd className="mt-1 font-semibold text-text-primary">{attendanceDay.status}</dd></div><div><dt className="text-text-tertiary">{labels.available}</dt><dd className="mt-1 font-semibold text-text-primary">{attendanceDay.totalWorkedMinutes}m</dd></div></dl>}
            <div className="mt-4 flex flex-wrap gap-2">
              <button type="button" onClick={() => void handleClock(1)} disabled={clock.isPending} className="inline-flex min-h-10 items-center justify-center rounded-md bg-primary px-3 text-sm font-semibold text-white shadow-xs transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-border-focus">{labels.clockIn}</button>
              <button type="button" onClick={() => void handleClock(2)} disabled={clock.isPending} className="inline-flex min-h-10 items-center justify-center rounded-md border border-border-default px-3 text-sm font-semibold text-text-primary transition hover:bg-surface focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-border-focus">{labels.clockOut}</button>
            </div>
            {clockSaved && <p className="mt-3 text-sm text-success" role="status">{labels.clockSaved}</p>}
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="rounded-lg border border-border-default p-4" aria-labelledby="self-balances-title">
              <h3 id="self-balances-title" className="font-semibold text-text-primary">{labels.leaveBalances}</h3>
              {leaveBalances.isLoading ? <div className="mt-4 h-14 animate-pulse rounded-md bg-surface-subtle" aria-label={labels.loading} /> : leaveBalances.isError ? <p className="mt-4 text-sm text-danger" role="alert">{labels.leaveError}</p> : balanceItems.length ? <ul className="mt-3 space-y-2">{balanceItems.map((balance) => <li key={balance.id} className="flex items-center justify-between gap-3 border-b border-border-default pb-2 text-sm last:border-b-0 last:pb-0"><span className="truncate text-text-secondary">{isAr ? balance.leaveTypeNameAr : balance.leaveTypeNameEn}</span><strong className="shrink-0 text-text-primary">{String(balance.availableDays)}d</strong></li>)}</ul> : <p className="mt-3 text-sm text-text-secondary">{labels.noBalances}</p>}
            </div>
            <div className="rounded-lg border border-border-default p-4" aria-labelledby="self-requests-title">
              <h3 id="self-requests-title" className="font-semibold text-text-primary">{labels.leaveRequests}</h3>
              <form onSubmit={(event) => void handleLeaveSubmit(event)} className="mt-3 space-y-2 border-b border-border-default pb-3">
                {leaveTypes.isLoading ? <div className="h-10 animate-pulse rounded-md bg-surface-subtle" aria-label={labels.loading} /> : leaveTypes.data?.length ? <>
                  <label className="block text-xs font-medium text-text-secondary">{labels.leaveType}<select required value={leaveTypeId} onChange={(event) => setLeaveTypeId(event.target.value)} className="mt-1 min-h-10 w-full rounded-md border border-border-default bg-surface px-2 text-sm text-text-primary"><option value="" disabled>{labels.leaveType}</option>{leaveTypes.data.map((type) => <option key={type.id} value={type.id}>{isAr ? type.nameAr : type.nameEn}</option>)}</select></label>
                  <div className="grid grid-cols-2 gap-2"><label className="block text-xs font-medium text-text-secondary">{labels.startDate}<input required type="date" value={leaveStartDate} onChange={(event) => setLeaveStartDate(event.target.value)} className="mt-1 min-h-10 w-full rounded-md border border-border-default bg-surface px-2 text-sm text-text-primary" /></label><label className="block text-xs font-medium text-text-secondary">{labels.endDate}<input required type="date" value={leaveEndDate} onChange={(event) => setLeaveEndDate(event.target.value)} className="mt-1 min-h-10 w-full rounded-md border border-border-default bg-surface px-2 text-sm text-text-primary" /></label></div>
                  <label className="block text-xs font-medium text-text-secondary">{labels.reason}<textarea required rows={2} value={leaveReason} onChange={(event) => setLeaveReason(event.target.value)} className="mt-1 w-full rounded-md border border-border-default bg-surface p-2 text-sm text-text-primary" /></label>
                  <button type="submit" disabled={submitLeave.isPending || !leaveTypeId} className="inline-flex min-h-10 items-center justify-center rounded-md bg-primary px-3 text-sm font-semibold text-white shadow-xs transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-border-focus">{submitLeave.isPending ? '…' : labels.requestLeave}</button>
                </> : <p className="text-xs text-text-secondary">{labels.noLeaveTypes}</p>}
                {leaveSubmitError && <p className="text-xs text-danger" role="alert">{leaveSubmitError}</p>}
                {leaveSubmitted && <p className="text-xs text-success" role="status">{labels.submitted}</p>}
              </form>
              {leaveRequests.isLoading ? <div className="mt-4 h-14 animate-pulse rounded-md bg-surface-subtle" aria-label={labels.loading} /> : leaveRequests.isError ? <p className="mt-4 text-sm text-danger" role="alert">{labels.leaveError}</p> : requestItems.length ? <ul className="mt-3 space-y-2">{requestItems.map((request) => <li key={request.id} className="flex items-center justify-between gap-3 border-b border-border-default pb-2 text-sm last:border-b-0 last:pb-0"><span className="truncate text-text-secondary">{request.leaveTypeCode} · {request.startDate}</span><strong className="shrink-0 text-text-primary">{request.status}</strong></li>)}</ul> : <p className="mt-3 text-sm text-text-secondary">{labels.noRequests}</p>}
            </div>
          </div>
        </div>
        {operationsError && <p className="mt-4 text-sm text-danger" role="alert">{operationsError}</p>}
      </section>

      <section className="mt-5 rounded-xl border border-border-default bg-surface p-5 shadow-xs" aria-labelledby="self-documents-title">
        <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
          <div>
            <p className="zainx-eyebrow">{isAr ? 'السجل الوظيفي' : 'Employment record'}</p>
            <h2 id="self-documents-title" className="mt-1 text-lg font-semibold text-text-primary">{labels.documents}</h2>
            <p className="mt-1 text-sm text-text-secondary">{labels.documentsDescription}</p>
          </div>
          <span className="rounded-full bg-surface-subtle px-2.5 py-1 text-xs font-semibold text-text-secondary">{documentItems.length}</span>
        </div>
        {documents.isLoading ? (
          <div className="space-y-3" aria-label={labels.loading}>
            <div className="h-14 animate-pulse rounded-md bg-surface-subtle" />
            <div className="h-14 animate-pulse rounded-md bg-surface-subtle" />
          </div>
        ) : documents.isError ? (
          <p className="text-sm text-danger" role="alert">{labels.documentsError}</p>
        ) : documentItems.length ? (
          <div className="divide-y divide-border-default rounded-lg border border-border-default">
            {documentItems.map((documentItem) => {
              const id = documentItem.id ?? '';
              const title = documentItem.title || documentItem.latestFileName || labels.documents;
              const expiry = documentItem.expiryDate
                ? new Intl.DateTimeFormat(isAr ? 'ar-EG' : 'en-GB', { dateStyle: 'medium' }).format(new Date(documentItem.expiryDate))
                : labels.noExpiry;
              return (
                <div key={id || title} className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-semibold text-text-primary">{title}</p>
                    <p className="mt-1 text-xs text-text-secondary">{isAr ? documentItem.documentTypeNameAr : documentItem.documentTypeNameEn} · {expiry}</p>
                  </div>
                  <button
                    type="button"
                    disabled={!id || documentDownloadId === id}
                    onClick={() => void handleDocumentDownload(id, title)}
                    className="inline-flex min-h-10 shrink-0 items-center justify-center rounded-md border border-border-default px-3 text-sm font-semibold text-text-primary transition hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-border-focus"
                  >
                    {documentDownloadId === id ? labels.downloading : labels.download}
                  </button>
                </div>
              );
            })}
          </div>
        ) : (
          <p className="rounded-lg bg-surface-subtle p-4 text-sm text-text-secondary">{labels.noDocuments}</p>
        )}
        {documentDownloadError && <p className="mt-4 text-sm text-danger" role="alert">{documentDownloadError}</p>}
      </section>
    </main>
  );
}

function StatePanel({ icon, title, body }: { icon: 'refresh' | 'alert-circle'; title: string; body: string }) {
  return <main className="mx-auto w-full max-w-[1200px]"><section className="rounded-xl border border-border-default bg-surface p-6 shadow-xs" role="status"><div className="flex items-center gap-3"><Icon name={icon} size="sm" aria-hidden="true" /><div><h1 className="text-lg font-semibold text-text-primary">{title}</h1>{body && <p className="mt-1 text-sm text-text-secondary">{body}</p>}</div></div></section></main>;
}
