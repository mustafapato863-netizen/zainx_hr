import React, { useState } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import {
  RequisitionsGrid,
  CandidateWorkspace,
  InterviewCalendar,
  JobWorkspace,
  ApplicationWorkspace,
} from '@zainx/recruitment';
import { JobRequisition, Candidate } from '@zainx/contracts';

export const recruitmentRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/recruitment',
  component: RecruitmentPage,
});

function RecruitmentPage() {
  const [activeTab, setActiveTab] = useState<'requisitions' | 'candidates' | 'interviews'>('requisitions');
  const [selectedRequisition, setSelectedRequisition] = useState<JobRequisition | null>(null);
  const [selectedApplicationId, setSelectedApplicationId] = useState<string | null>(null);

  const handleSelectRequisition = (req: JobRequisition) => {
    setSelectedRequisition(req);
    setSelectedApplicationId(null);
  };

  return (
    <div className="p-8 space-y-6 max-w-7xl mx-auto" data-testid="recruitment-page">
      {/* Top Header & Subnav */}
      <div className="flex items-center justify-between border-b border-slate-200 pb-4">
        <div>
          <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">
            Recruitment & Applicant Tracking (ATS)
          </h1>
          <p className="text-sm text-slate-500">
            Phase 5 enterprise recruitment engine with cross-boundary hire handoff, confidential scorecards, and salary masking.
          </p>
        </div>

        <div className="flex bg-slate-100 p-1 rounded-lg border border-slate-200">
          <button
            className={`px-4 py-1.5 text-sm font-medium rounded-md transition-all ${
              activeTab === 'requisitions'
                ? 'bg-white shadow-xs text-slate-900'
                : 'text-slate-600 hover:text-slate-900'
            }`}
            onClick={() => {
              setActiveTab('requisitions');
              setSelectedRequisition(null);
              setSelectedApplicationId(null);
            }}
            id="nav-tab-requisitions"
          >
            Job Requisitions
          </button>
          <button
            className={`px-4 py-1.5 text-sm font-medium rounded-md transition-all ${
              activeTab === 'candidates'
                ? 'bg-white shadow-xs text-slate-900'
                : 'text-slate-600 hover:text-slate-900'
            }`}
            onClick={() => {
              setActiveTab('candidates');
              setSelectedRequisition(null);
              setSelectedApplicationId(null);
            }}
            id="nav-tab-candidates"
          >
            Candidate Intake & Duplicate Detection
          </button>
          <button
            className={`px-4 py-1.5 text-sm font-medium rounded-md transition-all ${
              activeTab === 'interviews'
                ? 'bg-white shadow-xs text-slate-900'
                : 'text-slate-600 hover:text-slate-900'
            }`}
            onClick={() => {
              setActiveTab('interviews');
              setSelectedRequisition(null);
              setSelectedApplicationId(null);
            }}
            id="nav-tab-interviews"
          >
            Interview Schedule
          </button>
        </div>
      </div>

      {/* Main Content Area */}
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
          onApplyForJob={(candidate: Candidate) => {
            setActiveTab('requisitions');
          }}
        />
      )}

      {activeTab === 'interviews' && <InterviewCalendar />}
    </div>
  );
}
