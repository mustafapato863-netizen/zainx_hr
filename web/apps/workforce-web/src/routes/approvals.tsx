import React, { useState, Suspense, lazy } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import { ApprovalInboxItemDto } from '@zainx/contracts';

// Lazy load approvals components with strict route-level chunk isolation
const ApprovalInbox = lazy(() =>
  import('@zainx/approvals').then((m) => ({ default: m.ApprovalInbox }))
);
const ApprovalDecisionDialog = lazy(() =>
  import('@zainx/approvals').then((m) => ({ default: m.ApprovalDecisionDialog }))
);

const ApprovalStatus = {
  Pending: 'Pending',
  Approved: 'Approved',
  Rejected: 'Rejected'
} as const;

const initialApprovals: ApprovalInboxItemDto[] = [
  {
    id: 'app-01',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    sourceModule: 'leave',
    sourceEntityId: 'req-01',
    workflowType: 'LeaveRequest',
    title: 'Annual Leave Request (5 Days)',
    requesterUserId: 'usr-01',
    requesterEmploymentId: '44444444-4444-4444-4444-444444444444',
    currentStepOrder: 1,
    totalSteps: 2,
    status: ApprovalStatus.Pending,
    createdAt: '2026-08-24T09:00:00Z',
    rowVersion: 1
  },
  {
    id: 'app-02',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    sourceModule: 'attendance',
    sourceEntityId: 'adj-01',
    workflowType: 'AttendanceAdjustment',
    title: 'Attendance Regularisation (+60 mins)',
    requesterUserId: 'usr-02',
    requesterEmploymentId: '55555555-5555-5555-5555-555555555555',
    currentStepOrder: 1,
    totalSteps: 1,
    status: ApprovalStatus.Pending,
    createdAt: '2026-08-24T08:30:00Z',
    rowVersion: 1
  }
];

export function ApprovalsComponent() {
  const [items, setItems] = useState<ApprovalInboxItemDto[]>(initialApprovals);
  const [selectedItemForDecision, setSelectedItemForDecision] = useState<ApprovalInboxItemDto | null>(null);
  const [selectedAction, setSelectedAction] = useState<'approve' | 'reject' | null>(null);

  const handleSelectDecision = (item: ApprovalInboxItemDto, action: 'approve' | 'reject') => {
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
          if (Number(i.rowVersion) !== rowVersion) {
            throw new Error('Concurrency conflict: Approval item was modified by another approver.');
          }
          const currentOrder = Number(i.currentStepOrder);
          const total = Number(i.totalSteps);
          const isFinalStep = currentOrder >= total;
          return {
            ...i,
            status:
              action === 'approve'
                ? isFinalStep
                  ? ApprovalStatus.Approved
                  : ApprovalStatus.Pending
                : ApprovalStatus.Rejected,
            currentStepOrder:
              action === 'approve' && !isFinalStep
                ? currentOrder + 1
                : currentOrder,
            rowVersion: Number(i.rowVersion || 0) + 1
          };
        }
        return i;
      }).filter((i) => i.status === ApprovalStatus.Pending)
    );
  };

  const handleBulkApprove = (selectedItems: ApprovalInboxItemDto[]) => {
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
