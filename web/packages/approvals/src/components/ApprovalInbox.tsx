import React, { useState, useMemo } from 'react';
import {
  PageHeader,
  Badge,
  Button,
  EmptyState,
  NoResults,
  ErrorState,
  Skeleton
} from '@zainx/design-system';
import { ApprovalInboxItemDto } from '@zainx/contracts';

export const ApprovalStatus = {
  Pending: 'Pending',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
} as const;

export interface ApprovalInboxProps {
  items?: ApprovalInboxItemDto[];
  isLoading?: boolean;
  isError?: boolean;
  onRefresh?: () => void;
  onSelectDecision?: (item: ApprovalInboxItemDto, action: 'approve' | 'reject') => void;
  onBulkApprove?: (items: ApprovalInboxItemDto[]) => void;
}

export const ApprovalInbox: React.FC<ApprovalInboxProps> = ({
  items = [],
  isLoading = false,
  isError = false,
  onRefresh,
  onSelectDecision,
  onBulkApprove
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [domainFilter, setDomainFilter] = useState('all');
  const [selectedIds, setSelectedIds] = useState<string[]>([]);

  const filteredItems = useMemo(() => {
    return items.filter((item) => {
      const summary = (item as any).summary || item.title || '';
      const subjectName = (item as any).subjectEmployeeNameEn || '';
      const domain = (item as any).domain || item.sourceModule || '';

      const matchesSearch =
        !searchTerm ||
        item.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
        summary.toLowerCase().includes(searchTerm.toLowerCase()) ||
        subjectName.toLowerCase().includes(searchTerm.toLowerCase());
      const matchesDomain =
        domainFilter === 'all' || domain.toLowerCase() === domainFilter.toLowerCase();
      return matchesSearch && matchesDomain;
    });
  }, [items, searchTerm, domainFilter]);

  const toggleSelect = (id: string) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id]
    );
  };

  const getDomainBadge = (domain: string) => {
    switch (domain.toLowerCase()) {
      case 'leave':
        return <Badge variant="primary">Leave Request</Badge>;
      case 'attendance':
        return <Badge variant="warning">Attendance Adj.</Badge>;
      case 'recruitment':
        return <Badge variant="info">Recruitment</Badge>;
      default:
        return <Badge variant="neutral">{domain}</Badge>;
    }
  };

  return (
    <div className="flex flex-col gap-4 p-6 bg-surface-primary rounded-xl border border-border-primary shadow-sm" data-testid="approval-inbox">
      <PageHeader
        title="Universal Approval Inbox (My Work)"
        subtitle="Shared enterprise decision queue for Leave, Attendance adjustments, and workflow steps"
        actions={
          <div className="flex items-center gap-3">
            {selectedIds.length > 0 && (
              <Button
                variant="primary"
                onClick={() => {
                  const selectedItems = items.filter((i) => selectedIds.includes(i.id));
                  onBulkApprove?.(selectedItems);
                }}
                aria-label="Approve all selected requests"
              >
                Approve Selected ({selectedIds.length})
              </Button>
            )}
            <Button
              variant="outline"
              onClick={onRefresh}
              aria-label="Refresh approval queue"
            >
              Refresh
            </Button>
          </div>
        }
      />

      {/* Filter and Search Bar */}
      <div className="flex flex-wrap items-center justify-between gap-3 bg-surface-secondary/40 p-3 rounded-lg border border-border-secondary">
        <div className="flex items-center gap-2 flex-1 max-w-md">
          <input
            type="search"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Search pending approval requests..."
            className="w-full text-xs rounded-lg border border-border-secondary bg-surface-primary px-3 py-2 text-text-primary focus:border-brand-primary focus:outline-none"
          />
        </div>

        <div className="flex items-center gap-1.5">
          <span className="text-xs font-semibold text-text-secondary mr-1">Domain:</span>
          {['all', 'leave', 'attendance', 'recruitment'].map((d) => (
            <button
              key={d}
              type="button"
              onClick={() => setDomainFilter(d)}
              className={`text-xs px-3 py-1.5 rounded-lg font-medium transition-colors ${
                domainFilter === d
                  ? 'bg-brand-primary text-white shadow-sm'
                  : 'bg-surface-secondary text-text-secondary hover:bg-surface-tertiary'
              }`}
            >
              {d === 'all' ? 'All Modules' : d.charAt(0).toUpperCase() + d.slice(1)}
            </button>
          ))}
        </div>
      </div>

      {/* Requests List */}
      {isLoading ? (
        <div className="space-y-3 p-4" data-testid="approval-inbox-skeleton">
          <Skeleton className="h-24 w-full rounded-xl" />
          <Skeleton className="h-24 w-full rounded-xl" />
          <Skeleton className="h-24 w-full rounded-xl" />
        </div>
      ) : isError ? (
        <ErrorState
          title="Failed to Load Approval Queue"
          description="An error occurred while connecting to the Universal Approval engine."
          onRetry={onRefresh}
        />
      ) : filteredItems.length === 0 ? (
        items.length === 0 ? (
          <EmptyState
            title="All Caught Up!"
            description="You have no pending approvals in your queue."
          />
        ) : (
          <NoResults
            onClearFilters={() => {
              setSearchTerm('');
              setDomainFilter('all');
            }}
          />
        )
      ) : (
        <div className="space-y-3">
          {filteredItems.map((item) => {
            const isChecked = selectedIds.includes(item.id);
            const domain = (item as any).domain || item.sourceModule || 'Workflow';
            const summary = (item as any).summary || item.title;

            return (
              <div
                key={item.id}
                className="p-5 rounded-xl border border-border-secondary bg-surface-secondary/30 hover:border-brand-primary/50 transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-4"
                data-testid={`approval-item-${item.id}`}
              >
                <div className="flex items-start gap-3">
                  <input
                    type="checkbox"
                    checked={isChecked}
                    onChange={() => toggleSelect(item.id)}
                    aria-label={`Select approval request: ${item.title}`}
                    className="mt-1 h-4 w-4 rounded border-border-secondary text-brand-primary focus:ring-brand-primary cursor-pointer"
                  />
                  <div>
                    <div className="flex items-center gap-2 flex-wrap">
                      {getDomainBadge(domain)}
                      <span className="text-sm font-bold text-text-primary">{item.title}</span>
                      <span className="text-xs px-2 py-0.5 rounded-full bg-surface-tertiary font-mono text-text-secondary">
                        Step {item.currentStepOrder} of {item.totalSteps}
                      </span>
                    </div>

                    <p className="text-xs text-text-secondary mt-1">{summary}</p>

                    <div className="mt-2 flex items-center gap-3 text-[11px] text-text-muted">
                      {(item as any).subjectEmployeeNameEn && (
                        <span>For: {(item as any).subjectEmployeeNameEn}</span>
                      )}
                      <span>Created: {new Date(item.createdAt).toLocaleDateString()}</span>
                    </div>
                  </div>
                </div>

                <div className="flex items-center gap-2 self-end sm:self-center">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => onSelectDecision?.(item, 'reject')}
                    aria-label={`Reject ${item.title}`}
                  >
                    Reject
                  </Button>
                  <Button
                    variant="primary"
                    size="sm"
                    onClick={() => onSelectDecision?.(item, 'approve')}
                    aria-label={`Approve ${item.title}`}
                  >
                    Approve
                  </Button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
