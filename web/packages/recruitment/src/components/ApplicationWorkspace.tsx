import React, { useState } from 'react';
import {
  Card,
  Badge,
  Button,
  Spinner,
  Alert,
} from '@zainx/design-system';
import {
  useGetApplicationById,
  useGetInterviewsForApplication,
  useScheduleInterview,
  useGetScorecards,
  Interview,
} from '@zainx/contracts';
import { ScorecardDialog } from './ScorecardDialog';
import { OfferWorkspace } from './OfferWorkspace';
import { HireCandidateDialog } from './HireCandidateDialog';

interface ApplicationWorkspaceProps {
  applicationId: string;
  onBack?: () => void;
}

export const ApplicationWorkspace: React.FC<ApplicationWorkspaceProps> = ({
  applicationId,
  onBack,
}) => {
  const { data: detail, isLoading, error, refetch } = useGetApplicationById(applicationId);
  const { data: interviews = [], refetch: refetchInterviews } = useGetInterviewsForApplication(
    undefined,
    { query: { enabled: !!applicationId } }
  );

  const [activeTab, setActiveTab] = useState<'timeline' | 'interviews' | 'offers'>('timeline');
  const [selectedInterviewForScorecard, setSelectedInterviewForScorecard] = useState<string | null>(null);
  const [isHireOpen, setIsHireOpen] = useState(false);
  const [isScheduleOpen, setIsScheduleOpen] = useState(false);

  const scheduleMutation = useScheduleInterview();

  const [scheduleForm, setScheduleForm] = useState({
    title: 'Technical Deep-Dive Interview',
    interviewType: 'Technical',
    scheduledStartUtc: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString().slice(0, 16),
    scheduledEndUtc: new Date(Date.now() + 25 * 60 * 60 * 1000).toISOString().slice(0, 16),
    timezone: 'Africa/Cairo',
    locationOrMeetingUrl: 'https://meet.google.com/zainx-interview',
  });

  if (isLoading) {
    return (
      <div className="flex justify-center p-12">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error || !detail) {
    return (
      <Alert variant="danger">
        Failed to load application: {(error as any)?.message || 'Not found'}
      </Alert>
    );
  }

  const { application, candidate, requisition, stageHistory = [] } = detail;

  const isStatus = (currentStatus: any, expected: string, numericVal: number) => {
    const s = String(currentStatus);
    return s === expected || s === String(numericVal);
  };

  const handleScheduleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await scheduleMutation.mutateAsync({
        data: {
          applicationId: application.id,
          stageId: application.currentStageId || '00000000-0000-0000-0000-000000000000',
          title: scheduleForm.title,
          interviewType: 1 as any, // Technical
          scheduledStartUtc: new Date(scheduleForm.scheduledStartUtc).toISOString(),
          scheduledEndUtc: new Date(scheduleForm.scheduledEndUtc).toISOString(),
          timezone: scheduleForm.timezone,
          locationOrMeetingUrl: scheduleForm.locationOrMeetingUrl,
          interviewKitJson: null,
          participants: [
            {
              interviewerUserId: '11111111-1111-1111-1111-111111111111',
              role: 0,
              isRequired: true,
            },
          ],
        },
      });
      setIsScheduleOpen(false);
      refetchInterviews();
    } catch (err: any) {
      alert(`Scheduling failed: ${err?.response?.data?.detail || err.message}`);
    }
  };

  return (
    <div className="space-y-6" data-testid="application-workspace">
      {/* Header Banner */}
      <Card className="p-6 bg-card border-border">
        <div className="flex items-start justify-between">
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              {onBack && (
                <Button size="sm" variant="ghost" onClick={onBack}>
                  ← Back
                </Button>
              )}
              <h2 className="text-xl font-bold text-foreground">
                {candidate.firstNameEn} {candidate.lastNameEn}
              </h2>
              <span className="text-sm text-muted-foreground">
                ({candidate.firstNameAr} {candidate.lastNameAr})
              </span>
              <Badge
                variant={
                  isStatus(application.status, 'Hired', 3)
                    ? 'success'
                    : isStatus(application.status, 'Rejected', 2)
                    ? 'danger'
                    : 'primary'
                }
              >
                {isStatus(application.status, 'Hired', 3)
                  ? 'Hired'
                  : isStatus(application.status, 'Rejected', 2)
                  ? 'Rejected'
                  : 'Active'}
              </Badge>
              <Badge variant="outline" size="sm">
                v{application.rowVersion}
              </Badge>
            </div>

            <p className="text-sm text-muted-foreground">
              Applied for: <span className="font-semibold text-foreground">{requisition.titleEn}</span> ({requisition.requisitionNumber}) • ✉ {candidate.email} • 📞 {candidate.phoneNumber}
            </p>
          </div>

          <div className="flex items-center gap-2">
            {!isStatus(application.status, 'Hired', 3) && (
              <Button
                variant="primary"
                className="bg-emerald-600 hover:bg-emerald-700 text-white"
                onClick={() => setIsHireOpen(true)}
                id="btn-open-hire-modal"
              >
                ✓ Hire Candidate
              </Button>
            )}
            {isStatus(application.status, 'Hired', 3) && (
              <Badge variant="success" size="lg">
                Hired into People Module
              </Badge>
            )}
          </div>
        </div>

        {/* Tab Navigation */}
        <div className="flex gap-4 border-b border-border mt-6 pt-2">
          <button
            className={`pb-2 text-sm font-semibold border-b-2 transition-colors ${
              activeTab === 'timeline'
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
            onClick={() => setActiveTab('timeline')}
            id="tab-stage-timeline"
          >
            Stage History & Timeline ({stageHistory.length})
          </button>
          <button
            className={`pb-2 text-sm font-semibold border-b-2 transition-colors ${
              activeTab === 'interviews'
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
            onClick={() => setActiveTab('interviews')}
            id="tab-interviews"
          >
            Interviews & Scorecards ({interviews.length})
          </button>
          <button
            className={`pb-2 text-sm font-semibold border-b-2 transition-colors ${
              activeTab === 'offers'
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
            onClick={() => setActiveTab('offers')}
            id="tab-offers"
          >
            Offers & Compensation
          </button>
        </div>
      </Card>

      {/* Tab Contents */}
      {activeTab === 'timeline' && (
        <Card className="p-6 space-y-4">
          <h3 className="font-semibold text-base">Stage Progression History</h3>
          <div className="space-y-4 relative before:absolute before:left-3.5 before:top-2 before:bottom-2 before:w-0.5 before:bg-border">
            {stageHistory.map((sh, idx) => (
              <div key={sh.id} className="flex items-start gap-4 relative pl-8">
                <div className="absolute left-2 top-1.5 w-3.5 h-3.5 rounded-full bg-primary ring-4 ring-background" />
                <div className="flex-1 bg-muted/30 p-3 rounded-lg border border-border/50">
                  <div className="flex items-center justify-between text-xs">
                    <span className="font-semibold text-foreground">
                      Stage Transition #{stageHistory.length - idx}
                    </span>
                    <span className="text-muted-foreground">
                      {sh.changedAtUtc ? new Date(sh.changedAtUtc).toLocaleString() : ''}
                    </span>
                  </div>
                  {sh.reason && (
                    <p className="text-xs text-muted-foreground mt-1">Reason: {sh.reason}</p>
                  )}
                  {sh.idempotencyKey && (
                    <span className="text-[10px] font-mono text-muted-foreground/60 block mt-1">
                      Idempotency Key: {sh.idempotencyKey}
                    </span>
                  )}
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}

      {activeTab === 'interviews' && (
        <div className="space-y-4">
          <div className="flex justify-between items-center">
            <h3 className="font-semibold text-base">Scheduled Evaluation Rounds</h3>
            <Button
              size="sm"
              variant="primary"
              onClick={() => setIsScheduleOpen(true)}
              id="btn-schedule-interview"
            >
              + Schedule Interview
            </Button>
          </div>

          <div className="space-y-3">
            {interviews.map((iv) => (
              <InterviewCard
                key={iv.id}
                interview={iv}
                onOpenScorecard={() => setSelectedInterviewForScorecard(iv.id)}
              />
            ))}

            {interviews.length === 0 && (
              <div className="text-center p-8 border border-dashed border-border rounded-xl text-xs text-muted-foreground">
                No interviews scheduled for this candidate yet.
              </div>
            )}
          </div>
        </div>
      )}

      {activeTab === 'offers' && (
        <OfferWorkspace
          applicationId={application.id}
          candidateId={candidate.id}
          onOfferAccepted={() => refetch()}
        />
      )}

      {/* Scorecard Dialog */}
      {selectedInterviewForScorecard && (
        <ScorecardDialog
          isOpen={!!selectedInterviewForScorecard}
          interviewId={selectedInterviewForScorecard}
          expectedRowVersion={1}
          onClose={() => setSelectedInterviewForScorecard(null)}
          onSubmitted={() => {
            setSelectedInterviewForScorecard(null);
            refetchInterviews();
          }}
        />
      )}

      {/* Hire Candidate Dialog */}
      <HireCandidateDialog
        isOpen={isHireOpen}
        applicationDetail={detail}
        onClose={() => setIsHireOpen(false)}
        onHired={() => {
          setIsHireOpen(false);
          refetch();
        }}
      />

      {/* Schedule Interview Modal */}
      {isScheduleOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4"
          data-testid="schedule-interview-modal"
        >
          <div className="bg-card w-full max-w-lg rounded-xl border border-border shadow-2xl p-6 space-y-4">
            <div className="flex items-center justify-between border-b border-border pb-3">
              <h3 className="text-lg font-semibold">Schedule Interview Round</h3>
              <Button size="sm" variant="ghost" onClick={() => setIsScheduleOpen(false)}>
                ✕
              </Button>
            </div>

            <form onSubmit={handleScheduleSubmit} className="space-y-3">
              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  Interview Round Title
                </label>
                <input
                  className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm"
                  value={scheduleForm.title}
                  onChange={(e) => setScheduleForm({ ...scheduleForm, title: e.target.value })}
                  required
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs font-medium text-muted-foreground block mb-1">
                    Interview Type
                  </label>
                  <select
                    className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm"
                    value={scheduleForm.interviewType}
                    onChange={(e) =>
                      setScheduleForm({ ...scheduleForm, interviewType: e.target.value })
                    }
                  >
                    <option value="Screening">Screening</option>
                    <option value="Technical">Technical</option>
                    <option value="Behavioral">Behavioral</option>
                    <option value="Manager">Manager</option>
                    <option value="Panel">Panel</option>
                  </select>
                </div>
                <div>
                  <label className="text-xs font-medium text-muted-foreground block mb-1">
                    Timezone
                  </label>
                  <select
                    className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm"
                    value={scheduleForm.timezone}
                    onChange={(e) =>
                      setScheduleForm({ ...scheduleForm, timezone: e.target.value })
                    }
                  >
                    <option value="Africa/Cairo">Africa/Cairo (EET/EEST)</option>
                    <option value="Asia/Riyadh">Asia/Riyadh (AST)</option>
                    <option value="UTC">UTC</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs font-medium text-muted-foreground block mb-1">
                    Start (Local Time)
                  </label>
                  <input
                    type="datetime-local"
                    className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm"
                    value={scheduleForm.scheduledStartUtc}
                    onChange={(e) =>
                      setScheduleForm({ ...scheduleForm, scheduledStartUtc: e.target.value })
                    }
                    required
                  />
                </div>
                <div>
                  <label className="text-xs font-medium text-muted-foreground block mb-1">
                    End (Local Time)
                  </label>
                  <input
                    type="datetime-local"
                    className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm"
                    value={scheduleForm.scheduledEndUtc}
                    onChange={(e) =>
                      setScheduleForm({ ...scheduleForm, scheduledEndUtc: e.target.value })
                    }
                    required
                  />
                </div>
              </div>

              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  Meeting Link / Location
                </label>
                <input
                  className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm"
                  value={scheduleForm.locationOrMeetingUrl}
                  onChange={(e) =>
                    setScheduleForm({ ...scheduleForm, locationOrMeetingUrl: e.target.value })
                  }
                />
              </div>

              <div className="flex justify-end gap-2 pt-3 border-t border-border">
                <Button size="sm" variant="outline" type="button" onClick={() => setIsScheduleOpen(false)}>
                  Cancel
                </Button>
                <Button size="sm" variant="primary" type="submit" id="btn-confirm-schedule">
                  Confirm Schedule
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

const InterviewCard: React.FC<{
  interview: Interview;
  onOpenScorecard: () => void;
}> = ({ interview, onOpenScorecard }) => {
  const { data: scorecards = [] } = useGetScorecards(interview.id);

  return (
    <Card className="p-4 border-border space-y-3" id={`interview-card-${interview.id}`}>
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-2">
            <span className="font-semibold text-sm">{interview.title}</span>
            <Badge variant="outline" size="sm">
              Round
            </Badge>
            <Badge variant="neutral" size="sm">
              {String(interview.status)}
            </Badge>
          </div>
          <div className="text-xs text-muted-foreground mt-1">
            🕒 {new Date(interview.scheduledStartUtc).toUTCString()} ({interview.timezone})
          </div>
          {interview.locationOrMeetingUrl && (
            <a
              href={interview.locationOrMeetingUrl}
              target="_blank"
              rel="noreferrer"
              className="text-xs text-primary underline mt-1 block truncate max-w-md"
            >
              {interview.locationOrMeetingUrl}
            </a>
          )}
        </div>

        <Button
          size="sm"
          variant="secondary"
          onClick={onOpenScorecard}
          id={`btn-scorecard-${interview.id}`}
        >
          ✍ Submit Scorecard
        </Button>
      </div>

      {scorecards.length > 0 && (
        <div className="bg-muted/30 p-3 rounded-lg border border-border/40 text-xs space-y-2">
          <span className="font-semibold text-muted-foreground block">
            Submitted Scorecards ({scorecards.length}) - Confidentiality Enforced
          </span>
          {scorecards.map((sc) => (
            <div key={sc.id} className="p-2 bg-card rounded border border-border/60 space-y-1">
              <div className="flex justify-between items-center">
                <span className="font-medium">Recommendation:</span>
                <Badge
                  variant={
                    String(sc.recommendation) === 'StrongYes' || String(sc.recommendation) === '0' || String(sc.recommendation) === '1'
                      ? 'success'
                      : String(sc.recommendation) === 'Neutral' || String(sc.recommendation) === '2'
                      ? 'warning'
                      : 'danger'
                  }
                  size="sm"
                >
                  {String(sc.recommendation) === '0' ? 'StrongYes' : String(sc.recommendation) === '1' ? 'Yes' : String(sc.recommendation) === '2' ? 'Neutral' : String(sc.recommendation) === '3' ? 'No' : String(sc.recommendation) === '4' ? 'StrongNo' : String(sc.recommendation)}
                </Badge>
              </div>
              {sc.strengths && (
                <div className="text-muted-foreground">
                  <span className="font-semibold">Strengths:</span> {sc.strengths}
                </div>
              )}
              {sc.concerns && (
                <div className="text-muted-foreground">
                  <span className="font-semibold">Concerns:</span> {sc.concerns}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </Card>
  );
};
