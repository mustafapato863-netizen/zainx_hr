import React, { useState } from 'react';
import { Badge, Button, Icon } from '@zainx/design-system';
import { PayrollException } from '../types';

interface PayrollExceptionsQueueProps {
  runId: string;
  exceptions: PayrollException[];
  onClose: () => void;
  onResolve: (exceptionId: string, note: string) => void;
  onWaive: (exceptionId: string, justification: string) => void;
}

export const PayrollExceptionsQueue: React.FC<PayrollExceptionsQueueProps> = ({
  runId,
  exceptions,
  onClose,
  onResolve,
  onWaive,
}) => {
  const [selectedEx, setSelectedEx] = useState<PayrollException | null>(null);
  const [actionType, setActionType] = useState<'resolve' | 'waive' | null>(null);
  const [note, setNote] = useState('');

  const handleAction = () => {
    if (!selectedEx || !actionType) return;
    if (actionType === 'resolve') {
      onResolve(selectedEx.id, note);
    } else {
      onWaive(selectedEx.id, note);
    }
    setSelectedEx(null);
    setActionType(null);
    setNote('');
  };

  const getSeverityBadge = (severity: string) => {
    switch (severity) {
      case 'Blocking':
        return <Badge variant="danger">Blocking</Badge>;
      case 'Warning':
        return <Badge variant="warning">Warning</Badge>;
      default:
        return <Badge variant="neutral">Info</Badge>;
    }
  };

  return (
    <div
      role="dialog"
      aria-labelledby="exceptions-drawer-title"
      className="fixed inset-y-0 right-0 z-50 w-full max-w-xl bg-white dark:bg-neutral-900 shadow-2xl border-l border-neutral-200 dark:border-neutral-800 flex flex-col"
    >
      <div className="p-5 border-b border-neutral-200 dark:border-neutral-800 flex items-center justify-between">
        <div>
          <h3 id="exceptions-drawer-title" className="font-semibold text-lg text-neutral-900 dark:text-neutral-100">
            P7 Exceptions Queue
          </h3>
          <span className="text-xs text-neutral-500">
            Review calculation anomalies, regulatory limits, and blocking gates.
          </span>
        </div>
        <Button id="btn-close-exceptions" variant="ghost" className="p-2" onPress={onClose} aria-label="Close Exceptions Queue">
          <Icon name="x" className="w-5 h-5" />
        </Button>
      </div>

      <div className="flex-1 overflow-y-auto p-5 space-y-4">
        {exceptions.map((ex) => (
          <div
            key={ex.id}
            id={`exception-card-${ex.id}`}
            className="p-4 rounded-xl border border-neutral-200 dark:border-neutral-800 bg-neutral-50 dark:bg-neutral-800/40 space-y-2"
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                {getSeverityBadge(ex.severity)}
                <span className="font-semibold text-xs text-neutral-900 dark:text-neutral-100 font-mono">
                  {ex.category}
                </span>
              </div>
              <Badge variant={ex.status === 'Open' ? 'warning' : 'success'}>
                {ex.status}
              </Badge>
            </div>

            <p className="text-sm text-neutral-800 dark:text-neutral-200">{ex.reason}</p>
            <p className="text-xs text-neutral-500 italic">Guidance: {ex.resolutionGuidance}</p>

            {ex.status === 'Open' && (
              <div className="flex items-center gap-2 pt-2 border-t border-neutral-200 dark:border-neutral-700">
                <Button
                  id={`btn-resolve-ex-${ex.id}`}
                  variant="secondary"
                  className="text-xs py-1"
                  onPress={() => {
                    setSelectedEx(ex);
                    setActionType('resolve');
                  }}
                >
                  Resolve
                </Button>
                {ex.severity !== 'Blocking' && (
                  <Button
                    id={`btn-waive-ex-${ex.id}`}
                    variant="ghost"
                    className="text-xs py-1 text-neutral-600 dark:text-neutral-400"
                    onPress={() => {
                      setSelectedEx(ex);
                      setActionType('waive');
                    }}
                  >
                    Waive Warning
                  </Button>
                )}
              </div>
            )}

            {ex.status !== 'Open' && ex.resolutionNote && (
              <div className="text-xs text-emerald-600 dark:text-emerald-400 pt-1">
                Note: {ex.resolutionNote}
              </div>
            )}
          </div>
        ))}

        {exceptions.length === 0 && (
          <div className="p-8 text-center text-neutral-400">
            <Icon name="check-circle" className="w-10 h-10 mx-auto text-emerald-500 mb-2" />
            <p className="text-sm font-medium text-neutral-700 dark:text-neutral-300">
              No Exceptions Found
            </p>
            <p className="text-xs text-neutral-500 mt-1">
              All employee calculations and statutory validations passed cleanly.
            </p>
          </div>
        )}
      </div>

      {/* Action Sub-Modal */}
      {selectedEx && actionType && (
        <div className="p-4 border-t border-neutral-200 dark:border-neutral-800 bg-white dark:bg-neutral-900 space-y-3">
          <h4 className="text-xs font-semibold text-neutral-900 dark:text-neutral-100 uppercase">
            {actionType === 'resolve' ? 'Resolve Exception' : 'Waive Warning'}
          </h4>
          <input
            id="input-exception-note"
            type="text"
            placeholder={actionType === 'resolve' ? 'Enter resolution note...' : 'Enter justification for waiver...'}
            className="w-full px-3 py-2 text-sm rounded-lg border border-neutral-300 dark:border-neutral-700 bg-white dark:bg-neutral-800 text-neutral-900 dark:text-neutral-100"
            value={note}
            onChange={(e) => setNote(e.target.value)}
          />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onPress={() => { setSelectedEx(null); setActionType(null); }}>
              Cancel
            </Button>
            <Button id="btn-submit-exception-action" variant="primary" onPress={handleAction}>
              Confirm
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};
