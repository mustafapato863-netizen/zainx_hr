import React, { useState, useEffect, Suspense, lazy } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import type {
  PayrollRun,
  PayrollPeriod,
  PayrollEmployeeResult,
  PayrollException,
  PayrollEmployeeResultDetail,
  SettlementBatch,
  SettlementBatchDetail,
} from '@zainx/payroll';
import { PageHeader } from '@zainx/design-system';

const PayrollRunsGrid = lazy(() =>
  import('@zainx/payroll').then((m) => ({ default: m.PayrollRunsGrid })),
);
const PayrollRunWorkspace = lazy(() =>
  import('@zainx/payroll').then((m) => ({ default: m.PayrollRunWorkspace })),
);
const SettlementBatchView = lazy(() =>
  import('@zainx/payroll').then((m) => ({ default: m.SettlementBatchView })),
);

const API_BASE = '/api/v1';

export const payrollRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/payroll',
  component: PayrollPage,
});

export function PayrollPage() {
  const [view, setView] = useState<'grid' | 'workspace' | 'settlement'>('grid');
  const [selectedRunId, setSelectedRunId] = useState<string | null>(null);

  const [runs, setRuns] = useState<PayrollRun[]>([]);
  const [periods, setPeriods] = useState<PayrollPeriod[]>([]);
  const [results, setResults] = useState<PayrollEmployeeResult[]>([]);
  const [exceptions, setExceptions] = useState<PayrollException[]>([]);
  const [batches, setBatches] = useState<SettlementBatch[]>([]);

  const [isLoading, setIsLoading] = useState(false);
  const [isCalculating, setIsCalculating] = useState(false);
  const [actionMessage, setActionMessage] = useState<string | null>(null);

  // Fetch runs & periods on load
  const fetchRunsAndPeriods = async () => {
    setIsLoading(true);
    try {
      const [pRes, rRes] = await Promise.all([
        fetch(`${API_BASE}/payroll/periods`),
        fetch(`${API_BASE}/payroll/runs`),
      ]);

      if (pRes.ok) setPeriods(await pRes.json());
      if (rRes.ok) setRuns(await rRes.json());
    } catch (err) {
      console.error('Failed to fetch payroll runs or periods', err);
    } finally {
      setIsLoading(false);
    }
  };

  const fetchBatches = async () => {
    try {
      const res = await fetch(`${API_BASE}/settlement/batches`);
      if (res.ok) setBatches(await res.json());
    } catch (err) {
      console.error('Failed to fetch settlement batches', err);
    }
  };

  const fetchRunDetail = async (runId: string) => {
    try {
      const [resRes, exRes] = await Promise.all([
        fetch(`${API_BASE}/payroll/runs/${runId}/results`),
        fetch(`${API_BASE}/payroll/runs/${runId}/exceptions`),
      ]);

      if (resRes.ok) setResults(await resRes.json());
      if (exRes.ok) setExceptions(await exRes.json());
    } catch (err) {
      console.error('Failed to fetch run details', err);
    }
  };

  useEffect(() => {
    fetchRunsAndPeriods();
    fetchBatches();
  }, []);

  const handleSelectRun = async (runId: string) => {
    setSelectedRunId(runId);
    await fetchRunDetail(runId);
    setView('workspace');
  };

  const handleCreateRun = async (periodId: string, code: string, currency: string) => {
    try {
      const res = await fetch(`${API_BASE}/payroll/runs`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ periodId, code, currency }),
      });
      if (res.ok) {
        await fetchRunsAndPeriods();
      }
    } catch (err) {
      console.error('Failed to create run', err);
    }
  };

  const handleLoadInputs = async () => {
    setActionMessage(
      'Authoritative payroll input snapshots are not exposed by the current frontend contract. No placeholder employee data was submitted. Connect the approved upstream input read model before loading this run.',
    );
  };

  const handleCalculate = async () => {
    if (!selectedRunId) return;
    const currentRun = runs.find((r) => r.id === selectedRunId);
    if (!currentRun) return;

    setIsCalculating(true);
    try {
      const res = await fetch(`${API_BASE}/payroll/runs/${selectedRunId}/calculate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          expectedRowVersion: currentRun.rowVersion,
        }),
      });

      if (res.ok) {
        await Promise.all([fetchRunsAndPeriods(), fetchRunDetail(selectedRunId)]);
      }
    } catch (err) {
      console.error('Failed to calculate run', err);
    } finally {
      setIsCalculating(false);
    }
  };

  const handleFinalize = async () => {
    if (!selectedRunId) return;
    const currentRun = runs.find((r) => r.id === selectedRunId);
    if (!currentRun) return;

    try {
      const res = await fetch(`${API_BASE}/payroll/runs/${selectedRunId}/finalize`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          expectedRowVersion: currentRun.rowVersion,
        }),
      });

      if (res.ok) {
        await fetchRunsAndPeriods();
      }
    } catch (err) {
      console.error('Failed to finalize run', err);
    }
  };

  const handleFetchEmployeeDetail = async (
    empId: string,
  ): Promise<PayrollEmployeeResultDetail | null> => {
    if (!selectedRunId) return null;
    try {
      const res = await fetch(`${API_BASE}/payroll/runs/${selectedRunId}/results/${empId}`);
      if (res.ok) return await res.json();
    } catch (err) {
      console.error('Failed to fetch employee detail', err);
    }
    return null;
  };

  const handleResolveException = async (exceptionId: string, note: string) => {
    if (!selectedRunId) return;
    try {
      const res = await fetch(
        `${API_BASE}/payroll/runs/${selectedRunId}/exceptions/${exceptionId}/resolve`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ note }),
        },
      );
      if (res.ok) await fetchRunDetail(selectedRunId);
    } catch (err) {
      console.error('Failed to resolve exception', err);
    }
  };

  const handleWaiveException = async (exceptionId: string, justification: string) => {
    if (!selectedRunId) return;
    try {
      const res = await fetch(
        `${API_BASE}/payroll/runs/${selectedRunId}/exceptions/${exceptionId}/waive`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ justification }),
        },
      );
      if (res.ok) await fetchRunDetail(selectedRunId);
    } catch (err) {
      console.error('Failed to waive exception', err);
    }
  };

  const handleGenerateBatch = async (runId: string, batchNumber: string, paymentDate: string) => {
    try {
      const res = await fetch(`${API_BASE}/settlement/batches/generate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ payrollRunId: runId, batchNumber, paymentDate }),
      });
      if (res.ok) await fetchBatches();
    } catch (err) {
      console.error('Failed to generate settlement batch', err);
    }
  };

  const handleApproveBatch = async (batchId: string, rowVersion: number) => {
    try {
      const res = await fetch(`${API_BASE}/settlement/batches/${batchId}/approve`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ expectedRowVersion: rowVersion }),
      });
      if (res.ok) await fetchBatches();
    } catch (err) {
      console.error('Failed to approve batch', err);
    }
  };

  const handleExportBatch = async (batchId: string) => {
    try {
      const res = await fetch(`${API_BASE}/settlement/batches/${batchId}/export`, {
        method: 'POST',
      });
      if (res.ok) {
        const blob = await res.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `SETTLEMENT_EXPORT_${batchId.slice(0, 8)}.csv`;
        document.body.appendChild(a);
        a.click();
        a.remove();
        await fetchBatches();
      }
    } catch (err) {
      console.error('Failed to export batch', err);
    }
  };

  const handleFetchBatchDetail = async (batchId: string): Promise<SettlementBatchDetail | null> => {
    try {
      const res = await fetch(`${API_BASE}/settlement/batches/${batchId}`);
      if (res.ok) return await res.json();
    } catch (err) {
      console.error('Failed to fetch batch detail', err);
    }
    return null;
  };

  const selectedRun = runs.find((r) => r.id === selectedRunId);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Payroll & settlement"
        subtitle="Run payroll with controlled inputs, explainable results, and settlement handoff."
      />
      {actionMessage && (
        <div
          role="status"
          className="rounded-lg border border-warning/30 bg-warning-subtle px-4 py-3 text-sm text-warning-subtle-text"
        >
          {actionMessage}
        </div>
      )}
      {/* Top Navigation Tabs */}
      <div className="flex border-b border-border-default pb-3 gap-4">
        <button
          id="tab-payroll-runs"
          className={`text-sm font-semibold pb-1 border-b-2 transition-colors ${
            view === 'grid' || view === 'workspace'
              ? 'border-info text-info '
              : 'border-transparent text-text-muted hover:text-text-secondary'
          }`}
          onClick={() => setView(selectedRunId ? 'workspace' : 'grid')}
        >
          Payroll Runs
        </button>
        <button
          id="tab-settlement-batches"
          className={`text-sm font-semibold pb-1 border-b-2 transition-colors ${
            view === 'settlement'
              ? 'border-info text-info '
              : 'border-transparent text-text-muted hover:text-text-secondary'
          }`}
          onClick={() => setView('settlement')}
        >
          Settlement & Banking
        </button>
      </div>

      <Suspense
        fallback={
          <div className="rounded-lg border border-border-default bg-surface p-8 text-sm text-text-secondary">
            Loading payroll workspace…
          </div>
        }
      >
        {view === 'grid' && (
          <PayrollRunsGrid
            runs={runs}
            periods={periods}
            onSelectRun={handleSelectRun}
            onCreateRun={handleCreateRun}
            isLoading={isLoading}
          />
        )}

        {view === 'workspace' && selectedRun && (
          <PayrollRunWorkspace
            run={selectedRun}
            results={results}
            exceptions={exceptions}
            onLoadInputs={handleLoadInputs}
            onCalculate={handleCalculate}
            onFinalize={handleFinalize}
            onNavigateSettlement={() => setView('settlement')}
            onBack={() => setView('grid')}
            onFetchEmployeeDetail={handleFetchEmployeeDetail}
            onResolveException={handleResolveException}
            onWaiveException={handleWaiveException}
            isCalculating={isCalculating}
          />
        )}

        {view === 'settlement' && (
          <SettlementBatchView
            batches={batches}
            onGenerateBatch={handleGenerateBatch}
            onApproveBatch={handleApproveBatch}
            onExportBatch={handleExportBatch}
            onFetchBatchDetail={handleFetchBatchDetail}
            finalizedRunId={selectedRun?.id}
            finalizedRunCode={selectedRun?.code}
          />
        )}
      </Suspense>
    </div>
  );
}
