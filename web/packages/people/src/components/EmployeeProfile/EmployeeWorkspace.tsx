import React, { useState } from 'react';
import {
  PageHeader,
  Tabs,
  TabList,
  Tab,
  TabPanel,
  Badge,
  Button,
  Card,
  CardHeader,
  CardTitle,
  CardContent,
  SensitiveValue,
  Skeleton
} from '@zainx/design-system';
import { EmployeeProfileDto, DocumentSummaryDto } from '@zainx/contracts';

export interface EmployeeWorkspaceProps {
  profile?: EmployeeProfileDto;
  documents?: DocumentSummaryDto[];
  isLoading?: boolean;
  onBack?: () => void;
  onChangeAssignment?: () => void;
  onUploadDocument?: () => void;
  onDownloadDocument?: (docId: string) => void;
  onRevealSensitive?: (fieldName: string) => Promise<string | null>;
}

export const EmployeeWorkspace: React.FC<EmployeeWorkspaceProps> = ({
  profile,
  documents = [],
  isLoading = false,
  onBack,
  onChangeAssignment,
  onUploadDocument,
  onDownloadDocument,
  onRevealSensitive
}) => {
  const [selectedTabKey, setSelectedTabKey] = useState<string>('overview');
  const [revealedFields, setRevealedFields] = useState<Record<string, string>>({});

  const handleReveal = async (field: string) => {
    if (onRevealSensitive) {
      const val = await onRevealSensitive(field);
      if (val) {
        setRevealedFields(prev => ({ ...prev, [field]: val }));
      }
    }
  };

  if (isLoading || !profile) {
    return (
      <div style={{ padding: '1.5rem', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        <Skeleton height="60px" width="100%" />
        <Skeleton height="400px" width="100%" />
      </div>
    );
  }

  const status = (profile.status || 'active').toLowerCase();
  const statusVariant =
    status === 'active'
      ? 'success'
      : status === 'inactive'
      ? 'warning'
      : 'danger';

  const isNationalIdRevealed = !!revealedFields['nationalId'];
  const nationalIdValue = isNationalIdRevealed ? revealedFields['nationalId'] : profile.maskedNationalId;

  const isDobRevealed = !!revealedFields['dateOfBirth'];
  const dobValue = isDobRevealed ? revealedFields['dateOfBirth'] : profile.maskedDateOfBirth;

  return (
    <div className="zainx-employee-workspace" style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem', padding: '1.5rem' }}>
      {/* Workspace Header */}
      <PageHeader
        title={`${profile.fullNameEn || ''} (${profile.fullNameAr || ''})`}
        subtitle={`ID: ${profile.employeeNumber} • ${profile.currentAssignment?.jobTitleEn || 'N/A'} • ${profile.currentAssignment?.departmentNameEn || 'Unassigned'}`}
        badge={<Badge variant={statusVariant}>{profile.status || 'Active'}</Badge>}
        actions={
          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
            {onBack && (
              <Button size="xs" variant="secondary" onClick={onBack}>
                ← Back / عودة
              </Button>
            )}
            <Button size="xs" variant="secondary" onClick={onChangeAssignment}>
              Change Assignment / تغيير التكليف
            </Button>
            <Button size="xs" variant="primary" onClick={onUploadDocument}>
              Upload Document / رفع مستند
            </Button>
          </div>
        }
      />

      {/* Accessible semantic heading hierarchy */}
      <h2 className="sr-only">Employee Profile Workspace Sections</h2>

      {/* Navigation Tabs */}
      <Tabs
        selectedKey={selectedTabKey}
        onSelectionChange={(k) => setSelectedTabKey(k as string)}
      >
        <TabList>
          <Tab id="overview">Overview / نظرة عامة</Tab>
          <Tab id="employment">Employment & Assignment / التعيين والتكليف</Tab>
          <Tab id="organization">Organization / الهيكل التنظيمي</Tab>
          <Tab id="documents">Documents / المستندات ({documents.length})</Tab>
          <Tab id="audit">Audit History / سجل التدقيق</Tab>
        </TabList>

        {/* Tab: Overview */}
        <TabPanel id="overview">
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '1.5rem' }}>
            <Card>
              <CardHeader>
                <CardTitle>Personal Identity / البيانات الشخصية</CardTitle>
              </CardHeader>
              <CardContent>
                <dl style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem 1rem', fontSize: '0.875rem' }}>
                  <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>English Name:</dt>
                  <dd style={{ fontWeight: 600 }}>{profile.fullNameEn}</dd>

                  <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>Arabic Name:</dt>
                  <dd style={{ fontWeight: 600 }}>{profile.fullNameAr}</dd>

                  <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>Gender / الجنس:</dt>
                  <dd>{profile.gender}</dd>

                  <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>Nationality / الجنسية:</dt>
                  <dd>{profile.nationality}</dd>

                  <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>Date of Birth / تاريخ الميلاد:</dt>
                  <dd>
                    <SensitiveValue
                      value={dobValue}
                      state={isDobRevealed ? 'revealed' : 'masked'}
                      onRevealRequest={() => handleReveal('dateOfBirth')}
                      onMask={() => setRevealedFields(prev => {
                        const next = { ...prev };
                        delete next['dateOfBirth'];
                        return next;
                      })}
                    />
                  </dd>

                  <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>National ID / الهوية الوطنية:</dt>
                  <dd>
                    <SensitiveValue
                      value={nationalIdValue}
                      state={isNationalIdRevealed ? 'revealed' : 'masked'}
                      onRevealRequest={() => handleReveal('nationalId')}
                      onMask={() => setRevealedFields(prev => {
                        const next = { ...prev };
                        delete next['nationalId'];
                        return next;
                      })}
                    />
                  </dd>
                </dl>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Contact Information / بيانات الاتصال</CardTitle>
              </CardHeader>
              <CardContent>
                <dl style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem 1rem', fontSize: '0.875rem' }}>
                  <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>Work Email / البريد:</dt>
                  <dd style={{ fontWeight: 500 }}>{profile.primaryEmail || 'N/A'}</dd>

                  <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>Phone / الجوال:</dt>
                  <dd>{profile.phoneNumber || 'N/A'}</dd>

                  <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>Work Location / المقر:</dt>
                  <dd>{profile.currentAssignment?.locationNameEn || 'HQ - Riyadh'}</dd>
                </dl>
              </CardContent>
            </Card>
          </div>
        </TabPanel>

        {/* Tab: Employment & Assignment */}
        <TabPanel id="employment">
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
            <Card>
              <CardHeader>
                <CardTitle>Current Employment Terms / شروط العمل الحالية</CardTitle>
              </CardHeader>
              <CardContent>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', fontSize: '0.875rem' }}>
                  <div>
                    <span style={{ color: 'var(--zainx-color-text-muted, #94a3b8)', display: 'block' }}>Hire Date / تاريخ المباشرة</span>
                    <strong style={{ fontSize: '1rem' }}>{profile.hireDate}</strong>
                  </div>
                  <div>
                    <span style={{ color: 'var(--zainx-color-text-muted, #94a3b8)', display: 'block' }}>Probation End / نهاية التجربة</span>
                    <strong>{profile.probationEndDate || 'N/A'}</strong>
                  </div>
                  <div>
                    <span style={{ color: 'var(--zainx-color-text-muted, #94a3b8)', display: 'block' }}>Status / الحالة</span>
                    <Badge variant={statusVariant}>{profile.status}</Badge>
                  </div>
                  <div>
                    <span style={{ color: 'var(--zainx-color-text-muted, #94a3b8)', display: 'block' }}>Concurrency Token</span>
                    <code style={{ fontSize: '0.8rem' }}>v{profile.rowVersion}</code>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Assignment History Timeline / سجل التكليفات التاريخي</CardTitle>
              </CardHeader>
              <CardContent>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                  {(profile.assignmentHistory || []).map((assign) => (
                    <div
                      key={assign.id}
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        padding: '0.875rem 1rem',
                        borderRadius: 'var(--zainx-radius-md, 6px)',
                        border: '1px solid var(--zainx-color-border, #e2e8f0)',
                        background: assign.isCurrent ? 'var(--zainx-color-surface-selected, #f8fafc)' : 'transparent'
                      }}
                    >
                      <div style={{ display: 'flex', flexDirection: 'column' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                          <strong style={{ fontSize: '0.95rem' }}>{assign.jobTitleEn}</strong>
                          <span style={{ color: 'var(--zainx-color-text-muted, #94a3b8)', fontSize: '0.85rem' }}>({assign.jobTitleAr})</span>
                          {assign.isCurrent && <Badge variant="success">Current / الحالي</Badge>}
                        </div>
                        <span style={{ fontSize: '0.825rem', color: 'var(--zainx-color-text-muted, #64748b)' }}>
                          {assign.departmentNameEn} • {assign.locationNameEn}
                        </span>
                      </div>

                      <div style={{ textAlign: 'right', fontSize: '0.825rem' }}>
                        <span style={{ color: 'var(--zainx-color-text-muted, #94a3b8)', display: 'block' }}>Effective Period / الفترة</span>
                        <strong>
                          {assign.effectiveFrom} → {assign.effectiveTo || 'Present / حتى الآن'}
                        </strong>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        </TabPanel>

        {/* Tab: Organization */}
        <TabPanel id="organization">
          <Card>
            <CardHeader>
              <CardTitle>Organization Placement / الموضع التنظيمي</CardTitle>
            </CardHeader>
            <CardContent>
              <div style={{ padding: '1rem', background: 'var(--zainx-color-surface-subtle, #f8fafc)', borderRadius: '8px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                  <div style={{ padding: '0.75rem 1.25rem', background: 'var(--zainx-color-primary, #6366f1)', color: '#ffffff', borderRadius: '6px', fontWeight: 600 }}>
                    {profile.currentAssignment?.departmentNameEn || 'Department'}
                  </div>
                  <span>→</span>
                  <div style={{ padding: '0.75rem 1.25rem', background: '#ffffff', border: '1px solid var(--zainx-color-border, #cbd5e1)', borderRadius: '6px', fontWeight: 600 }}>
                    {profile.currentAssignment?.jobTitleEn || 'Job Role'}
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabPanel>

        {/* Tab: Documents */}
        <TabPanel id="documents">
          <Card>
            <CardHeader>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <CardTitle>Attached Workforce Documents / المستندات المرفقة</CardTitle>
                <Button size="xs" variant="primary" onClick={onUploadDocument}>
                  + Upload / رفع مستند
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              {documents.length === 0 ? (
                <p style={{ color: 'var(--zainx-color-text-muted, #94a3b8)', fontSize: '0.875rem' }}>
                  No documents attached yet / لا توجد مستندات مرفقة حتى الآن.
                </p>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                  {documents.map((doc) => {
                    const docStatus = (doc.status || 'Active').toLowerCase();
                    const docSizeKb = doc.latestFileSize ? (Number(doc.latestFileSize) / 1024).toFixed(1) : '0';
                    return (
                      <div
                        key={doc.id}
                        style={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                          padding: '0.75rem 1rem',
                          border: '1px solid var(--zainx-color-border, #e2e8f0)',
                          borderRadius: '6px'
                        }}
                      >
                        <div>
                          <strong>{doc.title}</strong> ({doc.documentTypeNameEn})
                          <div style={{ fontSize: '0.75rem', color: 'var(--zainx-color-text-muted, #94a3b8)' }}>
                            File: {doc.latestFileName} ({docSizeKb} KB) • Expiry: {doc.expiryDate || 'N/A'}
                          </div>
                        </div>
                        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                          <Badge variant={docStatus === 'active' ? 'success' : 'neutral'}>
                            {doc.status}
                          </Badge>
                          <Button
                            size="xs"
                            variant="secondary"
                            onClick={() => onDownloadDocument && doc.id && onDownloadDocument(doc.id)}
                          >
                            Download / تحميل
                          </Button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </CardContent>
          </Card>
        </TabPanel>

        {/* Tab: Audit History */}
        <TabPanel id="audit">
          <Card>
            <CardHeader>
              <CardTitle>System Audit Record / سجل التدقيق</CardTitle>
            </CardHeader>
            <CardContent>
              <dl style={{ display: 'grid', gridTemplateColumns: '1fr 2fr', gap: '0.75rem', fontSize: '0.875rem' }}>
                <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>Employment ID:</dt>
                <dd><code>{profile.id}</code></dd>

                <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>Person ID:</dt>
                <dd><code>{profile.personId}</code></dd>

                <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>Tenant ID:</dt>
                <dd><code>{profile.tenantId}</code></dd>

                <dt style={{ color: 'var(--zainx-color-text-muted, #94a3b8)' }}>Optimistic RowVersion:</dt>
                <dd><code>{profile.rowVersion}</code></dd>
              </dl>
            </CardContent>
          </Card>
        </TabPanel>
      </Tabs>
    </div>
  );
};
