import React, { useState } from 'react';
import {
  Card,
  Badge,
  Button,
  Input,
  Alert,
  Spinner,
} from '@zainx/design-system';
import {
  useGetOffersForApplication,
  useCreateOffer,
  useSubmitOfferForApproval,
  useApproveOffer,
  useIssueOffer,
  useAcceptOffer,
  OfferDetailDto,
} from '@zainx/contracts';

interface OfferWorkspaceProps {
  applicationId: string;
  candidateId: string;
  onOfferAccepted?: () => void;
}

export const OfferWorkspace: React.FC<OfferWorkspaceProps> = ({
  applicationId,
  candidateId,
  onOfferAccepted,
}) => {
  const { data: offers = [], isLoading, refetch } = useGetOffersForApplication(
    { applicationId },
    { query: { enabled: !!applicationId } }
  );

  const createOfferMutation = useCreateOffer();
  const submitApprovalMutation = useSubmitOfferForApproval();
  const approveOfferMutation = useApproveOffer();
  const issueOfferMutation = useIssueOffer();
  const acceptOfferMutation = useAcceptOffer();

  const [isDrafting, setIsDrafting] = useState(false);
  const [formData, setFormData] = useState({
    titleEn: 'Senior Backend Engineer',
    titleAr: 'مهندس برمجيات أول',
    proposedStartDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    baseSalaryMonthly: 65000,
    currency: 'EGP',
    conditionsNote: 'Offer valid for 7 days upon issue. Subject to background reference check.',
  });

  const [error, setError] = useState<string | null>(null);

  const handleCreateDraft = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await createOfferMutation.mutateAsync({
        data: {
          applicationId,
          candidateId,
          titleEn: formData.titleEn,
          titleAr: formData.titleAr,
          proposedStartDate: formData.proposedStartDate,
          baseSalaryMonthly: Number(formData.baseSalaryMonthly),
          currency: formData.currency,
          allowancesJson: JSON.stringify([
            { nameEn: 'Transportation Allowance', amount: 5000 },
            { nameEn: 'Mobile Allowance', amount: 1500 },
          ]),
          conditionsNote: formData.conditionsNote,
          expiryDate: null,
          offerDocumentId: null,
        },
      });
      setIsDrafting(false);
      refetch();
    } catch (err: any) {
      setError(err?.response?.data?.detail || err.message || 'Failed to create offer');
    }
  };

  const handleAction = async (action: string, offer: OfferDetailDto) => {
    setError(null);
    try {
      if (action === 'submit') {
        await submitApprovalMutation.mutateAsync({
          id: offer.id,
          data: { rowVersion: Number(offer.rowVersion) },
        });
      } else if (action === 'approve') {
        await approveOfferMutation.mutateAsync({
          id: offer.id,
          data: { rowVersion: Number(offer.rowVersion) },
        });
      } else if (action === 'issue') {
        await issueOfferMutation.mutateAsync({
          id: offer.id,
          data: { rowVersion: Number(offer.rowVersion) },
        });
      } else if (action === 'accept') {
        await acceptOfferMutation.mutateAsync({
          id: offer.id,
          data: { rowVersion: Number(offer.rowVersion) },
        });
        onOfferAccepted?.();
      }
      refetch();
    } catch (err: any) {
      setError(err?.response?.data?.detail || err.message || `Action ${action} failed`);
    }
  };

  const getStatusBadge = (status: any) => {
    const s = String(status);
    if (s === 'Accepted' || s === '4') return <Badge variant="success">Accepted</Badge>;
    if (s === 'Issued' || s === '3') return <Badge variant="primary">Issued to Candidate</Badge>;
    if (s === 'Approved' || s === '2') return <Badge variant="info">Approved</Badge>;
    if (s === 'PendingApproval' || s === '1') return <Badge variant="warning">Pending Approval</Badge>;
    if (s === 'Draft' || s === '0') return <Badge variant="neutral">Draft</Badge>;
    return <Badge variant="neutral">{s}</Badge>;
  };

  const isStatus = (currentStatus: any, expected: string, numericVal: number) => {
    const s = String(currentStatus);
    return s === expected || s === String(numericVal);
  };

  return (
    <div className="space-y-4" data-testid="offer-workspace">
      {error && (
        <Alert variant="danger" onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <div className="flex items-center justify-between">
        <div>
          <h4 className="text-base font-semibold">Offer Packages & Compensation</h4>
          <p className="text-xs text-muted-foreground">
            Author and version formal offer terms, request compensation sign-off, and track acceptance.
          </p>
        </div>
        {!isDrafting && (
          <Button
            size="sm"
            variant="primary"
            onClick={() => setIsDrafting(true)}
            id="btn-new-offer-version"
          >
            + Create Offer Version
          </Button>
        )}
      </div>

      {isDrafting && (
        <Card className="p-4 bg-muted/20 border-primary/40 space-y-4">
          <div className="flex items-center justify-between border-b border-border pb-2">
            <span className="font-semibold text-sm">Author Offer Terms</span>
            <Button size="sm" variant="ghost" onClick={() => setIsDrafting(false)}>
              ✕
            </Button>
          </div>

          <form onSubmit={handleCreateDraft} className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  Position Title (EN)
                </label>
                <Input
                  value={formData.titleEn}
                  onChange={(e) => setFormData({ ...formData, titleEn: e.target.value })}
                  id="input-offer-title-en"
                  required
                />
              </div>
              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  Position Title (AR)
                </label>
                <Input
                  dir="rtl"
                  value={formData.titleAr}
                  onChange={(e) => setFormData({ ...formData, titleAr: e.target.value })}
                  id="input-offer-title-ar"
                  required
                />
              </div>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  Monthly Base Salary
                </label>
                <Input
                  type="number"
                  value={formData.baseSalaryMonthly}
                  onChange={(e) =>
                    setFormData({ ...formData, baseSalaryMonthly: Number(e.target.value) })
                  }
                  id="input-offer-salary"
                  required
                />
              </div>
              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  Currency
                </label>
                <Input
                  value={formData.currency}
                  onChange={(e) => setFormData({ ...formData, currency: e.target.value })}
                  id="input-offer-currency"
                  required
                />
              </div>
              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  Proposed Start Date
                </label>
                <Input
                  type="date"
                  value={formData.proposedStartDate}
                  onChange={(e) =>
                    setFormData({ ...formData, proposedStartDate: e.target.value })
                  }
                  id="input-offer-start-date"
                  required
                />
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-2">
              <Button size="sm" variant="outline" type="button" onClick={() => setIsDrafting(false)}>
                Cancel
              </Button>
              <Button
                size="sm"
                variant="primary"
                type="submit"
                disabled={createOfferMutation.isPending}
                id="btn-save-draft-offer"
              >
                {createOfferMutation.isPending ? 'Saving...' : 'Save Draft Offer'}
              </Button>
            </div>
          </form>
        </Card>
      )}

      {isLoading ? (
        <div className="flex justify-center p-6">
          <Spinner />
        </div>
      ) : (
        <div className="space-y-3">
          {offers.map((off) => (
            <Card key={off.id} className="p-4 border-border space-y-3" id={`offer-card-${off.id}`}>
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-2">
                  <span className="font-bold text-sm">
                    {off.titleEn} ({off.titleAr})
                  </span>
                  <Badge variant="outline" size="sm">
                    v{off.offerVersionNumber}
                  </Badge>
                  {getStatusBadge(off.status)}
                </div>

                <div className="flex items-center gap-2">
                  {isStatus(off.status, 'Draft', 0) && (
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => handleAction('submit', off)}
                      id={`btn-submit-offer-${off.id}`}
                    >
                      Submit for Approval
                    </Button>
                  )}
                  {isStatus(off.status, 'PendingApproval', 1) && (
                    <Button
                      size="sm"
                      variant="primary"
                      onClick={() => handleAction('approve', off)}
                      id={`btn-approve-offer-${off.id}`}
                    >
                      Approve Offer
                    </Button>
                  )}
                  {isStatus(off.status, 'Approved', 2) && (
                    <Button
                      size="sm"
                      variant="primary"
                      onClick={() => handleAction('issue', off)}
                      id={`btn-issue-offer-${off.id}`}
                    >
                      Issue to Candidate
                    </Button>
                  )}
                  {isStatus(off.status, 'Issued', 3) && (
                    <Button
                      size="sm"
                      variant="primary"
                      onClick={() => handleAction('accept', off)}
                      id={`btn-accept-offer-${off.id}`}
                    >
                      Mark as Accepted
                    </Button>
                  )}
                </div>
              </div>

              <div className="grid grid-cols-3 gap-4 text-xs bg-muted/30 p-3 rounded-lg">
                <div>
                  <span className="text-muted-foreground block">Monthly Base Salary</span>
                  <span className="font-mono text-sm font-semibold">
                    {off.baseSalaryMonthly === null || off.baseSalaryMonthly === undefined ? (
                      <span className="text-muted-foreground">•••••• {off.currency}</span>
                    ) : (
                      `${Number(off.baseSalaryMonthly).toLocaleString()} ${off.currency}`
                    )}
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Proposed Start Date</span>
                  <span className="font-medium">{off.proposedStartDate}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Created At</span>
                  <span className="font-medium">
                    {off.createdAtUtc ? new Date(off.createdAtUtc).toLocaleDateString() : 'N/A'}
                  </span>
                </div>
              </div>

              {off.conditionsNote && (
                <div className="text-xs text-muted-foreground italic">
                  Note: {off.conditionsNote}
                </div>
              )}
            </Card>
          ))}

          {offers.length === 0 && !isDrafting && (
            <div className="text-center p-8 border border-dashed border-border rounded-xl text-xs text-muted-foreground">
              No offer versions drafted for this candidate yet. Click "Create Offer Version" to begin.
            </div>
          )}
        </div>
      )}
    </div>
  );
};
