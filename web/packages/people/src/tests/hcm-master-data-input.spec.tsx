import React from 'react';
import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { CreateEmployeeModal } from '../components/CreateEmployeeModal/CreateEmployeeModal';
import { DocumentsTab } from '../components/DocumentsTab/DocumentsTab';
import type { DocumentTypeDto, LocationDto, OrganizationUnitDto } from '@zainx/contracts';

const units: OrganizationUnitDto[] = [{
  id: '11111111-1111-1111-1111-111111111111',
  nameEn: 'People Operations',
  nameAr: 'عمليات الموظفين'
}];

const locations: LocationDto[] = [{
  id: '22222222-2222-2222-2222-222222222222',
  nameEn: 'Cairo Office',
  nameAr: 'مكتب القاهرة',
  city: 'Cairo'
}];

const documentTypes: DocumentTypeDto[] = [{
  id: '33333333-3333-3333-3333-333333333333',
  code: 'NATIONAL_ID',
  nameEn: 'National ID',
  nameAr: 'بطاقة الهوية'
}];

describe('HCM master-data input boundaries', () => {
  it('does not prefill fabricated employee identity or employment dates', () => {
    render(
      <CreateEmployeeModal
        isOpen
        onClose={vi.fn()}
        onSubmit={vi.fn()}
        departments={units}
        locations={locations}
      />
    );

    expect((screen.getByLabelText(/Employee Number/i) as HTMLInputElement).value).toBe('');
    expect((screen.getByLabelText(/National ID/i) as HTMLInputElement).value).toBe('');
    expect((screen.getByLabelText(/Date of Birth/i) as HTMLInputElement).value).toBe('');
    expect((screen.getByLabelText(/Hire Date/i) as HTMLInputElement).value).toBe('');
  });

  it('passes the selected document and metadata to the real upload callback', async () => {
    const onUpload = vi.fn().mockResolvedValue(undefined);
    render(
      <DocumentsTab
        documents={[]}
        documentTypes={documentTypes}
        onUpload={onUpload}
      />
    );

    fireEvent.click(screen.getByRole('button', { name: /Upload \/ رفع مستند/i }));
    fireEvent.change(screen.getByLabelText(/Document Title/i), { target: { value: 'Identity document' } });

    const file = new File(['identity'], 'identity.pdf', { type: 'application/pdf' });
    fireEvent.change(screen.getByLabelText(/Select File/i), { target: { files: [file] } });
    fireEvent.click(screen.getByRole('button', { name: /^Upload \/ رفع$/i }));

    await waitFor(() => expect(onUpload).toHaveBeenCalledWith({
      documentTypeId: documentTypes[0].id,
      title: 'Identity document',
      expiryDate: undefined,
      file
    }));
  });
});
