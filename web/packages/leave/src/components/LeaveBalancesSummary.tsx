import React from 'react';
import { Badge, Skeleton } from '@zainx/design-system';
import { LeaveBalanceDto } from '@zainx/contracts';

export interface LeaveBalancesSummaryProps {
  balances?: LeaveBalanceDto[];
  isLoading?: boolean;
  onRequestLeave?: (leaveTypeId?: string) => void;
}

export const LeaveBalancesSummary: React.FC<LeaveBalancesSummaryProps> = ({
  balances = [],
  isLoading = false,
  onRequestLeave,
}) => {
  return (
    <div
      className="flex flex-col gap-4 p-6 bg-surface-primary rounded-xl border border-border-primary shadow-sm"
      data-testid="leave-balances-summary"
    >
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-border-primary pb-4">
        <div>
          <h2 className="text-lg font-bold text-text-primary">Authoritative Leave Balances</h2>
          <p className="text-xs text-text-muted mt-0.5">
            Engine-verified entitlements, accrued accruals, and reserved days
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant="primary">Engine Managed</Badge>
          <button
            type="button"
            onClick={() => onRequestLeave?.()}
            className="px-3.5 py-1.5 rounded-lg bg-brand-primary text-text-inverse text-xs font-semibold hover:bg-brand-primary/90 transition-colors shadow-sm"
          >
            + Request Time Off
          </button>
        </div>
      </div>

      {isLoading ? (
        <div
          className="grid grid-cols-1 md:grid-cols-3 gap-4"
          data-testid="leave-balances-skeleton"
        >
          <Skeleton className="h-32 w-full rounded-xl" />
          <Skeleton className="h-32 w-full rounded-xl" />
          <Skeleton className="h-32 w-full rounded-xl" />
        </div>
      ) : balances.length === 0 ? (
        <div className="text-center py-8 text-sm text-text-muted">
          No leave balance records available for the active employment.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {balances.map((b) => {
            const total = Number(b.entitledDays || 21);
            const available = Number(b.availableDays || 0);
            const percentUsed = Math.min(
              100,
              Math.round(((Number(b.usedDays || 0) + Number(b.pendingDays || 0)) / total) * 100),
            );

            return (
              <div
                key={b.id || b.leaveTypeId}
                className="p-5 rounded-xl border border-border-secondary bg-surface-secondary/40 hover:border-brand-primary/40 transition-all flex flex-col justify-between"
                data-testid={`leave-balance-card-${b.leaveTypeId}`}
              >
                <div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-bold text-text-primary">
                      {b.leaveTypeNameEn || 'Annual Leave'}
                    </span>
                    <span className="text-xs font-mono text-text-muted">{b.year}</span>
                  </div>

                  <div className="mt-4 flex items-baseline gap-2">
                    <span className="text-3xl font-extrabold text-brand-primary font-mono">
                      {available}
                    </span>
                    <span className="text-xs text-text-secondary">days available</span>
                  </div>

                  {/* Progress Bar */}
                  <div className="mt-3 w-full bg-surface-tertiary rounded-full h-2 overflow-hidden">
                    <div
                      className="bg-brand-primary h-full rounded-full transition-all"
                      style={{ width: `${percentUsed}%` }}
                    />
                  </div>
                </div>

                <div className="mt-4 pt-3 border-t border-border-secondary grid grid-cols-3 text-center text-xs">
                  <div>
                    <span className="text-text-muted block text-[10px] uppercase">Entitled</span>
                    <span className="font-semibold text-text-primary font-mono">
                      {b.entitledDays}
                    </span>
                  </div>
                  <div>
                    <span className="text-text-muted block text-[10px] uppercase">Used</span>
                    <span className="font-semibold text-text-primary font-mono">{b.usedDays}</span>
                  </div>
                  <div>
                    <span className="text-text-muted block text-[10px] uppercase">Reserved</span>
                    <span className="font-semibold text-warning font-mono">{b.pendingDays}</span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
