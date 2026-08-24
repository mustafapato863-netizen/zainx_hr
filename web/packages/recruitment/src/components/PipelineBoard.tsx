import React, { useState } from 'react';
import {
  Card,
  Badge,
  Button,
  Spinner,
  Alert,
} from '@zainx/design-system';
import {
  useGetPipelineBoard,
  useMoveStage,
  ApplicationSummaryDto,
  RecruitmentStage,
} from '@zainx/contracts';

interface PipelineBoardProps {
  requisitionId: string;
  onSelectApplication?: (applicationId: string) => void;
  onAddCandidate?: () => void;
}

export const PipelineBoard: React.FC<PipelineBoardProps> = ({
  requisitionId,
  onSelectApplication,
  onAddCandidate,
}) => {
  const { data: board, isLoading, error, refetch } = useGetPipelineBoard(requisitionId);
  const moveStageMutation = useMoveStage();

  const [draggedAppId, setDraggedAppId] = useState<string | null>(null);
  const [conflictError, setConflictError] = useState<string | null>(null);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-12">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error || !board) {
    return (
      <Alert variant="danger">
        Failed to load pipeline board: {(error as any)?.message || 'Unknown error'}
      </Alert>
    );
  }

  const stages: RecruitmentStage[] = board.stages || [];
  const applications: ApplicationSummaryDto[] = board.applications || [];

  const handleDragStart = (e: React.DragEvent, appId: string) => {
    setDraggedAppId(appId);
    e.dataTransfer.setData('text/plain', appId);
    e.dataTransfer.effectAllowed = 'move';
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  };

  const handleDrop = async (e: React.DragEvent, targetStageId: string) => {
    e.preventDefault();
    const appId = e.dataTransfer.getData('text/plain') || draggedAppId;
    if (!appId) return;

    const app = applications.find((a) => a.id === appId);
    if (!app || app.currentStageId === targetStageId) return;

    setConflictError(null);
    try {
      await moveStageMutation.mutateAsync({
        id: appId,
        data: {
          targetStageId,
          expectedRowVersion: Number(app.rowVersion),
          idempotencyKey: `dnd-${appId}-${targetStageId}-${Date.now()}`,
          reason: 'Stage updated via ATS Kanban Board',
        },
      });
      refetch();
    } catch (err: any) {
      if (err?.response?.status === 409) {
        setConflictError(
          'Stage move conflict: This application was updated by another recruiter. Refreshing board...'
        );
      } else {
        setConflictError(`Move failed: ${err?.response?.data?.detail || err.message}`);
      }
      refetch();
    } finally {
      setDraggedAppId(null);
    }
  };

  const handleKeyboardMove = async (app: ApplicationSummaryDto, direction: 'next' | 'prev') => {
    const currentIndex = stages.findIndex((s) => s.id === app.currentStageId);
    if (currentIndex === -1) return;

    const targetIndex = direction === 'next' ? currentIndex + 1 : currentIndex - 1;
    if (targetIndex < 0 || targetIndex >= stages.length) return;

    const targetStage = stages[targetIndex];
    setConflictError(null);

    try {
      await moveStageMutation.mutateAsync({
        id: app.id,
        data: {
          targetStageId: targetStage.id,
          expectedRowVersion: Number(app.rowVersion),
          idempotencyKey: `kb-${app.id}-${targetStage.id}-${Date.now()}`,
          reason: `Stage moved ${direction} via accessible keyboard shortcut`,
        },
      });
      refetch();
    } catch (err: any) {
      setConflictError(`Move failed: ${err?.response?.data?.detail || err.message}`);
      refetch();
    }
  };

  return (
    <div className="space-y-4" data-testid="pipeline-board">
      {conflictError && (
        <Alert variant="warning" onClose={() => setConflictError(null)}>
          {conflictError}
        </Alert>
      )}

      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-bold">
            {board.requisitionTitleEn} ({board.requisitionTitleAr})
          </h3>
          <p className="text-xs text-muted-foreground">
            Total active candidates: {applications.length} across {stages.length} pipeline stages
          </p>
        </div>
        {onAddCandidate && (
          <Button variant="primary" size="sm" onClick={onAddCandidate} id="btn-board-add-candidate">
            + Add Candidate
          </Button>
        )}
      </div>

      <div className="flex gap-4 overflow-x-auto pb-4 pt-1 min-h-[560px]">
        {stages.map((stage, sIndex) => {
          const stageApps = applications.filter((a) => a.currentStageId === stage.id);

          return (
            <div
              key={stage.id}
              className="flex flex-col flex-shrink-0 w-72 bg-muted/40 rounded-xl border border-border p-3"
              onDragOver={handleDragOver}
              onDrop={(e) => handleDrop(e, stage.id)}
              data-testid={`stage-column-${stage.stageOrder}`}
            >
              <div className="flex items-center justify-between mb-3 px-1">
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-sm">{stage.nameEn}</span>
                  <Badge variant="neutral" size="sm">
                    {stageApps.length}
                  </Badge>
                </div>
                <span className="text-xs text-muted-foreground">{stage.nameAr}</span>
              </div>

              <div className="flex-1 space-y-2.5 overflow-y-auto">
                {stageApps.map((app) => (
                  <Card
                    key={app.id}
                    className="p-3 bg-card hover:shadow-md transition-shadow cursor-grab active:cursor-grabbing border-border"
                    draggable
                    onDragStart={(e) => handleDragStart(e, app.id)}
                    onClick={() => onSelectApplication?.(app.id)}
                    id={`card-app-${app.id}`}
                  >
                    <div className="flex items-start justify-between">
                      <div className="font-medium text-sm text-foreground">
                        {app.candidateNameEn}
                      </div>
                      <Badge size="sm" variant="outline">
                        v{app.rowVersion}
                      </Badge>
                    </div>
                    <div className="text-xs text-muted-foreground">{app.candidateNameAr}</div>

                    <div className="mt-2 text-xs text-muted-foreground truncate">
                      {app.email}
                    </div>

                    <div className="mt-3 flex items-center justify-between pt-2 border-t border-border/40">
                      <span className="text-[10px] text-muted-foreground">
                        {app.appliedAtUtc ? new Date(app.appliedAtUtc).toLocaleDateString() : ''}
                      </span>
                      <div className="flex items-center gap-1">
                        {sIndex > 0 && (
                          <button
                            className="text-xs p-1 hover:bg-muted rounded"
                            title="Move to previous stage (Keyboard accessible)"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleKeyboardMove(app, 'prev');
                            }}
                          >
                            ◀
                          </button>
                        )}
                        {sIndex < stages.length - 1 && (
                          <button
                            className="text-xs p-1 hover:bg-muted rounded"
                            title="Move to next stage (Keyboard accessible)"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleKeyboardMove(app, 'next');
                            }}
                          >
                            ▶
                          </button>
                        )}
                      </div>
                    </div>
                  </Card>
                ))}

                {stageApps.length === 0 && (
                  <div className="flex items-center justify-center h-28 border-2 border-dashed border-border/50 rounded-lg text-xs text-muted-foreground/60">
                    Drop candidate here
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
