import React from 'react';
import type { Meta, StoryObj } from '@storybook/react';
import { EmployeeDirectory } from '../components/EmployeeDirectory/EmployeeDirectory';
import { EmployeeWorkspace } from '../components/EmployeeProfile/EmployeeWorkspace';
import { EmployeeSummaryDto, EmployeeProfileDto, DocumentSummaryDto } from '@zainx/contracts';

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
  },
  {
    id: '22222222-2222-2222-2222-222222222222',
    employeeNumber: 'EMP-1002',
    firstNameEn: 'Sara',
    lastNameEn: 'Al-Otaibi',
    firstNameAr: 'سارة',
    lastNameAr: 'العتيبي',
    fullNameEn: 'Sara Al-Otaibi',
    fullNameAr: 'سارة العتيبي',
    primaryEmail: 'sara@zainx.com',
    phoneNumber: '+966500000002',
    departmentNameEn: 'Engineering',
    departmentNameAr: 'الهندسة والتقنية',
    jobTitleEn: 'Lead Software Engineer',
    jobTitleAr: 'مهندس برمجيات أول',
    locationNameEn: 'Riyadh HQ',
    status: 'Active',
    hireDate: '2024-03-01',
    maskedNationalId: '108*******',
    rowVersion: 1
  },
  {
    id: '33333333-3333-3333-3333-333333333333',
    employeeNumber: 'EMP-1003',
    firstNameEn: 'Faisal',
    lastNameEn: 'Al-Ghamdi',
    firstNameAr: 'فيصل',
    lastNameAr: 'الغامدي',
    fullNameEn: 'Faisal Al-Ghamdi',
    fullNameAr: 'فيصل الغامدي',
    primaryEmail: 'faisal@zainx.com',
    phoneNumber: '+966500000003',
    departmentNameEn: 'Finance & Accounts',
    departmentNameAr: 'المالية والحسابات',
    jobTitleEn: 'Senior Financial Analyst',
    jobTitleAr: 'محلل مالي أول',
    locationNameEn: 'Jeddah Regional Office',
    status: 'Inactive',
    hireDate: '2023-08-10',
    maskedNationalId: '107*******',
    rowVersion: 2
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
  probationEndDate: '2024-04-15',
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

const sampleDocs: DocumentSummaryDto[] = [
  {
    id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    ownerType: 'Employee',
    ownerId: '11111111-1111-1111-1111-111111111111',
    documentTypeId: 'a1111111-1111-1111-1111-111111111111',
    documentTypeCode: 'NATIONAL_ID',
    documentTypeNameEn: 'National ID / Iqama',
    documentTypeNameAr: 'الهوية الوطنية / الإقامة',
    title: 'Tariq National ID 2026',
    status: 'Active',
    expiryDate: '2030-01-01',
    createdAt: '2024-01-15T10:00:00Z',
    latestVersionNumber: 1,
    latestFileName: 'national_id.pdf',
    latestFileSize: 204800,
    latestContentType: 'application/pdf'
  },
  {
    id: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
    tenantId: '22222222-2222-2222-2222-222222222222',
    legalEntityId: '33333333-3333-3333-3333-333333333333',
    ownerType: 'Employee',
    ownerId: '11111111-1111-1111-1111-111111111111',
    documentTypeId: 'a3333333-3333-3333-3333-333333333333',
    documentTypeCode: 'CONTRACT',
    documentTypeNameEn: 'Employment Contract',
    documentTypeNameAr: 'عقد العمل',
    title: 'Standard Executive Employment Agreement',
    status: 'Active',
    createdAt: '2024-01-15T10:30:00Z',
    latestVersionNumber: 1,
    latestFileName: 'employment_contract_signed.pdf',
    latestFileSize: 512000,
    latestContentType: 'application/pdf'
  }
];

const meta: Meta = {
  title: 'People / Employee Workspace',
  parameters: {
    layout: 'fullscreen'
  }
};

export default meta;

export const DirectoryOperational: StoryObj = {
  render: () => (
    <EmployeeDirectory
      employees={sampleEmployees}
      onRevealSensitive={async (_id, _field) => '1098765432'}
      onCreateEmployee={() => alert('Open Create Employee Modal')}
      onSelectEmployee={(emp) => alert(`Selected Employee: ${emp.fullNameEn}`)}
    />
  )
};

export const ProfileWorkspace: StoryObj = {
  render: () => (
    <EmployeeWorkspace
      profile={sampleProfile}
      documents={sampleDocs}
      onRevealSensitive={async (_field) => '1098765432'}
      onChangeAssignment={() => alert('Open Change Assignment Modal')}
      onUploadDocument={() => alert('Open Upload Document Modal')}
      onDownloadDocument={(id) => alert(`Downloading Document: ${id}`)}
    />
  )
};
