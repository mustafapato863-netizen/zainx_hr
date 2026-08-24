import React, { useState, useMemo } from 'react';
import {
  PageHeader,
  ZainXDataGrid,
  ZainXColumnDef,
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
  ICellRendererParams
} from '@zainx/design-system';
import { LeaveRequestDto, LeaveRequestStatus } from '@zainx/contracts';

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
  onCancelRequest
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
    actions: true
  });

  const getStatusBadge = (status: number, statusName: string) => {
    switch (status) {
      case LeaveRequestStatus.Approved:
        return <Badge variant="success" label={statusName || 'Approved'} />;
      case LeaveRequestStatus.PendingApproval:
        return <Badge variant="warning" label={statusName || 'Pending'} />;
      case LeaveRequestStatus.Rejected:
        return <Badge variant="danger" label={statusName || 'Rejected'} />;
      case LeaveRequestStatus.Cancelled:
        return <Badge variant="secondary" label={statusName || 'Cancelled'} />;
      default:
        return <Badge variant="default" label={statusName || 'Draft'} />;
    }
  };

  const columnDefs: ZainXColumnDef[] = useMemo(() => [
    {
      field: 'employeeNameEn',
      headerName: 'Employee',
      minWidth: 180,
      flex: 1,
      hide: !visibleColumns.employeeNameEn,
      cellRenderer: (params: ICellRendererParams<LeaveRequestDto>) => (
        <div className="flex flex-col py-1">
          <span className="text-sm font-semibold text-text-primary">
            {params.data?.employeeNameEn || 'Employee'}
          </span>
          <span className="text-xs text-text-muted">
            {params.data?.employeeNumber || 'EMP-XXXX'}
          </span>
        </div>
      )
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
      )
    },
    {
      field: 'startDate',
      headerName: 'Start Date',
      width: 120,
      hide: !visibleColumns.startDate,
      cellRenderer: (params: ICellRendererParams<LeaveRequestDto>) => (
        <span className="text-sm font-mono text-text-primary">
          {params.data?.startDate}
        </span>
      )
    },
    {
      field: 'endDate',
      headerName: 'End Date',
      width: 120,
      hide: !visibleColumns.endDate,
      cellRenderer: (params: ICellRendererParams<LeaveRequestDto>) => (
        <span className="text-sm font-mono text-text-primary">
          {params.data?.endDate}
        </span>
      )
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
      )
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
      )
    },
    {
      field: 'status',
      headerName: 'Status',
      width: 140,
      hide: !visibleColumns.status,
      cellRenderer: (params: ICellRendererParams<LeaveRequestDto>) => {
        const d = params.data;
        if (!d) return null;
        return getStatusBadge(d.status, d.statusName);
      }
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
        if (d.status === LeaveRequestStatus.PendingApproval) {
          return (
            <div className="flex items-center gap-1.5 py-1">
              <Button
                variant="outline"
                size="sm"
                onClick={() => onApproveRequest?.(d)}
                ariaLabel={`Approve leave for ${d.employeeNameEn}`}
              >
                Approve
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => onRejectRequest?.(d)}
                ariaLabel={`Reject leave for ${d.employeeNameEn}`}
              >
                Reject
              </Button>
            </div>
          );
        }
        return (
          <span className="text-xs text-text-muted">
            {d.statusName || 'Finalized'}
          </span>
        );
      }
    }
  ], [visibleColumns, onApproveRequest, onRejectRequest]);

  const filteredRequests = useMemo(() => {
    return requests.filter((r) => {
      const matchesSearch =
        !searchTerm ||
        r.employeeNameEn?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        r.leaveTypeNameEn?.toLowerCase().includes(searchTerm.toLowerCase());
      const matchesStatus =
        !statusFilter || r.status.toString() === statusFilter || r.statusName === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [requests, searchTerm, statusFilter]);

  const filterItems: FilterItem[] = [
    {
      id: 'search',
      type: 'search',
      label: 'Search',
      placeholder: 'Search employee or leave type...',
      value: searchTerm,
      onChange: (val) => setSearchTerm(val as string)
    },
    {
      id: 'status',
      type: 'select',
      label: 'Status',
      placeholder: 'All Statuses',
      value: statusFilter,
      options: [
        { label: 'All Statuses', value: '' },
        { label: 'Pending Approval', value: LeaveRequestStatus.PendingApproval.toString() },
        { label: 'Approved', value: LeaveRequestStatus.Approved.toString() },
        { label: 'Rejected', value: LeaveRequestStatus.Rejected.toString() },
        { label: 'Cancelled', value: LeaveRequestStatus.Cancelled.toString() }
      ],
      onChange: (val) => setStatusFilter(val as string)
    }
  ];

  const columnItems: ColumnItem[] = Object.keys(visibleColumns).map((key) => ({
    id: key,
    label: key.charAt(0).toUpperCase() + key.slice(1).replace(/([A-Z])/g, ' $1'),
    visible: visibleColumns[key]
  }));

  const handleColumnToggle = (columnId: string, visible: boolean) => {
    setVisibleColumns((prev) => ({ ...prev, [columnId]: visible }));
  };

  return (
    <div className="flex flex-col gap-4 p-6 bg-surface-primary rounded-xl border border-border-primary shadow-sm" data-testid="leave-requests-grid">
      <PageHeader
        title="Leave Requests"
        subtitle="Employee time off requests, approvals, and exclusion-enforced reservations"
        actions={
          <div className="flex items-center gap-3">
            <Button
              variant="outline"
              onClick={onRefresh}
              ariaLabel="Refresh leave requests"
            >
              Refresh
            </Button>
            <Button
              variant="primary"
              onClick={onRequestLeave}
              ariaLabel="Submit leave request"
            >
              + Submit Request
            </Button>
          </div>
        }
      />

      <div className="flex flex-wrap items-center justify-between gap-3 bg-surface-secondary/50 p-3 rounded-lg border border-border-secondary">
        <FilterBar
          filters={filterItems}
          onReset={() => {
            setSearchTerm('');
            setStatusFilter('');
          }}
        />
        <div className="flex items-center gap-2">
          <DensitySwitcher currentDensity={density} onDensityChange={setDensity} />
          <ColumnChooser columns={columnItems} onColumnToggle={handleColumnToggle} />
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
          message="An error occurred while communicating with the leave engine."
          onRetry={onRefresh}
        />
      ) : filteredRequests.length === 0 ? (
        requests.length === 0 ? (
          <EmptyState
            title="No Leave Requests Found"
            message="Submitted leave requests will appear here."
          />
        ) : (
          <NoResults
            searchTerm={searchTerm}
            onClearSearch={() => {
              setSearchTerm('');
              setStatusFilter('');
            }}
          />
        )
      ) : (
        <div className="w-full overflow-hidden rounded-lg border border-border-secondary">
          <ZainXDataGrid
            columnDefs={columnDefs}
            rowData={filteredRequests}
            density={density}
          />
        </div>
      )}
    </div>
  );
};
