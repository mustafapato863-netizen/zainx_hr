import React, { useState, Suspense, lazy } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import { 
  useGetApiV1PeopleEmployees, 
  useGetApiV1PeopleEmployeesId, 
  useGetApiV1Documents,
  useGetApiV1DocumentsTypes,
  getApiV1DocumentsIdDownload,
  usePostApiV1DocumentsUpload,
  useGetApiV1OrganizationUnits,
  useGetApiV1OrganizationLocations,
  usePostApiV1PeopleEmployees,
  usePostApiV1PeopleEmployeesIdAssignment,
  usePostApiV1PeopleEmployeesIdRevealSensitive,
  CreateEmployeeRequest,
  ChangeAssignmentRequest,
  EmployeeSummaryDto,
} from '@zainx/contracts';
import { Icon, PageHeader } from '@zainx/design-system';

// Lazy load people components with strict route-level chunk isolation
const EmployeeDirectory = lazy(() => import('@zainx/people/components/EmployeeDirectory/EmployeeDirectory').then(m => ({ default: m.EmployeeDirectory })));
const EmployeeWorkspace = lazy(() => import('@zainx/people/components/EmployeeProfile/EmployeeWorkspace').then(m => ({ default: m.EmployeeWorkspace })));
const CreateEmployeeModal = lazy(() => import('@zainx/people/components/CreateEmployeeModal/CreateEmployeeModal').then(m => ({ default: m.CreateEmployeeModal })));
const ChangeAssignmentModal = lazy(() => import('@zainx/people/components/ChangeAssignmentModal/ChangeAssignmentModal').then(m => ({ default: m.ChangeAssignmentModal })));

