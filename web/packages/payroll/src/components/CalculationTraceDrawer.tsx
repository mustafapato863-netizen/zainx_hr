import React from 'react';
import { Money, Button, Icon } from '@zainx/design-system';
import { PayrollEmployeeResultDetail } from '../types';

interface CalculationTraceDrawerProps {
  detail: PayrollEmployeeResultDetail;
  currency: string;
  onClose: () => void;
}

export const CalculationTraceDrawer: React.FC<CalculationTraceDrawerProps> = ({
  detail,
  currency,
  onClose,
}) => {
  return (
    <div
      role="dialog"
      aria-labelledby="trace-drawer-title"
      className="fixed inset-y-0 right-0 z-50 w-full max-w-2xl bg-surface shadow-overlay border-l border-border-default flex flex-col"
    >
      {/* Header */}
      <div className="p-5 border-b border-border-default flex items-center justify-between">
        <div>
          <h3 id="trace-drawer-title" className="font-semibold text-lg text-text-primary">
            Calculation Explainability Trace
          </h3>
          <span className="text-xs text-text-muted font-mono">
            Employment ID: {detail.employmentId}
          </span>
        </div>
        <Button
          id="btn-close-trace"
          variant="ghost"
          className="p-2"
          onPress={onClose}
          aria-label="Close Calculation Trace"
        >
          <Icon name="x" className="w-5 h-5" />
        </Button>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-6 space-y-6">
        {/* Result Snapshot Cards */}
        <div className="grid grid-cols-3 gap-3">
          <div className="p-3 rounded-lg bg-surface-subtle border border-border-default">
            <span className="text-xs text-text-muted block">Gross Earnings</span>
            <span className="font-bold text-sm text-text-primary mt-0.5 block">
              <Money amount={detail.grossPay} currency={currency} />
            </span>
          </div>
          <div className="p-3 rounded-lg bg-surface-subtle border border-border-default">
            <span className="text-xs text-text-muted block">Total Deductions</span>
            <span className="font-bold text-sm text-danger mt-0.5 block">
              <Money amount={detail.totalDeductions} currency={currency} />
            </span>
          </div>
          <div className="p-3 rounded-lg bg-success-subtle border border-success border-success">
            <span className="text-xs text-success block">Net Payable</span>
            <span className="font-bold text-sm text-success mt-0.5 block">
              <Money amount={detail.netPay} currency={currency} />
            </span>
          </div>
        </div>

        {/* Itemized Lines */}
        <div className="space-y-3">
          <h4 className="text-xs font-semibold text-text-muted uppercase tracking-wider">
            Itemized Component Breakdown
          </h4>
          <div className="rounded-lg border border-border-default overflow-hidden divide-y divide-border-subtle divide-border-subtle text-xs">
            {detail.lines.map((l) => (
              <div key={l.id} className="p-3 flex items-center justify-between bg-surface">
                <div>
                  <div className="font-semibold text-text-primary">
                    {l.nameEn} ({l.nameAr})
                  </div>
                  <span className="text-text-tertiary font-mono">
                    Code: {l.componentCode} | Type: {l.calculationType}
                  </span>
                </div>
                <div className="font-bold text-text-primary">
                  <Money amount={l.amount} currency={currency} />
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Step-by-Step Traces */}
        <div className="space-y-3">
          <h4 className="text-xs font-semibold text-text-muted uppercase tracking-wider">
            Deterministic Engine Steps
          </h4>
          <div className="space-y-3">
            {detail.traces.map((t) => (
              <div
                key={t.id}
                className="p-4 rounded-xl border border-border-default bg-surface-subtle space-y-2 text-xs"
              >
                <div className="flex items-center justify-between">
                  <span className="font-semibold text-text-primary">
                    Step {t.stepOrder}: {t.description}
                  </span>
                  <span className="px-2 py-0.5 rounded bg-info-subtle text-info font-mono text-[10px]">
                    {t.ruleReference}
                  </span>
                </div>

                <div className="bg-surface p-2.5 rounded-lg border border-border-default font-mono text-[11px] text-text-primary">
                  Formula: {t.formulaApplied}
                </div>

                <div className="flex items-center justify-between text-text-muted pt-1">
                  <span>
                    Inputs: <code className="text-text-secondary">{t.inputValuesJson}</code>
                  </span>
                  <span className="font-bold text-text-primary">
                    Result: <Money amount={t.finalAmount} currency={currency} />
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
