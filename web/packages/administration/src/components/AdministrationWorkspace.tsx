import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';

export interface RoleItem {
  id: string;
  code: string;
  nameEn: string;
  nameAr: string;
  description: string;
  permissionsJson: string;
  isSystemRole: boolean;
  rowVersion: number;
}

export interface RoleAssignmentItem {
  id: string;
  userId: string;
  roleId: string;
  legalEntityScopeId?: string;
  assignedByUserId: string;
  assignedAtUtc: string;
}

export interface SettingItem {
  id: string;
  category: string;
  key: string;
  valueJson: string;
  effectiveStartDate: string;
  effectiveEndDate?: string;
  isCurrent: boolean;
  rowVersion: number;
}

export interface RetentionPolicyItem {
  id: string;
  module: string;
  dataCategory: string;
  retentionDays: number;
  actionOnExpiry: number; // 1=Anonymize, 2=Archive, 3=Purge
  isActive: boolean;
  effectiveStartDate: string;
  rowVersion: number;
}

export interface ConnectorItem {
  id: string;
  code: string;
  nameEn: string;
  nameAr: string;
  connectorType: number;
  direction: number;
  endpointUrl: string;
  authType: number;
  isActive: boolean;
  eventSubscriptionsJson: string;
  rowVersion: number;
}

export interface DeliveryItem {
  id: string;
  connectorId: string;
  eventType: string;
  status: string | number;
  attemptCount: number;
  maxAttempts: number;
  lastHttpStatus?: number;
  lastErrorMessage?: string;
  createdAtUtc: string;
}

export interface AuditRecordItem {
  id: string;
  actorUserId: string;
  actorType: string;
  actionCode: string;
  entityType: string;
  entityId: string;
  occurredAtUtc: string;
  correlationId?: string;
  changesBeforeJson?: string;
  changesAfterJson?: string;
  dataClassification: string;
}

