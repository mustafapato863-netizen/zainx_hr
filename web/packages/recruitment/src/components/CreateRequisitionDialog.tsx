import React, { useState } from 'react';
import {
  Button,
  Input,
} from '@zainx/design-system';
import {
  useCreateRequisition,
  useGetPipelines,
} from '@zainx/contracts';

interface CreateRequisitionDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: () => void;
}

export const CreateRequisitionDialog: React.FC<CreateRequisitionDialogProps> = ({
  isOpen,
  onClose,
  onCreated,
}) => {
  const { data: rawPipelines } = useGetPipelines();
  const pipelines: any[] = Array.isArray(rawPipelines) ? rawPipelines : (rawPipelines as any)?.items || [];
  const createMutation = useCreateRequisition();

  const [formData, setFormData] = useState({
    titleEn: '',
    titleAr: '',
    requisitionNumber: `REQ-${new Date().getFullYear()}-${Math.floor(100 + Math.random() * 900)}`,
    openingsCount: 1,
    employmentType: 'FullTime',
    pipelineId: '',
    organizationUnitId: '44444444-4444-4444-4444-444444444444',
    hiringManagerId: '55555555-5555-5555-5555-555555555555',
    recruiterId: '66666666-6666-6666-6666-666666666666',
    requisitionReason: 'Growth headcount',
    targetStartDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
  });

  const [error, setError] = useState<string | null>(null);

  React.useEffect(() => {
    if (pipelines.length > 0 && !formData.pipelineId) {
      setFormData((prev) => ({ ...prev, pipelineId: pipelines[0].id }));
    }
  }, [pipelines]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!formData.titleEn || !formData.titleAr) {
      setError('Both English and Arabic job titles are required.');
      return;
    }

    try {
      await createMutation.mutateAsync({
        data: {
          titleEn: formData.titleEn,
          titleAr: formData.titleAr,
          requisitionNumber: formData.requisitionNumber,
          openingsCount: Number(formData.openingsCount),
          employmentType: formData.employmentType,
          pipelineId: formData.pipelineId || pipelines[0]?.id || '00000000-0000-0000-0000-000000000000',
          organizationUnitId: formData.organizationUnitId,
          positionId: null,
          locationId: null,
          hiringManagerId: formData.hiringManagerId,
          recruiterId: formData.recruiterId,
          requisitionReason: formData.requisitionReason,
          targetStartDate: formData.targetStartDate,
        },
      });
      onCreated();
    } catch (err: any) {
      setError(err?.response?.data?.detail || err.message || 'Failed to create requisition');
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4"
      data-testid="create-requisition-modal"
    >
      <div className="bg-card w-full max-w-lg rounded-xl border border-border shadow-2xl p-6 space-y-4">
        <div className="flex items-center justify-between border-b border-border pb-3">
          <h3 className="text-lg font-semibold">Create Job Requisition</h3>
          <Button size="sm" variant="ghost" onClick={onClose}>
            ✕
          </Button>
        </div>

        {error && (
          <div className="p-3 text-sm rounded bg-destructive/15 text-destructive border border-destructive/30">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Requisition #
              </label>
              <Input
                value={formData.requisitionNumber}
                onChange={(e) =>
                  setFormData({ ...formData, requisitionNumber: e.target.value })
                }
                id="input-req-number"
                required
              />
            </div>
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Openings Count
              </label>
              <Input
                type="number"
                min="1"
                value={formData.openingsCount}
                onChange={(e) =>
                  setFormData({ ...formData, openingsCount: parseInt(e.target.value, 10) })
                }
                id="input-openings-count"
                required
              />
            </div>
          </div>

          <div>
            <label className="text-xs font-medium text-muted-foreground block mb-1">
              Job Title (English)
            </label>
            <Input
              placeholder="e.g. Senior Backend Engineer"
              value={formData.titleEn}
              onChange={(e) => setFormData({ ...formData, titleEn: e.target.value })}
              id="input-title-en"
              required
            />
          </div>

          <div>
            <label className="text-xs font-medium text-muted-foreground block mb-1">
              Job Title (Arabic)
            </label>
            <Input
              placeholder="مثال: مهندس برمجيات خلفية أول"
              dir="rtl"
              value={formData.titleAr}
              onChange={(e) => setFormData({ ...formData, titleAr: e.target.value })}
              id="input-title-ar"
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Employment Type
              </label>
              <select
                className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm"
                value={formData.employmentType}
                onChange={(e) =>
                  setFormData({ ...formData, employmentType: e.target.value })
                }
                id="select-employment-type"
              >
                <option value="FullTime">Full Time</option>
                <option value="PartTime">Part Time</option>
                <option value="Contract">Contract</option>
                <option value="Internship">Internship</option>
              </select>
            </div>
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Target Start Date
              </label>
              <Input
                type="date"
                value={formData.targetStartDate}
                onChange={(e) =>
                  setFormData({ ...formData, targetStartDate: e.target.value })
                }
                id="input-target-date"
              />
            </div>
          </div>

          <div>
            <label className="text-xs font-medium text-muted-foreground block mb-1">
              Pipeline Selection
            </label>
            <select
              className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm"
              value={formData.pipelineId}
              onChange={(e) => setFormData({ ...formData, pipelineId: e.target.value })}
              id="select-pipeline"
            >
              {pipelines.map((p: any) => (
                <option key={p.id} value={p.id}>
                  {p.nameEn} ({p.nameAr})
                </option>
              ))}
            </select>
          </div>

          <div className="flex items-center justify-end gap-2 border-t border-border pt-4">
            <Button variant="outline" type="button" onClick={onClose}>
              Cancel
            </Button>
            <Button
              variant="primary"
              type="submit"
              disabled={createMutation.isPending}
              id="btn-submit-create-req"
            >
              {createMutation.isPending ? 'Creating...' : 'Create Draft'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
