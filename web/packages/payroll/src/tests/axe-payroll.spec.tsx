import React from 'react';
import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { axe } from 'vitest-axe';
import * as matchers from 'vitest-axe/matchers';
import { PayrollRunsGrid } from '../components/PayrollRunsGrid';
import { PayrollRunWorkspace } from '../components/PayrollRunWorkspace';
import { PayrollExceptionsQueue } from '../components/PayrollExceptionsQueue';
import { CalculationTraceDrawer } from '../components/CalculationTraceDrawer';
import { FinalizeRunDialog } from '../components/FinalizeRunDialog';
import { SettlementBatchView } from '../components/SettlementBatchView';
import {
  PayrollRun,
  PayrollPeriod,
  PayrollEmployeeResult,
  PayrollException,
  PayrollEmployeeResultDetail,
  SettlementBatch,
} from '../types';

expect.extend(matchers);

const samplePeriods: PayrollPeriod[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    code: '2026-08-MONTHLY',
    periodStart: '2026-08-01',
    periodEnd: '2026-08-31',
    paymentDate: '2026-08-31',
    isActive: true,
  },
];

const sampleRuns: PayrollRun[] = [
  {
    id: '22222222-2222-2222-2222-222222222222',
    periodId: '11111111-1111-1111-1111-111111111111',
    code: 'RUN-2026-08-MAIN',
    status: 'Calculated',
    currency: 'EGP',
    totalGross: 65000,
    totalNet: 52400,
    totalEmployerContributions: 4725,
    employeeCount: 2,
    reproducibilityHash: 'E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855',
    rowVersion: 2,
  },
];

const sampleResults: PayrollEmployeeResult[] = [
  {
    id: '33333333-3333-3333-3333-333333333333',
    payrollRunId: '22222222-2222-2222-2222-222222222222',
    employmentId: '44444444-4444-4444-4444-444444444444',
    grossPay: 35000,
    netPay: 28200,
    totalEarnings: 35000,
    totalDeductions: 6800,
    employerContributions: 2362.5,
  },
];

const sampleExceptions: PayrollException[] = [
  {
    id: '55555555-5555-5555-5555-555555555555',
    payrollRunId: '22222222-2222-2222-2222-222222222222',
    employmentId: '44444444-4444-4444-4444-444444444444',
    severity: 'Warning',
    category: 'ALLOWANCE_ROUNDING',
    reason: 'Allowance amount required half-up rounding.',
    resolutionGuidance: 'Informational only.',
    status: 'Open',
  },
];

const sampleDetail: PayrollEmployeeResultDetail = {
  ...sampleResults[0],
  lines: [
    {
      id: '66666666-6666-6666-6666-666666666666',
      componentCode: 'BASE_SALARY',
      nameEn: 'Base Salary',
      nameAr: 'الراتب الأساسي',
      category: 'BaseSalary',
      amount: 30000,
      calculationType: 'FixedAmount',
      rate: 0,
      hoursOrDays: 0,
    },
  ],
  traces: [
    {
      id: '77777777-7777-7777-7777-777777777777',
      stepOrder: 1,
      ruleReference: 'BASE_SALARY',
      description: 'Monthly Base Compensation',
      formulaApplied: '30000.00',
      inputValuesJson: '{"baseSalary": 30000}',
      intermediateAmount: 30000,
      roundingDelta: 0,
      finalAmount: 30000,
    },
  ],
};

const sampleBatches: SettlementBatch[] = [
  {
    id: '88888888-8888-8888-8888-888888888888',
    payrollRunId: '22222222-2222-2222-2222-222222222222',
    batchNumber: 'BATCH-2026-08',
    totalAmount: 52400,
    currency: 'EGP',
    paymentDate: '2026-08-31',
    status: 'Draft',
    instructionCount: 2,
    rowVersion: 1,
  },
];

describe('Phase 4 Payroll & Settlement Accessibility Verification (Axe WCAG AA)', () => {
  it('PayrollRunsGrid passes axe accessibility check', async () => {
    const { container } = render(
      <PayrollRunsGrid
        runs={sampleRuns}
        periods={samplePeriods}
        onSelectRun={() => {}}
        onCreateRun={() => {}}
      />
    );
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });

  it('PayrollRunWorkspace passes axe accessibility check', async () => {
    const { container } = render(
      <PayrollRunWorkspace
        run={sampleRuns[0]}
        results={sampleResults}
        exceptions={sampleExceptions}
        onLoadInputs={() => {}}
        onCalculate={() => {}}
        onFinalize={() => {}}
        onBack={() => {}}
        onFetchEmployeeDetail={async () => sampleDetail}
        onResolveException={() => {}}
        onWaiveException={() => {}}
      />
    );
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });

  it('PayrollExceptionsQueue passes axe accessibility check', async () => {
    const { container } = render(
      <PayrollExceptionsQueue
        runId={sampleRuns[0].id}
        exceptions={sampleExceptions}
        onClose={() => {}}
        onResolve={() => {}}
        onWaive={() => {}}
      />
    );
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });

  it('CalculationTraceDrawer passes axe accessibility check', async () => {
    const { container } = render(
      <CalculationTraceDrawer
        detail={sampleDetail}
        currency="EGP"
        onClose={() => {}}
      />
    );
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });

  it('FinalizeRunDialog passes axe accessibility check', async () => {
    const { container } = render(
      <FinalizeRunDialog
        run={sampleRuns[0]}
        onConfirm={() => {}}
        onClose={() => {}}
      />
    );
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });

  it('SettlementBatchView passes axe accessibility check', async () => {
    const { container } = render(
      <SettlementBatchView
        batches={sampleBatches}
        onGenerateBatch={() => {}}
        onApproveBatch={() => {}}
        onExportBatch={() => {}}
        onFetchBatchDetail={async () => null}
      />
    );
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });
});
