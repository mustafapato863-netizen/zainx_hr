import React, { useState } from 'react';
import { Button } from '@zainx/design-system';
import { AttendanceDayDto } from '@zainx/contracts';

export interface AttendanceAdjustmentModalProps {
  record: AttendanceDayDto | null;
  isOpen: boolean;
  onClose: () => void;
  onSubmitAdjustment?: (dayId: string, adjustedMinutes: number, reason: string, rowVersion: number) => Promise<void>;
}

export const AttendanceAdjustmentModal: React.FC<AttendanceAdjustmentModalProps> = ({
  record,
  isOpen,
  onClose,
  onSubmitAdjustment
}) => {
  const [adjustedHours, setAdjustedHours] = useState<number>(() =>
    record ? Math.floor(record.totalWorkedMinutes / 60) : 8
  );
  const [adjustedMins, setAdjustedMins] = useState<number>(() =>
    record ? record.totalWorkedMinutes % 60 : 0
  );
  const [reason, setReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  if (!isOpen || !record) return null;

  const originalMinutes = record.totalWorkedMinutes;
  const newMinutes = adjustedHours * 60 + adjustedMins;
  const deltaMinutes = newMinutes - originalMinutes;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reason.trim()) {
      setErrorMessage('A detailed reason is mandatory for all attendance time adjustments.');
      return;
    }

    try {
      setIsSubmitting(true);
      setErrorMessage(null);
      await onSubmitAdjustment?.(record.id, newMinutes, reason, record.rowVersion);
      onClose();
    } catch (err: any) {
      setErrorMessage(err.message || 'Failed to apply adjustment. Please verify version concurrency.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="adjust-attendance-title"
      data-testid="attendance-adjustment-modal"
    >
      <div className="w-full max-w-lg rounded-xl bg-surface-primary p-6 shadow-2xl border border-border-primary">
        <div className="flex items-center justify-between border-b border-border-primary pb-4">
          <div>
            <h2 id="adjust-attendance-title" className="text-lg font-bold text-text-primary">
              Adjust Attendance Record
            </h2>
            <p className="text-xs text-text-muted mt-0.5">
              {record.employeeNameEn} ({record.employeeNumber}) — {record.businessDate}
            </p>
          </div>
          <Button variant="ghost" size="sm" onClick={onClose} ariaLabel="Close adjustment modal">
            ✕
          </Button>
        </div>

        {errorMessage && (
          <div className="mt-4 rounded-lg bg-rose-500/10 border border-rose-500/20 p-3 text-xs text-rose-600 dark:text-rose-400">
            {errorMessage}
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-5 space-y-5">
          {/* Audit Explainability Comparison */}
          <div className="grid grid-cols-2 gap-4 rounded-lg bg-surface-secondary/50 p-4 border border-border-secondary">
            <div>
              <span className="text-xs font-semibold text-text-secondary uppercase tracking-wider">
                Original Worked
              </span>
              <p className="text-lg font-bold text-text-primary mt-1 font-mono">
                {Math.floor(originalMinutes / 60)}h {originalMinutes % 60}m
              </p>
              <span className="text-xs text-text-muted">({originalMinutes} minutes)</span>
            </div>
            <div>
              <span className="text-xs font-semibold text-text-secondary uppercase tracking-wider">
                Adjusted Total
              </span>
              <p className="text-lg font-bold text-brand-primary mt-1 font-mono">
                {adjustedHours}h {adjustedMins}m
              </p>
              <span className={`text-xs font-medium ${deltaMinutes >= 0 ? 'text-emerald-600' : 'text-rose-600'}`}>
                {deltaMinutes >= 0 ? `+${deltaMinutes}` : deltaMinutes} mins difference
              </span>
            </div>
          </div>

          {/* Time Input Inputs */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label htmlFor="adjust-hours" className="block text-xs font-medium text-text-secondary mb-1">
                Hours
              </label>
              <input
                id="adjust-hours"
                type="number"
                min={0}
                max={24}
                value={adjustedHours}
                onChange={(e) => setAdjustedHours(Math.max(0, parseInt(e.target.value) || 0))}
                className="w-full rounded-lg border border-border-secondary bg-surface-primary px-3 py-2 text-sm text-text-primary focus:border-brand-primary focus:outline-none"
              />
            </div>
            <div>
              <label htmlFor="adjust-minutes" className="block text-xs font-medium text-text-secondary mb-1">
                Minutes
              </label>
              <input
                id="adjust-minutes"
                type="number"
                min={0}
                max={59}
                value={adjustedMins}
                onChange={(e) => setAdjustedMins(Math.min(59, Math.max(0, parseInt(e.target.value) || 0)))}
                className="w-full rounded-lg border border-border-secondary bg-surface-primary px-3 py-2 text-sm text-text-primary focus:border-brand-primary focus:outline-none"
              />
            </div>
          </div>

          {/* Reason Input */}
          <div>
            <label htmlFor="adjust-reason" className="block text-xs font-medium text-text-secondary mb-1">
              Adjustment Reason & Audit Justification (Required)
            </label>
            <textarea
              id="adjust-reason"
              rows={3}
              required
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Provide clear rationale (e.g. Biometric device failure, offsite mission approved by manager)..."
              className="w-full rounded-lg border border-border-secondary bg-surface-primary p-3 text-sm text-text-primary focus:border-brand-primary focus:outline-none"
            />
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3 pt-2 border-t border-border-primary">
            <Button variant="outline" size="md" onClick={onClose} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button variant="primary" size="md" type="submit" disabled={isSubmitting || !reason.trim()}>
              {isSubmitting ? 'Saving Adjustment...' : 'Apply Adjustment'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
