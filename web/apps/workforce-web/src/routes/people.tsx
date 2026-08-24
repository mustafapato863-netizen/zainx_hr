import React, { useState, Suspense, lazy } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import { 
  useGetApiV1PeopleEmployees, 
  useGetApiV1PeopleEmployeesId, 
  useGetApiV1Documents,
  useGetApiV1OrganizationUnits,
  useGetApiV1OrganizationLocations,
  usePostApiV1PeopleEmployees,
  usePostApiV1PeopleEmployeesIdAssignment,
  usePostApiV1PeopleEmployeesIdRevealSensitive,
  CreateEmployeeRequest,
  ChangeAssignmentRequest,
  EmployeeSummaryDto,
  OrganizationUnitDto,
  LocationDto
} from '@zainx/contracts';

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
  const { data: documentsData } = useGetApiV1Documents(
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

  // 5. Mutations
  const createEmployeeMutation = usePostApiV1PeopleEmployees();
  const changeAssignmentMutation = usePostApiV1PeopleEmployeesIdAssignment();
  const revealPiiMutation = usePostApiV1PeopleEmployeesIdRevealSensitive();

  const handleCreateEmployee = async (formData: CreateEmployeeRequest) => {
    try {
      await createEmployeeMutation.mutateAsync({
        data: formData
      });
      setIsCreateModalOpen(false);
      refetchDirectory();
    } catch (err: any) {
      alert(`Creation failed: ${err.message}`);
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

  const defaultUnits: OrganizationUnitDto[] = unitsData && unitsData.length > 0 ? unitsData : [
    {
      id: '11111111-2222-3333-4444-555555555555',
      tenantId: '22222222-2222-2222-2222-222222222222',
      legalEntityId: '33333333-3333-3333-3333-333333333333',
      code: 'ENG-01',
      nameEn: 'Engineering',
      nameAr: 'الهندسة',
      type: 'Department',
      isActive: true,
      effectiveFrom: '2024-01-01',
      rowVersion: 1
    }
  ];

  const defaultLocations: LocationDto[] = locationsData && locationsData.length > 0 ? locationsData : [
    {
      id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
      tenantId: '22222222-2222-2222-2222-222222222222',
      legalEntityId: '33333333-3333-3333-3333-333333333333',
      code: 'HQ-RUH',
      nameEn: 'Riyadh Headquarters',
      nameAr: 'المقر الرئيسي بالرياض',
      city: 'Riyadh',
      country: 'SA',
      isActive: true
    }
  ];

  return (
    <div className="space-y-6" data-testid="people-page-container">
      {concurrencyConflictError && (
        <div 
          data-testid="concurrency-conflict-banner" 
          className="p-4 bg-amber-50 border border-amber-300 rounded-lg flex items-center justify-between text-amber-900"
        >
          <div className="flex items-center gap-2">
            <span className="font-semibold">⚠️ Conflict Detected:</span>
            <span>{concurrencyConflictError}</span>
          </div>
          <button 
            data-testid="conflict-refresh-btn"
            onClick={() => {
              setConcurrencyConflictError(null);
              refetchProfile();
            }}
            className="px-3 py-1 bg-amber-600 hover:bg-amber-700 text-white rounded text-sm font-medium"
          >
            Refresh & Review
          </button>
        </div>
      )}

      <Suspense fallback={<div className="p-8 text-center text-slate-500 font-medium">Loading Workforce Experience...</div>}>
        {selectedEmployeeId && profileData ? (
          <div className="space-y-4">
            <button 
              data-testid="back-to-directory-btn"
              onClick={() => setSelectedEmployeeId(null)}
              className="text-sm font-medium text-indigo-600 hover:underline flex items-center gap-1"
            >
              ← Back to Directory
            </button>

            <EmployeeWorkspace 
              profile={profileData} 
              documents={documentsData || []} 
              isLoading={isProfileLoading}
              onBack={() => setSelectedEmployeeId(null)}
              onChangeAssignment={() => setIsChangeAssignmentModalOpen(true)}
              onRevealSensitive={handleRevealSensitive}
            />

            {isChangeAssignmentModalOpen && (
              <ChangeAssignmentModal
                isOpen={isChangeAssignmentModalOpen}
                onClose={() => setIsChangeAssignmentModalOpen(false)}
                onSubmit={handleChangeAssignment}
                departments={defaultUnits}
                locations={defaultLocations}
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
                departments={defaultUnits}
                locations={defaultLocations}
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
