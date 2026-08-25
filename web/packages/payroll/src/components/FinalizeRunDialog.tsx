import React from 'react';
import { Button, Money, Icon } from '@zainx/design-system';
import { PayrollRun } from '../types';

interface FinalizeRunDialogProps {
  run: PayrollRun;
  onConfirm: () => void;
  onClose: () => void;
}

export const FinalizeRunDialog: React.FC<FinalizeRunDialogProps> = ({
  run,
  onConfirm,
  onClose,
}) => {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div
        role="dialog"
        aria-labelledby="finalize-dialog-title"
        className="bg-surface rounded-xl shadow-overlay max-w-lg w-full p-6 space-y-5 border border-border-default"
      >
        <div className="flex items-start gap-4">
          <div className="p-3 rounded-full bg-warning-subtle text-warning">
            <Icon name="alert-triangle" className="w-6 h-6" />
          </div>
          <div>
            <h3 id="finalize-dialog-title" className="text-lg font-bold text-text-primary">
              Finalize Payroll Run: {run.code}
            </h3>
            <p className="text-sm text-text-muted mt-1">
              FINALIZATION IS A HARD BOUNDARY. Once finalized, this run is permanently locked and
              immutable.
            </p>
          </div>
        </div>

        <div className="p-4 rounded-xl bg-surface-subtle border border-border-default space-y-2 text-xs">
          <div className="flex justify-between">
            <span className="text-text-muted">Total Net Disbursements:</span>
            <span className="font-bold text-success text-sm">
              <Money amount={run.totalNet} currency={run.currency} />
            </span>
          </div>
          <div className="flex justify-between">
            <span className="text-text-muted">Total Gross Earnings:</span>
            <span className="font-medium text-text-primary">
              <Money amount={run.totalGross} currency={run.currency} />
            </span>
          </div>
          <div className="flex justify-between">
            <span className="text-text-muted">Covered Employees:</span>
            <span className="font-medium text-text-primary">{run.employeeCount}</span>
          </div>
          <div className="flex justify-between border-t border-border-default pt-2 font-mono text-[10px]">
            <span className="text-text-tertiary">Reproducibility Fingerprint:</span>
            <span className="text-text-secondary truncate max-w-[200px]">
              {run.reproducibilityHash || 'PENDING'}
            </span>
          </div>
        </div>

        <div className="p-3 rounded-lg bg-danger-subtle border border-danger border-danger/50 text-xs text-danger">
          <strong>Immutable Rules Notice:</strong> No editing employee results, no recalculating in
          place, no changing deductions manually. Future adjustments require off-cycle runs.
        </div>

        <div className="flex justify-end gap-3 pt-2">
          <Button id="btn-cancel-finalize" variant="secondary" onPress={onClose}>
            Cancel
          </Button>
          <Button
            id="btn-confirm-finalize"
            variant="primary"
            className="bg-success hover:bg-success-hover text-text-inverse"
            onPress={onConfirm}
          >
            Lock & Finalize Run
          </Button>
        </div>
      </div>
    </div>
  );
};
