import React, { useState, Suspense, lazy } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import {
  AttendanceDayDto,
  AttendanceExceptionDto
} from '@zainx/contracts';

// Lazy load attendance components with strict route-level chunk isolation
const AttendanceRecordsGrid = lazy(() =>
  import('@zainx/attendance').then((m) => ({ default: m.AttendanceRecordsGrid }))
);
const AttendanceExceptionsQueue = lazy(() =>
  import('@zainx/attendance').then((m) => ({ default: m.AttendanceExceptionsQueue }))
);
const AttendanceAdjustmentModal = lazy(() =>
  import('@zainx/attendance').then((m) => ({ default: m.AttendanceAdjustmentModal }))
);

const AttendanceStatus = {
  Locked: 'Locked',
  Approved: 'Approved',
  Reviewed: 'Reviewed',
  Unreviewed: 'Unreviewed',
} as const;

// Initial fallback mock data for testing/offline rendering
const initialRecords: AttendanceDayDto[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    employmentId: '44444444-4444-4444-4444-444444444444',
    businessDate: '2026-08-24',
    timeZoneId: 'Asia/Riyadh',
    status: AttendanceStatus.Reviewed,
    scheduledMinutes: 480,
    firstClockInUtc: '2026-08-24T05:00:00Z',
    lastClockOutUtc: '2026-08-24T13:30:00Z',
    totalWorkedMinutes: 510,
    lateMinutes: 0,
    earlyDepartureMinutes: 0,
    isAbsent: false,
    rowVersion: 1
  },
  {
    id: '22222222-2222-2222-2222-222222222222',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    employmentId: '55555555-5555-5555-5555-555555555555',
    businessDate: '2026-08-24',
    timeZoneId: 'Asia/Riyadh',
    status: AttendanceStatus.Unreviewed,
    scheduledMinutes: 480,
    firstClockInUtc: '2026-08-24T05:15:00Z',
    lastClockOutUtc: null,
    totalWorkedMinutes: 0,
    lateMinutes: 15,
    earlyDepartureMinutes: 0,
    isAbsent: false,
    rowVersion: 1
  }
];

const initialExceptions: AttendanceExceptionDto[] = [
  {
    id: 'ex-01',
    attendanceDayId: '22222222-2222-2222-2222-222222222222',
    tenantId: '22222222-2222-2222-2222-222222222222',
    employmentId: '55555555-5555-5555-5555-555555555555',
    type: 'MissingClockOut',
    status: 'Pending',
    details: 'No clock-out recorded for shift ending at 16:30.',
    resolutionNotes: null,
    resolvedByUserId: null,
    resolvedAtUtc: null,
    createdAtUtc: '2026-08-24T14:00:00Z'
  }
];

export function AttendanceComponent() {
  const [records, setRecords] = useState<AttendanceDayDto[]>(initialRecords);
  const [exceptions, setExceptions] = useState<AttendanceExceptionDto[]>(initialExceptions);
  const [selectedRecordForAdjustment, setSelectedRecordForAdjustment] = useState<AttendanceDayDto | null>(null);
  const [isExceptionsDrawerOpen, setIsExceptionsDrawerOpen] = useState(false);

  const pendingExceptions = exceptions.filter((e) => e.status === 'Pending');

  const handleAdjustRecord = (record: AttendanceDayDto) => {
    setSelectedRecordForAdjustment(record);
  };

  const handleApproveRecord = (record: AttendanceDayDto) => {
    setRecords((prev) =>
      prev.map((r) =>
        r.id === record.id
          ? {
              ...r,
              status: AttendanceStatus.Approved,
              rowVersion: Number(r.rowVersion || 0) + 1
            }
          : r
      )
    );
  };

  const handleSubmitAdjustment = async (
    dayId: string,
    adjustedMinutes: number,
    reason: string,
    rowVersion: number
  ) => {
    setRecords((prev) =>
      prev.map((r) => {
        if (r.id === dayId) {
          if (Number(r.rowVersion) !== rowVersion) {
            throw new Error('Concurrency conflict: Record was updated by another process.');
          }
          return {
            ...r,
            totalWorkedMinutes: adjustedMinutes,
            status: AttendanceStatus.Reviewed,
            rowVersion: Number(r.rowVersion || 0) + 1
          };
        }
        return r;
      })
    );
  };

  const handleResolveException = async (
    exceptionId: string,
    notes: string,
    waive: boolean
  ) => {
    setExceptions((prev) => prev.filter((e) => e.id !== exceptionId));
  };

  return (
    <div className="space-y-6" data-testid="attendance-route-page">
      <Suspense fallback={<div className="p-8 text-sm text-text-muted">Loading attendance module...</div>}>
        <AttendanceRecordsGrid
          records={records}
          pendingExceptionsCount={pendingExceptions.length}
          onOpenExceptionsQueue={() => setIsExceptionsDrawerOpen(true)}
          onAdjustRecord={handleAdjustRecord}
          onApproveRecord={handleApproveRecord}
          onRefresh={() => {}}
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
          onClose={() => setIsExceptionsDrawerOpen(false)}
          onResolveException={handleResolveException}
        />
      </Suspense>
    </div>
  );
}

export const attendanceRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/attendance',
  component: AttendanceComponent
});
