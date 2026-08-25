import React, { Suspense, lazy, useState } from 'react';
import { createRoute } from '@tanstack/react-router';
import {
  useGetApiV1LeaveBalances,
  useGetApiV1LeaveRequests,
  useGetApiV1LeaveTypes,
  usePostApiV1LeaveRequests,
  usePostApiV1LeaveRequestsIdApprove,
  usePostApiV1LeaveRequestsIdReject,
} from '@zainx/contracts';
import { Route as rootRoute } from './__root';

const LeaveBalancesSummary = lazy(() => import('@zainx/leave').then((m) => ({ default: m.LeaveBalancesSummary })));
const LeaveRequestsGrid = lazy(() => import('@zainx/leave').then((m) => ({ default: m.LeaveRequestsGrid })));
const LeaveRequestModal = lazy(() => import('@zainx/leave').then((m) => ({ default: m.LeaveRequestModal })));
const LeaveCalendar = lazy(() => import('@zainx/leave').then((m) => ({ default: m.LeaveCalendar })));

export function LeaveComponent() {
  const [isRequestModalOpen, setIsRequestModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState<'requests' | 'calendar'>('requests');
  const balancesQuery = useGetApiV1LeaveBalances();
  const requestsQuery = useGetApiV1LeaveRequests({ page: 1, pageSize: 100 });
  const leaveTypesQuery = useGetApiV1LeaveTypes();
  const createMutation = usePostApiV1LeaveRequests();
  const approveMutation = usePostApiV1LeaveRequestsIdApprove();
  const rejectMutation = usePostApiV1LeaveRequestsIdReject();

  const balances = balancesQuery.data ?? [];
  const requests = requestsQuery.data?.items ?? [];
  const leaveTypes = leaveTypesQuery.data ?? [];
  const activeEmploymentId = requests[0]?.employmentId ?? balances[0]?.employmentId ?? '';
  const isLoading = balancesQuery.isLoading || requestsQuery.isLoading || leaveTypesQuery.isLoading;
  const isError = balancesQuery.isError || requestsQuery.isError || leaveTypesQuery.isError;
  const refresh = async () => { await Promise.all([balancesQuery.refetch(), requestsQuery.refetch(), leaveTypesQuery.refetch()]); };

  const handleSubmitLeaveRequest = async (leaveTypeId: string, startDate: string, endDate: string, durationDays: number, reason: string) => {
    if (!activeEmploymentId) throw new Error('An active employment context is required before submitting leave.');
    await createMutation.mutateAsync({ data: { employmentId: activeEmploymentId, leaveTypeId, startDate, endDate, durationDays, reason } });
    setIsRequestModalOpen(false);
    await refresh();
  };

  const handleApproveRequest = async (request: (typeof requests)[number]) => {
    await approveMutation.mutateAsync({ id: request.id, data: { rowVersion: request.rowVersion } });
    await refresh();
  };

  const handleRejectRequest = async (request: (typeof requests)[number]) => {
    await rejectMutation.mutateAsync({ id: request.id, data: { reason: 'Rejected from the leave operations workspace.', rowVersion: request.rowVersion } });
    await refresh();
  };

  return (
    <div className="mx-auto w-full max-w-[1440px] space-y-6" data-testid="leave-route-page">
      {!isLoading && !isError && !activeEmploymentId && <div role="status" className="rounded-lg border border-border-default bg-surface px-4 py-3 text-sm text-text-secondary">No active employment context is available for this workspace. Leave records will appear when the authorized context is selected.</div>}
      <Suspense fallback={<div className="rounded-lg border border-border-default bg-surface p-8 text-sm text-text-secondary">Loading leave workspace…</div>}>
        <LeaveBalancesSummary balances={balances} isLoading={isLoading} onRequestLeave={() => activeEmploymentId && setIsRequestModalOpen(true)} />
        <div className="flex items-center gap-2 border-b border-border-default pb-2">
          <button type="button" onClick={() => setActiveTab('requests')} className={`rounded-md px-3 py-2 text-xs font-semibold transition-colors ${activeTab === 'requests' ? 'bg-primary text-white shadow-xs' : 'text-text-secondary hover:bg-surface-subtle'}`}>Requests list</button>
          <button type="button" onClick={() => setActiveTab('calendar')} className={`rounded-md px-3 py-2 text-xs font-semibold transition-colors ${activeTab === 'calendar' ? 'bg-primary text-white shadow-xs' : 'text-text-secondary hover:bg-surface-subtle'}`}>Team calendar</button>
        </div>
        {activeTab === 'requests' ? <LeaveRequestsGrid requests={requests} isLoading={isLoading} isError={isError} onRefresh={refresh} onRequestLeave={() => activeEmploymentId && setIsRequestModalOpen(true)} onApproveRequest={handleApproveRequest} onRejectRequest={handleRejectRequest} /> : <LeaveCalendar requests={requests} />}
        <LeaveRequestModal isOpen={isRequestModalOpen} employmentId={activeEmploymentId} leaveTypes={leaveTypes} balances={balances} onClose={() => setIsRequestModalOpen(false)} onSubmitRequest={handleSubmitLeaveRequest} />
      </Suspense>
    </div>
  );
}

export const leaveRoute = createRoute({ getParentRoute: () => rootRoute, path: '/leave', component: LeaveComponent });
