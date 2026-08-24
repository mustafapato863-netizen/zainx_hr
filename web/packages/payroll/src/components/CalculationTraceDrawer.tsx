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
      className="fixed inset-y-0 right-0 z-50 w-full max-w-2xl bg-white dark:bg-neutral-900 shadow-2xl border-l border-neutral-200 dark:border-neutral-800 flex flex-col"
    >
      {/* Header */}
      <div className="p-5 border-b border-neutral-200 dark:border-neutral-800 flex items-center justify-between">
        <div>
          <h3 id="trace-drawer-title" className="font-semibold text-lg text-neutral-900 dark:text-neutral-100">
            Calculation Explainability Trace
          </h3>
          <span className="text-xs text-neutral-500 font-mono">
            Employment ID: {detail.employmentId}
          </span>
        </div>
        <Button id="btn-close-trace" variant="ghost" className="p-2" onPress={onClose} aria-label="Close Calculation Trace">
          <Icon name="x" className="w-5 h-5" />
        </Button>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-6 space-y-6">
        {/* Result Snapshot Cards */}
        <div className="grid grid-cols-3 gap-3">
          <div className="p-3 rounded-lg bg-neutral-50 dark:bg-neutral-800/50 border border-neutral-200 dark:border-neutral-800">
            <span className="text-xs text-neutral-500 block">Gross Earnings</span>
            <span className="font-bold text-sm text-neutral-900 dark:text-neutral-100 mt-0.5 block">
              <Money amount={detail.grossPay} currency={currency} />
            </span>
          </div>
          <div className="p-3 rounded-lg bg-neutral-50 dark:bg-neutral-800/50 border border-neutral-200 dark:border-neutral-800">
            <span className="text-xs text-neutral-500 block">Total Deductions</span>
            <span className="font-bold text-sm text-red-600 dark:text-red-400 mt-0.5 block">
              <Money amount={detail.totalDeductions} currency={currency} />
            </span>
          </div>
          <div className="p-3 rounded-lg bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800">
            <span className="text-xs text-emerald-600 dark:text-emerald-400 block">Net Payable</span>
            <span className="font-bold text-sm text-emerald-700 dark:text-emerald-300 mt-0.5 block">
              <Money amount={detail.netPay} currency={currency} />
            </span>
          </div>
        </div>

        {/* Itemized Lines */}
        <div className="space-y-3">
          <h4 className="text-xs font-semibold text-neutral-500 uppercase tracking-wider">
            Itemized Component Breakdown
          </h4>
          <div className="rounded-lg border border-neutral-200 dark:border-neutral-800 overflow-hidden divide-y divide-neutral-200 dark:divide-neutral-800 text-xs">
            {detail.lines.map((l) => (
              <div key={l.id} className="p-3 flex items-center justify-between bg-white dark:bg-neutral-900">
                <div>
                  <div className="font-semibold text-neutral-900 dark:text-neutral-100">
                    {l.nameEn} ({l.nameAr})
                  </div>
                  <span className="text-neutral-400 font-mono">
                    Code: {l.componentCode} | Type: {l.calculationType}
                  </span>
                </div>
                <div className="font-bold text-neutral-900 dark:text-neutral-100">
                  <Money amount={l.amount} currency={currency} />
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Step-by-Step Traces */}
        <div className="space-y-3">
          <h4 className="text-xs font-semibold text-neutral-500 uppercase tracking-wider">
            Deterministic Engine Steps
          </h4>
          <div className="space-y-3">
            {detail.traces.map((t) => (
              <div
                key={t.id}
                className="p-4 rounded-xl border border-neutral-200 dark:border-neutral-800 bg-neutral-50 dark:bg-neutral-800/40 space-y-2 text-xs"
              >
                <div className="flex items-center justify-between">
                  <span className="font-semibold text-neutral-900 dark:text-neutral-100">
                    Step {t.stepOrder}: {t.description}
                  </span>
                  <span className="px-2 py-0.5 rounded bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 font-mono text-[10px]">
                    {t.ruleReference}
                  </span>
                </div>

                <div className="bg-white dark:bg-neutral-900 p-2.5 rounded-lg border border-neutral-200 dark:border-neutral-700 font-mono text-[11px] text-neutral-800 dark:text-neutral-200">
                  Formula: {t.formulaApplied}
                </div>

                <div className="flex items-center justify-between text-neutral-500 pt-1">
                  <span>Inputs: <code className="text-neutral-700 dark:text-neutral-300">{t.inputValuesJson}</code></span>
                  <span className="font-bold text-neutral-900 dark:text-neutral-100">
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
