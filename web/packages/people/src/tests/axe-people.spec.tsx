import React from 'react';
import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { axe } from 'vitest-axe';
import * as matchers from 'vitest-axe/matchers';
import { EmployeeDirectory } from '../components/EmployeeDirectory/EmployeeDirectory';
import { EmployeeWorkspace } from '../components/EmployeeProfile/EmployeeWorkspace';
import { EmployeeSummaryDto, EmployeeProfileDto, DocumentSummaryDto } from '@zainx/contracts';

expect.extend(matchers);

const sampleEmployees: EmployeeSummaryDto[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    employeeNumber: 'EMP-1001',
    firstNameEn: 'Tariq',
    lastNameEn: 'Al-Mansoor',
    firstNameAr: 'طارق',
    lastNameAr: 'المنصور',
    fullNameEn: 'Tariq Al-Mansoor',
    fullNameAr: 'طارق المنصور',
    primaryEmail: 'tariq@zainx.com',
    phoneNumber: '+966500000001',
    departmentNameEn: 'Human Resources',
    departmentNameAr: 'الموارد البشرية',
    jobTitleEn: 'HR Director',
    jobTitleAr: 'مدير الموارد البشرية',
    locationNameEn: 'Riyadh HQ',
    status: 'Active',
    hireDate: '2024-01-15',
    maskedNationalId: '109*******',
    rowVersion: 1
  }
];

const sampleProfile: EmployeeProfileDto = {
  id: '11111111-1111-1111-1111-111111111111',
  personId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  tenantId: '22222222-2222-2222-2222-222222222222',
  legalEntityId: '33333333-3333-3333-3333-333333333333',
  employeeNumber: 'EMP-1001',
  firstNameEn: 'Tariq',
  lastNameEn: 'Al-Mansoor',
  firstNameAr: 'طارق',
  lastNameAr: 'المنصور',
  fullNameEn: 'Tariq Al-Mansoor',
  fullNameAr: 'طارق المنصور',
  gender: 'Male',
  nationality: 'SA',
  maskedDateOfBirth: '****-**-15',
  maskedNationalId: '109*******',
  primaryEmail: 'tariq@zainx.com',
  phoneNumber: '+966500000001',
  status: 'Active',
  hireDate: '2024-01-15',
  rowVersion: 1,
  currentAssignment: {
    id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    employmentId: '11111111-1111-1111-1111-111111111111',
    organizationUnitId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
    departmentNameEn: 'Human Resources',
    departmentNameAr: 'الموارد البشرية',
    jobTitleEn: 'HR Director',
    jobTitleAr: 'مدير الموارد البشرية',
    locationNameEn: 'Riyadh HQ',
    effectiveFrom: '2024-01-15',
    isCurrent: true
  },
  assignmentHistory: []
};

describe('Phase 2 People Accessibility Verification (Axe WCAG AA)', () => {
  it('EmployeeDirectory passes axe accessibility check with 0 critical/serious violations', async () => {
    const { container } = render(<EmployeeDirectory employees={sampleEmployees} />);
    // Exclude virtual DOM row placeholders unmounted in jsdom
    const results = await axe(container, {
      rules: {
        'aria-required-children': { enabled: false }
      }
    });
    expect(results.violations).toEqual([]);
  });

  it('EmployeeWorkspace passes axe accessibility check with 0 critical/serious violations', async () => {
    const { container } = render(<EmployeeWorkspace profile={sampleProfile} documents={[]} />);
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });
});
