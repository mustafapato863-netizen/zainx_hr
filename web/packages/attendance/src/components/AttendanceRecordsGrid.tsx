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
import { AttendanceDayDto } from '@zainx/contracts';

export const AttendanceStatus = {
  Locked: 'Locked',
  Approved: 'Approved',
  Reviewed: 'Reviewed',
  Unreviewed: 'Unreviewed',
} as const;

export interface AttendanceRecordsGridProps {
  records?: AttendanceDayDto[];
  isLoading?: boolean;
  isError?: boolean;
  onRefresh?: () => void;
  onAdjustRecord?: (record: AttendanceDayDto) => void;
  onApproveRecord?: (record: AttendanceDayDto) => void;
  onBulkApprove?: (ids: string[]) => void;
  onBulkAdjust?: (ids: string[]) => void;
  onOpenExceptions?: () => void;
  onOpenExceptionsQueue?: () => void;
  pendingExceptionsCount?: number;
}

export const AttendanceRecordsGrid: React.FC<AttendanceRecordsGridProps> = ({
  records = [],
  isLoading = false,
  isError = false,
  onRefresh,
  onAdjustRecord,
  onApproveRecord,
  onBulkApprove,
  onBulkAdjust,
  onOpenExceptions,
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

  const getStatusBadge = (status: any, statusName?: string) => {
    const s = String(status);
    if (s === 'Locked' || s === '3') return <Badge variant="neutral">{statusName || 'Locked'}</Badge>;
    if (s === 'Approved' || s === '2') return <Badge variant="success">{statusName || 'Approved'}</Badge>;
    if (s === 'Reviewed' || s === '1') return <Badge variant="primary">{statusName || 'Reviewed'}</Badge>;
    return <Badge variant="warning">{statusName || s || 'Unreviewed'}</Badge>;
  };

  const formatMinutes = (totalMinutes: number) => {
    const hours = Math.floor(totalMinutes / 60);
    const mins = totalMinutes % 60;
    return `${hours}h ${mins}m`;
  };

  const columnDefs: ZainXColumnDef[] = useMemo(() => [
    {
      field: 'employeeNameEn',
      headerName: 'Employee',
      width: 200,
      hide: !visibleColumns.employeeNameEn,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => {
        const data = params.data;
        if (!data) return null;
        return (
          <div className="flex flex-col py-1">
            <span className="font-semibold text-text-primary text-xs leading-tight">
              {(data as any).employeeNameEn || 'Employee'}
            </span>
            <span className="text-[11px] text-text-muted">
              {(data as any).employeeNumber || 'EMP-XXXX'}
            </span>
          </div>
        );
      }
    },
    {
      field: 'businessDate',
      headerName: 'Date',
      width: 120,
      hide: !visibleColumns.businessDate,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <span className="font-mono text-xs text-text-secondary">
          {params.value}
        </span>
      )
    },
    {
      field: 'shiftName',
      headerName: 'Shift Pattern',
      width: 140,
      hide: !visibleColumns.shiftName,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <span className="text-xs text-text-secondary">
          {params.value || 'Standard Shift'}
        </span>
      )
    },
    {
      field: 'firstInUtc',
      headerName: 'First In',
      width: 100,
      hide: !visibleColumns.firstInUtc,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <span className="font-mono text-xs text-text-primary">
          {params.value ? new Date(params.value).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '—'}
        </span>
      )
    },
    {
      field: 'lastOutUtc',
      headerName: 'Last Out',
      width: 100,
      hide: !visibleColumns.lastOutUtc,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <span className="font-mono text-xs text-text-primary">
          {params.value ? new Date(params.value).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '—'}
        </span>
      )
    },
    {
      field: 'totalWorkedMinutes',
      headerName: 'Worked Time',
      width: 130,
      hide: !visibleColumns.totalWorkedMinutes,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <span className="font-mono text-xs font-semibold text-text-primary">
          {formatMinutes(params.value || 0)}
        </span>
      )
    },
    {
      field: 'scheduledMinutes',
      headerName: 'Scheduled',
      width: 110,
      hide: !visibleColumns.scheduledMinutes,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => (
        <span className="font-mono text-xs text-text-muted">
          {formatMinutes(params.value || 0)}
        </span>
      )
    },
    {
      field: 'overtimeMinutes',
      headerName: 'Overtime',
      width: 100,
      hide: !visibleColumns.overtimeMinutes,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => {
        const val = params.value || 0;
        return val > 0 ? (
          <span className="font-mono text-xs font-semibold text-emerald-600 dark:text-emerald-400">
            +{formatMinutes(val)}
          </span>
        ) : (
          <span className="text-text-muted text-xs">—</span>
        );
      }
    },
    {
      field: 'shortfallMinutes',
      headerName: 'Shortfall',
      width: 100,
      hide: !visibleColumns.shortfallMinutes,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => {
        const val = params.value || 0;
        return val > 0 ? (
          <span className="font-mono text-xs font-semibold text-rose-600 dark:text-rose-400">
            -{formatMinutes(val)}
          </span>
        ) : (
          <span className="text-text-muted text-xs">—</span>
        );
      }
    },
    {
      field: 'exceptions',
      headerName: 'Exceptions',
      width: 130,
      hide: !visibleColumns.exceptions,
      cellRenderer: (params: ICellRendererParams<AttendanceDayDto>) => {
        const count = (params.data as any)?.exceptions?.length || 0;
        return count > 0 ? (
          <Badge variant="danger">{`${count} Exception${count > 1 ? 's' : ''}`}</Badge>
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
        return getStatusBadge(data.status, (data as any).statusName);
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
              aria-label={`Adjust attendance for ${(data as any).employeeNameEn || 'employee'}`}
            >
              Adjust
            </Button>
            {data.status !== AttendanceStatus.Approved && !isLocked && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => onApproveRecord?.(data)}
                aria-label={`Approve attendance for ${(data as any).employeeNameEn || 'employee'}`}
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
        (r as any).employeeNameEn?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (r as any).employeeNumber?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        r.businessDate.includes(searchTerm);
      const matchesStatus =
        !statusFilter || r.status.toString() === statusFilter || (r as any).statusName === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [records, searchTerm, statusFilter]);

  const filterItems: FilterItem[] = statusFilter
    ? [{ id: 'status', label: 'Status', value: statusFilter }]
    : [];

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
                aria-label="View exception queue"
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
              aria-label="Refresh attendance records"
            >
              Refresh
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

      {selectedIds.length > 0 && (
        <BulkActionBar
          selectedCount={selectedIds.length}
          onClearSelection={() => setSelectedIds([])}
          actions={
            <Button variant="primary" size="xs" onClick={() => {}}>
              Approve Selected
            </Button>
          }
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
          description="An error occurred while communicating with the attendance engine."
          onRetry={onRefresh}
        />
      ) : filteredRecords.length === 0 ? (
        records.length === 0 ? (
          <EmptyState
            title="No Attendance Records Found"
            description="Clock events and daily records will appear here once processed."
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
          <ZainXDataGrid
            columnDefs={columnDefs}
            rowData={filteredRecords}
            density={density}
          />
        </div>
      )}
    </div>
  );
};
