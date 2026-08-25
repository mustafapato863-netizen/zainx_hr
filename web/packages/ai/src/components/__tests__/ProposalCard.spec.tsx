import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import React from 'react';
import { axe } from 'vitest-axe';
import * as matchers from 'vitest-axe/matchers';
import { ProposalCard } from '../ProposalCard';
import type { AiActionProposalDto } from '@zainx/contracts';

expect.extend(matchers);

// Mock react-i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: {
      language: 'en',
      dir: () => 'ltr',
      changeLanguage: () => Promise.resolve()
    }
  })
}));

const sampleProposal: AiActionProposalDto = {
  id: '33333333-3333-3333-3333-333333333333',
  conversationId: '11111111-1111-1111-1111-111111111111',
  actionCode: 'people.assignment.change_location',
  targetEntityType: 'Employee',
  targetEntityId: '99999999-9999-9999-9999-999999999999',
  expectedRowVersion: 1,
  effectiveDateUtc: '2026-09-01T00:00:00.000Z',
  beforeSnapshotJson: JSON.stringify({ locationNameEn: 'Alexandria Branch' }),
  afterSnapshotJson: JSON.stringify({ locationNameEn: 'Cairo HQ', locationId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' }),
  impactSummaryJson: JSON.stringify({ description: 'Creates new assignment version without backdating past finalized payroll.' }),
  proposalHash: 'abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789',
  status: 'ReadyForConfirmation',
  expiresAtUtc: '2026-09-01T00:15:00.000Z',
  confirmedAtUtc: null,
  completedAtUtc: null,
  errorMessage: null,
  createdAtUtc: '2026-09-01T00:00:00.000Z'
};

describe('ZainX Workforce — Phase 7B ProposalCard Component & WCAG AA Unit Tests', () => {

  it('ProposalCard renders full proposal details and passes Axe WCAG AA audit (0 violations)', async () => {
    let container: HTMLElement;
    await act(async () => {
      const rendered = render(
        <ProposalCard
          proposal={sampleProposal}
          onConfirm={vi.fn()}
          onCancel={vi.fn()}
        />
      );
      container = rendered.container;
    });

    // Verify fields
    expect(screen.getByTestId('ai-action-proposal-card')).toBeDefined();
    expect(screen.getByTestId('proposal-action-code').textContent).toContain('people.assignment.change_location');
    expect(screen.getByTestId('proposal-status-ready')).toBeDefined();
    expect(screen.getByTestId('proposal-effective-date')).toBeDefined();
    expect(screen.getByTestId('proposal-before-snapshot')).toBeDefined();
    expect(screen.getByTestId('proposal-after-snapshot')).toBeDefined();
    expect(screen.getByTestId('proposal-impact-summary')).toBeDefined();

    // Verify accessibility audit
    const results = await axe(container!);
    expect(results).toHaveNoViolations();
  });

  it('Clicking Confirm Action reveals reason input and invokes onConfirm handler', async () => {
    const onConfirmMock = vi.fn().mockResolvedValue({
      success: true,
      proposalId: sampleProposal.id,
      status: 'Completed',
      errorMessage: null,
      newRowVersion: 2,
      resultDataJson: '{}'
    });

    render(
      <ProposalCard
        proposal={sampleProposal}
        onConfirm={onConfirmMock}
      />
    );

    // Expand confirm drawer
    const expandBtn = screen.getByTestId('proposal-confirm-expand-button');
    act(() => {
      fireEvent.click(expandBtn);
    });

    // Reason input should appear
    const reasonInput = screen.getByTestId('proposal-confirm-reason-input');
    act(() => {
      fireEvent.change(reasonInput, { target: { value: 'Approved by HR Director' } });
    });

    // Click confirm execute
    const confirmBtn = screen.getByTestId('proposal-confirm-button');
    await act(async () => {
      fireEvent.click(confirmBtn);
    });

    expect(onConfirmMock).toHaveBeenCalledWith(sampleProposal.id, 'Approved by HR Director');
  });

  it('Clicking Cancel reveals reason input and invokes onCancel handler', async () => {
    const onCancelMock = vi.fn().mockResolvedValue({
      ...sampleProposal,
      status: 'Cancelled'
    });

    render(
      <ProposalCard
        proposal={sampleProposal}
        onCancel={onCancelMock}
      />
    );

    // Expand cancel drawer
    const expandBtn = screen.getByTestId('proposal-cancel-expand-button');
    act(() => {
      fireEvent.click(expandBtn);
    });

    // Reason input should appear
    const reasonInput = screen.getByTestId('proposal-cancel-reason-input');
    act(() => {
      fireEvent.change(reasonInput, { target: { value: 'Candidate declined position' } });
    });

    // Click confirm cancel
    const cancelBtn = screen.getByTestId('proposal-cancel-button');
    await act(async () => {
      fireEvent.click(cancelBtn);
    });

    expect(onCancelMock).toHaveBeenCalledWith(sampleProposal.id, 'Candidate declined position');
  });

  it('Renders Stale status and 409 conflict alert banner when proposal is stale', async () => {
    const staleProposal: AiActionProposalDto = {
      ...sampleProposal,
      status: 'Stale'
    };

    let container: HTMLElement;
    await act(async () => {
      const rendered = render(
        <ProposalCard
          proposal={staleProposal}
        />
      );
      container = rendered.container;
    });

    expect(screen.getByTestId('proposal-status-stale')).toBeDefined();
    expect(screen.getByTestId('proposal-stale-alert')).toBeDefined();

    const results = await axe(container!);
    expect(results).toHaveNoViolations();
  });

  it('Renders Expired status when proposal has passed expiry time', async () => {
    const expiredProposal: AiActionProposalDto = {
      ...sampleProposal,
      status: 'Expired'
    };

    render(
      <ProposalCard
        proposal={expiredProposal}
      />
    );

    expect(screen.getByTestId('proposal-status-expired')).toBeDefined();
  });

});

