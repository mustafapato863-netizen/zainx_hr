import type { Meta, StoryObj } from "@storybook/react"
import { Badge } from "../components/Badge/Badge"
import { Tag } from "../components/Tag/Tag"
import { Alert, Banner } from "../components/Alert/Alert"
import { Spinner } from "../components/Spinner/Spinner"
import { Skeleton } from "../components/Skeleton/Skeleton"
import { JobStatus } from "../components/JobStatus/JobStatus"
import { EmptyState, NoResults } from "../components/EmptyState/EmptyState"
import { ErrorState } from "../components/ErrorState/ErrorState"
import { AccessDenied } from "../components/AccessDenied/AccessDenied"
import { ReadOnlyState, LockedState, FinalizedState } from "../components/StatusTreatment/StatusTreatment"
import { Button } from "../components/Button/Button"

const meta: Meta = {
  title: "Feedback/States & Status",
  tags: ["autodocs"],
}

export default meta

export const BadgesAndTags: StoryObj = {
  render: () => (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-2 items-center">
        <Badge variant="neutral">Draft</Badge>
        <Badge variant="primary">In Review</Badge>
        <Badge variant="success" dot>Approved</Badge>
        <Badge variant="warning" dot>Pending Verification</Badge>
        <Badge variant="danger">Rejected</Badge>
        <Badge variant="info">Automated</Badge>
      </div>

      <div className="flex flex-wrap gap-2 items-center">
        <Tag onRemove={() => {}}>Engineering</Tag>
        <Tag onRemove={() => {}} variant="primary">Full-Time</Tag>
        <Tag onRemove={() => {}} variant="success">Active Payroll</Tag>
        <Tag onRemove={() => {}} variant="warning">Probationary</Tag>
        <Tag onRemove={() => {}} variant="danger">Terminated</Tag>
      </div>
    </div>
  ),
}

export const AlertsAndBanners: StoryObj = {
  render: () => (
    <div className="space-y-4 max-w-xl">
      <Banner variant="warning" onClose={() => {}}>
        Statutory employer contributions will adjust automatically on the 1st of next month.
      </Banner>

      <Alert variant="info" title="Direct Deposit Notice" onClose={() => {}}>
        IBAN validation service is operating under standard SLA.
      </Alert>

      <Alert variant="danger" title="Validation Failed">
        Employee national ID format does not conform to Saudi Government standard.
      </Alert>

      <Alert variant="success" title="Payroll Run Finalized">
        Batch 2026-08 authorized by Chief Financial Officer.
      </Alert>
    </div>
  ),
}

export const LoadingSemantics: StoryObj = {
  render: () => (
    <div className="space-y-6 max-w-lg">
      <div>
        <div className="text-xs font-semibold uppercase text-text-tertiary mb-2">1. Local Spinners (Buttons / Micro actions)</div>
        <div className="flex items-center gap-3">
          <Spinner size="xs" />
          <Spinner size="sm" />
          <Spinner size="md" />
          <Spinner size="lg" />
        </div>
      </div>

      <div>
        <div className="text-xs font-semibold uppercase text-text-tertiary mb-2">2. Skeletons (Layout content loading)</div>
        <div className="space-y-2">
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-10 w-full" />
        </div>
      </div>

      <div>
        <div className="text-xs font-semibold uppercase text-text-tertiary mb-2">3. JobStatus (Long-running server operations)</div>
        <JobStatus
          title="Monthly Payroll Batch Calculation"
          status="running"
          progress={68}
          message="Processing tax and deduction formulas for 1,420 employees..."
        />
      </div>
    </div>
  ),
}

export const ViewStates: StoryObj = {
  render: () => (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      <EmptyState
        title="No Leave Requests"
        description="There are currently no pending leave requests for this department."
        action={<Button size="xs" variant="primary">Create Request</Button>}
      />
      <NoResults onClearFilters={() => {}} />
      <ErrorState
        title="Calculation Engine Timeout"
        description="The backend computation service did not respond within the allocated timeout."
        onRetry={() => {}}
      />
      <AccessDenied
        title="Access Denied"
        description="You do not have the required capability to finalize this payroll batch."
        requiredPermission="payroll.run.finalize"
        correlationId="req_trace_9a82e7ec014a309c1"
        onGoBack={() => {}}
        onRequestAccess={() => {}}
      />
    </div>
  ),
}

export const StatusTreatments: StoryObj = {
  render: () => (
    <div className="flex flex-wrap gap-3 items-center">
      <ReadOnlyState reason="View-Only Permission" />
      <LockedState lockedBy="Khalid Al-Mansoor" />
      <FinalizedState date="2026-08-23" />
    </div>
  ),
}
