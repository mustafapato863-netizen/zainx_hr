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
  BulkActionBar,
  Button,
  Badge,
  EmptyState,
  NoResults,
  ErrorState,
  Skeleton,
  ICellRendererParams
} from '@zainx/design-system';
import { AttendanceDayDto, AttendanceStatus } from '@zainx/contracts';

export interface AttendanceRecordsGridProps {
  records?: AttendanceDayDto[];
  isLoading?: boolean;
  isError?: boolean;
  onRefresh?: () => void;
  onSelectRecord?: (record: AttendanceDayDto) => void;
  onAdjustRecord?: (record: AttendanceDayDto) => void;
  onApproveRecord?: (record: AttendanceDayDto) => void;
  onLockRecord?: (record: AttendanceDayDto) => void;
  onOpenExceptionsQueue?: () => void;
  pendingExceptionsCount?: number;
}

export const AttendanceRecordsGrid: React.FC<AttendanceRecordsGridProps> = ({
  records = [],
  isLoading = false,
  isError = false,
  onRefresh,
  onSelectRecord,
  onAdjustRecord,
  onApproveRecord,
  onLockRecord,
  onOpenExceptionsQueue,
  pendingExceptionsCount = 0
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [density, setDensity] = useState<DensityType>('standard');
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [visibleColumns, setVisibleColumns] = useState<Record<string, boolean>>({
    employeeNumber: true,
    fullNameEn: true,
    businessDate: true,
    shift: true,
    firstClockIn: true,
    lastClockOut: true,
    totalWorked: true,
    lateMinutes: true,
    exceptions: true,
    status: true,
    actions: true
  });

  const getStatusBadge = (status: number, statusName: string) => {
    switch (status) {
      case AttendanceStatus.Locked:
        return <Badge variant="secondary" label={statusName || 'Locked'} />;
      case AttendanceStatus.Approved:
        return <Badge variant="success" label={statusName || 'Approved'} />;
      case AttendanceStatus.Reviewed:
        return <Badge variant="primary" label={statusName || 'Reviewed'} />;
      default:
        return <Badge variant="warning" label={statusName || 'Unreviewed'} />;
    }
  };

  const formatMinutes = (totalMinutes: number) => {
    const hours = Math.floor(totalMinutes / 60);
    const mins = totalMinutes % 60;
    return `${hours}h ${mins}m`;
  };

  const formatUtcTime = (isoString?: string | null) => {
    if (!isoString) return '—';
    try {
      const date = new Date(isoString);
      return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false });
    } catch {
      return '—';
    }
  };

  const columnDefs: ZainXColumnDef[] = useMemo(() => [
    {
      field: 'employeeNumber',
      headerName: 'Emp #',
      width: 110,
      hide: !visibleColumns.employeeNumber,
      pinned: 'left',
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <span className="font-mono text-xs font-semibold text-text-primary">
          {params.data?.employeeNumber || '—'}
        </span>
      )
    },
    {
      field: 'fullNameEn',
      headerName: 'Employee Name',
      minWidth: 200,
      flex: 1,
      hide: !visibleColumns.fullNameEn,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <div className="flex flex-col py-1">
          <span className="text-sm font-semibold text-text-primary">
            {params.data?.employeeNameEn || 'Unknown'}
          </span>
          <span className="text-xs text-text-muted">
            {params.data?.departmentNameEn || 'Operations'}
          </span>
        </div>
      )
    },
    {
      field: 'businessDate',
      headerName: 'Date',
      width: 120,
      hide: !visibleColumns.businessDate,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <span className="text-sm text-text-primary">
          {params.data?.businessDate || '—'}
        </span>
      )
    },
    {
      field: 'shift',
      headerName: 'Scheduled Shift',
      width: 140,
      hide: !visibleColumns.shift,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <span className="text-xs font-mono text-text-secondary">
          {formatMinutes(params.data?.scheduledMinutes || 480)}
        </span>
      )
    },
    {
      field: 'firstClockIn',
      headerName: 'Clock In',
      width: 110,
      hide: !visibleColumns.firstClockIn,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <span className="text-xs font-mono font-medium text-emerald-600 dark:text-emerald-400">
          {formatUtcTime(params.data?.firstClockInUtc)}
        </span>
      )
    },
    {
      field: 'lastClockOut',
      headerName: 'Clock Out',
      width: 110,
      hide: !visibleColumns.lastClockOut,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <span className="text-xs font-mono font-medium text-blue-600 dark:text-blue-400">
          {formatUtcTime(params.data?.lastClockOutUtc)}
        </span>
      )
    },
    {
      field: 'totalWorked',
      headerName: 'Total Worked',
      width: 130,
      hide: !visibleColumns.totalWorked,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <span className="text-sm font-semibold text-text-primary">
          {formatMinutes(params.data?.totalWorkedMinutes || 0)}
        </span>
      )
    },
    {
      field: 'lateMinutes',
      headerName: 'Lateness',
      width: 110,
      hide: !visibleColumns.lateMinutes,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => {
        const late = params.data?.lateMinutes || 0;
        return late > 0 ? (
          <span className="text-xs font-semibold text-rose-600 dark:text-rose-400">
            +{late}m
          </span>
        ) : (
          <span className="text-xs text-text-muted">On Time</span>
        );
      }
    },
    {
      field: 'exceptions',
      headerName: 'Exceptions',
      width: 130,
      hide: !visibleColumns.exceptions,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => {
        const count = params.data?.exceptions?.length || 0;
        return count > 0 ? (
          <Badge variant="danger" label={`${count} Exception${count > 1 ? 's' : ''}`} />
        ) : (
          <span className="text-xs text-text-muted">None</span>
        );
      }
    },
    {
      field: 'status',
      headerName: 'Status',
      width: 130,
      hide: !visibleColumns.status,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => {
        const data = params.data;
        if (!data) return null;
        return getStatusBadge(data.status, data.statusName);
      }
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 160,
      hide: !visibleColumns.actions,
      pinned: 'right',
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => {
        const data = params.data;
        if (!data) return null;
        const isLocked = data.status === AttendanceStatus.Locked;
        return (
          <div className="flex items-center gap-1.5 py-1">
            <Button
              variant="outline"
              size="sm"
              disabled={isLocked}
              onClick={() => onAdjustRecord?.(data)}
              ariaLabel={`Adjust attendance for ${data.employeeNameEn}`}
            >
              Adjust
            </Button>
            {data.status !== AttendanceStatus.Approved && !isLocked && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => onApproveRecord?.(data)}
                ariaLabel={`Approve attendance for ${data.employeeNameEn}`}
              >
                Approve
              </Button>
            )}
          </div>
        );
      }
    }
  ], [visibleColumns, onAdjustRecord, onApproveRecord]);

  const filteredRecords = useMemo(() => {
    return records.filter((r) => {
      const matchesSearch =
        !searchTerm ||
        r.employeeNameEn?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        r.employeeNumber?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        r.businessDate.includes(searchTerm);
      const matchesStatus =
        !statusFilter || r.status.toString() === statusFilter || r.statusName === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [records, searchTerm, statusFilter]);

  const filterItems: FilterItem[] = [
    {
      id: 'search',
      type: 'search',
      label: 'Search',
      placeholder: 'Search employee name, number, date...',
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
        { label: 'Unreviewed', value: AttendanceStatus.Unreviewed.toString() },
        { label: 'Reviewed', value: AttendanceStatus.Reviewed.toString() },
        { label: 'Approved', value: AttendanceStatus.Approved.toString() },
        { label: 'Locked', value: AttendanceStatus.Locked.toString() }
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
    <div className="flex flex-col gap-4 p-6 bg-surface-primary rounded-xl border border-border-primary shadow-sm" data-testid="attendance-records-grid">
      <PageHeader
        title="Attendance Daily Review"
        subtitle="Operational time records, shift evaluations, and exception resolution"
        actions={
          <div className="flex items-center gap-3">
            {pendingExceptionsCount > 0 && (
              <Button
                variant="outline"
                onClick={onOpenExceptionsQueue}
                ariaLabel="View exception queue"
              >
                <span className="inline-flex items-center gap-1.5">
                  <span className="w-2 h-2 rounded-full bg-rose-500 animate-pulse" />
                  <span>Exceptions ({pendingExceptionsCount})</span>
                </span>
              </Button>
            )}
            <Button
              variant="outline"
              onClick={onRefresh}
              ariaLabel="Refresh attendance records"
            >
              Refresh
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

      {selectedIds.length > 0 && (
        <BulkActionBar
          selectedCount={selectedIds.length}
          onClearSelection={() => setSelectedIds([])}
          actions={[
            {
              id: 'bulk-approve',
              label: 'Approve Selected',
              variant: 'primary',
              onClick: () => {
                // Bulk approve
              }
            }
          ]}
        />
      )}

      {isLoading ? (
        <div className="p-8 space-y-4" data-testid="attendance-loading-skeleton">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
        </div>
      ) : isError ? (
        <ErrorState
          title="Failed to Load Attendance Records"
          message="An error occurred while communicating with the attendance engine."
          onRetry={onRefresh}
        />
      ) : filteredRecords.length === 0 ? (
        records.length === 0 ? (
          <EmptyState
            title="No Attendance Records Found"
            message="Clock events and daily records will appear here once processed."
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
            rowData={filteredRecords}
            density={density}
            rowSelection="multiple"
            onSelectionChanged={(rows) => setSelectedIds(rows.map((r: AttendanceDayDto) => r.id))}
            onRowClicked={(row) => onSelectRecord?.(row as AttendanceDayDto)}
          />
        </div>
      )}
    </div>
  );
};
