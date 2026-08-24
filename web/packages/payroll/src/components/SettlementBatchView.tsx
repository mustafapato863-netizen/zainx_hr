import React, { useState } from 'react';
import {
  Card,
  Badge,
  Button,
  Money,
  SensitiveValue,
  Icon,
} from '@zainx/design-system';
import { SettlementBatch, SettlementBatchDetail } from '../types';

interface SettlementBatchViewProps {
  batches: SettlementBatch[];
  onGenerateBatch: (runId: string, batchNumber: string, paymentDate: string) => void;
  onApproveBatch: (batchId: string, rowVersion: number) => void;
  onExportBatch: (batchId: string) => void;
  onFetchBatchDetail: (batchId: string) => Promise<SettlementBatchDetail | null>;
  finalizedRunId?: string;
  finalizedRunCode?: string;
}

export const SettlementBatchView: React.FC<SettlementBatchViewProps> = ({
  batches,
  onGenerateBatch,
  onApproveBatch,
  onExportBatch,
  onFetchBatchDetail,
  finalizedRunId,
  finalizedRunCode,
}) => {
  const [selectedBatchId, setSelectedBatchId] = useState<string | null>(batches[0]?.id || null);
  const [batchDetail, setBatchDetail] = useState<SettlementBatchDetail | null>(null);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [isGenerateOpen, setIsGenerateOpen] = useState(false);
  const [batchNumber, setBatchNumber] = useState(`BATCH-${new Date().toISOString().slice(0, 10)}`);
  const [paymentDate, setPaymentDate] = useState(new Date().toISOString().slice(0, 10));

  const handleSelectBatch = async (batchId: string) => {
    setSelectedBatchId(batchId);
    setIsLoadingDetail(true);
    const detail = await onFetchBatchDetail(batchId);
    setBatchDetail(detail);
    setIsLoadingDetail(false);
  };

  const handleGenerateSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!finalizedRunId) return;
    onGenerateBatch(finalizedRunId, batchNumber, paymentDate);
    setIsGenerateOpen(false);
  };

  return (
    <div className="space-y-6" data-testid="settlement-view">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">
            Payment Settlement & Banking Disbursals
          </h2>
          <p className="text-sm text-neutral-500">
            Generate 1:1 reconciled payment instructions and neutral bank exports from finalized runs.
          </p>
        </div>
        {finalizedRunId && (
          <Button
            id="btn-open-generate-batch"
            variant="primary"
            onPress={() => setIsGenerateOpen(true)}
          >
            <Icon name="plus" className="w-4 h-4 mr-2" />
            Generate Batch for {finalizedRunCode || 'Run'}
          </Button>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Batches List Column */}
        <div className="space-y-3">
          <h3 className="text-xs font-semibold text-neutral-500 uppercase tracking-wider">
            Settlement Batches
          </h3>
          <div className="space-y-3">
            {batches.map((b) => (
              <Card
                key={b.id}
                id={`settlement-batch-card-${b.id}`}
                className={`p-4 cursor-pointer border transition-all ${
                  selectedBatchId === b.id
                    ? 'border-blue-500 bg-blue-50/20 dark:bg-blue-900/10'
                    : 'border-neutral-200 dark:border-neutral-800 hover:border-neutral-300'
                }`}
                onClick={() => handleSelectBatch(b.id)}
              >
                <div className="flex items-start justify-between">
                  <div>
                    <h4 className="font-semibold text-sm text-neutral-900 dark:text-neutral-100">
                      {b.batchNumber}
                    </h4>
                    <span className="text-xs text-neutral-500 block mt-0.5">
                      Pay Date: {b.paymentDate}
                    </span>
                  </div>
                  <Badge variant={b.status === 'Exported' || b.status === 'Reconciled' ? 'success' : 'neutral'}>
                    {b.status}
                  </Badge>
                </div>

                <div className="flex items-center justify-between mt-3 pt-2 border-t border-neutral-100 dark:border-neutral-800 text-xs">
                  <span className="text-neutral-500">{b.instructionCount} Instructions</span>
                  <span className="font-bold text-emerald-600 dark:text-emerald-400">
                    <Money amount={b.totalAmount} currency={b.currency} />
                  </span>
                </div>
              </Card>
            ))}

            {batches.length === 0 && (
              <div className="p-8 text-center border-2 border-dashed border-neutral-200 dark:border-neutral-800 rounded-xl text-neutral-400 text-xs">
                No settlement batches generated yet. Finalize a payroll run to disburse funds.
              </div>
            )}
          </div>
        </div>

        {/* Selected Batch Detail Column */}
        <div className="col-span-2">
          {batchDetail ? (
            <Card className="p-5 border border-neutral-200 dark:border-neutral-800 space-y-5">
              <div className="flex items-start justify-between">
                <div>
                  <div className="flex items-center gap-2">
                    <h3 className="font-bold text-base text-neutral-900 dark:text-neutral-100">
                      {batchDetail.batchNumber}
                    </h3>
                    <Badge variant="success">1:1 Reconciled</Badge>
                  </div>
                  <span className="text-xs text-neutral-500 font-mono">
                    Batch ID: {batchDetail.id} | RowVersion: v{batchDetail.rowVersion}
                  </span>
                </div>

                <div className="flex items-center gap-2">
                  {batchDetail.status === 'Draft' && (
                    <Button
                      id="btn-approve-batch"
                      variant="secondary"
                      onPress={() => onApproveBatch(batchDetail.id, batchDetail.rowVersion)}
                    >
                      <Icon name="check" className="w-4 h-4 mr-1.5" />
                      Approve Batch
                    </Button>
                  )}

                  <Button
                    id="btn-export-batch-csv"
                    variant="primary"
                    onPress={() => onExportBatch(batchDetail.id)}
                  >
                    <Icon name="download" className="w-4 h-4 mr-1.5" />
                    Download Neutral CSV Export
                  </Button>
                </div>
              </div>

              {/* Instruction Table */}
              <div className="rounded-xl border border-neutral-200 dark:border-neutral-800 overflow-hidden">
                <table className="w-full text-left text-xs" id="table-payment-instructions">
                  <thead className="bg-neutral-50 dark:bg-neutral-800/50 text-neutral-500 uppercase font-medium">
                    <tr>
                      <th className="px-3 py-2.5">Beneficiary</th>
                      <th className="px-3 py-2.5">Bank Code</th>
                      <th className="px-3 py-2.5">Encrypted Account / IBAN</th>
                      <th className="px-3 py-2.5 text-right">Payable Amount</th>
                      <th className="px-3 py-2.5 text-center">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-neutral-200 dark:divide-neutral-800">
                    {batchDetail.instructions.map((inst) => (
                      <tr key={inst.id} className="hover:bg-neutral-50 dark:hover:bg-neutral-800/30">
                        <td className="px-3 py-2.5 font-medium text-neutral-900 dark:text-neutral-100">
                          {inst.beneficiaryName}
                        </td>
                        <td className="px-3 py-2.5 font-mono text-neutral-600 dark:text-neutral-400">
                          {inst.bankCode}
                        </td>
                        <td className="px-3 py-2.5 font-mono text-neutral-700 dark:text-neutral-300">
                          <SensitiveValue value={inst.accountMasked} maskedPlaceholder="••••••••" />
                        </td>
                        <td className="px-3 py-2.5 text-right font-semibold text-emerald-600 dark:text-emerald-400">
                          <Money amount={inst.amount} currency={batchDetail.currency} />
                        </td>
                        <td className="px-3 py-2.5 text-center">
                          <span className="px-2 py-0.5 rounded bg-neutral-100 dark:bg-neutral-800 text-neutral-600 dark:text-neutral-300 text-[10px]">
                            {inst.status}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Card>
          ) : (
            <div className="p-12 text-center border-2 border-dashed border-neutral-200 dark:border-neutral-800 rounded-xl text-neutral-400 text-sm">
              Select a settlement batch to inspect payment instructions and download banking files.
            </div>
          )}
        </div>
      </div>

      {/* Generate Batch Modal */}
      {isGenerateOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div
            role="dialog"
            aria-labelledby="generate-batch-dialog-title"
            className="bg-white dark:bg-neutral-900 rounded-xl shadow-xl max-w-md w-full p-6 space-y-4 border border-neutral-200 dark:border-neutral-800"
          >
            <h3 id="generate-batch-dialog-title" className="text-lg font-semibold text-neutral-900 dark:text-neutral-100">
              Generate Settlement Batch
            </h3>
            <form onSubmit={handleGenerateSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-neutral-700 dark:text-neutral-300 mb-1">
                  Batch Number
                </label>
                <input
                  id="input-batch-number"
                  type="text"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-neutral-300 dark:border-neutral-700 bg-white dark:bg-neutral-800 text-neutral-900 dark:text-neutral-100"
                  value={batchNumber}
                  onChange={(e) => setBatchNumber(e.target.value)}
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-neutral-700 dark:text-neutral-300 mb-1">
                  Payment Date
                </label>
                <input
                  id="input-payment-date"
                  type="date"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-neutral-300 dark:border-neutral-700 bg-white dark:bg-neutral-800 text-neutral-900 dark:text-neutral-100"
                  value={paymentDate}
                  onChange={(e) => setPaymentDate(e.target.value)}
                  required
                />
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <Button variant="secondary" onPress={() => setIsGenerateOpen(false)}>
                  Cancel
                </Button>
                <Button id="btn-submit-generate-batch" variant="primary" type="submit">
                  Generate Instructions
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
