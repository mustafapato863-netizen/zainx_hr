import React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { EmployeeWorkspace } from '../components/EmployeeProfile/EmployeeWorkspace';
import { EmployeeProfileDto, DocumentSummaryDto } from '@zainx/contracts';

const mockProfile: EmployeeProfileDto = {
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
  assignmentHistory: [
    {
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
    }
  ]
};

const mockDocs: DocumentSummaryDto[] = [
  {
    id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    ownerType: 'Employee',
    ownerId: '11111111-1111-1111-1111-111111111111',
    documentTypeId: 'a1111111-1111-1111-1111-111111111111',
    documentTypeCode: 'NATIONAL_ID',
    documentTypeNameEn: 'National ID / Iqama',
    documentTypeNameAr: 'الهوية الوطنية',
    title: 'Tariq National ID 2026',
    status: 'Active',
    expiryDate: '2030-01-01',
    createdAt: '2024-01-15T10:00:00Z',
    latestVersionNumber: 1,
    latestFileName: 'national_id.pdf',
    latestFileSize: 204800,
    latestContentType: 'application/pdf'
  }
];

describe('EmployeeWorkspace Component', () => {
  it('renders employee identity and navigation tabs', () => {
    render(<EmployeeWorkspace profile={mockProfile} documents={mockDocs} />);
    expect(screen.getByRole('heading', { level: 1 })).toBeDefined();
    expect(screen.getByText(/EMP-1001/i)).toBeDefined();
    expect(screen.getByRole('tab', { name: /Overview/i })).toBeDefined();
    expect(screen.getByRole('tab', { name: /Employment & Assignment/i })).toBeDefined();
    expect(screen.getByRole('tab', { name: /Documents/i })).toBeDefined();
  });

  it('triggers sensitive reveal request on clicking reveal button', async () => {
    const handleReveal = vi.fn().mockResolvedValue('1098765432');
    render(<EmployeeWorkspace profile={mockProfile} documents={mockDocs} onRevealSensitive={handleReveal} />);
    
    const revealBtns = screen.getAllByRole('button', { name: /Request reveal/i });
    expect(revealBtns.length).toBeGreaterThan(0);
    fireEvent.click(revealBtns[1]); // Click national ID reveal
    
    expect(handleReveal).toHaveBeenCalledWith('nationalId');
  });

  it('switches to employment and assignment history tab', () => {
    render(<EmployeeWorkspace profile={mockProfile} documents={mockDocs} />);
    const empTab = screen.getByRole('tab', { name: /Employment & Assignment/i });
    fireEvent.click(empTab);

    expect(screen.getByText(/Current Employment Terms/i)).toBeDefined();
    expect(screen.getByText(/Assignment History Timeline/i)).toBeDefined();
  });

  it('renders attached documents in the documents tab', () => {
    render(<EmployeeWorkspace profile={mockProfile} documents={mockDocs} />);
    const docsTab = screen.getByRole('tab', { name: /Documents/i });
    fireEvent.click(docsTab);

    expect(screen.getByText(/Tariq National ID 2026/i)).toBeDefined();
    expect(screen.getByRole('button', { name: /Download/i })).toBeDefined();
  });
});
