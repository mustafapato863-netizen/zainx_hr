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
        className="bg-white dark:bg-neutral-900 rounded-2xl shadow-2xl max-w-lg w-full p-6 space-y-5 border border-neutral-200 dark:border-neutral-800"
      >
        <div className="flex items-start gap-4">
          <div className="p-3 rounded-full bg-amber-100 text-amber-600 dark:bg-amber-950/50 dark:text-amber-400">
            <Icon name="alert-triangle" className="w-6 h-6" />
          </div>
          <div>
            <h3 id="finalize-dialog-title" className="text-lg font-bold text-neutral-900 dark:text-neutral-100">
              Finalize Payroll Run: {run.code}
            </h3>
            <p className="text-sm text-neutral-500 mt-1">
              FINALIZATION IS A HARD BOUNDARY. Once finalized, this run is permanently locked and immutable.
            </p>
          </div>
        </div>

        <div className="p-4 rounded-xl bg-neutral-50 dark:bg-neutral-800/50 border border-neutral-200 dark:border-neutral-700 space-y-2 text-xs">
          <div className="flex justify-between">
            <span className="text-neutral-500">Total Net Disbursements:</span>
            <span className="font-bold text-emerald-600 dark:text-emerald-400 text-sm">
              <Money amount={run.totalNet} currency={run.currency} />
            </span>
          </div>
          <div className="flex justify-between">
            <span className="text-neutral-500">Total Gross Earnings:</span>
            <span className="font-medium text-neutral-900 dark:text-neutral-100">
              <Money amount={run.totalGross} currency={run.currency} />
            </span>
          </div>
          <div className="flex justify-between">
            <span className="text-neutral-500">Covered Employees:</span>
            <span className="font-medium text-neutral-900 dark:text-neutral-100">
              {run.employeeCount}
            </span>
          </div>
          <div className="flex justify-between border-t border-neutral-200 dark:border-neutral-700 pt-2 font-mono text-[10px]">
            <span className="text-neutral-400">Reproducibility Fingerprint:</span>
            <span className="text-neutral-700 dark:text-neutral-300 truncate max-w-[200px]">
              {run.reproducibilityHash || 'PENDING'}
            </span>
          </div>
        </div>

        <div className="p-3 rounded-lg bg-red-50 dark:bg-red-950/30 border border-red-200 dark:border-red-800/50 text-xs text-red-700 dark:text-red-300">
          <strong>Immutable Rules Notice:</strong> No editing employee results, no recalculating in place, no changing deductions manually. Future adjustments require off-cycle runs.
        </div>

        <div className="flex justify-end gap-3 pt-2">
          <Button id="btn-cancel-finalize" variant="secondary" onPress={onClose}>
            Cancel
          </Button>
          <Button
            id="btn-confirm-finalize"
            variant="primary"
            className="bg-emerald-600 hover:bg-emerald-700 text-white"
            onPress={onConfirm}
          >
            Lock & Finalize Run
          </Button>
        </div>
      </div>
    </div>
  );
};
