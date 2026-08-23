import React, { useState } from 'react';
import {
  Card,
  CardHeader,
  CardTitle,
  CardContent,
  Button,
  Badge,
  Dialog,
  Field,
  Input
} from '@zainx/design-system';
import { DocumentSummaryDto, DocumentTypeDto } from '@zainx/contracts';

export interface DocumentsTabProps {
  documents: DocumentSummaryDto[];
  documentTypes: DocumentTypeDto[];
  onUpload: (data: { documentTypeId: string; title: string; expiryDate?: string; file: File }) => Promise<void>;
  onDownload: (docId: string) => void;
}

export const DocumentsTab: React.FC<DocumentsTabProps> = ({
  documents,
  documentTypes,
  onUpload,
  onDownload
}) => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [docTypeId, setDocTypeId] = useState(documentTypes[0]?.id || '');
  const [title, setTitle] = useState('');
  const [expiryDate, setExpiryDate] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const handleUploadSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file || !title || !docTypeId) {
      setErrorMsg('Please select a file, document type, and title.');
      return;
    }

    try {
      setIsUploading(true);
      setErrorMsg('');
      await onUpload({
        documentTypeId: docTypeId,
        title,
        expiryDate: expiryDate || undefined,
        file
      });
      setIsModalOpen(false);
      setTitle('');
      setExpiryDate('');
      setFile(null);
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to upload document.');
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <CardTitle>Workforce Documents / مستندات الموظف</CardTitle>
          <Button size="xs" variant="primary" onClick={() => setIsModalOpen(true)}>
            + Upload / رفع مستند
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {documents.length === 0 ? (
          <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--zainx-color-text-muted, #94a3b8)' }}>
            No documents found for this employee.
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
            {documents.map((doc) => {
              const docStatus = (doc.status || 'active').toLowerCase();
              const docSizeKb = doc.latestFileSize ? (Number(doc.latestFileSize) / 1024).toFixed(1) : '0';
              return (
                <div
                  key={doc.id}
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    padding: '0.875rem 1rem',
                    border: '1px solid var(--zainx-color-border, #e2e8f0)',
                    borderRadius: '6px'
                  }}
                >
                  <div>
                    <div style={{ fontWeight: 600, fontSize: '0.95rem' }}>{doc.title}</div>
                    <div style={{ fontSize: '0.8rem', color: 'var(--zainx-color-text-muted, #64748b)' }}>
                      Type: {doc.documentTypeNameEn} ({doc.documentTypeNameAr}) • File: {doc.latestFileName} (
                      {docSizeKb} KB)
                    </div>
                    {doc.expiryDate && (
                      <div style={{ fontSize: '0.75rem', color: '#d97706', marginTop: '0.25rem' }}>
                        Expires: {doc.expiryDate}
                      </div>
                    )}
                  </div>

                  <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
                    <Badge variant={docStatus === 'active' ? 'success' : 'neutral'}>
                      {doc.status || 'Active'}
                    </Badge>
                    <Button size="xs" variant="secondary" onClick={() => doc.id && onDownload(doc.id)}>
                      Download / تحميل
                    </Button>
                  </div>
                </div>
              );
            })}
          </div>
        )}

        {/* Upload Dialog */}
        <Dialog
          isOpen={isModalOpen}
          onOpenChange={(open) => {
            if (!open) setIsModalOpen(false);
          }}
          title="Upload Document / رفع مستند"
          description="Attach a verified workforce document to the employee master record."
        >
          <form onSubmit={handleUploadSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            {errorMsg && (
              <div style={{ padding: '0.75rem', background: '#fef2f2', color: '#b91c1c', borderRadius: '6px', fontSize: '0.875rem' }}>
                {errorMsg}
              </div>
            )}

            <Field label="Document Type / نوع المستند *" isRequired>
              <select
                value={docTypeId}
                onChange={(e) => setDocTypeId(e.target.value)}
                style={{
                  width: '100%',
                  padding: '0.5rem',
                  borderRadius: '6px',
                  border: '1px solid var(--zainx-color-border, #cbd5e1)'
                }}
              >
                {documentTypes.map((dt) => (
                  <option key={dt.id} value={dt.id}>
                    {dt.nameEn} ({dt.nameAr})
                  </option>
                ))}
              </select>
            </Field>

            <Field label="Document Title / عنوان المستند *" isRequired>
              <Input
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="e.g. National ID Copy 2026"
              />
            </Field>

            <Field label="Expiry Date / تاريخ الانتهاء (إن وجد)">
              <Input
                type="date"
                value={expiryDate}
                onChange={(e) => setExpiryDate(e.target.value)}
              />
            </Field>

            <Field label="Select File / اختر الملف (PDF, PNG, JPG) *" isRequired>
              <input
                type="file"
                accept=".pdf,.png,.jpg,.jpeg"
                onChange={(e) => setFile(e.target.files?.[0] || null)}
                style={{ display: 'block', marginTop: '0.25rem', fontSize: '0.875rem' }}
              />
            </Field>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1rem' }}>
              <Button variant="secondary" onClick={() => setIsModalOpen(false)} disabled={isUploading}>
                Cancel / إلغاء
              </Button>
              <Button variant="primary" type="submit" disabled={isUploading}>
                {isUploading ? 'Uploading... / جاري الرفع' : 'Upload / رفع'}
              </Button>
            </div>
          </form>
        </Dialog>
      </CardContent>
    </Card>
  );
};
