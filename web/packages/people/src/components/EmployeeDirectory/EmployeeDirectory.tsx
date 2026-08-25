import React, { useState, useMemo } from 'react';
import {
  PageHeader,
  FilterBar,
  FilterItem,
  DensitySwitcher,
  DensityType,
  ColumnChooser,
  ColumnItem,
  BulkActionBar,
  Button,
  Badge,
  SensitiveValue,
  EmptyState,
  NoResults,
  ErrorState,
  Skeleton
} from '@zainx/design-system';
import { ZainXDataGrid, type ZainXColumnDef, type ICellRendererParams } from '@zainx/design-system/components/ZainXDataGrid/ZainXDataGrid';
import { EmployeeSummaryDto } from '@zainx/contracts';

export interface EmployeeDirectoryProps {
  employees?: EmployeeSummaryDto[];
  isLoading?: boolean;
  isError?: boolean;
  onRefresh?: () => void;
  onSelectEmployee?: (employee: EmployeeSummaryDto) => void;
  onCreateEmployee?: () => void;
  onRevealSensitive?: (employeeId: string, fieldName: string) => Promise<string | null>;
}

export const EmployeeDirectory: React.FC<EmployeeDirectoryProps> = ({
  employees = [],
  isLoading = false,
  isError = false,
  onRefresh,
  onSelectEmployee,
  onCreateEmployee,
  onRevealSensitive
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [departmentFilter, setDepartmentFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [density, setDensity] = useState<DensityType>('standard');
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [revealedValues, setRevealedValues] = useState<Record<string, string>>({});
  const [visibleColumns, setVisibleColumns] = useState<Record<string, boolean>>({
    employeeNumber: true,
    fullNameEn: true,
    departmentNameEn: true,
    jobTitleEn: true,
    locationNameEn: true,
    hireDate: true,
    maskedNationalId: true,
    status: true,
    actions: true
  });

  // Client-side filtering in Community-Safe mode
  const filteredEmployees = useMemo(() => {
    return employees.filter(emp => {
      const matchSearch =
        !searchTerm ||
        (emp.fullNameEn && emp.fullNameEn.toLowerCase().includes(searchTerm.toLowerCase())) ||
        (emp.fullNameAr && emp.fullNameAr.includes(searchTerm)) ||
        (emp.employeeNumber && emp.employeeNumber.toLowerCase().includes(searchTerm.toLowerCase())) ||
        (emp.primaryEmail && emp.primaryEmail.toLowerCase().includes(searchTerm.toLowerCase())) ||
        (emp.jobTitleEn && emp.jobTitleEn.toLowerCase().includes(searchTerm.toLowerCase()));

      const matchDept =
        !departmentFilter || emp.departmentNameEn === departmentFilter || emp.departmentNameAr === departmentFilter;

      const matchStatus = !statusFilter || (emp.status && emp.status.toLowerCase() === statusFilter.toLowerCase());

      return matchSearch && matchDept && matchStatus;
    });
  }, [employees, searchTerm, departmentFilter, statusFilter]);

  const departments = useMemo(() => {
    return Array.from(new Set(employees.map(e => e.departmentNameEn).filter(Boolean))) as string[];
  }, [employees]);

  const handleReveal = async (id: string, fieldName: string) => {
    if (onRevealSensitive) {
      const plain = await onRevealSensitive(id, fieldName);
      if (plain) {
        setRevealedValues(prev => ({ ...prev, [id]: plain }));
      }
    }
  };

  // AG Grid Column Definitions (Community-Safe)
  const columnDefs = useMemo<ZainXColumnDef<EmployeeSummaryDto>[]>(() => {
    const allDefs: ZainXColumnDef<EmployeeSummaryDto>[] = [
      {
        field: 'employeeNumber',
        headerName: 'Employee ID / الرقم الوظيفي',
        pinned: 'left',
        width: 150,
        checkboxSelection: true,
        headerCheckboxSelection: true,
        hide: !visibleColumns.employeeNumber,
        cellRenderer: (params: ICellRendererParams<EmployeeSummaryDto>) => (
          <span style={{ fontFamily: 'monospace', fontWeight: 600, color: 'var(--zainx-color-primary, #6366f1)' }}>
            {params.value}
          </span>
        )
      },
      {
        field: 'fullNameEn',
        headerName: 'Name / الاسم',
        pinned: 'left',
        width: 240,
        hide: !visibleColumns.fullNameEn,
        cellRenderer: (params: ICellRendererParams<EmployeeSummaryDto>) => {
          const emp = params.data;
          if (!emp) return null;
          return (
            <div style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
              <span style={{ fontWeight: 600, fontSize: '0.875rem' }}>{emp.fullNameEn}</span>
              <span style={{ fontSize: '0.75rem', color: 'var(--zainx-color-text-muted, #94a3b8)' }}>{emp.fullNameAr}</span>
            </div>
          );
        }
      },
      {
        field: 'departmentNameEn',
        headerName: 'Department / الإدارة',
        width: 180,
        hide: !visibleColumns.departmentNameEn,
        cellRenderer: (params: ICellRendererParams<EmployeeSummaryDto>) => (
          <span>{params.value || 'Unassigned'}</span>
        )
      },
      {
        field: 'jobTitleEn',
        headerName: 'Job Title / المسمى الوظيفي',
        width: 200,
        hide: !visibleColumns.jobTitleEn,
        cellRenderer: (params: ICellRendererParams<EmployeeSummaryDto>) => (
          <span>{params.value || 'N/A'}</span>
        )
      },
      {
        field: 'locationNameEn',
        headerName: 'Location / الموقع',
        width: 140,
        hide: !visibleColumns.locationNameEn,
        cellRenderer: (params: ICellRendererParams<EmployeeSummaryDto>) => (
          <span>{params.value || 'HQ'}</span>
        )
      },
      {
        field: 'hireDate',
        headerName: 'Hire Date / تاريخ التعيين',
        width: 140,
        hide: !visibleColumns.hireDate
      },
      {
        field: 'maskedNationalId',
        headerName: 'National ID / الهوية',
        width: 180,
        hide: !visibleColumns.maskedNationalId,
        cellRenderer: (params: ICellRendererParams<EmployeeSummaryDto>) => {
          const emp = params.data;
          if (!emp) return null;
          const isRevealed = emp.id ? !!revealedValues[emp.id] : false;
          const displayVal = emp.id && isRevealed ? revealedValues[emp.id] : emp.maskedNationalId;

          return (
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <SensitiveValue
                value={displayVal}
                state={isRevealed ? 'revealed' : 'masked'}
                onRevealRequest={() => emp.id && handleReveal(emp.id, 'nationalId')}
                onMask={() => {
                  if (!emp.id) return;
                  setRevealedValues(prev => {
                    const next = { ...prev };
                    delete next[emp.id!];
                    return next;
                  });
                }}
              />
            </div>
          );
        }
      },
      {
        field: 'status',
        headerName: 'Status / الحالة',
        width: 130,
        hide: !visibleColumns.status,
        cellRenderer: (params: ICellRendererParams<EmployeeSummaryDto>) => {
          const status = (params.value || 'active').toLowerCase();
          const variant = status === 'active' ? 'success' : status === 'inactive' ? 'warning' : 'danger';
          return <Badge variant={variant}>{params.value}</Badge>;
        }
      },
      {
        field: 'id' as keyof EmployeeSummaryDto,
        headerName: 'Actions / إجراءات',
        width: 130,
        pinned: 'right',
        sortable: false,
        filter: false,
        hide: !visibleColumns.actions,
        cellRenderer: (params: ICellRendererParams<EmployeeSummaryDto>) => {
          const emp = params.data;
          if (!emp) return null;
          return (
            <Button
              size="xs"
              variant="secondary"
              onClick={(e) => {
                e.stopPropagation();
                if (onSelectEmployee) onSelectEmployee(emp);
              }}
            >
              Profile / ملف
            </Button>
          );
        }
      }
    ];

    return allDefs;
  }, [revealedValues, visibleColumns, onSelectEmployee]);

  const activeFilters = useMemo<FilterItem[]>(() => {
    const items: FilterItem[] = [];
    if (departmentFilter) items.push({ id: 'dept', label: 'Department', value: departmentFilter });
    if (statusFilter) items.push({ id: 'status', label: 'Status', value: statusFilter });
    return items;
  }, [departmentFilter, statusFilter]);

  const handleRemoveFilter = (id: string) => {
    if (id === 'dept') setDepartmentFilter('');
    if (id === 'status') setStatusFilter('');
  };

  const handleClearAllFilters = () => {
    setSearchTerm('');
    setDepartmentFilter('');
    setStatusFilter('');
  };

  const columnItems = useMemo<ColumnItem[]>(() => [
    { id: 'employeeNumber', label: 'Employee ID', visible: visibleColumns.employeeNumber },
    { id: 'fullNameEn', label: 'Name', visible: visibleColumns.fullNameEn },
    { id: 'departmentNameEn', label: 'Department', visible: visibleColumns.departmentNameEn },
    { id: 'jobTitleEn', label: 'Job Title', visible: visibleColumns.jobTitleEn },
    { id: 'locationNameEn', label: 'Location', visible: visibleColumns.locationNameEn },
    { id: 'hireDate', label: 'Hire Date', visible: visibleColumns.hireDate },
    { id: 'maskedNationalId', label: 'National ID', visible: visibleColumns.maskedNationalId },
    { id: 'status', label: 'Status', visible: visibleColumns.status }
  ], [visibleColumns]);

  const handleToggleColumn = (id: string, visible: boolean) => {
    setVisibleColumns(prev => ({ ...prev, [id]: visible }));
  };

  if (isError) {
    return (
      <div style={{ padding: '1.5rem' }}>
        <ErrorState
          title="Failed to Load Employees / تعذر تحميل بيانات الموظفين"
          description="A network or server error occurred while retrieving the employee directory."
          onRetry={onRefresh}
        />
      </div>
    );
  }

  return (
    <div className="zainx-employee-directory flex flex-col gap-4 p-4 sm:p-6">
      {/* Page Header */}
      <PageHeader
        title="Employee Directory / دليل الموظفين"
        subtitle="Authoritative master record of all active and historical employees across the organization."
        badge={<Badge variant="neutral">{filteredEmployees.length} Total / إجمالي</Badge>}
        actions={
          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
            <Button variant="primary" onClick={onCreateEmployee} data-testid="open-create-employee-modal-btn">
              + Add Employee / إضافة موظف
            </Button>
          </div>
        }
      />

      {/* Toolbar & Filters */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex w-full flex-wrap items-center gap-3 lg:w-auto lg:flex-1">
          <select
            value={departmentFilter}
            onChange={(e) => setDepartmentFilter(e.target.value)}
            aria-label="Filter by Department"
            className="w-full sm:w-auto"
            style={{
              padding: '0.5rem 0.875rem',
              borderRadius: 'var(--zainx-radius-md, 6px)',
              border: '1px solid var(--zainx-color-border, #cbd5e1)',
              background: 'var(--zainx-color-surface, #ffffff)',
              color: 'var(--zainx-color-text, #0f172a)',
              fontSize: '0.875rem'
            }}
          >
            <option value="">All Departments / كافة الإدارات</option>
            {departments.map((d) => (
              <option key={d} value={d}>
                {d}
              </option>
            ))}
          </select>

          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            aria-label="Filter by Status"
            className="w-full sm:w-auto"
            style={{
              padding: '0.5rem 0.875rem',
              borderRadius: 'var(--zainx-radius-md, 6px)',
              border: '1px solid var(--zainx-color-border, #cbd5e1)',
              background: 'var(--zainx-color-surface, #ffffff)',
              color: 'var(--zainx-color-text, #0f172a)',
              fontSize: '0.875rem'
            }}
          >
            <option value="">All Statuses / كافة الحالات</option>
            <option value="active">Active / نشط</option>
            <option value="inactive">Inactive / غير نشط</option>
            <option value="terminated">Terminated / منتهي الخدمة</option>
          </select>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <DensitySwitcher density={density} onChange={setDensity} />
          <ColumnChooser
            columns={columnItems}
            onToggleColumn={handleToggleColumn}
            onReset={() => setVisibleColumns({
              employeeNumber: true,
              fullNameEn: true,
              departmentNameEn: true,
              jobTitleEn: true,
              locationNameEn: true,
              hireDate: true,
              maskedNationalId: true,
              status: true,
              actions: true
            })}
          />
        </div>
      </div>

      {/* Filter Bar with Search */}
      <FilterBar
        searchValue={searchTerm}
        onSearchChange={setSearchTerm}
        filters={activeFilters}
        onRemoveFilter={handleRemoveFilter}
        onClearAll={handleClearAllFilters}
      />

      {/* Bulk Action Bar */}
      <BulkActionBar
        selectedCount={selectedIds.length}
        onClearSelection={() => setSelectedIds([])}
        actions={
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <Button size="xs" variant="secondary">Export Selected / تصدير المحدد</Button>
            <Button size="xs" variant="secondary">Bulk Notify / إشعار جماعي</Button>
          </div>
        }
      />

      {/* Content Area */}
      {isLoading ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          <Skeleton height="40px" width="100%" />
          <Skeleton height="350px" width="100%" />
        </div>
      ) : employees.length === 0 ? (
        <EmptyState
          title="No Employees Found / لا يوجد موظفين"
          description="Get started by adding your first employee record to the organization."
          action={<Button variant="primary" onClick={onCreateEmployee}>Add Employee / إضافة موظف</Button>}
        />
      ) : filteredEmployees.length === 0 ? (
        <NoResults
          onClearFilters={handleClearAllFilters}
        />
      ) : (
        <div className="min-h-[480px] w-full overflow-x-auto">
          <div className="h-full min-w-[1100px]">
            <ZainXDataGrid<EmployeeSummaryDto>
              rowData={filteredEmployees}
              columnDefs={columnDefs}
              density={density}
              gridOptions={{
                pagination: true,
                paginationPageSize: 20,
                rowSelection: 'multiple',
                onRowClicked: (event) => {
                  if (event.data && onSelectEmployee) {
                    onSelectEmployee(event.data);
                  }
                },
                onSelectionChanged: (event) => {
                  const selected = event.api.getSelectedRows();
                  const ids = selected.map((r: EmployeeSummaryDto) => r.id).filter(Boolean) as string[];
                  setSelectedIds(ids);
                }
              }}
            />
          </div>
        </div>
      )}
    </div>
  );
};
