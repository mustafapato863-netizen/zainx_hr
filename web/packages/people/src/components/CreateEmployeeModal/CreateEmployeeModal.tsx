import React, { useState } from 'react';
import {
  Dialog,
  Button,
  Field,
  Input
} from '@zainx/design-system';
import { CreateEmployeeRequest, OrganizationUnitDto, LocationDto } from '@zainx/contracts';

export interface CreateEmployeeModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (request: CreateEmployeeRequest) => Promise<void>;
  departments: OrganizationUnitDto[];
  locations: LocationDto[];
}

export const CreateEmployeeModal: React.FC<CreateEmployeeModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
  departments,
  locations
}) => {
  const [formData, setFormData] = useState<CreateEmployeeRequest>({
    firstNameEn: '',
    lastNameEn: '',
    firstNameAr: '',
    lastNameAr: '',
    employeeNumber: '',
    dateOfBirth: '',
    gender: undefined,
    nationality: undefined,
    nationalIdentifier: '',
    primaryEmail: '',
    phoneNumber: '',
    hireDate: '',
    organizationUnitId: departments[0]?.id || '',
    locationId: locations[0]?.id,
    jobTitleEn: '',
    jobTitleAr: ''
  });

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const handleChange = (field: keyof CreateEmployeeRequest, val: any) => {
    setFormData(prev => ({ ...prev, [field]: val }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const missingFields = [
      ['employeeNumber', formData.employeeNumber],
      ['firstNameEn', formData.firstNameEn],
      ['lastNameEn', formData.lastNameEn],
      ['firstNameAr', formData.firstNameAr],
      ['lastNameAr', formData.lastNameAr],
      ['dateOfBirth', formData.dateOfBirth],
      ['nationalIdentifier', formData.nationalIdentifier],
      ['hireDate', formData.hireDate],
      ['organizationUnitId', formData.organizationUnitId],
      ['jobTitleEn', formData.jobTitleEn],
      ['jobTitleAr', formData.jobTitleAr]
    ].filter(([, value]) => !String(value ?? '').trim()).map(([field]) => field);

    if (missingFields.length > 0) {
      setErrorMsg(`Please provide all required employee fields: ${missingFields.join(', ')}.`);
      return;
    }

    try {
      setIsSubmitting(true);
      setErrorMsg('');
      await onSubmit(formData);
      onClose();
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to create employee.');
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
      title="Add New Employee / إضافة موظف جديد"
      description="Create a canonical person, employment record, and initial organizational assignment."
    >
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
        {errorMsg && (
          <div style={{ padding: '0.75rem', background: '#fef2f2', color: '#b91c1c', borderRadius: '6px', fontSize: '0.875rem' }}>
            {errorMsg}
          </div>
        )}

        {/* Personal Details */}
        <fieldset style={{ border: '1px solid var(--zainx-color-border, #e2e8f0)', borderRadius: '6px', padding: '1rem' }}>
          <legend style={{ fontWeight: 600, padding: '0 0.5rem', fontSize: '0.875rem' }}>
            1. Personal Identity / الهوية الشخصية
          </legend>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
            <Field label="First Name (English) *" isRequired>
              <Input
                value={formData.firstNameEn}
                onChange={(e) => handleChange('firstNameEn', e.target.value)}
                placeholder="e.g. Tariq"
              />
            </Field>
            <Field label="Last Name (English) *" isRequired>
              <Input
                value={formData.lastNameEn}
                onChange={(e) => handleChange('lastNameEn', e.target.value)}
                placeholder="e.g. Al-Mansoor"
              />
            </Field>
            <Field label="الاسم الأول (عربي) *" isRequired>
              <Input
                value={formData.firstNameAr}
                onChange={(e) => handleChange('firstNameAr', e.target.value)}
                placeholder="مثال: طارق"
              />
            </Field>
            <Field label="اسم العائلة (عربي) *" isRequired>
              <Input
                value={formData.lastNameAr}
                onChange={(e) => handleChange('lastNameAr', e.target.value)}
                placeholder="مثال: المنصور"
              />
            </Field>
            <Field label="National ID / الهوية الوطنية *" isRequired>
              <Input
                value={formData.nationalIdentifier}
                onChange={(e) => handleChange('nationalIdentifier', e.target.value)}
                placeholder="10XXXXXXXX"
              />
            </Field>
            <Field label="Employee Number / الرقم الوظيفي *" isRequired>
              <Input
                value={formData.employeeNumber || ''}
                onChange={(e) => handleChange('employeeNumber', e.target.value)}
                placeholder="e.g. EMP-000123"
              />
            </Field>
            <Field label="Date of Birth / تاريخ الميلاد *" isRequired>
              <Input
                type="date"
                value={formData.dateOfBirth}
                onChange={(e) => handleChange('dateOfBirth', e.target.value)}
              />
            </Field>
          </div>
        </fieldset>

        {/* Contact Info */}
        <fieldset style={{ border: '1px solid var(--zainx-color-border, #e2e8f0)', borderRadius: '6px', padding: '1rem' }}>
          <legend style={{ fontWeight: 600, padding: '0 0.5rem', fontSize: '0.875rem' }}>
            2. Contact / بيانات الاتصال
          </legend>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
            <Field label="Work Email / البريد الإلكتروني">
              <Input
                type="email"
                value={formData.primaryEmail || ''}
                onChange={(e) => handleChange('primaryEmail', e.target.value)}
                placeholder="tariq@zainx.com"
              />
            </Field>
            <Field label="Phone / رقم الجوال">
              <Input
                value={formData.phoneNumber || ''}
                onChange={(e) => handleChange('phoneNumber', e.target.value)}
                placeholder="+9665XXXXXXXX"
              />
            </Field>
          </div>
        </fieldset>

        {/* Employment & Assignment */}
        <fieldset style={{ border: '1px solid var(--zainx-color-border, #e2e8f0)', borderRadius: '6px', padding: '1rem' }}>
          <legend style={{ fontWeight: 600, padding: '0 0.5rem', fontSize: '0.875rem' }}>
            3. Employment & Assignment / التعيين والتكليف
          </legend>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
            <Field label="Department / الإدارة *" isRequired>
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
            <Field label="Job Title (English) *" isRequired>
              <Input
                value={formData.jobTitleEn}
                onChange={(e) => handleChange('jobTitleEn', e.target.value)}
                placeholder="Senior HR Specialist"
              />
            </Field>
            <Field label="المسمى الوظيفي (عربي) *" isRequired>
              <Input
                value={formData.jobTitleAr}
                onChange={(e) => handleChange('jobTitleAr', e.target.value)}
                placeholder="أخصائي أول موارد بشرية"
              />
            </Field>
            <Field label="Hire Date / تاريخ المباشرة *" isRequired>
              <Input
                type="date"
                value={formData.hireDate}
                onChange={(e) => handleChange('hireDate', e.target.value)}
              />
            </Field>
          </div>
        </fieldset>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1rem' }}>
          <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
            Cancel / إلغاء
          </Button>
          <Button variant="primary" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving... / جاري الحفظ' : 'Save Employee / حفظ الموظف'}
          </Button>
        </div>
      </form>
    </Dialog>
  );
};
