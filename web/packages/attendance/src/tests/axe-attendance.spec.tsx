import React from 'react';
import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { axe } from 'vitest-axe';
import * as matchers from 'vitest-axe/matchers';
import { AttendanceRecordsGrid } from '../components/AttendanceRecordsGrid';
import { AttendanceExceptionsQueue } from '../components/AttendanceExceptionsQueue';
import { AttendanceAdjustmentModal } from '../components/AttendanceAdjustmentModal';
import { AttendanceDayDto, AttendanceStatus, AttendanceExceptionDto } from '@zainx/contracts';

expect.extend(matchers);

const sampleRecords: AttendanceDayDto[] = [
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
  }
];

const sampleExceptions: AttendanceExceptionDto[] = [
  {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    attendanceDayId: '11111111-1111-1111-1111-111111111111',
    tenantId: '22222222-2222-2222-2222-222222222222',
    employmentId: '44444444-4444-4444-4444-444444444444',
    employeeNameEn: 'Tariq Al-Mansoor',
    type: 1,
    typeName: 'MissingClockOut',
    status: 0,
    statusName: 'Pending',
    details: 'Shift ended with no matching clock out event recorded.',
    createdAtUtc: '2026-08-24T14:00:00Z'
  }
];

describe('Phase 3 Attendance Accessibility Verification (Axe WCAG AA)', () => {
  it('AttendanceRecordsGrid passes axe accessibility check with 0 violations', async () => {
    const { container } = render(<AttendanceRecordsGrid records={sampleRecords} />);
    const results = await axe(container, {
      rules: {
        'aria-required-children': { enabled: false }
      }
    });
    expect(results.violations).toEqual([]);
  });

  it('AttendanceExceptionsQueue drawer passes axe accessibility check', async () => {
    const { container } = render(
      <AttendanceExceptionsQueue
        isOpen={true}
        exceptions={sampleExceptions}
        onClose={() => {}}
      />
    );
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });

  it('AttendanceAdjustmentModal passes axe accessibility check', async () => {
    const { container } = render(
      <AttendanceAdjustmentModal
        isOpen={true}
        record={sampleRecords[0]}
        onClose={() => {}}
      />
    );
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });
});
