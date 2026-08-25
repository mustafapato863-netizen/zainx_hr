import React, { useState } from 'react';
import {
  Button,
  Badge,
  Card,
  Input,
} from '@zainx/design-system';
import { ZainXDataGrid } from '@zainx/design-system/components/ZainXDataGrid/ZainXDataGrid';
import {
  useGetRequisitions,
  useOpenRequisition,
  usePutRequisitionOnHold,
  useCloseRequisition,
  useSubmitRequisitionForApproval,
  useApproveRequisition,
  JobRequisition,
} from '@zainx/contracts';
import { RequisitionStatus } from '../types';
import { CreateRequisitionDialog } from './CreateRequisitionDialog';

interface RequisitionsGridProps {
  onSelectRequisition?: (requisition: JobRequisition) => void;
}

export const RequisitionsGrid: React.FC<RequisitionsGridProps> = ({ onSelectRequisition }) => {
  const [statusFilter, setStatusFilter] = useState<string>('All');
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [isCreateOpen, setIsCreateOpen] = useState<boolean>(false);

  const { data: responseData, isLoading, refetch } = useGetRequisitions(
    statusFilter === 'All' ? undefined : ({ status: statusFilter } as any)
  );

  const requisitions: JobRequisition[] = (responseData as any)?.items || (Array.isArray(responseData) ? responseData : []);

  const openMutation = useOpenRequisition();
  const holdMutation = usePutRequisitionOnHold();
  const closeMutation = useCloseRequisition();
  const submitApprovalMutation = useSubmitRequisitionForApproval();
  const approveMutation = useApproveRequisition();

  const handleAction = async (action: string, req: JobRequisition) => {
    try {
      if (action === 'submitApproval') {
        await submitApprovalMutation.mutateAsync({
          id: req.id,
          data: { rowVersion: Number(req.rowVersion) },
        });
      } else if (action === 'approve') {
        await approveMutation.mutateAsync({
          id: req.id,
          data: { rowVersion: Number(req.rowVersion) },
        });
      } else if (action === 'open') {
        await openMutation.mutateAsync({
          id: req.id,
          data: { rowVersion: Number(req.rowVersion) },
        });
      } else if (action === 'hold') {
        await holdMutation.mutateAsync({
          id: req.id,
          data: { rowVersion: Number(req.rowVersion) },
        });
      } else if (action === 'close') {
        await closeMutation.mutateAsync({
          id: req.id,
          data: { rowVersion: Number(req.rowVersion) },
        });
      }
      refetch();
    } catch (err: any) {
      alert(`Action failed: ${err?.response?.data?.detail || err.message}`);
    }
  };

  const getStatusBadge = (status?: any) => {
    const s = String(status);
    if (s === 'Open' || s === '3') return <Badge variant="success">Open</Badge>;
    if (s === 'PendingApproval' || s === '1') return <Badge variant="warning">Pending Approval</Badge>;
    if (s === 'Approved' || s === '2') return <Badge variant="info">Approved</Badge>;
    if (s === 'Draft' || s === '0') return <Badge variant="neutral">Draft</Badge>;
    if (s === 'OnHold' || s === '4') return <Badge variant="warning">On Hold</Badge>;
    if (s === 'Closed' || s === '5') return <Badge variant="neutral">Closed</Badge>;
    if (s === 'Cancelled' || s === '6') return <Badge variant="danger">Cancelled</Badge>;
    return <Badge variant="neutral">{s}</Badge>;
  };

  const isStatus = (currentStatus: any, expected: string, numericVal: number) => {
    const s = String(currentStatus);
    return s === expected || s === String(numericVal);
  };

  const filteredRequisitions = requisitions.filter((r: JobRequisition) => {
    const term = searchTerm.toLowerCase();
    return (
      r.requisitionNumber?.toLowerCase().includes(term) ||
      r.titleEn?.toLowerCase().includes(term) ||
      r.titleAr?.includes(term)
    );
  });

  const columnDefs = [
    {
      field: 'requisitionNumber',
      headerName: 'Requisition #',
      flex: 1,
      cellRenderer: (params: any) => (
        <span className="font-mono font-semibold text-primary">{params.value}</span>
      ),
    },
    {
      field: 'titleEn',
      headerName: 'Job Title (EN / AR)',
      flex: 2,
      cellRenderer: (params: any) => (
        <div>
          <div className="font-medium text-foreground">{params.data.titleEn}</div>
          <div className="text-xs text-muted-foreground">{params.data.titleAr}</div>
        </div>
      ),
    },
    {
      field: 'openingsCount',
      headerName: 'Openings',
      width: 110,
    },
    {
      field: 'status',
      headerName: 'Status',
      width: 160,
      cellRenderer: (params: any) => getStatusBadge(params.value),
    },
    {
      field: 'targetStartDate',
      headerName: 'Target Start',
      width: 140,
    },
    {
      headerName: 'Actions',
      width: 260,
      cellRenderer: (params: any) => {
        const req: JobRequisition = params.data;
        return (
          <div className="flex items-center gap-1.5 py-1">
            {onSelectRequisition && (
              <Button
                size="sm"
                variant="outline"
                onClick={() => onSelectRequisition(req)}
                id={`btn-view-${req.requisitionNumber}`}
              >
                Workspace
              </Button>
            )}
            {isStatus(req.status, 'Draft', 0) && (
              <Button
                size="sm"
                variant="secondary"
                onClick={() => handleAction('submitApproval', req)}
              >
                Submit
              </Button>
            )}
            {isStatus(req.status, 'PendingApproval', 1) && (
              <Button
                size="sm"
                variant="primary"
                onClick={() => handleAction('approve', req)}
              >
                Approve
              </Button>
            )}
            {isStatus(req.status, 'Approved', 2) && (
              <Button
                size="sm"
                variant="primary"
                onClick={() => handleAction('open', req)}
              >
                Open
              </Button>
            )}
            {isStatus(req.status, 'Open', 3) && (
              <Button
                size="sm"
                variant="outline"
                onClick={() => handleAction('hold', req)}
              >
                Hold
              </Button>
            )}
            {isStatus(req.status, 'OnHold', 4) && (
              <Button
                size="sm"
                variant="outline"
                onClick={() => handleAction('open', req)}
              >
                Resume
              </Button>
            )}
            {(isStatus(req.status, 'Open', 3) || isStatus(req.status, 'OnHold', 4)) && (
              <Button
                size="sm"
                variant="danger"
                onClick={() => handleAction('close', req)}
              >
                Close
              </Button>
            )}
          </div>
        );
      },
    },
  ];

  return (
    <div className="space-y-4" data-testid="requisitions-grid-container">
      <div className="flex flex-col items-start gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-xl font-bold tracking-tight">Job Requisitions</h2>
          <p className="text-sm text-muted-foreground">
            Manage headcount vacancies, requisition lifecycles, and ATS pipeline assignments.
          </p>
        </div>
        <Button
          variant="primary"
          onClick={() => setIsCreateOpen(true)}
          id="btn-create-requisition"
        >
          + New Requisition
        </Button>
      </div>

      <Card className="overflow-hidden p-3 sm:p-4">
        <div className="mb-4 flex flex-col items-stretch gap-3 xl:flex-row xl:items-center xl:gap-4">
          <div className="w-full shrink-0 xl:w-72">
            <Input
              placeholder="Search by title or #..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              id="input-search-requisitions"
            />
          </div>
          <div className="flex min-w-0 items-center gap-2 overflow-x-auto pb-1">
            {['All', 'Open', 'PendingApproval', 'Draft', 'OnHold', 'Closed'].map((st) => (
              <Button
                key={st}
                size="sm"
                variant={statusFilter === st ? 'primary' : 'outline'}
                onClick={() => setStatusFilter(st)}
              >
                {st}
              </Button>
            ))}
          </div>
        </div>

        <div className="h-[480px] w-full overflow-x-auto">
          <div className="h-full min-w-[880px]">
            <ZainXDataGrid
              rowData={filteredRequisitions}
              columnDefs={columnDefs as any}
              loading={isLoading}
            />
          </div>
        </div>
      </Card>

      <CreateRequisitionDialog
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        onCreated={() => {
          setIsCreateOpen(false);
          refetch();
        }}
      />
    </div>
  );
};
