import React, { Suspense, lazy, useState } from 'react';
import { createRoute } from '@tanstack/react-router';
import {
  AttendanceDayDto,
  AttendanceExceptionDto,
  useGetApiV1AttendanceDays,
  useGetApiV1AttendanceExceptions,
  usePostApiV1AttendanceDaysIdAdjustments,
  usePostApiV1AttendanceDaysIdApprove,
  usePostApiV1AttendanceExceptionsIdResolve,
} from '@zainx/contracts';
import { Route as rootRoute } from './__root';

const AttendanceRecordsGrid = lazy(() => import('@zainx/attendance').then((m) => ({ default: m.AttendanceRecordsGrid })));
const AttendanceExceptionsQueue = lazy(() => import('@zainx/attendance').then((m) => ({ default: m.AttendanceExceptionsQueue })));
const AttendanceAdjustmentModal = lazy(() => import('@zainx/attendance').then((m) => ({ default: m.AttendanceAdjustmentModal })));

export function AttendanceComponent() {
  const [selectedRecordForAdjustment, setSelectedRecordForAdjustment] = useState<AttendanceDayDto | null>(null);
  const [isExceptionsDrawerOpen, setIsExceptionsDrawerOpen] = useState(false);
  const daysQuery = useGetApiV1AttendanceDays({ page: 1, pageSize: 100 });
  // The API contract uses AttendanceExceptionStatus.Open = 1. Keep the
  // filtering server-side so the queue does not fetch resolved exceptions.
  const exceptionsQuery = useGetApiV1AttendanceExceptions({ status: 1, page: 1, pageSize: 100 });
  const approveMutation = usePostApiV1AttendanceDaysIdApprove();
  const adjustMutation = usePostApiV1AttendanceDaysIdAdjustments();
  const resolveMutation = usePostApiV1AttendanceExceptionsIdResolve();

  const records = daysQuery.data?.items ?? [];
  const exceptions = exceptionsQuery.data?.items ?? [];
  const pendingExceptions = exceptions.filter((exception) => String(exception.status) === 'Open' || String(exception.status) === 'Pending');
  const refresh = async () => { await Promise.all([daysQuery.refetch(), exceptionsQuery.refetch()]); };

  const handleApproveRecord = async (record: AttendanceDayDto) => {
    await approveMutation.mutateAsync({ id: record.id, data: { rowVersion: record.rowVersion } });
    await daysQuery.refetch();
  };

  const handleSubmitAdjustment = async (dayId: string, adjustedMinutes: number, reason: string, rowVersion: number) => {
    await adjustMutation.mutateAsync({ id: dayId, data: { adjustedWorkedMinutes: adjustedMinutes, reason, rowVersion } });
    setSelectedRecordForAdjustment(null);
    await daysQuery.refetch();
  };

  const handleResolveException = async (exceptionId: string, notes: string, waive: boolean) => {
    const resolvedNotes = waive ? `${notes} [Waived by authorized operator]` : notes;
    await resolveMutation.mutateAsync({ id: exceptionId, data: { notes: resolvedNotes } });
    await exceptionsQuery.refetch();
  };

  return (
    <div className="mx-auto w-full max-w-[1440px] space-y-6" data-testid="attendance-route-page">
      <Suspense fallback={<div className="rounded-lg border border-border-default bg-surface p-8 text-sm text-text-secondary">Loading attendance workspace…</div>}>
        <AttendanceRecordsGrid
          records={records}
          isLoading={daysQuery.isLoading}
          isError={daysQuery.isError}
          pendingExceptionsCount={pendingExceptions.length}
          onOpenExceptionsQueue={() => setIsExceptionsDrawerOpen(true)}
          onAdjustRecord={setSelectedRecordForAdjustment}
          onApproveRecord={handleApproveRecord}
          onRefresh={refresh}
        />
        <AttendanceAdjustmentModal
          isOpen={!!selectedRecordForAdjustment}
          record={selectedRecordForAdjustment}
          onClose={() => setSelectedRecordForAdjustment(null)}
          onSubmitAdjustment={handleSubmitAdjustment}
        />
        <AttendanceExceptionsQueue
          isOpen={isExceptionsDrawerOpen}
          exceptions={pendingExceptions}
          isLoading={exceptionsQuery.isLoading}
          onClose={() => setIsExceptionsDrawerOpen(false)}
          onResolveException={handleResolveException}
        />
      </Suspense>
    </div>
  );
}

export const attendanceRoute = createRoute({ getParentRoute: () => rootRoute, path: '/attendance', component: AttendanceComponent });
