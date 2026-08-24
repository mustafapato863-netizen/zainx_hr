import React, { useState, Suspense, lazy } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import {
  AttendanceDayDto,
  AttendanceStatus,
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

// Initial fallback mock data for testing/offline rendering
const initialRecords: AttendanceDayDto[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    employmentId: '44444444-4444-4444-4444-444444444444',
    employeeNumber: 'EMP-1001',
    employeeNameEn: 'Tariq Al-Mansoor',
    departmentNameEn: 'Human Resources',
    businessDate: '2026-08-24',
    timezoneId: 'Asia/Riyadh',
    status: AttendanceStatus.Reviewed,
    statusName: 'Reviewed',
    scheduledMinutes: 480,
    firstClockInUtc: '2026-08-24T05:00:00Z',
    lastClockOutUtc: '2026-08-24T13:30:00Z',
    totalWorkedMinutes: 510,
    lateMinutes: 0,
    earlyDepartureMinutes: 0,
    isAbsent: false,
    rowVersion: 1,
    exceptions: [],
    adjustments: []
  },
  {
    id: '22222222-2222-2222-2222-222222222222',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    employmentId: '55555555-5555-5555-5555-555555555555',
    employeeNumber: 'EMP-1002',
    employeeNameEn: 'Sara Al-Otaibi',
    departmentNameEn: 'Engineering',
    businessDate: '2026-08-24',
    timezoneId: 'Asia/Riyadh',
    status: AttendanceStatus.Unreviewed,
    statusName: 'Unreviewed',
    scheduledMinutes: 480,
    firstClockInUtc: '2026-08-24T05:15:00Z',
    lastClockOutUtc: null,
    totalWorkedMinutes: 0,
    lateMinutes: 15,
    earlyDepartureMinutes: 0,
    isAbsent: false,
    rowVersion: 1,
    exceptions: [
      {
        id: 'ex-01',
        attendanceDayId: '22222222-2222-2222-2222-222222222222',
        tenantId: '22222222-2222-2222-2222-222222222222',
        employmentId: '55555555-5555-5555-5555-555555555555',
        employeeNameEn: 'Sara Al-Otaibi',
        type: 1,
        typeName: 'MissingClockOut',
        status: 0,
        statusName: 'Pending',
        details: 'No clock-out recorded for shift ending at 16:30.',
        createdAtUtc: '2026-08-24T14:00:00Z'
      }
    ],
    adjustments: []
  }
];

export function AttendanceComponent() {
  const [records, setRecords] = useState<AttendanceDayDto[]>(initialRecords);
  const [selectedRecordForAdjustment, setSelectedRecordForAdjustment] = useState<AttendanceDayDto | null>(null);
  const [isExceptionsDrawerOpen, setIsExceptionsDrawerOpen] = useState(false);

  const pendingExceptions = records.flatMap((r) => r.exceptions || []);

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
              statusName: 'Approved',
              rowVersion: r.rowVersion + 1
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
          if (r.rowVersion !== rowVersion) {
            throw new Error('Concurrency conflict: Record was updated by another process.');
          }
          return {
            ...r,
            totalWorkedMinutes: adjustedMinutes,
            status: AttendanceStatus.Reviewed,
            statusName: 'Reviewed',
            rowVersion: r.rowVersion + 1,
            adjustments: [
              ...(r.adjustments || []),
              {
                id: `adj-${Date.now()}`,
                attendanceDayId: dayId,
                employmentId: r.employmentId,
                adjustedWorkedMinutes: adjustedMinutes,
                beforeWorkedMinutes: r.totalWorkedMinutes,
                afterWorkedMinutes: adjustedMinutes,
                reason,
                actorUserId: 'usr-current',
                createdAtUtc: new Date().toISOString()
              }
            ]
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
    setRecords((prev) =>
      prev.map((r) => ({
        ...r,
        exceptions: (r.exceptions || []).filter((e) => e.id !== exceptionId)
      }))
    );
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
