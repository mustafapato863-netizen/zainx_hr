import React, { useState } from 'react';
import {
  Card,
  Badge,
  Button,
  Spinner,
  Alert,
} from '@zainx/design-system';
import {
  useGetRequisitionById,
} from '@zainx/contracts';
import { PipelineBoard } from './PipelineBoard';
import { ApplicationWorkspace } from './ApplicationWorkspace';

interface JobWorkspaceProps {
  requisitionId: string;
  onBack?: () => void;
}

export const JobWorkspace: React.FC<JobWorkspaceProps> = ({
  requisitionId,
  onBack,
}) => {
  const { data: requisition, isLoading, error } = useGetRequisitionById(requisitionId);
  const [selectedApplicationId, setSelectedApplicationId] = useState<string | null>(null);

  if (isLoading) {
    return (
      <div className="flex justify-center p-12">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error || !requisition) {
    return (
      <Alert variant="danger">
        Failed to load requisition: {(error as any)?.message || 'Not found'}
      </Alert>
    );
  }

  if (selectedApplicationId) {
    return (
      <ApplicationWorkspace
        applicationId={selectedApplicationId}
        onBack={() => setSelectedApplicationId(null)}
      />
    );
  }

  const isStatus = (currentStatus: any, expected: string, numericVal: number) => {
    const s = String(currentStatus);
    return s === expected || s === String(numericVal);
  };

  return (
    <div className="space-y-6" data-testid="job-workspace">
      {/* Requisition Banner */}
      <Card className="p-6 bg-card border-border space-y-4">
        <div className="flex items-start justify-between">
          <div>
            <div className="flex items-center gap-2">
              {onBack && (
                <Button size="sm" variant="ghost" onClick={onBack}>
                  ← All Requisitions
                </Button>
              )}
              <h2 className="text-xl font-bold text-foreground">{requisition.titleEn}</h2>
              <span className="text-sm text-muted-foreground">({requisition.titleAr})</span>
              <Badge
                variant={
                  isStatus(requisition.status, 'Open', 3)
                    ? 'success'
                    : isStatus(requisition.status, 'Approved', 2)
                    ? 'info'
                    : 'neutral'
                }
              >
                {isStatus(requisition.status, 'Open', 3)
                  ? 'Open'
                  : isStatus(requisition.status, 'Approved', 2)
                  ? 'Approved'
                  : String(requisition.status)}
              </Badge>
              <Badge variant="outline" size="sm">
                {requisition.requisitionNumber}
              </Badge>
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Openings: {requisition.openingsCount} • Type: {requisition.employmentType} • Target Start: {requisition.targetStartDate || 'ASAP'}
            </p>
          </div>
        </div>
      </Card>

      {/* Interactive Kanban Pipeline Board */}
      <PipelineBoard
        requisitionId={requisition.id}
        onSelectApplication={(appId) => setSelectedApplicationId(appId)}
      />
    </div>
  );
};
