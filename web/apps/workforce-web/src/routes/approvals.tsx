import React, { useState, Suspense, lazy } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import {
  ApprovalItemDto,
  ApprovalStatus
} from '@zainx/contracts';

// Lazy load approvals components with strict route-level chunk isolation
const ApprovalInbox = lazy(() =>
  import('@zainx/approvals').then((m) => ({ default: m.ApprovalInbox }))
);
const ApprovalDecisionDialog = lazy(() =>
  import('@zainx/approvals').then((m) => ({ default: m.ApprovalDecisionDialog }))
);

const initialApprovals: ApprovalItemDto[] = [
  {
    id: 'app-01',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    domain: 'leave',
    sourceEntityId: 'req-01',
    sourceEntityType: 'LeaveRequest',
    title: 'Annual Leave Request (5 Days)',
    summary: 'Tariq Al-Mansoor requested 5 days annual leave from 2026-09-01 to 2026-09-05.',
    requesterUserId: 'usr-01',
    subjectEmploymentId: '44444444-4444-4444-4444-444444444444',
    subjectEmployeeNameEn: 'Tariq Al-Mansoor',
    currentStepOrder: 1,
    totalSteps: 2,
    status: ApprovalStatus.Pending,
    statusName: 'Pending',
    createdAtUtc: '2026-08-24T09:00:00Z',
    rowVersion: 1,
    assignedApproverId: 'mgr-current',
    stepOrder: 1
  },
  {
    id: 'app-02',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    domain: 'attendance',
    sourceEntityId: 'adj-01',
    sourceEntityType: 'AttendanceAdjustment',
    title: 'Attendance Regularisation (+60 mins)',
    summary: 'Sara Al-Otaibi requested punch correction due to terminal biometric timeout.',
    requesterUserId: 'usr-02',
    subjectEmploymentId: '55555555-5555-5555-5555-555555555555',
    subjectEmployeeNameEn: 'Sara Al-Otaibi',
    currentStepOrder: 1,
    totalSteps: 1,
    status: ApprovalStatus.Pending,
    statusName: 'Pending',
    createdAtUtc: '2026-08-24T08:30:00Z',
    rowVersion: 1,
    assignedApproverId: 'mgr-current',
    stepOrder: 1
  }
];

export function ApprovalsComponent() {
  const [items, setItems] = useState<ApprovalItemDto[]>(initialApprovals);
  const [selectedItemForDecision, setSelectedItemForDecision] = useState<ApprovalItemDto | null>(null);
  const [selectedAction, setSelectedAction] = useState<'approve' | 'reject' | null>(null);

  const handleSelectDecision = (item: ApprovalItemDto, action: 'approve' | 'reject') => {
    setSelectedItemForDecision(item);
    setSelectedAction(action);
  };

  const handleConfirmDecision = async (
    requestId: string,
    action: 'approve' | 'reject',
    comments: string,
    rowVersion: number
  ) => {
    setItems((prev) =>
      prev.map((i) => {
        if (i.id === requestId) {
          if (i.rowVersion !== rowVersion) {
            throw new Error('Concurrency conflict: Approval item was modified by another approver.');
          }
          const isFinalStep = i.currentStepOrder >= i.totalSteps;
          return {
            ...i,
            status:
              action === 'approve'
                ? isFinalStep
                  ? ApprovalStatus.Approved
                  : ApprovalStatus.Pending
                : ApprovalStatus.Rejected,
            statusName:
              action === 'approve'
                ? isFinalStep
                  ? 'Approved'
                  : 'Pending Next Step'
                : 'Rejected',
            currentStepOrder:
              action === 'approve' && !isFinalStep
                ? i.currentStepOrder + 1
                : i.currentStepOrder,
            rowVersion: i.rowVersion + 1
          };
        }
        return i;
      }).filter((i) => i.status === ApprovalStatus.Pending)
    );
  };

  const handleBulkApprove = (selectedItems: ApprovalItemDto[]) => {
    const selectedIds = selectedItems.map((i) => i.id);
    setItems((prev) => prev.filter((i) => !selectedIds.includes(i.id)));
  };

  return (
    <div className="space-y-6" data-testid="approvals-route-page">
      <Suspense fallback={<div className="p-8 text-sm text-text-muted">Loading approvals module...</div>}>
        <ApprovalInbox
          items={items}
          onSelectDecision={handleSelectDecision}
          onBulkApprove={handleBulkApprove}
          onRefresh={() => {}}
        />

        <ApprovalDecisionDialog
          isOpen={!!selectedItemForDecision}
          item={selectedItemForDecision}
          action={selectedAction}
          onClose={() => {
            setSelectedItemForDecision(null);
            setSelectedAction(null);
          }}
          onConfirmDecision={handleConfirmDecision}
        />
      </Suspense>
    </div>
  );
}

export const approvalsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/approvals',
  component: ApprovalsComponent
});
