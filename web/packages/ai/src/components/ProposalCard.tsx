import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Icon } from '@zainx/design-system';
import type { AiActionProposalDto, AiProposalExecutionResponseDto } from '@zainx/contracts';

export interface ProposalCardProps {
  proposal: AiActionProposalDto;
  onConfirm?: (proposalId: string, reason?: string) => Promise<AiProposalExecutionResponseDto | void>;
  onCancel?: (proposalId: string, reason?: string) => Promise<AiActionProposalDto | void>;
  isExecuting?: boolean;
}

export const ProposalCard: React.FC<ProposalCardProps> = ({
  proposal,
  onConfirm,
  onCancel,
  isExecuting = false,
}) => {
  const { i18n } = useTranslation();
  const isRtl = i18n.language === 'ar' || (typeof document !== 'undefined' && document.documentElement.dir === 'rtl');

  const [confirmReason, setConfirmReason] = useState('');
  const [cancelReason, setCancelReason] = useState('');
  const [showConfirmInput, setShowConfirmInput] = useState(false);
  const [showCancelInput, setShowCancelInput] = useState(false);
  const [localExecuting, setLocalExecuting] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);

  // Parse snapshots safely
  const beforeData = React.useMemo(() => {
    try {
      return proposal.beforeSnapshotJson ? JSON.parse(proposal.beforeSnapshotJson) : {};
    } catch {
      return { raw: proposal.beforeSnapshotJson };
    }
  }, [proposal.beforeSnapshotJson]);

  const afterData = React.useMemo(() => {
    try {
      return proposal.afterSnapshotJson ? JSON.parse(proposal.afterSnapshotJson) : {};
    } catch {
      return { raw: proposal.afterSnapshotJson };
    }
  }, [proposal.afterSnapshotJson]);

  const impactData = React.useMemo(() => {
    try {
      return proposal.impactSummaryJson ? JSON.parse(proposal.impactSummaryJson) : {};
    } catch {
      return { raw: proposal.impactSummaryJson };
    }
  }, [proposal.impactSummaryJson]);

  const statusBadge = () => {
    switch (proposal.status) {
      case 'ReadyForConfirmation':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-warning-subtle text-warning border border-warning" data-testid="proposal-status-ready">
            <span className="w-1.5 h-1.5 rounded-full bg-warning-subtle animate-pulse"></span>
            {isRtl ? 'بانتظار التأكيد' : 'Ready For Confirmation'}
          </span>
        );
      case 'Confirmed':
      case 'Executing':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-sky-500/10 text-sky-500 border border-sky-500/20" data-testid="proposal-status-executing">
            <Icon name="refresh" size="xs" className="animate-spin" />
            {isRtl ? 'قيد التنفيذ...' : 'Executing...'}
          </span>
        );
      case 'Completed':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-success-subtle text-success border border-success" data-testid="proposal-status-completed">
            <Icon name="check" size="xs" />
            {isRtl ? 'تم التنفيذ بنجاح' : 'Completed'}
          </span>
        );
      case 'Cancelled':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-surface-subtle text-text-muted border border-border-default" data-testid="proposal-status-cancelled">
            <Icon name="x" size="xs" />
            {isRtl ? 'ملغي' : 'Cancelled'}
          </span>
        );
      case 'Expired':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-zinc-500/10 text-zinc-400 border border-zinc-500/20" data-testid="proposal-status-expired">
            <Icon name="clock" size="xs" />
            {isRtl ? 'منتهي الصلاحية' : 'Expired'}
          </span>
        );
      case 'Stale':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-danger-subtle text-danger border border-danger" data-testid="proposal-status-stale">
            <Icon name="alert-triangle" size="xs" />
            {isRtl ? 'بيانات قديمة (تغيير متزامن)' : 'Stale Target (409 Conflict)'}
          </span>
        );
      case 'Failed':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-danger-subtle text-danger border border-danger" data-testid="proposal-status-failed">
            <Icon name="alert-circle" size="xs" />
            {isRtl ? 'فشل التنفيذ' : 'Failed'}
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-surface-panel text-text-secondary">
            {proposal.status}
          </span>
        );
    }
  };

  const handleConfirmClick = async () => {
    if (localExecuting || isExecuting) return;
    setLocalExecuting(true);
    setLocalError(null);
    try {
      if (onConfirm) {
        await onConfirm(proposal.id, confirmReason || undefined);
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : String(err);
      setLocalError(message);
    } finally {
      setLocalExecuting(false);
    }
  };

  const handleCancelClick = async () => {
    if (localExecuting || isExecuting) return;
    setLocalExecuting(true);
    setLocalError(null);
    try {
      if (onCancel) {
        await onCancel(proposal.id, cancelReason || undefined);
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : String(err);
      setLocalError(message);
    } finally {
      setLocalExecuting(false);
    }
  };

  const isPending = proposal.status === 'ReadyForConfirmation';

  return (
    <div
      className="w-full my-3 p-4 bg-surface-panel border border-primary rounded-xl shadow-lg backdrop-blur-sm text-text-secondary transition-all duration-200 hover:border-primary"
      data-testid="ai-action-proposal-card"
      data-proposal-id={proposal.id}
      dir={isRtl ? 'rtl' : 'ltr'}
      role="region"
      aria-label={isRtl ? 'اقتراح إجراء الذكاء الاصطناعي' : 'AI Action Proposal'}
    >
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-2 pb-3 border-b border-border-default">
        <div className="flex items-center gap-2">
          <div className="p-2 rounded-lg bg-primary-subtle text-primary">
            <Icon name="sparkles" size="sm" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="text-xs font-mono font-bold uppercase tracking-wider text-primary">
                {isRtl ? 'إجراء مقترح' : 'PROPOSED ACTION'}
              </span>
              <span className="text-xs text-text-muted">|</span>
              <span className="text-xs font-mono text-text-secondary font-semibold" data-testid="proposal-action-code">
                {proposal.actionCode}
              </span>
            </div>
            <div className="text-xs text-text-muted mt-0.5">
              {isRtl ? 'الهدف:' : 'Target:'} <span className="font-semibold text-text-secondary">{proposal.targetEntityType}</span> (ID: <code className="text-text-muted">{proposal.targetEntityId.slice(0, 8)}...</code>)
            </div>
          </div>
        </div>
        <div>{statusBadge()}</div>
      </div>

      {/* Metadata & Effective Date */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 my-3 text-xs">
        {proposal.effectiveDateUtc && (
          <div className="p-2.5 rounded-lg bg-surface-panel border border-border-default flex items-center gap-2">
            <Icon name="calendar" size="xs" className="text-primary shrink-0" />
            <div>
              <span className="text-text-muted block">{isRtl ? 'تاريخ السريان:' : 'Effective Date:'}</span>
              <span className="font-medium text-text-secondary" data-testid="proposal-effective-date">
                {new Date(proposal.effectiveDateUtc).toLocaleDateString(isRtl ? 'ar-EG' : 'en-US', {
                  year: 'numeric',
                  month: 'short',
                  day: 'numeric',
                })}
              </span>
            </div>
          </div>
        )}

        <div className="p-2.5 rounded-lg bg-surface-panel border border-border-default flex items-center gap-2">
          <Icon name="shield-alert" size="xs" className="text-primary shrink-0" />
          <div className="truncate">
            <span className="text-text-muted block">{isRtl ? 'رمز التحقق الرقمي:' : 'Integrity Hash:'}</span>
            <span className="font-mono text-text-muted text-[10px] truncate block" title={proposal.proposalHash}>
              {proposal.proposalHash.slice(0, 16)}...
            </span>
          </div>
        </div>
      </div>

      {/* Impact Summary */}
      {impactData && (
        <div className="my-3 p-3 bg-surface-panel border border-border-default rounded-lg text-xs">
          <div className="flex items-center gap-1.5 text-primary font-semibold mb-1">
            <Icon name="info" size="xs" />
            <span>{isRtl ? 'ملخص الأثر المتوقع' : 'Expected Impact'}</span>
          </div>
          <p className="text-text-secondary leading-relaxed" data-testid="proposal-impact-summary">
            {typeof impactData === 'string'
              ? impactData
              : impactData.description || impactData.summary || JSON.stringify(impactData, null, 2)}
          </p>
        </div>
      )}

      {/* Before / After Diff */}
      <div className="my-3 grid grid-cols-1 md:grid-cols-2 gap-3">
        {/* Before */}
        <div className="p-3 bg-surface-panel border border-danger rounded-lg">
          <div className="flex items-center gap-1.5 text-xs font-semibold text-danger mb-2">
            <span className="w-2 h-2 rounded-full bg-danger-subtle"></span>
            <span>{isRtl ? 'الحالة الحالية (قبل)' : 'Current State (Before)'}</span>
          </div>
          <div className="font-mono text-xs text-text-secondary bg-surface-panel p-2.5 rounded border border-border-default overflow-x-auto max-h-36" data-testid="proposal-before-snapshot">
            {Object.keys(beforeData).length > 0 ? (
              <pre className="whitespace-pre-wrap">{JSON.stringify(beforeData, null, 2)}</pre>
            ) : (
              <span className="text-text-muted italic">{isRtl ? 'لا يوجد بيانات سابقة' : 'No prior state'}</span>
            )}
          </div>
        </div>

        {/* After */}
        <div className="p-3 bg-surface-panel border border-success rounded-lg">
          <div className="flex items-center gap-1.5 text-xs font-semibold text-success mb-2">
            <span className="w-2 h-2 rounded-full bg-success-subtle"></span>
            <span>{isRtl ? 'الحالة المقترحة (بعد)' : 'Proposed State (After)'}</span>
          </div>
          <div className="font-mono text-xs text-text-secondary bg-surface-panel p-2.5 rounded border border-border-default overflow-x-auto max-h-36" data-testid="proposal-after-snapshot">
            {Object.keys(afterData).length > 0 ? (
              <pre className="whitespace-pre-wrap">{JSON.stringify(afterData, null, 2)}</pre>
            ) : (
              <span className="text-text-muted italic">{isRtl ? 'لا يوجد تعديلات' : 'No changes'}</span>
            )}
          </div>
        </div>
      </div>

      {/* Stale or Error message if applicable */}
      {proposal.status === 'Stale' && (
        <div className="my-2 p-2.5 bg-danger-subtle border border-danger rounded-lg text-danger text-xs flex items-center gap-2" role="alert" data-testid="proposal-stale-alert">
          <Icon name="alert-triangle" size="xs" className="shrink-0 text-danger" />
          <span>
            {isRtl
              ? 'تغيرت بيانات السجل المستهدف منذ إنشاء هذا الاقتراح. يرجى إعادة تقديم الطلب للحصول على أحدث نسخة.'
              : 'Target entity data changed since this proposal was created (HTTP 409 Conflict). Auto-rebase is forbidden. Please create a new proposal.'}
          </span>
        </div>
      )}

      {proposal.errorMessage && proposal.status !== 'Stale' && (
        <div className="my-2 p-2.5 bg-danger-subtle border border-danger rounded-lg text-danger text-xs flex items-center gap-2" role="alert">
          <Icon name="alert-circle" size="xs" className="shrink-0 text-danger" />
          <span>{proposal.errorMessage}</span>
        </div>
      )}

      {localError && (
        <div className="my-2 p-2.5 bg-danger-subtle border border-danger rounded-lg text-danger text-xs flex items-center gap-2" role="alert">
          <Icon name="alert-circle" size="xs" className="shrink-0 text-danger" />
          <span>{localError}</span>
        </div>
      )}

      {/* Actions & Confirmation Gate */}
      {isPending && (
        <div className="mt-4 pt-3 border-t border-border-default flex flex-col gap-3">
          {/* Optional reason inputs */}
          {showConfirmInput && (
            <div className="space-y-1">
              <label className="text-xs text-text-muted">
                {isRtl ? 'سبب التأكيد (اختياري):' : 'Confirmation Reason (optional):'}
              </label>
              <input
                type="text"
                value={confirmReason}
                onChange={(e) => setConfirmReason(e.target.value)}
                placeholder={isRtl ? 'أدخل ملاحظات أو سبب الموافقة...' : 'e.g., Verified by HR Director'}
                className="w-full px-3 py-1.5 bg-surface-panel border border-border-default rounded text-xs text-text-secondary focus:outline-hidden focus:border-primary"
                data-testid="proposal-confirm-reason-input"
              />
            </div>
          )}

          {showCancelInput && (
            <div className="space-y-1">
              <label className="text-xs text-text-muted">
                {isRtl ? 'سبب الإلغاء:' : 'Cancellation Reason:'}
              </label>
              <input
                type="text"
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                placeholder={isRtl ? 'أدخل سبب الإلغاء...' : 'e.g., Candidate requested different date'}
                className="w-full px-3 py-1.5 bg-surface-panel border border-border-default rounded text-xs text-text-secondary focus:outline-hidden focus:border-primary"
                data-testid="proposal-cancel-reason-input"
              />
            </div>
          )}

          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="text-[11px] text-text-muted italic">
              {isRtl
                ? '⚠️ يتطلب هذا الإجراء تأكيداً صريحاً ولا يتم تنفيذه تلقائياً.'
                : '⚠️ Action requires explicit confirmation. Proposal != Execution.'}
            </div>

            <div className="flex items-center gap-2">
              {!showCancelInput ? (
                <button
                  type="button"
                  onClick={() => setShowCancelInput(true)}
                  disabled={localExecuting || isExecuting}
                  className="px-3 py-1.5 rounded-lg border border-border-default bg-surface-panel text-text-secondary hover:bg-surface-tertiary text-xs font-medium transition disabled:opacity-50"
                  data-testid="proposal-cancel-expand-button"
                >
                  {isRtl ? 'إلغاء...' : 'Cancel...'}
                </button>
              ) : (
                <button
                  type="button"
                  onClick={handleCancelClick}
                  disabled={localExecuting || isExecuting}
                  className="px-3 py-1.5 rounded-lg bg-danger-subtle border border-danger text-danger hover:bg-danger-subtle text-xs font-medium transition disabled:opacity-50"
                  data-testid="proposal-cancel-button"
                >
                  {localExecuting ? (isRtl ? 'جاري الإلغاء...' : 'Cancelling...') : (isRtl ? 'تأكيد الإلغاء' : 'Confirm Cancel')}
                </button>
              )}

              {!showConfirmInput ? (
                <button
                  type="button"
                  onClick={() => setShowConfirmInput(true)}
                  disabled={localExecuting || isExecuting}
                  className="px-4 py-1.5 rounded-lg bg-primary-subtle hover:bg-primary-subtle text-text-inverse text-xs font-semibold shadow-sm transition flex items-center gap-1.5 disabled:opacity-50"
                  data-testid="proposal-confirm-expand-button"
                >
                  <Icon name="check" size="xs" />
                  {isRtl ? 'تأكيد الإجراء' : 'Confirm Action'}
                </button>
              ) : (
                <button
                  type="button"
                  onClick={handleConfirmClick}
                  disabled={localExecuting || isExecuting}
                  className="px-4 py-1.5 rounded-lg bg-success-subtle hover:bg-success-subtle text-text-inverse text-xs font-semibold shadow-sm transition flex items-center gap-1.5 disabled:opacity-50"
                  data-testid="proposal-confirm-button"
                >
                  {localExecuting ? (
                    <>
                      <Icon name="refresh" size="xs" className="animate-spin" />
                      {isRtl ? 'جاري التنفيذ...' : 'Executing...'}
                    </>
                  ) : (
                    <>
                      <Icon name="check" size="xs" />
                      {isRtl ? 'تنفيذ الإجراء الآن' : 'Execute Action Now'}
                    </>
                  )}
                </button>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

