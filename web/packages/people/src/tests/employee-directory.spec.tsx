import React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { EmployeeDirectory } from '../components/EmployeeDirectory/EmployeeDirectory';
import { EmployeeSummaryDto } from '@zainx/contracts';

const mockEmployees: EmployeeSummaryDto[] = [
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
  }
];

describe('EmployeeDirectory Component', () => {
  it('renders directory title and employee counts accurately', () => {
    render(<EmployeeDirectory employees={mockEmployees} />);
    expect(screen.getByText(/Employee Directory/i)).toBeDefined();
    expect(screen.getByText(/2 Total/i)).toBeDefined();
  });

  it('renders the add employee action button', () => {
    const handleCreate = vi.fn();
    render(<EmployeeDirectory employees={mockEmployees} onCreateEmployee={handleCreate} />);
    const addBtn = screen.getByRole('button', { name: /\+ Add Employee/i });
    expect(addBtn).toBeDefined();
    fireEvent.click(addBtn);
    expect(handleCreate).toHaveBeenCalledTimes(1);
  });

  it('filters employees by department selector', () => {
    render(<EmployeeDirectory employees={mockEmployees} />);
    const deptSelect = screen.getByRole('combobox', { name: /Filter by Department/i });
    fireEvent.change(deptSelect, { target: { value: 'Engineering' } });
    expect(screen.getByText(/1 Total/i)).toBeDefined();
  });

  it('shows empty state when employee list is empty', () => {
    render(<EmployeeDirectory employees={[]} />);
    expect(screen.getByText(/No Employees Found/i)).toBeDefined();
  });

  it('shows error state on failure and triggers retry', () => {
    const handleRefresh = vi.fn();
    render(<EmployeeDirectory isError={true} onRefresh={handleRefresh} />);
    expect(screen.getByText(/Failed to Load Employees/i)).toBeDefined();
    const retryBtn = screen.getByRole('button', { name: /Try Again/i });
    fireEvent.click(retryBtn);
    expect(handleRefresh).toHaveBeenCalledTimes(1);
  });
});
