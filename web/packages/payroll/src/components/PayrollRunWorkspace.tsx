import React, { useState } from 'react';
import {
  Card,
  Badge,
  Button,
  Money,
  SensitiveValue,
  Icon,
} from '@zainx/design-system';
import {
  PayrollRun,
  PayrollEmployeeResult,
  PayrollException,
  PayrollEmployeeResultDetail,
} from '../types';
import { PayrollExceptionsQueue } from './PayrollExceptionsQueue';
import { CalculationTraceDrawer } from './CalculationTraceDrawer';
import { FinalizeRunDialog } from './FinalizeRunDialog';

interface PayrollRunWorkspaceProps {
  run: PayrollRun;
  results: PayrollEmployeeResult[];
  exceptions: PayrollException[];
  onLoadInputs: () => void;
  onCalculate: () => void;
  onFinalize: () => void;
  onNavigateSettlement?: () => void;
  onBack: () => void;
  onFetchEmployeeDetail: (empId: string) => Promise<PayrollEmployeeResultDetail | null>;
  onResolveException: (exceptionId: string, note: string) => void;
  onWaiveException: (exceptionId: string, justification: string) => void;
  isCalculating?: boolean;
}

const STEPS = [
  { id: 'Draft', label: '1. Draft' },
  { id: 'InputsLoaded', label: '2. Inputs Loaded' },
  { id: 'Calculated', label: '3. Calculated' },
  { id: 'UnderReview', label: '4. Under Review' },
  { id: 'Approved', label: '5. Approved' },
  { id: 'Finalized', label: '6. Finalized' },
];

