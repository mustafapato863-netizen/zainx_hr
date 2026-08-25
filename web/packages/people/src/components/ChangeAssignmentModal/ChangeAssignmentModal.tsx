import React, { useState } from 'react';
import {
  Dialog,
  Button,
  Field,
  Input
} from '@zainx/design-system';
import { ChangeAssignmentRequest, OrganizationUnitDto, LocationDto } from '@zainx/contracts';

export interface ChangeAssignmentModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (request: ChangeAssignmentRequest) => Promise<void>;
  departments: OrganizationUnitDto[];
  locations: LocationDto[];
  currentRowVersion: number;
}

export const ChangeAssignmentModal: React.FC<ChangeAssignmentModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
  departments,
  locations,
  currentRowVersion
}) => {
  const [formData, setFormData] = useState<ChangeAssignmentRequest>({
    organizationUnitId: departments[0]?.id || '',
    locationId: locations[0]?.id,
    jobTitleEn: '',
    jobTitleAr: '',
    effectiveFrom: new Date().toISOString().split('T')[0],
    rowVersion: currentRowVersion
  });

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const handleChange = (field: keyof ChangeAssignmentRequest, val: any) => {
    setFormData(prev => ({ ...prev, [field]: val }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.jobTitleEn || !formData.jobTitleAr || !formData.effectiveFrom) {
      setErrorMsg('Please provide all mandatory fields.');
      return;
    }

    try {
      setIsSubmitting(true);
      setErrorMsg('');
      await onSubmit({ ...formData, rowVersion: currentRowVersion });
      onClose();
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to update assignment.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog
      isOpen={isOpen}
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
      title="Change Organizational Assignment / تعديل التكليف التنظيمي"
      description="Record an effective-dated assignment transfer while preserving previous history."
    >
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
        {errorMsg && (
          <div style={{ padding: '0.75rem', background: '#fef2f2', color: '#b91c1c', borderRadius: '6px', fontSize: '0.875rem' }}>
            {errorMsg}
          </div>
        )}

        <Field label="New Department / الإدارة الجديدة *" isRequired>
          <select
            value={formData.organizationUnitId}
            onChange={(e) => handleChange('organizationUnitId', e.target.value)}
            style={{
              width: '100%',
              padding: '0.5rem',
              borderRadius: '6px',
              border: '1px solid var(--zainx-color-border, #cbd5e1)'
            }}
          >
            {departments.map((d) => (
              <option key={d.id} value={d.id}>
                {d.nameEn} ({d.nameAr})
              </option>
            ))}
          </select>
        </Field>

        <Field label="New Job Title (English) *" isRequired>
          <Input
            value={formData.jobTitleEn}
            onChange={(e) => handleChange('jobTitleEn', e.target.value)}
            placeholder="e.g. Lead Talent Specialist"
          />
        </Field>

        <Field label="المسمى الوظيفي الجديد (عربي) *" isRequired>
          <Input
            value={formData.jobTitleAr}
            onChange={(e) => handleChange('jobTitleAr', e.target.value)}
            placeholder="مثال: أخصائي أول استقطاب كفاءات"
          />
        </Field>

        <Field label="Location / الموقع">
          <select
            value={formData.locationId || ''}
              onChange={(e) => handleChange('locationId', e.target.value || undefined)}
            style={{
              width: '100%',
              padding: '0.5rem',
              borderRadius: '6px',
              border: '1px solid var(--zainx-color-border, #cbd5e1)'
            }}
          >
            {locations.map((loc) => (
              <option key={loc.id} value={loc.id}>
                {loc.nameEn} ({loc.city})
              </option>
            ))}
          </select>
        </Field>

        <Field label="Effective Date / تاريخ السريان *" isRequired>
          <Input
            type="date"
            value={formData.effectiveFrom}
            onChange={(e) => handleChange('effectiveFrom', e.target.value)}
          />
        </Field>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1rem' }}>
          <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
            Cancel / إلغاء
          </Button>
          <Button variant="primary" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving... / جاري الحفظ' : 'Apply Assignment / اعتماد التكليف'}
          </Button>
        </div>
      </form>
    </Dialog>
  );
};
