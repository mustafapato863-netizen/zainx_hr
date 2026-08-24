import React, { useState } from 'react';
import {
  Button,
  Input,
  Alert,
} from '@zainx/design-system';
import {
  useHireCandidate,
  ApplicationDetailDto,
} from '@zainx/contracts';

interface HireCandidateDialogProps {
  isOpen: boolean;
  applicationDetail: ApplicationDetailDto;
  onClose: () => void;
  onHired: () => void;
}

export const HireCandidateDialog: React.FC<HireCandidateDialogProps> = ({
  isOpen,
  applicationDetail,
  onClose,
  onHired,
}) => {
  const hireMutation = useHireCandidate();
  const { application, candidate, requisition } = applicationDetail;

  const [formData, setFormData] = useState({
    nationalIdentifier: '29501011234567',
    dateOfBirth: '1995-01-01',
    employeeNumber: `EMP-${Math.floor(100000 + Math.random() * 900000)}`,
    gender: 'Male',
    nationality: 'EG',
    hireDate: new Date().toISOString().split('T')[0],
  });

  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    try {
      await hireMutation.mutateAsync({
        id: application.id,
        data: {
          nationalIdentifier: formData.nationalIdentifier,
          dateOfBirth: formData.dateOfBirth,
          employeeNumber: formData.employeeNumber,
          gender: formData.gender,
          nationality: formData.nationality,
          hireDate: formData.hireDate,
          expectedRowVersion: Number(application.rowVersion),
        },
      });
      onHired();
    } catch (err: any) {
      setError(err?.response?.data?.detail || err.message || 'Failed to hire candidate');
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4"
      data-testid="hire-candidate-modal"
    >
      <div className="bg-card w-full max-w-lg rounded-xl border border-border shadow-2xl p-6 space-y-4">
        <div className="flex items-center justify-between border-b border-border pb-3">
          <div>
            <h3 className="text-lg font-semibold">Hire Candidate & Instantiate Employee</h3>
            <p className="text-xs text-muted-foreground">
              Executes the cross-boundary contract to create Person & Employment records in People.
            </p>
          </div>
          <Button size="sm" variant="ghost" onClick={onClose}>
            ✕
          </Button>
        </div>

        <div className="bg-muted/40 p-3 rounded-lg text-sm space-y-1">
          <div className="flex justify-between">
            <span className="text-muted-foreground text-xs">Candidate:</span>
            <span className="font-semibold text-xs">{candidate.firstNameEn} {candidate.lastNameEn}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground text-xs">Requisition:</span>
            <span className="font-semibold text-xs">{requisition.titleEn} ({requisition.requisitionNumber})</span>
          </div>
        </div>

        {error && (
          <Alert variant="danger">
            {error}
          </Alert>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Employee Number
              </label>
              <Input
                value={formData.employeeNumber}
                onChange={(e) => setFormData({ ...formData, employeeNumber: e.target.value })}
                id="input-emp-number"
                required
              />
            </div>
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Hire / Start Date
              </label>
              <Input
                type="date"
                value={formData.hireDate}
                onChange={(e) => setFormData({ ...formData, hireDate: e.target.value })}
                id="input-hire-date"
                required
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                National ID (PII Encrypted)
              </label>
              <Input
                value={formData.nationalIdentifier}
                onChange={(e) => setFormData({ ...formData, nationalIdentifier: e.target.value })}
                id="input-national-id"
                required
              />
            </div>
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Date of Birth
              </label>
              <Input
                type="date"
                value={formData.dateOfBirth}
                onChange={(e) => setFormData({ ...formData, dateOfBirth: e.target.value })}
                id="input-dob"
                required
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Gender
              </label>
              <select
                className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm"
                value={formData.gender}
                onChange={(e) => setFormData({ ...formData, gender: e.target.value })}
                id="select-gender"
              >
                <option value="Male">Male</option>
                <option value="Female">Female</option>
                <option value="Unspecified">Unspecified</option>
              </select>
            </div>
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Nationality
              </label>
              <Input
                value={formData.nationality}
                onChange={(e) => setFormData({ ...formData, nationality: e.target.value })}
                id="input-nationality"
                required
              />
            </div>
          </div>

          <div className="flex items-center justify-end gap-2 border-t border-border pt-4">
            <Button variant="outline" type="button" onClick={onClose}>
              Cancel
            </Button>
            <Button
              variant="primary"
              type="submit"
              disabled={hireMutation.isPending}
              id="btn-confirm-hire"
            >
              {hireMutation.isPending ? 'Hiring...' : 'Confirm Hire & Create Employee'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