export const PayrollRunWorkspace: React.FC<PayrollRunWorkspaceProps> = ({
  run,
  results,
  exceptions,
  onLoadInputs,
  onCalculate,
  onFinalize,
  onNavigateSettlement,
  onBack,
  onFetchEmployeeDetail,
  onResolveException,
  onWaiveException,
  isCalculating = false,
}) => {
  const [isExceptionsOpen, setIsExceptionsOpen] = useState(false);
  const [isFinalizeOpen, setIsFinalizeOpen] = useState(false);
  const [selectedDetail, setSelectedDetail] = useState<PayrollEmployeeResultDetail | null>(null);
  const [isTraceOpen, setIsTraceOpen] = useState(false);
  const [isLoadingTrace, setIsLoadingTrace] = useState(false);

  const openBlockingCount = exceptions.filter(
    (e) => e.severity === 'Blocking' && e.status === 'Open'
  ).length;

  const openWarningCount = exceptions.filter(
    (e) => e.severity === 'Warning' && e.status === 'Open'
  ).length;

  const handleOpenTrace = async (empId: string) => {
    setIsLoadingTrace(true);
    const detail = await onFetchEmployeeDetail(empId);
    setSelectedDetail(detail);
    setIsLoadingTrace(false);
    setIsTraceOpen(true);
  };

  const isFinalized = run.status === 'Finalized' || run.status === 'OutputsPublished';

  return (
    <div className="space-y-6" data-testid="payroll-workspace">
      {/* Header & Back Navigation */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <Button
            id="btn-back-to-runs"
            variant="ghost"
            className="p-2"
            onPress={onBack}
            aria-label="Back to Payroll Runs"
          >
            <Icon name="arrow-left" className="w-5 h-5" />
          </Button>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-xl font-bold text-neutral-900 dark:text-neutral-100">
                {run.code}
              </h2>
              <Badge variant={isFinalized ? 'success' : 'warning'}>
                {run.status}
              </Badge>
              {isFinalized && (
                <span className="text-xs text-neutral-500 font-mono">
                  (Permanent Immutability Locked)
                </span>
              )}
            </div>
            <p className="text-xs text-neutral-500 font-mono">
              Fingerprint: {run.reproducibilityHash || 'Pending Calculation'} | Version: v{run.rowVersion}
            </p>
          </div>
        </div>

        {/* Action Controls */}
        <div className="flex items-center gap-2">
          {!isFinalized && (
            <>
              {run.status === 'Draft' && (
                <Button
                  id="btn-load-inputs"
                  variant="secondary"
                  onPress={onLoadInputs}
                >
                  <Icon name="upload" className="w-4 h-4 mr-1.5" />
                  Load Inputs
                </Button>
              )}

              {(run.status === 'InputsLoaded' || run.status === 'Calculated') && (
                <Button
                  id="btn-calculate-run"
                  variant="primary"
                  onPress={onCalculate}
                  isDisabled={isCalculating}
                >
                  <Icon name="refresh" className={`w-4 h-4 mr-1.5 ${isCalculating ? 'animate-spin' : ''}`} />
                  {isCalculating ? 'Calculating...' : 'Calculate'}
                </Button>
              )}

              {run.status === 'Calculated' && (
                <Button
                  id="btn-open-finalize-dialog"
                  variant="primary"
                  className="bg-emerald-600 hover:bg-emerald-700 text-white"
                  onPress={() => setIsFinalizeOpen(true)}
                  isDisabled={openBlockingCount > 0}
                >
                  <Icon name="lock" className="w-4 h-4 mr-1.5" />
                  Finalize Run
                </Button>
              )}
            </>
          )}

          <Button
            id="btn-toggle-exceptions"
            variant="secondary"
            className="relative"
            onPress={() => setIsExceptionsOpen(true)}
          >
            <Icon name="alert-circle" className="w-4 h-4 mr-1.5" />
            Exceptions
            {(openBlockingCount > 0 || openWarningCount > 0) && (
              <span className="ml-1.5 px-1.5 py-0.5 text-xs rounded-full bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300 font-semibold">
                {openBlockingCount + openWarningCount}
              </span>
            )}
          </Button>

          {isFinalized && onNavigateSettlement && (
            <Button
              id="btn-go-to-settlement"
              variant="primary"
              onPress={onNavigateSettlement}
            >
              <Icon name="dollar-sign" className="w-4 h-4 mr-1.5" />
              Disburse in Settlement
            </Button>
          )}
        </div>
      </div>

      {/* Stepper Progress */}
      <div className="bg-white dark:bg-neutral-900 rounded-xl p-4 border border-neutral-200 dark:border-neutral-800">
        <div className="grid grid-cols-2 md:grid-cols-6 gap-2">
          {STEPS.map((step, idx) => {
            const isCurrent = run.status === step.id;
            const isCompleted = STEPS.findIndex((s) => s.id === run.status) >= idx;
            return (
              <div
                key={step.id}
                className={`text-center py-2 px-3 rounded-lg text-xs font-medium transition-colors ${
                  isCurrent
                    ? 'bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300 border border-blue-200 dark:border-blue-800'
                    : isCompleted
                    ? 'text-neutral-700 dark:text-neutral-300 bg-neutral-50 dark:bg-neutral-800/50'
                    : 'text-neutral-400 dark:text-neutral-600'
                }`}
              >
                {step.label}
              </div>
            );
          })}
        </div>
      </div>

      {/* Financial Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card className="p-4 border border-neutral-200 dark:border-neutral-800">
          <span className="text-xs text-neutral-500 block">Total Gross Pay</span>
          <span className="text-xl font-bold text-neutral-900 dark:text-neutral-100 mt-1 block">
            <Money amount={run.totalGross} currency={run.currency} />
          </span>
          <span className="text-xs text-neutral-400 mt-1 block">
            {run.employeeCount} Total Employees
          </span>
        </Card>

        <Card className="p-4 border border-neutral-200 dark:border-neutral-800">
          <span className="text-xs text-neutral-500 block">Total Net Pay</span>
          <span className="text-xl font-bold text-emerald-600 dark:text-emerald-400 mt-1 block">
            <Money amount={run.totalNet} currency={run.currency} />
          </span>
          <span className="text-xs text-neutral-400 mt-1 block">
            Net Disbursements Due
          </span>
        </Card>

        <Card className="p-4 border border-neutral-200 dark:border-neutral-800">
          <span className="text-xs text-neutral-500 block">Employer Contributions</span>
          <span className="text-xl font-bold text-indigo-600 dark:text-indigo-400 mt-1 block">
            <Money amount={run.totalEmployerContributions} currency={run.currency} />
          </span>
          <span className="text-xs text-neutral-400 mt-1 block">
            Statutory Social Insurance
          </span>
        </Card>

        <Card className="p-4 border border-neutral-200 dark:border-neutral-800">
          <span className="text-xs text-neutral-500 block">Exceptions Gate</span>
          <div className="flex items-center gap-2 mt-1">
            <span className="text-xl font-bold text-neutral-900 dark:text-neutral-100">
              {openBlockingCount} Blocking
            </span>
            <Badge variant={openBlockingCount === 0 ? 'success' : 'danger'}>
              {openBlockingCount === 0 ? 'Clear to Finalize' : 'Blocked'}
            </Badge>
          </div>
          <span className="text-xs text-neutral-400 mt-1 block">
            {openWarningCount} Advisory Warnings
          </span>
        </Card>
      </div>

      {/* Employee Results Table */}
      <Card className="border border-neutral-200 dark:border-neutral-800 overflow-hidden">
        <div className="p-4 border-b border-neutral-200 dark:border-neutral-800 flex items-center justify-between">
          <h3 className="font-semibold text-neutral-900 dark:text-neutral-100 text-sm">
            Employee Calculation Results
          </h3>
          <span className="text-xs text-neutral-500 font-mono">
            {results.length} Calculated Lines
          </span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm" id="table-payroll-employee-results">
            <thead className="bg-neutral-50 dark:bg-neutral-800/50 text-neutral-500 text-xs uppercase font-medium">
              <tr>
                <th className="px-4 py-3">Employment ID</th>
                <th className="px-4 py-3">Gross Earnings</th>
                <th className="px-4 py-3">Total Deductions</th>
                <th className="px-4 py-3">Employer GOSI</th>
                <th className="px-4 py-3">Net Payable</th>
                <th className="px-4 py-3 text-right">Explainability</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-neutral-200 dark:divide-neutral-800">
              {results.map((res) => (
                <tr key={res.id} className="hover:bg-neutral-50 dark:hover:bg-neutral-800/40">
                  <td className="px-4 py-3 font-mono text-xs text-neutral-700 dark:text-neutral-300">
                    {res.employmentId.slice(0, 8)}...
                  </td>
                  <td className="px-4 py-3 text-neutral-900 dark:text-neutral-100">
                    <Money amount={res.grossPay} currency={run.currency} />
                  </td>
                  <td className="px-4 py-3 text-red-600 dark:text-red-400">
                    <Money amount={res.totalDeductions} currency={run.currency} />
                  </td>
                  <td className="px-4 py-3 text-neutral-600 dark:text-neutral-400">
                    <Money amount={res.employerContributions} currency={run.currency} />
                  </td>
                  <td className="px-4 py-3 font-semibold text-emerald-600 dark:text-emerald-400">
                    <SensitiveValue
                      value={`${res.netPay} ${run.currency}`}
                      maskedPlaceholder="••••••"
                    />
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Button
                      id={`btn-explain-${res.employmentId}`}
                      variant="ghost"
                      className="text-xs text-blue-600 hover:text-blue-700 dark:text-blue-400"
                      onPress={() => handleOpenTrace(res.employmentId)}
                    >
                      <Icon name="help-circle" className="w-3.5 h-3.5 mr-1" />
                      Explain Calculation
                    </Button>
                  </td>
                </tr>
              ))}

              {results.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-neutral-500 text-sm">
                    No results calculated yet. Click "Load Inputs" then "Calculate" to generate financial output.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Exceptions Queue Drawer */}
      {isExceptionsOpen && (
        <PayrollExceptionsQueue
          runId={run.id}
          exceptions={exceptions}
          onClose={() => setIsExceptionsOpen(false)}
          onResolve={onResolveException}
          onWaive={onWaiveException}
        />
      )}

      {/* Calculation Trace Drawer */}
      {isTraceOpen && selectedDetail && (
        <CalculationTraceDrawer
          detail={selectedDetail}
          currency={run.currency}
          onClose={() => setIsTraceOpen(false)}
        />
      )}

      {/* Finalize Confirmation Dialog */}
      {isFinalizeOpen && (
        <FinalizeRunDialog
          run={run}
          onConfirm={() => {
            onFinalize();
            setIsFinalizeOpen(false);
          }}
          onClose={() => setIsFinalizeOpen(false)}
        />
      )}
    </div>
  );
};
