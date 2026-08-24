import React, { useState } from 'react';
import {
  Card,
  Badge,
  Button,
  Input,
  Spinner,
  Alert,
} from '@zainx/design-system';
import {
  useGetCandidates,
  useGetCandidateById,
  useCheckDuplicateCandidates,
  useCreateCandidate,
  Candidate,
} from '@zainx/contracts';

interface CandidateWorkspaceProps {
  onSelectCandidate?: (candidate: Candidate) => void;
  onApplyForJob?: (candidate: Candidate) => void;
}

export const CandidateWorkspace: React.FC<CandidateWorkspaceProps> = ({
  onSelectCandidate,
  onApplyForJob,
}) => {
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [selectedCandidateId, setSelectedCandidateId] = useState<string | null>(null);
  const [isAddOpen, setIsAddOpen] = useState<boolean>(false);
  const [duplicates, setDuplicates] = useState<any[]>([]);

  const { data: responseData, isLoading, refetch } = useGetCandidates(
    searchTerm ? { search: searchTerm } : undefined
  );

  const candidates: Candidate[] = (responseData as any)?.items || (Array.isArray(responseData) ? responseData : []);

  const { data: selectedCandidate } = useGetCandidateById(
    selectedCandidateId || '',
    { query: { enabled: !!selectedCandidateId } }
  );

  const checkDuplicatesMutation = useCheckDuplicateCandidates();

  React.useEffect(() => {
    if (selectedCandidate?.email && selectedCandidate?.phoneNumber) {
      checkDuplicatesMutation
        .mutateAsync({
          data: {
            email: selectedCandidate.email,
            phoneNumber: selectedCandidate.phoneNumber,
            excludeCandidateId: selectedCandidate.id,
          },
        })
        .then((res: any) => setDuplicates(Array.isArray(res) ? res : res?.matches || []))
        .catch(() => setDuplicates([]));
    } else {
      setDuplicates([]);
    }
  }, [selectedCandidateId, selectedCandidate?.email, selectedCandidate?.phoneNumber]);

  return (
    <div className="space-y-6" data-testid="candidate-workspace">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold tracking-tight">Candidate Directory & Intake</h2>
          <p className="text-sm text-muted-foreground">
            Manage applicant profiles, view cross-tenant duplicate signals, and review resumes.
          </p>
        </div>
        <Button
          variant="primary"
          onClick={() => setIsAddOpen(true)}
          id="btn-add-candidate"
        >
          + Add Candidate
        </Button>
      </div>

      <div className="grid grid-cols-12 gap-6">
        {/* Candidates List */}
        <div className="col-span-5 space-y-3">
          <Input
            placeholder="Search candidates by name, email, or phone..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            id="input-search-candidates"
          />

          {isLoading ? (
            <div className="flex justify-center p-8">
              <Spinner />
            </div>
          ) : (
            <div className="space-y-2 max-h-[600px] overflow-y-auto pr-1">
              {candidates.map((c) => (
                <Card
                  key={c.id}
                  className={`p-3 cursor-pointer transition-all border ${
                    selectedCandidateId === c.id
                      ? 'border-primary bg-primary/5 shadow-xs'
                      : 'border-border hover:border-border/80'
                  }`}
                  onClick={() => {
                    setSelectedCandidateId(c.id);
                    onSelectCandidate?.(c);
                  }}
                  id={`candidate-item-${c.id}`}
                >
                  <div className="flex items-start justify-between">
                    <div>
                      <div className="font-semibold text-sm">
                        {c.firstNameEn} {c.lastNameEn}
                      </div>
                      <div className="text-xs text-muted-foreground">
                        {c.firstNameAr} {c.lastNameAr}
                      </div>
                    </div>
                    {c.source && (
                      <Badge variant="outline" size="sm">
                        {c.source}
                      </Badge>
                    )}
                  </div>
                  <div className="text-xs text-muted-foreground mt-2 truncate">
                    ✉ {c.email} | 📞 {c.phoneNumber}
                  </div>
                </Card>
              ))}
              {candidates.length === 0 && (
                <div className="text-center p-6 text-sm text-muted-foreground">
                  No candidates found matching filter.
                </div>
              )}
            </div>
          )}
        </div>

        {/* Candidate Detail Card */}
        <div className="col-span-7">
          {selectedCandidate ? (
            <Card className="p-6 space-y-6">
              <div className="flex items-start justify-between border-b border-border pb-4">
                <div>
                  <h3 className="text-xl font-bold text-foreground">
                    {selectedCandidate.firstNameEn} {selectedCandidate.lastNameEn}
                  </h3>
                  <div className="text-sm text-muted-foreground">
                    {selectedCandidate.firstNameAr} {selectedCandidate.lastNameAr}
                  </div>
                  {selectedCandidate.headline && (
                    <p className="text-sm font-medium text-primary mt-1">
                      {selectedCandidate.headline}
                    </p>
                  )}
                </div>
                {onApplyForJob && (
                  <Button
                    variant="primary"
                    size="sm"
                    onClick={() => onApplyForJob(selectedCandidate)}
                    id="btn-apply-candidate-job"
                  >
                    Apply to Requisition
                  </Button>
                )}
              </div>

              {duplicates.length > 0 && (
                <Alert variant="warning" title="Potential Duplicate Profiles Detected">
                  Found {duplicates.length} duplicate profile(s) with matching email/phone hashes:
                  <ul className="mt-1 list-disc list-inside text-xs">
                    {duplicates.map((dup: any) => (
                      <li key={dup.id}>
                        {dup.firstNameEn} {dup.lastNameEn} ({dup.email}) - {dup.source || 'Unknown source'}
                      </li>
                    ))}
                  </ul>
                </Alert>
              )}

              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-xs text-muted-foreground block">Email</span>
                  <span className="font-mono text-sm">{selectedCandidate.email}</span>
                </div>
                <div>
                  <span className="text-xs text-muted-foreground block">Phone</span>
                  <span className="font-mono text-sm">{selectedCandidate.phoneNumber}</span>
                </div>
                <div>
                  <span className="text-xs text-muted-foreground block">Location</span>
                  <span>{selectedCandidate.location || 'Not specified'}</span>
                </div>
                <div>
                  <span className="text-xs text-muted-foreground block">Intake Source</span>
                  <span>{selectedCandidate.source || 'Direct intake'}</span>
                </div>
              </div>

              {selectedCandidate.skillsJson && (
                <div>
                  <span className="text-xs text-muted-foreground block mb-2">Skills</span>
                  <div className="flex flex-wrap gap-1.5">
                    {JSON.parse(selectedCandidate.skillsJson || '[]').map((skill: string) => (
                      <Badge key={skill} variant="neutral" size="sm">
                        {skill}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}
            </Card>
          ) : (
            <div className="flex flex-col items-center justify-center h-80 border border-dashed border-border rounded-xl text-muted-foreground">
              <span className="text-sm">Select a candidate on the left to view profile details.</span>
            </div>
          )}
        </div>
      </div>

      <CandidateIntakeDialog
        isOpen={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        onCreated={() => {
          setIsAddOpen(false);
          refetch();
        }}
      />
    </div>
  );
};

interface CandidateIntakeDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: () => void;
}

export const CandidateIntakeDialog: React.FC<CandidateIntakeDialogProps> = ({
  isOpen,
  onClose,
  onCreated,
}) => {
  const createMutation = useCreateCandidate();

  const [formData, setFormData] = useState({
    firstNameEn: '',
    lastNameEn: '',
    firstNameAr: '',
    lastNameAr: '',
    email: '',
    phoneNumber: '',
    location: 'Cairo, Egypt',
    headline: 'Senior Full Stack Engineer',
    source: 'LinkedIn',
  });

  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    try {
      await createMutation.mutateAsync({
        data: {
          ...formData,
          resumeDocumentId: null,
          skillsJson: JSON.stringify(['TypeScript', 'C#', '.NET 10', 'PostgreSQL']),
        },
      });
      onCreated();
    } catch (err: any) {
      setError(err?.response?.data?.detail || err.message || 'Failed to add candidate');
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4"
      data-testid="add-candidate-modal"
    >
      <div className="bg-card w-full max-w-lg rounded-xl border border-border shadow-2xl p-6 space-y-4">
        <div className="flex items-center justify-between border-b border-border pb-3">
          <h3 className="text-lg font-semibold">Candidate Intake Form</h3>
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
                First Name (EN)
              </label>
              <Input
                value={formData.firstNameEn}
                onChange={(e) => setFormData({ ...formData, firstNameEn: e.target.value })}
                id="input-candidate-first-en"
                required
              />
            </div>
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Last Name (EN)
              </label>
              <Input
                value={formData.lastNameEn}
                onChange={(e) => setFormData({ ...formData, lastNameEn: e.target.value })}
                id="input-candidate-last-en"
                required
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                First Name (AR)
              </label>
              <Input
                dir="rtl"
                value={formData.firstNameAr}
                onChange={(e) => setFormData({ ...formData, firstNameAr: e.target.value })}
                id="input-candidate-first-ar"
                required
              />
            </div>
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Last Name (AR)
              </label>
              <Input
                dir="rtl"
                value={formData.lastNameAr}
                onChange={(e) => setFormData({ ...formData, lastNameAr: e.target.value })}
                id="input-candidate-last-ar"
                required
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">Email</label>
              <Input
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                id="input-candidate-email"
                required
              />
            </div>
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Phone Number
              </label>
              <Input
                value={formData.phoneNumber}
                onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                id="input-candidate-phone"
                required
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">
                Location
              </label>
              <Input
                value={formData.location}
                onChange={(e) => setFormData({ ...formData, location: e.target.value })}
                id="input-candidate-location"
              />
            </div>
            <div>
              <label className="text-xs font-medium text-muted-foreground block mb-1">Source</label>
              <select
                className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm"
                value={formData.source}
                onChange={(e) => setFormData({ ...formData, source: e.target.value })}
                id="select-candidate-source"
              >
                <option value="LinkedIn">LinkedIn</option>
                <option value="CareerSite">Career Site</option>
                <option value="Referral">Employee Referral</option>
                <option value="Agency">Agency</option>
                <option value="Direct">Direct Intake</option>
              </select>
            </div>
          </div>

          <div className="flex items-center justify-end gap-2 border-t border-border pt-4">
            <Button variant="outline" type="button" onClick={onClose}>
              Cancel
            </Button>
            <Button
              variant="primary"
              type="submit"
              disabled={createMutation.isPending}
              id="btn-submit-candidate-intake"
            >
              {createMutation.isPending ? 'Saving...' : 'Add Candidate'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
