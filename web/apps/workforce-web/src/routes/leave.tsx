import React, { useState, Suspense, lazy } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import {
  LeaveBalanceDto,
  LeaveRequestDto,
  LeaveTypeDto
} from '@zainx/contracts';

// Lazy load leave components with strict route-level chunk isolation
const LeaveBalancesSummary = lazy(() =>
  import('@zainx/leave').then((m) => ({ default: m.LeaveBalancesSummary }))
);
const LeaveRequestsGrid = lazy(() =>
  import('@zainx/leave').then((m) => ({ default: m.LeaveRequestsGrid }))
);
const LeaveRequestModal = lazy(() =>
  import('@zainx/leave').then((m) => ({ default: m.LeaveRequestModal }))
);
const LeaveCalendar = lazy(() =>
  import('@zainx/leave').then((m) => ({ default: m.LeaveCalendar }))
);

const LeaveRequestStatus = {
  PendingApproval: 'PendingApproval',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled'
} as const;

const initialLeaveTypes: LeaveTypeDto[] = [
  {
    id: 'type-annual',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    code: 'ANNUAL',
    nameEn: 'Annual Leave',
    nameAr: 'الإجازة السنوية',
    category: 'Annual',
    isPaid: true,
    requiresAttachment: false,
    allowHalfDay: true,
    isActive: true
  },
  {
    id: 'type-sick',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    code: 'SICK',
    nameEn: 'Sick Leave',
    nameAr: 'إجازة مرضية',
    category: 'Sick',
    isPaid: true,
    requiresAttachment: true,
    allowHalfDay: false,
    isActive: true
  }
];

const initialBalances: LeaveBalanceDto[] = [
  {
    id: 'bal-01',
    tenantId: '22222222-2222-2222-2222-222222222222',
    employmentId: '44444444-4444-4444-4444-444444444444',
    leaveTypeId: 'type-annual',
    leaveTypeCode: 'ANNUAL',
    leaveTypeNameEn: 'Annual Leave',
    leaveTypeNameAr: 'الإجازة السنوية',
    year: 2026,
    entitledDays: 21,
    accruedDays: 14,
    usedDays: 5,
    pendingDays: 2,
    availableDays: 14,
    rowVersion: 1
  },
  {
    id: 'bal-02',
    tenantId: '22222222-2222-2222-2222-222222222222',
    employmentId: '44444444-4444-4444-4444-444444444444',
    leaveTypeId: 'type-sick',
    leaveTypeCode: 'SICK',
    leaveTypeNameEn: 'Sick Leave',
    leaveTypeNameAr: 'إجازة مرضية',
    year: 2026,
    entitledDays: 30,
    accruedDays: 30,
    usedDays: 0,
    pendingDays: 0,
    availableDays: 30,
    rowVersion: 1
  }
];

const initialRequests: LeaveRequestDto[] = [
  {
    id: 'req-01',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    employmentId: '44444444-4444-4444-4444-444444444444',
    leaveTypeId: 'type-annual',
    leaveTypeCode: 'ANNUAL',
    leaveTypeNameEn: 'Annual Leave',
    leaveTypeNameAr: 'الإجازة السنوية',
    startDate: '2026-09-01',
    endDate: '2026-09-05',
    durationDays: 5,
    durationMinutes: 2400,
    status: LeaveRequestStatus.PendingApproval,
    reason: 'Annual family travel',
    attachmentDocumentId: null,
    approvalRequestId: null,
    rejectionReason: null,
    createdAt: '2026-08-24T09:00:00Z',
    rowVersion: 1
  }
];

