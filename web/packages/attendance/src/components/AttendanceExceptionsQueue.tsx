import React, { useState } from 'react';
import { Badge } from '@zainx/design-system/components/Badge/Badge';
import { Button } from '@zainx/design-system/components/Button/Button';
import { EmptyState, NoResults } from '@zainx/design-system/components/EmptyState/EmptyState';
import { Skeleton } from '@zainx/design-system/components/Skeleton/Skeleton';
import { AttendanceExceptionDto } from '@zainx/contracts';

export const AttendanceExceptionType = {
  MissingClockIn: 1,
  MissingClockOut: 2,
  UnexpectedAbsence: 3,
  LateArrival: 4,
  EarlyDeparture: 5,
} as const;

export interface AttendanceExceptionsQueueProps {
  exceptions?: AttendanceExceptionDto[];
  isLoading?: boolean;
  isOpen: boolean;
  onClose: () => void;
  onResolveException?: (exceptionId: string, notes: string, waive: boolean) => Promise<void>;
}

export const AttendanceExceptionsQueue: React.FC<AttendanceExceptionsQueueProps> = ({
  exceptions = [],
  isLoading = false,
  isOpen,
  onClose,
  onResolveException,
}) => {
  const [selectedException, setSelectedException] = useState<AttendanceExceptionDto | null>(null);
  const [resolutionNotes, setResolutionNotes] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [filterType, setFilterType] = useState<string>('all');

  if (!isOpen) return null;

  const getExceptionTypeBadge = (type: any, typeName?: string) => {
    const s = String(type);
    if (s === '1' || s === '2' || s === 'MissingClockIn' || s === 'MissingClockOut') {
      return <Badge variant="warning">{typeName || 'Missing Punch'}</Badge>;
    }
    if (s === '3' || s === 'UnexpectedAbsence') {
      return <Badge variant="danger">{typeName || 'Absence'}</Badge>;
    }
    return <Badge variant="neutral">{typeName || s || 'Time Anomaly'}</Badge>;
  };

  const filteredExceptions = exceptions.filter((e) => {
    if (filterType === 'all') return true;
    return e.type.toString() === filterType;
  });

  const handleResolve = async (waive: boolean) => {
    if (!selectedException || !resolutionNotes.trim()) return;
    try {
      setIsSubmitting(true);
      await onResolveException?.(selectedException.id, resolutionNotes, waive);
      setSelectedException(null);
      setResolutionNotes('');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex justify-end bg-black/40 backdrop-blur-sm transition-opacity"
      data-testid="attendance-exceptions-drawer"
      role="dialog"
      aria-modal="true"
      aria-labelledby="exceptions-drawer-title"
    >
      <div className="w-full max-w-xl h-full bg-surface-primary shadow-overlay flex flex-col border-l border-border-primary">
        {/* Drawer Header */}
        <div className="flex items-center justify-between p-6 border-b border-border-primary bg-surface-secondary/40">
          <div>
            <h2 id="exceptions-drawer-title" className="text-lg font-bold text-text-primary">
              Attendance Exceptions Queue
            </h2>
            <p className="text-xs text-text-muted mt-0.5">
              Review and resolve punch anomalies, missing clocks, and unplanned absences
            </p>
          </div>
          <Button variant="ghost" size="sm" onClick={onClose} aria-label="Close exceptions drawer">
            ✕
          </Button>
        </div>

        {/* Filters */}
        <div className="flex items-center gap-2 p-4 border-b border-border-secondary bg-surface-secondary/20">
          <span className="text-xs font-semibold text-text-secondary">Filter:</span>
          {['all', '0', '1', '2', '3', '4'].map((val) => (
            <button
              key={val}
              type="button"
              onClick={() => setFilterType(val)}
              className={`text-xs px-2.5 py-1 rounded-md font-medium transition-colors ${
                filterType === val
                  ? 'bg-brand-primary text-text-inverse shadow-sm'
                  : 'bg-surface-secondary text-text-secondary hover:bg-surface-tertiary'
              }`}
            >
              {val === 'all'
                ? 'All'
                : val === '0'
                  ? 'Missing In'
                  : val === '1'
                    ? 'Missing Out'
                    : val === '2'
                      ? 'Late'
                      : val === '3'
                        ? 'Early'
                        : 'Absence'}
            </button>
          ))}
        </div>

        {/* Exceptions List */}
        <div className="flex-1 overflow-y-auto p-4 space-y-3">
          {isLoading ? (
            <div className="space-y-3">
              <Skeleton className="h-20 w-full rounded-lg" />
              <Skeleton className="h-20 w-full rounded-lg" />
              <Skeleton className="h-20 w-full rounded-lg" />
            </div>
          ) : filteredExceptions.length === 0 ? (
            <EmptyState
              title="Exceptions Queue Clear"
              description="No outstanding attendance exceptions found for the active period."
            />
          ) : (
            filteredExceptions.map((item) => {
              const isSelected = selectedException?.id === item.id;
              return (
                <div
                  key={item.id}
                  onClick={() => setSelectedException(item)}
                  className={`p-4 rounded-lg border cursor-pointer transition-all ${
                    isSelected
                      ? 'border-brand-primary bg-brand-primary/5 shadow-sm'
                      : 'border-border-secondary bg-surface-secondary/40 hover:border-border-primary'
                  }`}
                  data-testid={`exception-card-${item.id}`}
                >
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <span className="text-sm font-semibold text-text-primary">
                        {(item as any).employeeNameEn || 'Employee'}
                      </span>
                      <p className="text-xs text-text-muted mt-0.5">{item.details}</p>
                    </div>
                    {getExceptionTypeBadge(
                      (item as any).type || (item as any).exceptionType,
                      (item as any).typeName,
                    )}
                  </div>
                  <div className="mt-3 flex items-center justify-between text-xs text-text-secondary font-mono">
                    <span>{new Date(item.createdAtUtc).toLocaleDateString()}</span>
                    <span className="capitalize">
                      {(item as any).statusName || item.status || 'Pending'}
                    </span>
                  </div>
                </div>
              );
            })
          )}
        </div>

        {/* Resolution Action Area */}
        {selectedException && (
          <div className="p-6 border-t border-border-primary bg-surface-secondary/40 space-y-4">
            <h3 className="text-sm font-semibold text-text-primary">
              Resolve Exception: {(selectedException as any).typeName || 'Attendance Exception'}
            </h3>
            <div>
              <label
                htmlFor="resolution-notes"
                className="block text-xs font-medium text-text-secondary mb-1"
              >
                Resolution Notes (Required)
              </label>
              <textarea
                id="resolution-notes"
                value={resolutionNotes}
                onChange={(e) => setResolutionNotes(e.target.value)}
                placeholder="Explain the supervisor verification, punch regularisation, or approved reason..."
                rows={3}
                className="w-full text-sm rounded-lg border border-border-secondary bg-surface-primary p-2.5 text-text-primary focus:border-brand-primary focus:outline-none"
              />
            </div>
            <div className="flex items-center justify-end gap-3">
              <Button
                variant="outline"
                size="sm"
                disabled={isSubmitting || !resolutionNotes.trim()}
                onClick={() => handleResolve(true)}
                aria-label="Waive exception without manual clock adjustment"
              >
                Waive Exception
              </Button>
              <Button
                variant="primary"
                size="sm"
                disabled={isSubmitting || !resolutionNotes.trim()}
                onClick={() => handleResolve(false)}
                aria-label="Resolve and apply clock regularisation"
              >
                {isSubmitting ? 'Resolving...' : 'Confirm Resolution'}
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
