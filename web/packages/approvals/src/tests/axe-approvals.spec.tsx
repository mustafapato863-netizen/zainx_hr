import React from 'react';
import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { axe } from 'vitest-axe';
import * as matchers from 'vitest-axe/matchers';
import { ApprovalInbox } from '../components/ApprovalInbox';
import { ApprovalDecisionDialog } from '../components/ApprovalDecisionDialog';
import { ApprovalItemDto, ApprovalStatus } from '@zainx/contracts';

expect.extend(matchers);

const sampleApprovals: ApprovalItemDto[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    domain: 'leave',
    sourceEntityId: '44444444-4444-4444-4444-444444444444',
    sourceEntityType: 'LeaveRequest',
    title: 'Annual Leave: 5 Days',
    summary: 'Tariq Al-Mansoor requested 5 days from 2026-09-01 to 2026-09-05',
    requesterUserId: '55555555-5555-5555-5555-555555555555',
    subjectEmploymentId: '66666666-6666-6666-6666-666666666666',
    subjectEmployeeNameEn: 'Tariq Al-Mansoor',
    currentStepOrder: 1,
    totalSteps: 2,
    status: 1,
    statusName: 'Pending',
    createdAtUtc: '2026-08-24T10:00:00Z',
    rowVersion: 1,
    assignedApproverId: '77777777-7777-7777-7777-777777777777',
    stepOrder: 1
  }
];

describe('Phase 3 Approvals Accessibility Verification (Axe WCAG AA)', () => {
  it('ApprovalInbox passes axe accessibility check with 0 violations', async () => {
    const { container } = render(<ApprovalInbox items={sampleApprovals} />);
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });

  it('ApprovalDecisionDialog passes axe accessibility check', async () => {
    const { container } = render(
      <ApprovalDecisionDialog
        isOpen={true}
        action="approve"
        item={sampleApprovals[0]}
        onClose={() => {}}
      />
    );
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });
});