export function LeaveComponent() {
  const [balances, setBalances] = useState<LeaveBalanceDto[]>(initialBalances);
  const [requests, setRequests] = useState<LeaveRequestDto[]>(initialRequests);
  const [isRequestModalOpen, setIsRequestModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState<'requests' | 'calendar'>('requests');

  const handleSubmitLeaveRequest = async (
    leaveTypeId: string,
    startDate: string,
    endDate: string,
    durationDays: number,
    reason: string
  ) => {
    // Check for local overlap before mock submission
    const hasOverlap = requests.some(
      (r) =>
        r.status !== LeaveRequestStatus.Rejected &&
        r.status !== LeaveRequestStatus.Cancelled &&
        ((startDate >= r.startDate && startDate <= r.endDate) ||
          (endDate >= r.startDate && endDate <= r.endDate) ||
          (startDate <= r.startDate && endDate >= r.endDate))
    );

    if (hasOverlap) {
      throw new Error(
        '409 Conflict: Overlapping leave request detected. PostgreSQL exclusion constraint rejected this active range.'
      );
    }

    const type = initialLeaveTypes.find((t) => t.id === leaveTypeId);
    const newReq: LeaveRequestDto = {
      id: `req-${Date.now()}`,
      tenantId: '22222222-2222-2222-2222-222222222222',
      legalEntityId: '33333333-3333-3333-3333-333333333333',
      employmentId: '44444444-4444-4444-4444-444444444444',
      leaveTypeId,
      leaveTypeCode: type?.code || 'LEAVE',
      leaveTypeNameEn: type?.nameEn || 'Leave',
      leaveTypeNameAr: type?.nameAr || 'إجازة',
      startDate,
      endDate,
      durationDays,
      durationMinutes: durationDays * 480,
      status: LeaveRequestStatus.PendingApproval,
      reason,
      attachmentDocumentId: null,
      approvalRequestId: null,
      rejectionReason: null,
      createdAt: new Date().toISOString(),
      rowVersion: 1
    };

    setRequests((prev) => [newReq, ...prev]);

    // Reserve pending days on balance
    setBalances((prev) =>
      prev.map((b) =>
        b.leaveTypeId === leaveTypeId
          ? {
              ...b,
              pendingDays: Number(b.pendingDays || 0) + Number(durationDays || 0),
              availableDays: Number(b.availableDays || 0) - Number(durationDays || 0),
              rowVersion: Number(b.rowVersion || 0) + 1
            }
          : b
      )
    );
  };

  const handleApproveRequest = (request: LeaveRequestDto) => {
    setRequests((prev) =>
      prev.map((r) =>
        r.id === request.id
          ? { ...r, status: LeaveRequestStatus.Approved, statusName: 'Approved', rowVersion: Number(r.rowVersion || 0) + 1 }
          : r
      )
    );
  };

  const handleRejectRequest = (request: LeaveRequestDto) => {
    setRequests((prev) =>
      prev.map((r) =>
        r.id === request.id
          ? { ...r, status: LeaveRequestStatus.Rejected, statusName: 'Rejected', rowVersion: Number(r.rowVersion || 0) + 1 }
          : r
      )
    );

    // Release pending reservation
    setBalances((prev) =>
      prev.map((b) =>
        b.leaveTypeId === request.leaveTypeId
          ? {
              ...b,
              pendingDays: Math.max(0, Number(b.pendingDays || 0) - Number(request.durationDays || 0)),
              availableDays: Number(b.availableDays || 0) + Number(request.durationDays || 0),
              rowVersion: Number(b.rowVersion || 0) + 1
            }
          : b
      )
    );
  };

  return (
    <div className="space-y-6" data-testid="leave-route-page">
      <Suspense fallback={<div className="p-8 text-sm text-text-muted">Loading leave module...</div>}>
        <LeaveBalancesSummary
          balances={balances}
          onRequestLeave={() => setIsRequestModalOpen(true)}
        />

        {/* View Switcher */}
        <div className="flex items-center gap-2 border-b border-border-primary pb-2">
          <button
            type="button"
            onClick={() => setActiveTab('requests')}
            className={`px-4 py-2 text-xs font-bold rounded-lg transition-colors ${
              activeTab === 'requests'
                ? 'bg-brand-primary text-white shadow-sm'
                : 'text-text-secondary hover:bg-surface-secondary'
            }`}
          >
            Requests List
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('calendar')}
            className={`px-4 py-2 text-xs font-bold rounded-lg transition-colors ${
              activeTab === 'calendar'
                ? 'bg-brand-primary text-white shadow-sm'
                : 'text-text-secondary hover:bg-surface-secondary'
            }`}
          >
            Team Calendar
          </button>
        </div>

        {activeTab === 'requests' ? (
          <LeaveRequestsGrid
            requests={requests}
            onRequestLeave={() => setIsRequestModalOpen(true)}
            onApproveRequest={handleApproveRequest}
            onRejectRequest={handleRejectRequest}
            onRefresh={() => {}}
          />
        ) : (
          <LeaveCalendar requests={requests} />
        )}

        <LeaveRequestModal
          isOpen={isRequestModalOpen}
          employmentId="44444444-4444-4444-4444-444444444444"
          leaveTypes={initialLeaveTypes}
          balances={balances}
          onClose={() => setIsRequestModalOpen(false)}
          onSubmitRequest={handleSubmitLeaveRequest}
        />
      </Suspense>
    </div>
  );
}

export const leaveRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/leave',
  component: LeaveComponent
});
