import React, { useState, useMemo } from 'react';
import {
  PageHeader,
  FilterBar,
  FilterItem,
  DensitySwitcher,
  DensityType,
  ColumnChooser,
  ColumnItem,
  Button,
  Badge,
  EmptyState,
  NoResults,
  ErrorState,
  Skeleton,
} from '@zainx/design-system';
import {
  ZainXDataGrid,
  type ZainXColumnDef,
  type ICellRendererParams,
} from '@zainx/design-system/components/ZainXDataGrid/ZainXDataGrid';
import { LeaveRequestDto } from '@zainx/contracts';

export const LeaveRequestStatus = {
  Draft: 'Draft',
  Submitted: 'Submitted',
  PendingApproval: 'PendingApproval',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
} as const;

export interface LeaveRequestsGridProps {
  requests?: LeaveRequestDto[];
  isLoading?: boolean;
  isError?: boolean;
  onRefresh?: () => void;
  onRequestLeave?: () => void;
  onApproveRequest?: (request: LeaveRequestDto) => void;
  onRejectRequest?: (request: LeaveRequestDto) => void;
  onCancelRequest?: (request: LeaveRequestDto) => void;
}

export const LeaveRequestsGrid: React.FC<LeaveRequestsGridProps> = ({
  requests = [],
  isLoading = false,
  isError = false,
  onRefresh,
  onRequestLeave,
  onApproveRequest,
  onRejectRequest,
  onCancelRequest,
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [density, setDensity] = useState<DensityType>('standard');
  const [visibleColumns, setVisibleColumns] = useState<Record<string, boolean>>({
    employeeNameEn: true,
    leaveTypeName: true,
    startDate: true,
    endDate: true,
    durationDays: true,
    reason: true,
    status: true,
    actions: true,
  });

  const getStatusBadge = (status: any, statusName?: string) => {
    const s = String(status);
    if (s === 'Approved' || s === '3')
      return <Badge variant="success">{statusName || 'Approved'}</Badge>;
    if (s === 'PendingApproval' || s === '1' || s === '2')
      return <Badge variant="warning">{statusName || 'Pending'}</Badge>;
    if (s === 'Rejected' || s === '4')
      return <Badge variant="danger">{statusName || 'Rejected'}</Badge>;
    if (s === 'Cancelled' || s === '5')
      return <Badge variant="neutral">{statusName || 'Cancelled'}</Badge>;
    return <Badge variant="neutral">{statusName || s || 'Draft'}</Badge>;
  };

  const columnDefs: ZainXColumnDef[] = useMemo(
    () => [
      {
        field: 'employeeNameEn',
        headerName: 'Employee',
        minWidth: 180,
        flex: 1,
        hide: !visibleColumns.employeeNameEn,
        cellRenderer: (params: ICellRendererParams<LeaveRequestDto>) => (
          <div className="flex flex-col py-1">
            <span className="text-sm font-semibold text-text-primary">
              {(params.data as any)?.employeeNameEn || 'Employee'}
            </span>
            <span className="text-xs text-text-muted">
              {(params.data as any)?.employeeNumber || 'EMP-XXXX'}
            </span>
          </div>
        ),
      },
      {
        field: 'leaveTypeName',
        headerName: 'Leave Type',
        width: 160,
        hide: !visibleColumns.leaveTypeName,
        cellRenderer: (params: ICellRendererParams<LeaveRequestDto>) => (
          <span className="text-sm font-medium text-text-primary">
            {params.data?.leaveTypeNameEn || 'Annual'}
          </span>
        ),
      },
      {
        field: 'startDate',
        headerName: 'Start Date',
        width: 120,
        hide: !visibleColumns.startDate,
        cellRenderer: (params: ICellRendererParams<LeaveRequestDto>) => (
          <span className="text-sm font-mono text-text-primary">{params.data?.startDate}</span>
        ),
      },
      {
        field: 'endDate',
        headerName: 'End Date',
        width: 120,
        hide: !visibleColumns.endDate,
        cellRenderer: (params: ICellRendererParams<LeaveRequestDto>) => (
          <span className="text-sm font-mono text-text-primary">{params.data?.endDate}</span>
        ),
      },
      {
        field: 'durationDays',
        headerName: 'Duration',
        width: 110,
        hide: !visibleColumns.durationDays,
        cellRenderer: (params: ICellRendererParams<LeaveRequestDto>) => (
          <span className="text-sm font-bold text-brand-primary font-mono">
            {params.data?.durationDays} d
          </span>
        ),
      },
      {
        field: 'reason',
        headerName: 'Reason',
        minWidth: 160,
        hide: !visibleColumns.reason,
        cellRenderer: (params: ICellRendererParams<LeaveRequestDto>) => (
          <span className="text-xs text-text-muted truncate max-w-xs block">
            {params.data?.reason || '—'}
          </span>
        ),
      },
      {
        field: 'status',
        headerName: 'Status',
        width: 140,
        hide: !visibleColumns.status,
        cellRenderer: (params: ICellRendererParams<LeaveRequestDto>) => {
          const d = params.data;
          if (!d) return null;
          return getStatusBadge(d.status, (d as any).statusName);
        },
      },
      {
        field: 'actions',
        headerName: 'Actions',
        width: 160,
        hide: !visibleColumns.actions,
        pinned: 'right',
        cellRenderer: (params: ICellRendererParams<LeaveRequestDto>) => {
          const d = params.data;
          if (!d) return null;
          const s = String(d.status);
          if (s === 'PendingApproval' || s === '1' || s === '2') {
            return (
              <div className="flex items-center gap-1.5 py-1">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => onApproveRequest?.(d)}
                  aria-label={`Approve leave for ${(d as any).employeeNameEn || 'employee'}`}
                >
                  Approve
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onRejectRequest?.(d)}
                  aria-label={`Reject leave for ${(d as any).employeeNameEn || 'employee'}`}
                >
                  Reject
                </Button>
              </div>
            );
          }
          return (
            <span className="text-xs text-text-muted">{(d as any).statusName || 'Finalized'}</span>
          );
        },
      },
    ],
    [visibleColumns, onApproveRequest, onRejectRequest],
  );

  const filteredRequests = useMemo(() => {
    return requests.filter((r) => {
      const matchesSearch =
        !searchTerm ||
        (r as any).employeeNameEn?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        r.leaveTypeNameEn?.toLowerCase().includes(searchTerm.toLowerCase());
      const matchesStatus =
        !statusFilter ||
        r.status.toString() === statusFilter ||
        (r as any).statusName === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [requests, searchTerm, statusFilter]);

  const filterItems: FilterItem[] = statusFilter
    ? [{ id: 'status', label: 'Status', value: statusFilter }]
    : [];

  const columnItems: ColumnItem[] = Object.keys(visibleColumns).map((key) => ({
    id: key,
    label: key.charAt(0).toUpperCase() + key.slice(1).replace(/([A-Z])/g, ' $1'),
    visible: visibleColumns[key],
  }));

  const handleColumnToggle = (columnId: string, visible: boolean) => {
    setVisibleColumns((prev) => ({ ...prev, [columnId]: visible }));
  };

  return (
    <div
      className="flex flex-col gap-4 p-6 bg-surface-primary rounded-xl border border-border-primary shadow-sm"
      data-testid="leave-requests-grid"
    >
      <PageHeader
        title="Leave Requests"
        subtitle="Employee time off requests, approvals, and exclusion-enforced reservations"
        actions={
          <div className="flex items-center gap-3">
            <Button variant="outline" onClick={onRefresh} aria-label="Refresh leave requests">
              Refresh
            </Button>
            <Button variant="primary" onClick={onRequestLeave} aria-label="Submit leave request">
              + Submit Request
            </Button>
          </div>
        }
      />

      <div className="flex flex-wrap items-center justify-between gap-3 bg-surface-secondary/50 p-3 rounded-lg border border-border-secondary">
        <FilterBar
          filters={filterItems}
          searchValue={searchTerm}
          onSearchChange={setSearchTerm}
          onClearAll={() => {
            setSearchTerm('');
            setStatusFilter('');
          }}
        />
        <div className="flex items-center gap-2">
          <DensitySwitcher density={density} onChange={setDensity} />
          <ColumnChooser columns={columnItems} onToggleColumn={handleColumnToggle} />
        </div>
      </div>

      {isLoading ? (
        <div className="p-8 space-y-4" data-testid="leave-requests-skeleton">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
        </div>
      ) : isError ? (
        <ErrorState
          title="Failed to Load Leave Requests"
          description="An error occurred while communicating with the leave engine."
          onRetry={onRefresh}
        />
      ) : filteredRequests.length === 0 ? (
        requests.length === 0 ? (
          <EmptyState
            title="No Leave Requests Found"
            description="Submitted leave requests will appear here."
          />
        ) : (
          <NoResults
            onClearFilters={() => {
              setSearchTerm('');
              setStatusFilter('');
            }}
          />
        )
      ) : (
        <div className="w-full overflow-hidden rounded-lg border border-border-secondary">
          <ZainXDataGrid columnDefs={columnDefs} rowData={filteredRequests} density={density} />
        </div>
      )}
    </div>
  );
};
