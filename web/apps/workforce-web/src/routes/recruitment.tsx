import React, { useState, Suspense, lazy } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import type { JobRequisition, Candidate } from '@zainx/contracts';

const RequisitionsGrid = lazy(() =>
  import('@zainx/recruitment').then((m) => ({ default: m.RequisitionsGrid }))
);
const CandidateWorkspace = lazy(() =>
  import('@zainx/recruitment').then((m) => ({ default: m.CandidateWorkspace }))
);
const InterviewCalendar = lazy(() =>
  import('@zainx/recruitment').then((m) => ({ default: m.InterviewCalendar }))
);
const JobWorkspace = lazy(() =>
  import('@zainx/recruitment').then((m) => ({ default: m.JobWorkspace }))
);

export const recruitmentRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/recruitment',
  component: RecruitmentPage,
});

function RecruitmentPage() {
  const [activeTab, setActiveTab] = useState<'requisitions' | 'candidates' | 'interviews'>('requisitions');
  const [selectedRequisition, setSelectedRequisition] = useState<JobRequisition | null>(null);

  const handleSelectRequisition = (req: JobRequisition) => {
    setSelectedRequisition(req);
  };

  return (
    <div className="mx-auto w-full max-w-[1440px] space-y-6" data-testid="recruitment-page">
      {/* Top Header & Subnav */}
      <div className="flex flex-col gap-4 border-b border-border-default pb-4 xl:flex-row xl:items-end xl:justify-between">
        <div>
          <h1 className="text-2xl font-extrabold tracking-tight text-text-primary">
            Recruitment & applicant tracking
          </h1>
          <p className="text-sm text-text-secondary">
            Requisitions, candidate pipeline, interviews, and controlled hiring handoff.
          </p>
        </div>

        <div className="grid w-full grid-cols-1 gap-1 rounded-lg border border-border-default bg-surface-subtle p-1 sm:grid-cols-3 xl:w-auto" role="tablist" aria-label="Recruitment views">
          <button
            className={`px-4 py-1.5 text-sm font-medium rounded-md transition-all ${
              activeTab === 'requisitions'
                ? 'bg-surface shadow-xs text-text-primary'
                : 'text-text-secondary hover:text-text-primary'
            }`}
            onClick={() => {
              setActiveTab('requisitions');
              setSelectedRequisition(null);
            }}
            id="nav-tab-requisitions"
            role="tab"
            aria-selected={activeTab === 'requisitions'}
          >
            Job Requisitions
          </button>
          <button
            className={`px-4 py-1.5 text-sm font-medium rounded-md transition-all ${
              activeTab === 'candidates'
                ? 'bg-surface shadow-xs text-text-primary'
                : 'text-text-secondary hover:text-text-primary'
            }`}
            onClick={() => {
              setActiveTab('candidates');
              setSelectedRequisition(null);
            }}
            id="nav-tab-candidates"
            role="tab"
            aria-selected={activeTab === 'candidates'}
          >
            Candidate Intake & Duplicate Detection
          </button>
          <button
            className={`px-4 py-1.5 text-sm font-medium rounded-md transition-all ${
              activeTab === 'interviews'
                ? 'bg-surface shadow-xs text-text-primary'
                : 'text-text-secondary hover:text-text-primary'
            }`}
            onClick={() => {
              setActiveTab('interviews');
              setSelectedRequisition(null);
            }}
            id="nav-tab-interviews"
            role="tab"
            aria-selected={activeTab === 'interviews'}
          >
            Interview Schedule
          </button>
        </div>
      </div>

      {/* Main Content Area */}
      <Suspense
        fallback={
          <div className="rounded-lg border border-border-default bg-surface p-8 text-sm text-text-secondary">
            Loading recruitment workspace…
          </div>
        }
      >
        {activeTab === 'requisitions' && (
          <>
            {selectedRequisition ? (
              <JobWorkspace
                requisitionId={selectedRequisition.id}
                onBack={() => setSelectedRequisition(null)}
              />
            ) : (
              <RequisitionsGrid onSelectRequisition={handleSelectRequisition} />
            )}
          </>
        )}

        {activeTab === 'candidates' && (
          <CandidateWorkspace
            onApplyForJob={(_candidate: Candidate) => {
              setActiveTab('requisitions');
            }}
          />
        )}

        {activeTab === 'interviews' && <InterviewCalendar />}
      </Suspense>
    </div>
  );
}