export function PeopleComponent() {
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string | null>(null);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isChangeAssignmentModalOpen, setIsChangeAssignmentModalOpen] = useState(false);
  const [concurrencyConflictError, setConcurrencyConflictError] = useState<string | null>(null);
  const [documentError, setDocumentError] = useState<string | null>(null);

  // 1. Directory Query
  const { data: directoryData, isLoading: isDirLoading, refetch: refetchDirectory } = useGetApiV1PeopleEmployees();

  // 2. Units and Locations
  const { data: unitsData } = useGetApiV1OrganizationUnits();
  const { data: locationsData } = useGetApiV1OrganizationLocations();

  // 3. Selected Employee Profile Query
  const { data: profileData, isLoading: isProfileLoading, refetch: refetchProfile } = useGetApiV1PeopleEmployeesId(
    selectedEmployeeId || '',
    {
      query: {
        enabled: !!selectedEmployeeId
      }
    }
  );

  // 4. Employee Documents Query
  const { data: documentsData, refetch: refetchDocuments } = useGetApiV1Documents(
    {
      ownerType: 'Employee',
      ownerId: selectedEmployeeId || ''
    },
    {
      query: {
        enabled: !!selectedEmployeeId
      }
    }
  );
  const { data: documentTypesData } = useGetApiV1DocumentsTypes();

  // 5. Mutations
  const createEmployeeMutation = usePostApiV1PeopleEmployees();
  const changeAssignmentMutation = usePostApiV1PeopleEmployeesIdAssignment();
  const revealPiiMutation = usePostApiV1PeopleEmployeesIdRevealSensitive();
  const uploadDocumentMutation = usePostApiV1DocumentsUpload();

  const handleCreateEmployee = async (formData: CreateEmployeeRequest) => {
    try {
      await createEmployeeMutation.mutateAsync({
        data: formData
      });
      await refetchDirectory();
      setIsCreateModalOpen(false);
    } catch (err: any) {
      throw new Error(err?.message || 'Creation failed. Please review the form and try again.', { cause: err });
    }
  };

  const handleChangeAssignment = async (data: ChangeAssignmentRequest) => {
    if (!selectedEmployeeId) return;
    setConcurrencyConflictError(null);
    try {
      await changeAssignmentMutation.mutateAsync({
        id: selectedEmployeeId,
        data: {
          organizationUnitId: data.organizationUnitId,
          jobTitleEn: data.jobTitleEn,
          jobTitleAr: data.jobTitleAr,
          effectiveFrom: data.effectiveFrom,
          rowVersion: data.rowVersion,
          positionId: data.positionId,
          locationId: data.locationId,
          managerEmploymentId: data.managerEmploymentId
        }
      });
      setIsChangeAssignmentModalOpen(false);
      refetchProfile();
      refetchDirectory();
    } catch (err: any) {
      if (err.status === 409 || err.message?.includes('409') || err.message?.includes('Conflict')) {
        setConcurrencyConflictError('Optimistic Concurrency Conflict: The record was modified by another operation. Please review and refresh.');
      } else {
        alert(`Assignment change failed: ${err.message}`);
      }
    }
  };

  const handleRevealSensitive = async (field: string): Promise<string | null> => {
    if (!selectedEmployeeId) return null;
    try {
      const res = await revealPiiMutation.mutateAsync({
        id: selectedEmployeeId,
        data: {
          fieldName: field,
          purpose: 'Operational Workforce Verification'
        }
      });
      return res.plaintextValue || null;
    } catch {
      return null;
    }
  };

  const handleUploadDocument = async (data: { documentTypeId: string; title: string; expiryDate?: string; file: File }) => {
    if (!selectedEmployeeId) throw new Error('Select an employee before uploading a document.');

    setDocumentError(null);
    await uploadDocumentMutation.mutateAsync({
      data: {
        OwnerType: 'Employee',
        OwnerId: selectedEmployeeId,
        DocumentTypeId: data.documentTypeId,
        Title: data.title,
        ExpiryDate: data.expiryDate,
        File: data.file
      }
    });
    await refetchProfile();
    await refetchDocuments();
  };

  const handleDownloadDocument = async (documentId: string) => {
    try {
      setDocumentError(null);
      const blob = await getApiV1DocumentsIdDownload(documentId);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = 'workforce-document';
      anchor.click();
      URL.revokeObjectURL(url);
    } catch {
      setDocumentError('The document could not be downloaded from the service.');
    }
  };

  const availableUnits = unitsData ?? [];
  const availableLocations = locationsData ?? [];

  return (
    <div className="space-y-6" data-testid="people-page-container">
      {concurrencyConflictError && (
        <div 
          data-testid="concurrency-conflict-banner" 
          className="flex items-center justify-between rounded-lg border border-warning/30 bg-warning-subtle p-4 text-warning-subtle-text"
        >
          <div className="flex items-center gap-2">
            <span className="inline-flex items-center gap-1.5 font-semibold"><Icon name="alert-triangle" size="sm" aria-hidden="true" />Conflict Detected:</span>
            <span>{concurrencyConflictError}</span>
          </div>
          <button 
            data-testid="conflict-refresh-btn"
            onClick={() => {
              setConcurrencyConflictError(null);
              refetchProfile();
            }}
            className="rounded-md bg-warning px-3 py-1 text-sm font-medium text-white hover:bg-warning-hover"
          >
            Refresh & Review
          </button>
        </div>
      )}

      {documentError && (
        <div role="alert" className="flex items-center justify-between rounded-lg border border-danger/30 bg-danger-subtle p-4 text-danger-subtle-text">
          <span>{documentError}</span>
          <button type="button" className="text-sm font-medium underline" onClick={() => setDocumentError(null)}>
            Dismiss
          </button>
        </div>
      )}

      <Suspense fallback={<div className="space-y-5" aria-busy="true"><PageHeader title="Employee Directory / دليل الموظفين" subtitle="Authoritative workforce master data / السجل الرئيسي للقوى العاملة" /><div className="rounded-lg border border-border-default bg-surface-subtle p-8 text-center text-sm font-medium text-text-secondary"><div className="mx-auto mb-3 h-8 w-8 animate-pulse rounded-full bg-primary-subtle" />Loading Workforce Experience...</div></div>}>
        {selectedEmployeeId && profileData ? (
          <div className="space-y-4">
            <button 
              data-testid="back-to-directory-btn"
              onClick={() => setSelectedEmployeeId(null)}
                className="flex items-center gap-1 text-sm font-medium text-text-link hover:underline"
            >
              <Icon name="arrow-left" size="sm" aria-hidden="true" />Back to Directory
            </button>

            <EmployeeWorkspace 
              profile={profileData} 
              documents={documentsData || []} 
              documentTypes={documentTypesData || []}
              isLoading={isProfileLoading}
              onBack={() => setSelectedEmployeeId(null)}
              onChangeAssignment={() => setIsChangeAssignmentModalOpen(true)}
              onUploadDocument={handleUploadDocument}
              onDownloadDocument={handleDownloadDocument}
              onRevealSensitive={handleRevealSensitive}
            />

            {isChangeAssignmentModalOpen && (
              <ChangeAssignmentModal
                isOpen={isChangeAssignmentModalOpen}
                onClose={() => setIsChangeAssignmentModalOpen(false)}
                onSubmit={handleChangeAssignment}
                departments={availableUnits}
                locations={availableLocations}
                currentRowVersion={Number(profileData.rowVersion) || 1}
              />
            )}
          </div>
        ) : (
          <div className="space-y-4">
            <EmployeeDirectory 
              employees={directoryData?.items || []} 
              onSelectEmployee={(emp: EmployeeSummaryDto) => setSelectedEmployeeId(emp.id || null)}
              onCreateEmployee={() => setIsCreateModalOpen(true)}
              onRefresh={() => refetchDirectory()}
              isLoading={isDirLoading}
            />

            {isCreateModalOpen && (
              <CreateEmployeeModal 
                isOpen={isCreateModalOpen} 
                onClose={() => setIsCreateModalOpen(false)} 
                onSubmit={handleCreateEmployee}
                departments={availableUnits}
                locations={availableLocations}
              />
            )}
          </div>
        )}
      </Suspense>
    </div>
  );
}

export const peopleRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/people',
  component: PeopleComponent,
});
