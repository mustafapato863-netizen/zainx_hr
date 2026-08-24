import React from 'react';
import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { axe } from 'vitest-axe';
import * as matchers from 'vitest-axe/matchers';
import { LeaveBalancesSummary } from '../components/LeaveBalancesSummary';
import { LeaveRequestModal } from '../components/LeaveRequestModal';
import { LeaveRequestsGrid } from '../components/LeaveRequestsGrid';
import { LeaveCalendar } from '../components/LeaveCalendar';
import { LeaveBalanceDto, LeaveRequestDto, LeaveRequestStatus, LeaveTypeDto } from '@zainx/contracts';

expect.extend(matchers);

const sampleBalances: LeaveBalanceDto[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    employmentId: '22222222-2222-2222-2222-222222222222',
    leaveTypeId: '33333333-3333-3333-3333-333333333333',
    leaveTypeNameEn: 'Annual Leave',
    leaveTypeNameAr: 'الإجازة السنوية',
    year: 2026,
    entitledDays: 21,
    accruedDays: 14,
    usedDays: 5,
    pendingDays: 2,
    availableDays: 14,
    rowVersion: 1
  }
];

const sampleLeaveTypes: LeaveTypeDto[] = [
  {
    id: '33333333-3333-3333-3333-333333333333',
    tenantId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    legalEntityId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    code: 'ANNUAL',
    nameEn: 'Annual Leave',
    nameAr: 'الإجازة السنوية',
    category: 0,
    categoryName: 'Annual',
    isPaid: true,
    requiresAttachment: false,
    allowHalfDay: true,
    isActive: true
  }
];

const sampleRequests: LeaveRequestDto[] = [
  {
    id: '44444444-4444-4444-4444-444444444444',
    tenantId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    legalEntityId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    employmentId: '22222222-2222-2222-2222-222222222222',
    employeeNumber: 'EMP-1001',
    employeeNameEn: 'Tariq Al-Mansoor',
    departmentNameEn: 'Human Resources',
    leaveTypeId: '33333333-3333-3333-3333-333333333333',
    leaveTypeNameEn: 'Annual Leave',
    leaveTypeNameAr: 'الإجازة السنوية',
    startDate: '2026-09-01',
    endDate: '2026-09-05',
    durationDays: 5,
    durationMinutes: 2400,
    status: LeaveRequestStatus.PendingApproval,
    statusName: 'PendingApproval',
    reason: 'Family holiday',
    createdAt: '2026-08-24T10:00:00Z',
    rowVersion: 1
  }
];

describe('Phase 3 Leave Accessibility Verification (Axe WCAG AA)', () => {
  it('LeaveBalancesSummary passes axe accessibility check', async () => {
    const { container } = render(<LeaveBalancesSummary balances={sampleBalances} />);
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });

  it('LeaveRequestModal passes axe accessibility check', async () => {
    const { container } = render(
      <LeaveRequestModal
        isOpen={true}
        employmentId="22222222-2222-2222-2222-222222222222"
        leaveTypes={sampleLeaveTypes}
        balances={sampleBalances}
        onClose={() => {}}
      />
    );
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });

  it('LeaveRequestsGrid passes axe accessibility check', async () => {
    const { container } = render(<LeaveRequestsGrid requests={sampleRequests} />);
    const results = await axe(container, {
      rules: {
        'aria-required-children': { enabled: false }
      }
    });
    expect(results.violations).toEqual([]);
  });

  it('LeaveCalendar passes axe accessibility check', async () => {
    const { container } = render(<LeaveCalendar requests={sampleRequests} />);
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });
});
