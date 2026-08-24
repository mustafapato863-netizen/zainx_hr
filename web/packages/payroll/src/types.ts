export type PayrollRunStatus =
  | 'Draft'
  | 'InputsLoaded'
  | 'Calculated'
  | 'UnderReview'
  | 'Approved'
  | 'Finalized'
  | 'OutputsPublished';

export interface PayrollPeriod {
  id: string;
  code: string;
  periodStart: string;
  periodEnd: string;
  paymentDate: string;
  isActive: boolean;
}

export interface PayrollRun {
  id: string;
  periodId: string;
  code: string;
  status: PayrollRunStatus;
  currency: string;
  totalGross: number;
  totalNet: number;
  totalEmployerContributions: number;
  employeeCount: number;
  reproducibilityHash: string;
  finalizedAtUtc?: string | null;
  rowVersion: number;
}

export interface PayrollEmployeeResult {
  id: string;
  payrollRunId: string;
  employmentId: string;
  grossPay: number;
  netPay: number;
  totalEarnings: number;
  totalDeductions: number;
  employerContributions: number;
}

export interface PayrollLine {
  id: string;
  componentCode: string;
  nameEn: string;
  nameAr: string;
  category: string;
  amount: number;
  calculationType: string;
  rate: number;
  hoursOrDays: number;
  traceId?: string | null;
}

export interface CalculationTrace {
  id: string;
  stepOrder: number;
  ruleReference: string;
  description: string;
  formulaApplied: string;
  inputValuesJson: string;
  intermediateAmount: number;
  roundingDelta: number;
  finalAmount: number;
}

export interface PayrollEmployeeResultDetail extends PayrollEmployeeResult {
  lines: PayrollLine[];
  traces: CalculationTrace[];
}

export interface PayrollException {
  id: string;
  payrollRunId: string;
  employmentId: string;
  severity: 'Info' | 'Warning' | 'Blocking';
  category: string;
  reason: string;
  resolutionGuidance: string;
  status: 'Open' | 'Resolved' | 'Waived';
  resolvedByUserId?: string | null;
  resolutionNote?: string | null;
}

export interface SettlementBatch {
  id: string;
  payrollRunId: string;
  batchNumber: string;
  totalAmount: number;
  currency: string;
  paymentDate: string;
  status: 'Draft' | 'Approved' | 'Processing' | 'Exported' | 'Reconciled';
  instructionCount: number;
  rowVersion: number;
}

export interface PaymentInstruction {
  id: string;
  employmentId: string;
  beneficiaryName: string;
  bankCode: string;
  accountMasked: string;
  amount: number;
  status: string;
}

export interface SettlementBatchDetail extends SettlementBatch {
  instructions: PaymentInstruction[];
}
