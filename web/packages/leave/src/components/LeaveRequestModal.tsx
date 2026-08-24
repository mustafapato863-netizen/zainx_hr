import React, { useState, useMemo } from 'react';
import { Button } from '@zainx/design-system';
import { LeaveTypeDto, LeaveBalanceDto } from '@zainx/contracts';

export interface LeaveRequestModalProps {
  isOpen: boolean;
  employmentId: string;
  leaveTypes?: LeaveTypeDto[];
  balances?: LeaveBalanceDto[];
  onClose: () => void;
  onSubmitRequest?: (
    leaveTypeId: string,
    startDate: string,
    endDate: string,
    durationDays: number,
    reason: string
  ) => Promise<void>;
}

export const LeaveRequestModal: React.FC<LeaveRequestModalProps> = ({
  isOpen,
  employmentId,
  leaveTypes = [],
  balances = [],
  onClose,
  onSubmitRequest
}) => {
  const [selectedTypeId, setSelectedTypeId] = useState<string>(() =>
    leaveTypes.length > 0 ? leaveTypes[0].id : ''
  );
  const [startDate, setStartDate] = useState<string>('');
  const [endDate, setEndDate] = useState<string>('');
  const [reason, setReason] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  if (!isOpen) return null;

  const selectedBalance = balances.find((b) => b.leaveTypeId === selectedTypeId);

  const calculatedDays = useMemo(() => {
    if (!startDate || !endDate) return 0;
    const start = new Date(startDate);
    const end = new Date(endDate);
    if (end < start) return 0;
    const diffTime = Math.abs(end.getTime() - start.getTime());
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
  }, [startDate, endDate]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedTypeId) {
      setErrorMessage('Please select a valid leave type.');
      return;
    }
    if (!startDate || !endDate) {
      setErrorMessage('Start and end dates are required.');
      return;
    }
    if (new Date(endDate) < new Date(startDate)) {
      setErrorMessage('End date cannot precede the start date.');
      return;
    }
    if (calculatedDays <= 0) {
      setErrorMessage('Leave duration must be at least 1 day.');
      return;
    }
    if (selectedBalance && calculatedDays > Number(selectedBalance.availableDays)) {
      setErrorMessage(
        `Insufficient available balance (${selectedBalance.availableDays} days available, ${calculatedDays} days requested).`
      );
      return;
    }

    try {
      setIsSubmitting(true);
      setErrorMessage(null);
      await onSubmitRequest?.(selectedTypeId, startDate, endDate, calculatedDays, reason);
      onClose();
    } catch (err: any) {
      // Handles 409 Overlap Conflict from PostgreSQL Exclusion Constraint
      setErrorMessage(
        err.message ||
          'Overlapping leave request detected or reservation failed. Exclusion constraint rejected this date range.'
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="leave-request-modal-title"
      data-testid="leave-request-modal"
    >
      <div className="w-full max-w-lg rounded-xl bg-surface-primary p-6 shadow-2xl border border-border-primary">
        <div className="flex items-center justify-between border-b border-border-primary pb-4">
          <div>
            <h2 id="leave-request-modal-title" className="text-lg font-bold text-text-primary">
              Submit Leave Request
            </h2>
            <p className="text-xs text-text-muted mt-0.5">
              Dates are validated against PostgreSQL exclusion constraints for non-overlapping integrity
            </p>
          </div>
          <Button variant="ghost" size="sm" onClick={onClose} aria-label="Close leave request modal">
            ✕
          </Button>
        </div>

        {errorMessage && (
          <div
            className="mt-4 rounded-lg bg-rose-500/10 border border-rose-500/20 p-3 text-xs text-rose-600 dark:text-rose-400 font-medium"
            data-testid="leave-request-error"
          >
            {errorMessage}
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-5 space-y-4">
          {/* Leave Type Selector */}
          <div>
            <label htmlFor="leave-type" className="block text-xs font-medium text-text-secondary mb-1">
              Leave Category / Type
            </label>
            <select
              id="leave-type"
              value={selectedTypeId}
              onChange={(e) => setSelectedTypeId(e.target.value)}
              className="w-full rounded-lg border border-border-secondary bg-surface-primary px-3 py-2 text-sm text-text-primary focus:border-brand-primary focus:outline-none"
            >
              {leaveTypes.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.nameEn} ({(t as any).categoryName || t.category || 'Paid'})
                </option>
              ))}
            </select>
          </div>

          {/* Balance Indicator */}
          {selectedBalance && (
            <div className="flex items-center justify-between rounded-lg bg-surface-secondary/60 px-3.5 py-2 border border-border-secondary text-xs">
              <span className="text-text-secondary">Available Balance:</span>
              <span className="font-bold text-brand-primary font-mono text-sm">
                {selectedBalance.availableDays} Days
              </span>
            </div>
          )}

          {/* Date Pickers */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label htmlFor="leave-start-date" className="block text-xs font-medium text-text-secondary mb-1">
                Start Date
              </label>
              <input
                id="leave-start-date"
                type="date"
                required
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full rounded-lg border border-border-secondary bg-surface-primary px-3 py-2 text-sm text-text-primary focus:border-brand-primary focus:outline-none"
              />
            </div>
            <div>
              <label htmlFor="leave-end-date" className="block text-xs font-medium text-text-secondary mb-1">
                End Date
              </label>
              <input
                id="leave-end-date"
                type="date"
                required
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="w-full rounded-lg border border-border-secondary bg-surface-primary px-3 py-2 text-sm text-text-primary focus:border-brand-primary focus:outline-none"
              />
            </div>
          </div>

          {/* Duration Summary */}
          {calculatedDays > 0 && (
            <div className="rounded-lg bg-brand-primary/5 p-3 border border-brand-primary/20 flex items-center justify-between text-xs">
              <span className="text-text-secondary font-medium">Requested Duration:</span>
              <span className="font-bold text-brand-primary font-mono text-sm">
                {calculatedDays} Calendar Day{calculatedDays > 1 ? 's' : ''}
              </span>
            </div>
          )}

          {/* Reason */}
          <div>
            <label htmlFor="leave-reason" className="block text-xs font-medium text-text-secondary mb-1">
              Reason / Remarks (Optional)
            </label>
            <textarea
              id="leave-reason"
              rows={2}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Provide context for manager approval..."
              className="w-full rounded-lg border border-border-secondary bg-surface-primary p-3 text-sm text-text-primary focus:border-brand-primary focus:outline-none"
            />
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3 pt-3 border-t border-border-primary">
            <Button variant="outline" size="md" onClick={onClose} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button
              variant="primary"
              size="md"
              type="submit"
              disabled={isSubmitting || !startDate || !endDate || calculatedDays <= 0}
            >
              {isSubmitting ? 'Reserving Balance...' : 'Submit Request'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
