import React, { useState } from 'react';
import { ApprovalInboxItemDto } from '@zainx/contracts';
import { Button } from '@zainx/design-system';

export interface ApprovalDecisionDialogProps {
  item: ApprovalInboxItemDto | null;
  action: 'approve' | 'reject' | null;
  isOpen: boolean;
  onClose: () => void;
  onConfirmDecision?: (
    requestId: string,
    action: 'approve' | 'reject',
    comments: string,
    rowVersion: number
  ) => Promise<void>;
}

export const ApprovalDecisionDialog: React.FC<ApprovalDecisionDialogProps> = ({
  item,
  action,
  isOpen,
  onClose,
  onConfirmDecision
}) => {
  const [comments, setComments] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  if (!isOpen || !item || !action) return null;

  const isApprove = action === 'approve';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isApprove && !comments.trim()) {
      setErrorMessage('A justification / reason is required when rejecting a request.');
      return;
    }

    try {
      setIsSubmitting(true);
      setErrorMessage(null);
      await onConfirmDecision?.(item.id, action, comments, Number(item.rowVersion));
      onClose();
    } catch (err: any) {
      setErrorMessage(
        err.message ||
          'Approval decision failed. Ensure you are authorized for the current workflow step and tenant context.'
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="decision-dialog-title"
      data-testid="approval-decision-dialog"
    >
      <div className="w-full max-w-md rounded-xl bg-surface-primary p-6 shadow-2xl border border-border-primary">
        <div className="flex items-center justify-between border-b border-border-primary pb-3">
          <h2 id="decision-dialog-title" className="text-base font-bold text-text-primary">
            {isApprove ? 'Confirm Approval' : 'Confirm Rejection'}
          </h2>
          <Button variant="ghost" size="sm" onClick={onClose} aria-label="Close dialog">
            ✕
          </Button>
        </div>

        {errorMessage && (
          <div className="mt-3 rounded-lg bg-rose-500/10 border border-rose-500/20 p-3 text-xs text-rose-600 dark:text-rose-400 font-medium">
            {errorMessage}
          </div>
        )}

        <div className="mt-4 p-3.5 rounded-lg bg-surface-secondary/50 border border-border-secondary text-xs space-y-1">
          <div className="font-semibold text-text-primary">{item.title}</div>
          <div className="text-text-secondary">{item.sourceModule} • {item.workflowType}</div>
          <div className="text-text-muted text-[11px] pt-1">
            Requester: {item.requesterEmploymentId} • Step {Number(item.currentStepOrder)} of {Number(item.totalSteps)}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div>
            <label htmlFor="decision-comments" className="block text-xs font-medium text-text-secondary mb-1">
              Comments / Decision Notes {!isApprove && <span className="text-rose-500">*</span>}
            </label>
            <textarea
              id="decision-comments"
              rows={3}
              required={!isApprove}
              value={comments}
              onChange={(e) => setComments(e.target.value)}
              placeholder={
                isApprove
                  ? 'Add optional remarks for the audit history...'
                  : 'Specify reason for rejection...'
              }
              className="w-full rounded-lg border border-border-secondary bg-surface-primary p-2.5 text-xs text-text-primary focus:border-brand-primary focus:outline-none"
            />
          </div>

          <div className="flex items-center justify-end gap-2.5 pt-2 border-t border-border-primary">
            <Button variant="outline" size="sm" onClick={onClose} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button
              variant={isApprove ? 'primary' : 'outline'}
              size="sm"
              type="submit"
              disabled={isSubmitting || (!isApprove && !comments.trim())}
            >
              {isSubmitting
                ? 'Submitting...'
                : isApprove
                ? 'Approve Request'
                : 'Confirm Rejection'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