export function AdministrationWorkspace() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === 'ar';

  const [activeTab, setActiveTab] = useState<'ROLES' | 'ASSIGNMENTS' | 'SETTINGS' | 'RETENTION' | 'INTEGRATIONS' | 'AUDIT'>('ROLES');

  // Tab Data States
  const [roles, setRoles] = useState<RoleItem[]>([]);
  const [assignments, setAssignments] = useState<RoleAssignmentItem[]>([]);
  const [settings, setSettings] = useState<SettingItem[]>([]);
  const [retentionPolicies, setRetentionPolicies] = useState<RetentionPolicyItem[]>([]);
  const [connectors, setConnectors] = useState<ConnectorItem[]>([]);
  const [deliveries, setDeliveries] = useState<DeliveryItem[]>([]);
  const [auditRecords, setAuditRecords] = useState<AuditRecordItem[]>([]);
  const [availablePermissions, setAvailablePermissions] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  // Modal States
  const [showRoleModal, setShowRoleModal] = useState(false);
  const [selectedRole, setSelectedRole] = useState<RoleItem | null>(null);
  const [roleCode, setRoleCode] = useState('');
  const [roleNameEn, setRoleNameEn] = useState('');
  const [roleNameAr, setRoleNameAr] = useState('');
  const [roleDesc, setRoleDesc] = useState('');
  const [selectedPerms, setSelectedPerms] = useState<string[]>([]);
  const [privilegeError, setPrivilegeError] = useState<string | null>(null);

  // Audit Diff Modal
  const [inspectAudit, setInspectAudit] = useState<AuditRecordItem | null>(null);

  // Connector Modal
  const [showConnectorModal, setShowConnectorModal] = useState(false);
  const [connCode, setConnCode] = useState('');
  const [connNameEn, setConnNameEn] = useState('');
  const [connNameAr, setConnNameAr] = useState('');
  const [connEndpoint, setConnEndpoint] = useState('');
  const [connSecret, setConnSecret] = useState('');

  const fetchRoles = async () => {
    try {
      const res = await fetch('/api/v1/admin/roles');
      if (res.ok) setRoles(await res.json());
      const pRes = await fetch('/api/v1/admin/permissions');
      if (pRes.ok) setAvailablePermissions(await pRes.json());
    } catch {
      setRoles([
        { id: '1', code: 'SUPER_ADMIN', nameEn: 'Super Administrator', nameAr: 'المشرف العام', description: 'Full access', permissionsJson: '["*"]', isSystemRole: true, rowVersion: 1 },
        { id: '2', code: 'HR_MANAGER', nameEn: 'HR Operations Manager', nameAr: 'مدير الموارد البشرية', description: 'HR workflows', permissionsJson: '["people.read", "people.write", "leave.read", "attendance.read"]', isSystemRole: true, rowVersion: 1 },
        { id: '3', code: 'PAYROLL_ADMIN', nameEn: 'Payroll Specialist', nameAr: 'مسؤول الرواتب', description: 'Payroll execution', permissionsJson: '["payroll.read", "payroll.run", "payroll.result.read_sensitive"]', isSystemRole: true, rowVersion: 1 }
      ]);
    }
  };

  const fetchAssignments = async () => {
    try {
      const res = await fetch('/api/v1/admin/role-assignments');
      if (res.ok) setAssignments(await res.json());
    } catch {
      setAssignments([]);
    }
  };

  const fetchSettings = async () => {
    try {
      const res = await fetch('/api/v1/admin/settings');
      if (res.ok) setSettings(await res.json());
    } catch {
      setSettings([
        { id: 's1', category: 'Compliance', key: 'GOSI_CONTRIBUTION_RATE', valueJson: '{"employee": 0.0975, "employer": 0.1175}', effectiveStartDate: '2026-01-01', isCurrent: true, rowVersion: 1 },
        { id: 's2', category: 'Security', key: 'PASSWORD_EXPIRY_DAYS', valueJson: '{"days": 90}', effectiveStartDate: '2026-01-01', isCurrent: true, rowVersion: 1 }
      ]);
    }
  };

  const fetchRetention = async () => {
    try {
      const res = await fetch('/api/v1/admin/retention-policies');
      if (res.ok) setRetentionPolicies(await res.json());
    } catch {
      setRetentionPolicies([
        { id: 'r1', module: 'Audit', dataCategory: 'SecurityLogs', retentionDays: 2555, actionOnExpiry: 2, isActive: true, effectiveStartDate: '2026-01-01', rowVersion: 1 },
        { id: 'r2', module: 'Recruitment', dataCategory: 'RejectedCandidateCV', retentionDays: 365, actionOnExpiry: 1, isActive: true, effectiveStartDate: '2026-01-01', rowVersion: 1 },
        { id: 'r3', module: 'Payroll', dataCategory: 'MonthlySnapshots', retentionDays: 3650, actionOnExpiry: 2, isActive: true, effectiveStartDate: '2026-01-01', rowVersion: 1 }
      ]);
    }
  };

  const fetchIntegrations = async () => {
    try {
      const cRes = await fetch('/api/v1/integrations/connectors');
      if (cRes.ok) setConnectors(await cRes.json());
      const dRes = await fetch('/api/v1/integrations/deliveries?pageSize=20');
      if (dRes.ok) {
        const dData = await dRes.json();
        setDeliveries(dData.items || []);
      }
    } catch {
      setConnectors([
        { id: 'c1', code: 'GENERIC_WEBHOOK', nameEn: 'Enterprise Webhook Dispatcher', nameAr: 'مرسل الويب هوك للمؤسسة', connectorType: 1, direction: 1, endpointUrl: 'https://api.enterprise.com/webhooks/zainx', authType: 3, isActive: true, eventSubscriptionsJson: '["CandidateHiredEvent"]', rowVersion: 1 }
      ]);
    }
  };

  const fetchAudit = async () => {
    try {
      const res = await fetch('/api/v1/audit?pageSize=50');
      if (res.ok) {
        const data = await res.json();
        setAuditRecords(data.items || []);
      }
    } catch {
      setAuditRecords([
        { id: 'a1', actorUserId: '11111111-1111-1111-1111-111111111111', actorType: 'User', actionCode: 'role.assigned', entityType: 'RoleAssignment', entityId: '22222222-2222-2222-2222-222222222222', occurredAtUtc: new Date().toISOString(), correlationId: 'corr-init-01', dataClassification: 'Restricted' }
      ]);
    }
  };

  useEffect(() => {
    if (activeTab === 'ROLES') fetchRoles();
    else if (activeTab === 'ASSIGNMENTS') fetchAssignments();
    else if (activeTab === 'SETTINGS') fetchSettings();
    else if (activeTab === 'RETENTION') fetchRetention();
    else if (activeTab === 'INTEGRATIONS') fetchIntegrations();
    else if (activeTab === 'AUDIT') fetchAudit();
  }, [activeTab]);

  const handleSaveRole = async () => {
    setPrivilegeError(null);
    try {
      if (selectedRole) {
        // Update
        const res = await fetch(`/api/v1/admin/roles/${selectedRole.id}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            nameEn: roleNameEn,
            nameAr: roleNameAr,
            description: roleDesc,
            permissions: selectedPerms,
            expectedVersion: selectedRole.rowVersion
          })
        });

        if (res.status === 403) {
          const err = await res.json();
          setPrivilegeError(err.detail || 'Privilege Escalation Forbidden.');
          return;
        }

        if (res.status === 409) {
          setPrivilegeError('Concurrency conflict: this role was modified by another administrator.');
          return;
        }

        if (res.ok) {
          setShowRoleModal(false);
          fetchRoles();
        }
      } else {
        // Create
        const res = await fetch('/api/v1/admin/roles', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            code: roleCode.toUpperCase(),
            nameEn: roleNameEn,
            nameAr: roleNameAr,
            description: roleDesc,
            permissions: selectedPerms
          })
        });

        if (res.status === 403) {
          const err = await res.json();
          setPrivilegeError(err.detail || 'Privilege Escalation Forbidden: you cannot grant permissions you do not hold.');
          return;
        }

        if (res.ok) {
          setShowRoleModal(false);
          fetchRoles();
        }
      }
    } catch (e: any) {
      setPrivilegeError(e.message);
    }
  };

  const handleCreateConnector = async () => {
    try {
      const res = await fetch('/api/v1/integrations/connectors', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          code: connCode.toUpperCase(),
          nameEn: connNameEn,
          nameAr: connNameAr,
          connectorType: 1,
          direction: 1,
          endpointUrl: connEndpoint,
          authType: 3, // HMAC
          secretCredential: connSecret,
          isActive: true,
          eventSubscriptionsJson: '["CandidateHiredEvent", "PayrollFinalizedEvent"]',
          configJson: '{}'
        })
      });

      if (res.ok) {
        setShowConnectorModal(false);
        fetchIntegrations();
      }
    } catch {
      setShowConnectorModal(false);
    }
  };

  const retryDelivery = async (id: string) => {
    try {
      await fetch(`/api/v1/integrations/deliveries/${id}/retry`, { method: 'POST' });
      fetchIntegrations();
    } catch { }
  };

  return (
    <div className="space-y-6" data-testid="administration-workspace">
      {/* Page Title */}
      <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">
            {isAr ? 'إدارة المنصة والتحكم التشغيلي' : 'Platform Administration & Governance'}
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            {isAr
              ? 'إدارة الأدوار والصلاحيات، التكوينات المحددة زمنياً، التكاملات، وسجل التدقيق غير القابل للتعديل.'
              : 'Role-based access control, effective-dated settings, outbox integration webhooks, and immutable audit trails.'}
          </p>
        </div>

        {activeTab === 'ROLES' && (
          <button
            data-testid="create-role-btn"
            onClick={() => {
              setSelectedRole(null);
              setRoleCode('');
              setRoleNameEn('');
              setRoleNameAr('');
              setRoleDesc('');
              setSelectedPerms([]);
              setPrivilegeError(null);
              setShowRoleModal(true);
            }}
            className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm rounded-xl shadow-sm transition-colors flex items-center gap-2"
          >
            + {isAr ? 'إنشاء دور جديد' : 'Create Role'}
          </button>
        )}

        {activeTab === 'INTEGRATIONS' && (
          <button
            data-testid="create-connector-btn"
            onClick={() => {
              setConnCode('');
              setConnNameEn('');
              setConnNameAr('');
              setConnEndpoint('');
              setConnSecret('');
              setShowConnectorModal(true);
            }}
            className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm rounded-xl shadow-sm transition-colors flex items-center gap-2"
          >
            + {isAr ? 'إضافة موصل تكاملي' : 'New Connector'}
          </button>
        )}
      </div>

      {/* Navigation Tabs */}
      <div className="flex border-b border-slate-200 bg-white rounded-2xl p-2 gap-2 shadow-sm">
        {[
          { key: 'ROLES', labelEn: 'Roles & Permissions', labelAr: 'الأدوار والصلاحيات' },
          { key: 'ASSIGNMENTS', labelEn: 'Role Assignments', labelAr: 'تعيينات الأدوار' },
          { key: 'SETTINGS', labelEn: 'Platform Settings', labelAr: 'إعدادات المنصة' },
          { key: 'RETENTION', labelEn: 'Retention Policies', labelAr: 'سياسات حفظ البيانات' },
          { key: 'INTEGRATIONS', labelEn: 'Integrations & Webhooks', labelAr: 'التكاملات والويب هوك' },
          { key: 'AUDIT', labelEn: 'Audit Trail', labelAr: 'سجل التدقيق' }
        ].map(tab => (
          <button
            key={tab.key}
            data-testid={`admin-tab-${tab.key.toLowerCase()}`}
            onClick={() => setActiveTab(tab.key as any)}
            className={`px-4 py-2.5 rounded-xl font-semibold text-xs transition-all ${activeTab === tab.key ? 'bg-slate-900 text-white shadow-sm' : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'}`}
          >
            {isAr ? tab.labelAr : tab.labelEn}
          </button>
        ))}
      </div>

      {/* TAB 1: Roles & Permissions */}
      {activeTab === 'ROLES' && (
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden p-6 space-y-4">
          <div className="flex justify-between items-center">
            <h2 className="font-bold text-slate-800 text-sm">
              {isAr ? 'الأدوار المعرفة في النظام' : 'Defined System & Custom Roles'}
            </h2>
            <span className="text-xs text-slate-400 font-medium">{roles.length} roles</span>
          </div>

          <div className="overflow-x-auto rounded-xl border border-slate-200">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="bg-slate-50 text-slate-700 border-b border-slate-200">
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Code</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Name</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Type</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Permissions</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Version</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {roles.map(r => (
                  <tr key={r.id} data-testid={`role-row-${r.code}`} className="hover:bg-slate-50 transition-colors">
                    <td className="py-3 px-4 font-mono font-bold text-slate-900">{r.code}</td>
                    <td className="py-3 px-4 text-slate-800 font-medium">{isAr ? r.nameAr : r.nameEn}</td>
                    <td className="py-3 px-4">
                      <span className={`text-[10px] font-semibold px-2 py-0.5 rounded border ${r.isSystemRole ? 'bg-indigo-50 text-indigo-700 border-indigo-200' : 'bg-emerald-50 text-emerald-700 border-emerald-200'}`}>
                        {r.isSystemRole ? 'System' : 'Custom'}
                      </span>
                    </td>
                    <td className="py-3 px-4 font-mono text-[11px] text-slate-500 max-w-xs truncate">{r.permissionsJson}</td>
                    <td className="py-3 px-4 font-mono text-slate-400">v{r.rowVersion}</td>
                    <td className="py-3 px-4">
                      <button
                        onClick={() => {
                          setSelectedRole(r);
                          setRoleCode(r.code);
                          setRoleNameEn(r.nameEn);
                          setRoleNameAr(r.nameAr);
                          setRoleDesc(r.description);
                          try { setSelectedPerms(JSON.parse(r.permissionsJson)); } catch { setSelectedPerms([]); }
                          setPrivilegeError(null);
                          setShowRoleModal(true);
                        }}
                        className="text-indigo-600 hover:text-indigo-900 font-semibold"
                      >
                        {isAr ? 'تعديل' : 'Edit'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 2: Role Assignments */}
      {activeTab === 'ASSIGNMENTS' && (
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden p-6 space-y-4">
          <div className="flex justify-between items-center">
            <h2 className="font-bold text-slate-800 text-sm">{isAr ? 'التعيينات النشطة' : 'Active Role Assignments'}</h2>
            <span className="text-xs text-slate-400 font-medium">{assignments.length} assignments</span>
          </div>

          <div className="overflow-x-auto rounded-xl border border-slate-200">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="bg-slate-50 text-slate-700 border-b border-slate-200">
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">User ID</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Role ID</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Scope</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Assigned At</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {assignments.map(a => (
                  <tr key={a.id} className="hover:bg-slate-50 transition-colors">
                    <td className="py-3 px-4 font-mono text-slate-900">{a.userId}</td>
                    <td className="py-3 px-4 font-mono text-slate-700">{a.roleId}</td>
                    <td className="py-3 px-4 text-slate-500">{a.legalEntityScopeId || 'Tenant-Wide'}</td>
                    <td className="py-3 px-4 text-slate-400">{new Date(a.assignedAtUtc).toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 3: Platform Settings */}
      {activeTab === 'SETTINGS' && (
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden p-6 space-y-4">
          <div className="flex justify-between items-center">
            <h2 className="font-bold text-slate-800 text-sm">{isAr ? 'الإعدادات المحددة زمنياً' : 'Effective-Dated Settings'}</h2>
            <span className="text-xs text-slate-400 font-medium">{settings.length} parameters</span>
          </div>

          <div className="overflow-x-auto rounded-xl border border-slate-200">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="bg-slate-50 text-slate-700 border-b border-slate-200">
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Category</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Key</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Value</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Effective From</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {settings.map(s => (
                  <tr key={s.id} className="hover:bg-slate-50 transition-colors">
                    <td className="py-3 px-4 font-bold text-slate-900">{s.category}</td>
                    <td className="py-3 px-4 font-mono text-indigo-700">{s.key}</td>
                    <td className="py-3 px-4 font-mono text-slate-600 max-w-sm truncate">{s.valueJson}</td>
                    <td className="py-3 px-4 text-slate-500">{s.effectiveStartDate}</td>
                    <td className="py-3 px-4">
                      <span className="text-[10px] font-semibold px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200">
                        Current
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 4: Retention Policies */}
      {activeTab === 'RETENTION' && (
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden p-6 space-y-4">
          <div className="flex justify-between items-center">
            <h2 className="font-bold text-slate-800 text-sm">{isAr ? 'سياسات دورة حياة البيانات' : 'Data Retention & Lifecycle Policies'}</h2>
            <span className="text-xs text-slate-400 font-medium">{retentionPolicies.length} policies</span>
          </div>

          <div className="overflow-x-auto rounded-xl border border-slate-200">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="bg-slate-50 text-slate-700 border-b border-slate-200">
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Module</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Data Category</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Retention Period</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Expiry Action</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Active</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {retentionPolicies.map(rp => (
                  <tr key={rp.id} className="hover:bg-slate-50 transition-colors">
                    <td className="py-3 px-4 font-bold text-slate-900">{rp.module}</td>
                    <td className="py-3 px-4 font-mono text-slate-700">{rp.dataCategory}</td>
                    <td className="py-3 px-4 text-slate-600 font-semibold">{rp.retentionDays} days</td>
                    <td className="py-3 px-4">
                      <span className="text-[10px] font-semibold px-2 py-0.5 rounded bg-slate-100 text-slate-800">
                        {rp.actionOnExpiry === 1 ? 'Anonymize' : rp.actionOnExpiry === 2 ? 'Archive' : 'Purge'}
                      </span>
                    </td>
                    <td className="py-3 px-4">
                      <span className={`text-[10px] font-semibold px-2 py-0.5 rounded ${rp.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-700'}`}>
                        {rp.isActive ? 'Active' : 'Disabled'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 5: Integrations & Webhooks */}
      {activeTab === 'INTEGRATIONS' && (
        <div className="space-y-6">
          {/* Connectors Table */}
          <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 space-y-4">
            <div className="flex justify-between items-center">
              <h2 className="font-bold text-slate-800 text-sm">{isAr ? 'الموصلات النشطة' : 'Active Connectors'}</h2>
              <span className="text-xs text-slate-400 font-medium">{connectors.length} configured</span>
            </div>

            <div className="overflow-x-auto rounded-xl border border-slate-200">
              <table className="w-full text-left border-collapse text-xs">
                <thead>
                  <tr className="bg-slate-50 text-slate-700 border-b border-slate-200">
                    <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Code</th>
                    <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Name</th>
                    <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Endpoint URL</th>
                    <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Auth</th>
                    <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Subscriptions</th>
                    <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {connectors.map(c => (
                    <tr key={c.id} data-testid={`connector-row-${c.code}`} className="hover:bg-slate-50 transition-colors">
                      <td className="py-3 px-4 font-mono font-bold text-slate-900">{c.code}</td>
                      <td className="py-3 px-4 font-medium text-slate-800">{isAr ? c.nameAr : c.nameEn}</td>
                      <td className="py-3 px-4 font-mono text-[11px] text-slate-600">{c.endpointUrl}</td>
                      <td className="py-3 px-4 text-slate-500 font-semibold">{c.authType === 3 ? 'HMAC-SHA256' : 'None'}</td>
                      <td className="py-3 px-4 font-mono text-[11px] text-slate-500 max-w-xs truncate">{c.eventSubscriptionsJson}</td>
                      <td className="py-3 px-4">
                        <span className={`text-[10px] font-semibold px-2 py-0.5 rounded ${c.isActive ? 'bg-emerald-50 text-emerald-700 border border-emerald-200' : 'bg-slate-100 text-slate-500'}`}>
                          {c.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Deliveries Queue */}
          <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 space-y-4">
            <div className="flex justify-between items-center">
              <h2 className="font-bold text-slate-800 text-sm">{isAr ? 'سجل تسليم الأحداث الخارجية' : 'Outbound Delivery Event Queue'}</h2>
              <span className="text-xs text-slate-400 font-medium">{deliveries.length} entries</span>
            </div>

            <div className="overflow-x-auto rounded-xl border border-slate-200">
              <table className="w-full text-left border-collapse text-xs">
                <thead>
                  <tr className="bg-slate-50 text-slate-700 border-b border-slate-200">
                    <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Event Type</th>
                    <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Status</th>
                    <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Attempts</th>
                    <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">HTTP</th>
                    <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Created At</th>
                    <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {deliveries.map(d => (
                    <tr key={d.id} className="hover:bg-slate-50 transition-colors">
                      <td className="py-3 px-4 font-mono font-bold text-slate-900">{d.eventType}</td>
                      <td className="py-3 px-4">
                        <span className={`text-[10px] font-semibold px-2 py-0.5 rounded ${d.status === 3 || d.status === 'Delivered' ? 'bg-emerald-50 text-emerald-700' : d.status === 6 || d.status === 'DeadLettered' ? 'bg-red-50 text-red-700' : 'bg-amber-50 text-amber-700'}`}>
                          {String(d.status)}
                        </span>
                      </td>
                      <td className="py-3 px-4 font-mono text-slate-600">{d.attemptCount} / {d.maxAttempts}</td>
                      <td className="py-3 px-4 font-mono font-semibold text-slate-700">{d.lastHttpStatus || '—'}</td>
                      <td className="py-3 px-4 text-slate-400">{new Date(d.createdAtUtc).toLocaleString()}</td>
                      <td className="py-3 px-4">
                        {(d.status === 6 || d.status === 4 || d.status === 'DeadLettered') && (
                          <button
                            onClick={() => retryDelivery(d.id)}
                            className="text-xs text-indigo-600 hover:text-indigo-900 font-bold"
                          >
                            Retry
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* TAB 6: Audit Trail */}
      {activeTab === 'AUDIT' && (
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 space-y-4">
          <div className="flex justify-between items-center">
            <div>
              <h2 className="font-bold text-slate-800 text-sm">{isAr ? 'سجل التدقيق الأمني غير القابل للتعديل' : 'Immutable Security Audit Trail'}</h2>
              <p className="text-xs text-slate-500 mt-0.5">
                {isAr
                  ? 'سجل غير قابل للحذف أو التعديل على مستوى قاعدة البيانات مع حماية البيانات الحساسة.'
                  : 'Database-enforced append-only audit trail with sensitive PII redaction.'}
              </p>
            </div>
            <span className="text-xs text-slate-400 font-medium">{auditRecords.length} records</span>
          </div>

          <div className="overflow-x-auto rounded-xl border border-slate-200">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="bg-slate-50 text-slate-700 border-b border-slate-200">
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Timestamp</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Actor</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Action</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Entity</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Correlation ID</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Classification</th>
                  <th className="py-3 px-4 font-semibold uppercase tracking-wider text-[11px]">Inspect</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {auditRecords.map(a => (
                  <tr key={a.id} data-testid={`audit-row-${a.actionCode}`} className="hover:bg-slate-50 transition-colors">
                    <td className="py-3 px-4 text-slate-500 font-mono text-[11px]">{new Date(a.occurredAtUtc).toLocaleString()}</td>
                    <td className="py-3 px-4 font-mono font-medium text-slate-800">{a.actorUserId.substring(0, 8)}...</td>
                    <td className="py-3 px-4 font-mono font-bold text-indigo-700">{a.actionCode}</td>
                    <td className="py-3 px-4 text-slate-700">{a.entityType}: {a.entityId}</td>
                    <td className="py-3 px-4 font-mono text-[11px] text-slate-400">{a.correlationId || '—'}</td>
                    <td className="py-3 px-4">
                      <span className="text-[10px] font-semibold px-2 py-0.5 rounded bg-slate-100 text-slate-700 border border-slate-200">
                        {a.dataClassification}
                      </span>
                    </td>
                    <td className="py-3 px-4">
                      <button
                        onClick={() => setInspectAudit(a)}
                        className="text-xs text-indigo-600 hover:text-indigo-900 font-semibold"
                      >
                        {isAr ? 'معاينة' : 'Diff'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Role Create / Edit Modal */}
      {showRoleModal && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl p-6 max-w-lg w-full shadow-2xl border border-slate-200 space-y-4">
            <h3 className="text-lg font-bold text-slate-900">
              {selectedRole ? (isAr ? 'تعديل الدور' : 'Edit Role') : (isAr ? 'إنشاء دور جديد' : 'Create New Role')}
            </h3>

            {privilegeError && (
              <div className="p-3 bg-red-50 border border-red-200 text-red-800 text-xs rounded-xl font-medium">
                {privilegeError}
              </div>
            )}

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Code</label>
                <input
                  type="text"
                  disabled={!!selectedRole}
                  value={roleCode}
                  onChange={e => setRoleCode(e.target.value)}
                  placeholder="e.g. AUDITOR"
                  className="w-full text-sm border border-slate-200 rounded-xl px-3 py-2 disabled:bg-slate-100 uppercase"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Name (EN)</label>
                <input
                  type="text"
                  value={roleNameEn}
                  onChange={e => setRoleNameEn(e.target.value)}
                  placeholder="Auditor"
                  className="w-full text-sm border border-slate-200 rounded-xl px-3 py-2"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Name (AR)</label>
              <input
                type="text"
                value={roleNameAr}
                onChange={e => setRoleNameAr(e.target.value)}
                placeholder="مدقق"
                className="w-full text-sm border border-slate-200 rounded-xl px-3 py-2"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Permissions</label>
              <div className="grid grid-cols-2 gap-2 max-h-48 overflow-y-auto border border-slate-200 rounded-xl p-3 bg-slate-50">
                {availablePermissions.map(p => (
                  <label key={p} className="flex items-center gap-2 text-xs text-slate-700 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={selectedPerms.includes(p)}
                      onChange={e => {
                        if (e.target.checked) setSelectedPerms([...selectedPerms, p]);
                        else setSelectedPerms(selectedPerms.filter(x => x !== p));
                      }}
                      className="rounded border-slate-300 text-indigo-600"
                    />
                    <span className="font-mono text-[11px]">{p}</span>
                  </label>
                ))}
              </div>
            </div>

            <div className="flex justify-end gap-3 pt-2">
              <button onClick={() => setShowRoleModal(false)} className="px-4 py-2 text-sm text-slate-600 hover:text-slate-900 font-medium">
                Cancel
              </button>
              <button
                data-testid="confirm-save-role-btn"
                onClick={handleSaveRole}
                className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl font-semibold text-sm shadow-sm"
              >
                Save
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Connector Modal */}
      {showConnectorModal && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl p-6 max-w-md w-full shadow-2xl border border-slate-200 space-y-4">
            <h3 className="text-lg font-bold text-slate-900">{isAr ? 'إضافة موصل تكاملي جديد' : 'New Outbound Integration Connector'}</h3>
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Code</label>
              <input type="text" value={connCode} onChange={e => setConnCode(e.target.value)} placeholder="WEBHOOK_ERP" className="w-full text-sm border rounded-xl px-3 py-2 uppercase" />
            </div>
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Name (EN)</label>
              <input type="text" value={connNameEn} onChange={e => setConnNameEn(e.target.value)} placeholder="ERP Dispatcher" className="w-full text-sm border rounded-xl px-3 py-2" />
            </div>
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Endpoint URL</label>
              <input type="text" value={connEndpoint} onChange={e => setConnEndpoint(e.target.value)} placeholder="https://api.thirdparty.com/webhook" className="w-full text-sm border rounded-xl px-3 py-2" />
            </div>
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Secret Key (HMAC-SHA256)</label>
              <input type="password" value={connSecret} onChange={e => setConnSecret(e.target.value)} placeholder="••••••••••••" className="w-full text-sm border rounded-xl px-3 py-2" />
            </div>
            <div className="flex justify-end gap-3 pt-2">
              <button onClick={() => setShowConnectorModal(false)} className="px-4 py-2 text-sm text-slate-600 font-medium">Cancel</button>
              <button onClick={handleCreateConnector} className="px-4 py-2 bg-indigo-600 text-white rounded-xl font-semibold text-sm">Save Connector</button>
            </div>
          </div>
        </div>
      )}

      {/* Inspect Audit Diff Modal */}
      {inspectAudit && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl p-6 max-w-lg w-full shadow-2xl border border-slate-200 space-y-4">
            <h3 className="text-lg font-bold text-slate-900">
              {isAr ? 'معاينة التغييرات المسجلة' : 'Audit Change Detail & Diff'}
            </h3>
            <div className="text-xs text-slate-600 space-y-1">
              <div><strong>Action:</strong> <span className="font-mono text-indigo-700">{inspectAudit.actionCode}</span></div>
              <div><strong>Entity:</strong> {inspectAudit.entityType} ({inspectAudit.entityId})</div>
              <div><strong>Correlation:</strong> <span className="font-mono">{inspectAudit.correlationId || '—'}</span></div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Changes Before</label>
                <pre className="p-3 bg-slate-50 border border-slate-200 rounded-xl text-[10px] font-mono text-slate-700 overflow-x-auto h-32">
                  {inspectAudit.changesBeforeJson || '(None)'}
                </pre>
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Changes After</label>
                <pre className="p-3 bg-slate-50 border border-slate-200 rounded-xl text-[10px] font-mono text-slate-700 overflow-x-auto h-32">
                  {inspectAudit.changesAfterJson || '(None)'}
                </pre>
              </div>
            </div>

            <div className="flex justify-end pt-2">
              <button onClick={() => setInspectAudit(null)} className="px-4 py-2 bg-slate-900 text-white text-sm font-semibold rounded-xl">
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
