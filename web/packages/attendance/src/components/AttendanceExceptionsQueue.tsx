import React, { useState } from 'react';
import {
  Badge,
  Button,
  EmptyState,
  NoResults,
  Skeleton
} from '@zainx/design-system';
import {
  AttendanceExceptionDto,
  AttendanceExceptionType,
  AttendanceExceptionStatus
} from '@zainx/contracts';

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
  onResolveException
}) => {
  const [selectedException, setSelectedException] = useState<AttendanceExceptionDto | null>(null);
  const [resolutionNotes, setResolutionNotes] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [filterType, setFilterType] = useState<string>('all');

  if (!isOpen) return null;

  const getExceptionTypeBadge = (type: number, typeName: string) => {
    switch (type) {
      case AttendanceExceptionType.MissingClockIn:
      case AttendanceExceptionType.MissingClockOut:
        return <Badge variant="warning" label={typeName || 'Missing Punch'} />;
      case AttendanceExceptionType.UnexpectedAbsence:
        return <Badge variant="danger" label={typeName || 'Absence'} />;
      case AttendanceExceptionType.LateArrival:
      case AttendanceExceptionType.EarlyDeparture:
        return <Badge variant="secondary" label={typeName || 'Time Anomaly'} />;
      default:
        return <Badge variant="default" label={typeName || 'Exception'} />;
    }
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
      <div className="w-full max-w-xl h-full bg-surface-primary shadow-2xl flex flex-col border-l border-border-primary">
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
          <Button variant="ghost" size="sm" onClick={onClose} ariaLabel="Close exceptions drawer">
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
                  ? 'bg-brand-primary text-white shadow-sm'
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
                ? 'Late In'
                : val === '3'
                ? 'Early Out'
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
              message="No outstanding attendance exceptions found for the active period."
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
                        {item.employeeNameEn || 'Employee'}
                      </span>
                      <p className="text-xs text-text-muted mt-0.5">{item.details}</p>
                    </div>
                    {getExceptionTypeBadge(item.type, item.typeName)}
                  </div>
                  <div className="mt-3 flex items-center justify-between text-xs text-text-secondary font-mono">
                    <span>{new Date(item.createdAtUtc).toLocaleDateString()}</span>
                    <span className="capitalize">{item.statusName || 'Pending'}</span>
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
              Resolve Exception: {selectedException.typeName}
            </h3>
            <div>
              <label htmlFor="resolution-notes" className="block text-xs font-medium text-text-secondary mb-1">
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
                ariaLabel="Waive exception without manual clock adjustment"
              >
                Waive Exception
              </Button>
              <Button
                variant="primary"
                size="sm"
                disabled={isSubmitting || !resolutionNotes.trim()}
                onClick={() => handleResolve(false)}
                ariaLabel="Resolve and apply clock regularisation"
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
