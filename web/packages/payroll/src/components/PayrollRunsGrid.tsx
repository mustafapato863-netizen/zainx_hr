import React, { useState } from 'react';
import { Card, Badge, Button, Money, Input, Dialog, Icon } from '@zainx/design-system';
import { PayrollRun, PayrollPeriod } from '../types';

interface PayrollRunsGridProps {
  runs: PayrollRun[];
  periods: PayrollPeriod[];
  onSelectRun: (runId: string) => void;
  onCreateRun: (periodId: string, code: string, currency: string) => void;
  isLoading?: boolean;
}

export const PayrollRunsGrid: React.FC<PayrollRunsGridProps> = ({
  runs,
  periods,
  onSelectRun,
  onCreateRun,
  isLoading = false,
}) => {
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [selectedPeriodId, setSelectedPeriodId] = useState(periods[0]?.id || '');
  const [runCode, setRunCode] = useState('');
  const [currency, setCurrency] = useState('EGP');

  const getStatusVariant = (status: string) => {
    switch (status) {
      case 'Finalized':
      case 'OutputsPublished':
        return 'success';
      case 'Calculated':
      case 'UnderReview':
      case 'Approved':
        return 'warning';
      default:
        return 'neutral';
    }
  };

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPeriodId || !runCode) return;
    onCreateRun(selectedPeriodId, runCode, currency);
    setIsCreateOpen(false);
    setRunCode('');
  };

  return (
    <div className="space-y-6" data-testid="payroll-runs-container">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-text-primary">Payroll Runs</h2>
          <p className="text-sm text-text-muted">
            Manage period calculations, approvals, and immutable finalization.
          </p>
        </div>
        <Button id="btn-create-payroll-run" variant="primary" onPress={() => setIsCreateOpen(true)}>
          <Icon name="plus" className="w-4 h-4 mr-2" />
          Create Run
        </Button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {runs.map((run) => (
          <Card
            key={run.id}
            id={`payroll-run-card-${run.id}`}
            className="hover:shadow-sm transition-shadow cursor-pointer border border-border-default"
            onClick={() => onSelectRun(run.id)}
          >
            <div className="p-5 space-y-4">
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="font-semibold text-lg text-text-primary">{run.code}</h3>
                  <span className="text-xs text-text-muted font-mono">
                    ID: {run.id.slice(0, 8)}
                  </span>
                </div>
                <Badge variant={getStatusVariant(run.status)}>{run.status}</Badge>
              </div>

              <div className="grid grid-cols-2 gap-4 py-2 border-y border-border-subtle">
                <div>
                  <span className="text-xs text-text-muted block">Total Gross</span>
                  <span className="font-medium text-text-primary">
                    <Money amount={run.totalGross} currency={run.currency} />
                  </span>
                </div>
                <div>
                  <span className="text-xs text-text-muted block">Total Net</span>
                  <span className="font-semibold text-success">
                    <Money amount={run.totalNet} currency={run.currency} />
                  </span>
                </div>
              </div>

              <div className="flex items-center justify-between text-xs text-text-muted">
                <span>{run.employeeCount} Employees</span>
                <span className="font-mono">v{run.rowVersion}</span>
              </div>

              <Button
                id={`btn-open-workspace-${run.id}`}
                variant="secondary"
                className="w-full justify-center"
                onPress={() => onSelectRun(run.id)}
              >
                Open Workspace
              </Button>
            </div>
          </Card>
        ))}

        {runs.length === 0 && !isLoading && (
          <div className="col-span-full p-12 text-center border-2 border-dashed border-border-default rounded-xl">
            <Icon name="table" className="w-12 h-12 mx-auto text-text-tertiary mb-3" />
            <h3 className="text-base font-medium text-text-primary">No Payroll Runs Created</h3>
            <p className="text-sm text-text-muted mt-1">
              Create a payroll period run to start calculating earnings and deductions.
            </p>
          </div>
        )}
      </div>

      {isCreateOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div
            role="dialog"
            aria-labelledby="create-run-dialog-title"
            className="bg-surface rounded-xl shadow-overlay max-w-md w-full p-6 space-y-4 border border-border-default"
          >
            <h3 id="create-run-dialog-title" className="text-lg font-semibold text-text-primary">
              Create Payroll Run
            </h3>
            <form onSubmit={handleCreate} className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-text-secondary mb-1">
                  Payroll Period
                </label>
                <select
                  id="select-payroll-period"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-border-strong bg-surface text-text-primary"
                  value={selectedPeriodId}
                  onChange={(e) => setSelectedPeriodId(e.target.value)}
                  required
                >
                  {periods.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.code} ({p.periodStart} to {p.periodEnd})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-text-secondary mb-1">
                  Run Code
                </label>
                <input
                  id="input-payroll-run-code"
                  type="text"
                  placeholder="e.g. RUN-2026-08-MAIN"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-border-strong bg-surface text-text-primary"
                  value={runCode}
                  onChange={(e) => setRunCode(e.target.value)}
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-text-secondary mb-1">
                  Currency
                </label>
                <select
                  id="select-payroll-currency"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-border-strong bg-surface text-text-primary"
                  value={currency}
                  onChange={(e) => setCurrency(e.target.value)}
                >
                  <option value="EGP">EGP - Egyptian Pound</option>
                  <option value="SAR">SAR - Saudi Riyal</option>
                  <option value="AED">AED - UAE Dirham</option>
                  <option value="USD">USD - US Dollar</option>
                </select>
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <Button
                  id="btn-cancel-create-run"
                  variant="secondary"
                  onPress={() => setIsCreateOpen(false)}
                >
                  Cancel
                </Button>
                <Button id="btn-submit-create-run" variant="primary" type="submit">
                  Create Run
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
