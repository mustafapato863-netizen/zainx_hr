import React from 'react';
import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { axe } from 'vitest-axe';
import * as matchers from 'vitest-axe/matchers';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RequisitionsGrid } from '../components/RequisitionsGrid';
import { CandidateWorkspace } from '../components/CandidateWorkspace';
import { PipelineBoard } from '../components/PipelineBoard';
import { InterviewCalendar } from '../components/InterviewCalendar';
import { ScorecardDialog } from '../components/ScorecardDialog';
import { OfferWorkspace } from '../components/OfferWorkspace';

expect.extend(matchers);

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
    mutations: { retry: false }
  }
});

const renderWithQuery = (ui: React.ReactElement) => {
  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  );
};

describe('Phase 5 Recruitment Accessibility Verification (Axe WCAG AA)', () => {
  it('RequisitionsGrid passes axe accessibility check with 0 critical/serious violations', async () => {
    const { container } = renderWithQuery(
      <RequisitionsGrid
        requisitions={[
          {
            id: '11111111-1111-1111-1111-111111111111',
            tenantId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
            legalEntityId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
            organizationUnitId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
            hiringManagerId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
            recruiterId: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
            requisitionNumber: 'REQ-2026-001',
            titleEn: 'Senior Backend Engineer',
            titleAr: 'مهندس برمجيات أول',
            openingsCount: 2,
            employmentType: 'FullTime',
            pipelineId: 'ffffffff-ffff-ffff-ffff-ffffffffffff',
            pipelineVersion: 1,
            status: 4,
            statusName: 'Open',
            createdAtUtc: '2026-08-24T10:00:00Z',
            rowVersion: 1
          }
        ]}
      />
    );
    const results = await axe(container, {
      rules: {
        'aria-required-children': { enabled: false }
      }
    });
    const seriousOrCritical = results.violations.filter(
      v => v.impact === 'critical' || v.impact === 'serious'
    );
    expect(seriousOrCritical).toEqual([]);
  });

  it('CandidateWorkspace passes axe accessibility check', async () => {
    const { container } = renderWithQuery(
      <CandidateWorkspace
        candidates={[
          {
            id: '22222222-2222-2222-2222-222222222222',
            tenantId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
            firstNameEn: 'Ahmed',
            lastNameEn: 'Hassan',
            firstNameAr: 'أحمد',
            lastNameAr: 'حسن',
            email: 'ahmed.hassan@enterprise.com',
            phoneNumber: '+201012345678',
            location: 'Cairo, Egypt',
            headline: 'Senior Cloud Architect',
            source: 'LinkedIn',
            skills: ['Go', '.NET', 'PostgreSQL'],
            createdAtUtc: '2026-08-24T10:00:00Z',
            rowVersion: 1
          }
        ]}
      />
    );
    const results = await axe(container);
    const seriousOrCritical = results.violations.filter(
      v => v.impact === 'critical' || v.impact === 'serious'
    );
    expect(seriousOrCritical).toEqual([]);
  });

  it('PipelineBoard passes axe accessibility check', async () => {
    const { container } = renderWithQuery(
      <PipelineBoard
        requisitionId="11111111-1111-1111-1111-111111111111"
        stages={[
          { id: 's1', stageOrder: 1, code: 'APPLIED', nameEn: 'Applied', nameAr: 'تم التقديم', stageKind: 1 },
          { id: 's2', stageOrder: 2, code: 'SCREENING', nameEn: 'Screening', nameAr: 'فرز أولي', stageKind: 2 }
        ]}
        applications={[
          {
            id: 'app1',
            candidateId: 'c1',
            candidateNameEn: 'Sarah Al-Mansoor',
            candidateNameAr: 'سارة المنصور',
            candidateEmail: 'sarah@test.com',
            candidatePhone: '+966501234567',
            currentStageId: 's1',
            status: 1,
            appliedAtUtc: '2026-08-24T10:00:00Z',
            rowVersion: 1
          }
        ]}
      />
    );
    const results = await axe(container);
    const seriousOrCritical = results.violations.filter(
      v => v.impact === 'critical' || v.impact === 'serious'
    );
    expect(seriousOrCritical).toEqual([]);
  });

  it('InterviewCalendar passes axe accessibility check', async () => {
    const { container } = renderWithQuery(
      <InterviewCalendar
        interviews={[
          {
            id: 'i1',
            title: 'Technical Panel',
            interviewType: 2,
            scheduledStartUtc: '2026-08-25T10:00:00Z',
            scheduledEndUtc: '2026-08-25T11:00:00Z',
            timezone: 'Africa/Cairo',
            status: 1,
            candidateName: 'Ahmed Hassan'
          }
        ]}
      />
    );
    const results = await axe(container);
    const seriousOrCritical = results.violations.filter(
      v => v.impact === 'critical' || v.impact === 'serious'
    );
    expect(seriousOrCritical).toEqual([]);
  });

  it('ScorecardDialog passes axe accessibility check', async () => {
    const { container } = renderWithQuery(
      <ScorecardDialog
        isOpen={true}
        onClose={() => {}}
        onSubmit={() => {}}
        interviewTitle="Senior Systems Architect Technical Panel"
        candidateName="Ahmed Hassan"
      />
    );
    const results = await axe(container);
    const seriousOrCritical = results.violations.filter(
      v => v.impact === 'critical' || v.impact === 'serious'
    );
    expect(seriousOrCritical).toEqual([]);
  });

  it('OfferWorkspace passes axe accessibility check', async () => {
    const { container } = renderWithQuery(
      <OfferWorkspace
        offer={{
          id: 'o1',
          applicationId: 'app1',
          candidateId: 'c1',
          candidateName: 'Ahmed Hassan',
          offerVersionNumber: 1,
          titleEn: 'Senior Backend Engineer',
          titleAr: 'مهندس برمجيات أول',
          proposedStartDate: '2026-10-01',
          baseSalaryMonthly: 75000,
          currency: 'EGP',
          allowances: [{ name: 'Housing', amount: 15000 }],
          status: 4,
          statusName: 'Issued',
          rowVersion: 1
        }}
        hasSensitiveCompensationPermission={true}
      />
    );
    const results = await axe(container);
    const seriousOrCritical = results.violations.filter(
      v => v.impact === 'critical' || v.impact === 'serious'
    );
    expect(seriousOrCritical).toEqual([]);
  });
});
