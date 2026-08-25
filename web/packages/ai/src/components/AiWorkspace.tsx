import React, { useState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Icon } from '@zainx/design-system';
import { ProposalCard } from './ProposalCard';
import type { AiActionProposalDto, AiActionDefinition } from '@zainx/contracts';

export interface SourceReference {
  id: string;
  sourceCategory: string;
  title: string;
  entityType?: string;
  entityId?: string;
  policyCode?: string;
  policyVersion?: number;
  payrollRunId?: string;
  metadataJson: string;
  retrievedAtUtc: string;
}

export interface ToolExecution {
  id: string;
  toolCode: string;
  durationMs: number;
  status: string;
  createdAtUtc: string;
}

export interface AiMessage {
  messageId: string;
  senderRole: 'User' | 'Assistant';
  content: string;
  sourceCategory: string;
  tokensUsed: number;
  createdAtUtc: string;
  sources: SourceReference[];
  toolExecutions: ToolExecution[];
  proposals?: AiActionProposalDto[];
}

export interface ConversationSummary {
  id: string;
  title: string;
  contextEntityType?: string;
  contextEntityId?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  messageCount: number;
}

export interface AiToolDefinition {
  toolCode: string;
  descriptionEn: string;
  descriptionAr: string;
  requiredPermission: string;
  dataClassification: string;
  isReadOnly: boolean;
}

interface AiWorkspaceProps {
  apiBaseUrl?: string;
  defaultPrompt?: string;
}

export const AiWorkspace: React.FC<AiWorkspaceProps> = ({
  apiBaseUrl = '/api/v1/ai',
  defaultPrompt = ''
}) => {
  const { i18n } = useTranslation();
  const isRtl = i18n.language === 'ar' || (typeof document !== 'undefined' && document.documentElement.dir === 'rtl');

  const [conversations, setConversations] = useState<ConversationSummary[]>([]);
  const [activeConversationId, setActiveConversationId] = useState<string | null>(null);
  const activeConvRef = useRef<string | null>(null);
  const isCreatingConversationRef = useRef(false);
  const [messages, setMessages] = useState<AiMessage[]>([]);
  const [promptInput, setPromptInput] = useState(defaultPrompt);
  const [isLoading, setIsLoading] = useState(false);
  const [tools, setTools] = useState<AiToolDefinition[]>([]);
  const [actions, setActions] = useState<AiActionDefinition[]>([]);
  const [proposals, setProposals] = useState<AiActionProposalDto[]>([]);
  const [activeTab, setActiveTab] = useState<'chat' | 'proposals'>('chat');
  const [selectedSource, setSelectedSource] = useState<SourceReference | null>(null);
  const [errorBanner, setErrorBanner] = useState<string | null>(null);

  // Load conversations, tools, and proposals on mount
  useEffect(() => {
    loadConversations();
    loadTools();
    loadActions();
    loadProposals();
  }, []);

  const loadConversations = async () => {
    try {
      const res = await fetch(`${apiBaseUrl}/conversations`);
      if (res.ok) {
        const data: ConversationSummary[] = await res.json();
        setConversations(data);
        if (data.length > 0 && !activeConvRef.current && !isCreatingConversationRef.current) {
          selectConversation(data[0].id);
        }
      }
    } catch (err) {
      console.warn('Failed to load conversations:', err);
    }
  };

  const loadTools = async () => {
    try {
      const res = await fetch(`${apiBaseUrl}/tools`);
      if (res.ok) {
        const data = await res.json();
        setTools(data);
      }
    } catch (err) {
      console.warn('Failed to load AI tools:', err);
    }
  };

  const loadActions = async () => {
    try {
      const res = await fetch(`${apiBaseUrl}/actions`);
      if (res.ok) {
        const data = await res.json();
        setActions(Array.isArray(data) ? data : []);
      }
    } catch (err) {
      console.warn('Failed to load AI actions:', err);
    }
  };

  const loadProposals = async () => {
    try {
      const res = await fetch(`${apiBaseUrl}/proposals`);
      if (res.ok) {
        const data = await res.json();
        setProposals(Array.isArray(data) ? data : []);
      }
    } catch (err) {
      console.warn('Failed to load AI proposals:', err);
    }
  };

  const handleConfirmProposal = async (proposalId: string, reason?: string) => {
    setErrorBanner(null);
    try {
      const res = await fetch(`${apiBaseUrl}/proposals/${proposalId}/confirm`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason })
      });

      const data = await res.json();

      if (res.status === 409) {
        // Concurrency conflict / Stale
        setProposals(prev => prev.map(p => p.id === proposalId ? { ...p, status: 'Stale' } : p));
        setErrorBanner(isRtl ? 'فشل التنفيذ: تغيرت بيانات السجل المستهدف (تعارض متزامن 409). يرجى تقديم مقترح جديد.' : 'Execution rejected: Target entity state has changed (409 Conflict). Auto-rebase is forbidden. Please create a new proposal.');
        return data;
      }

      if (res.status === 410) {
        setProposals(prev => prev.map(p => p.id === proposalId ? { ...p, status: 'Expired' } : p));
        setErrorBanner(isRtl ? 'انتهت صلاحية هذا المقترح.' : 'Proposal has expired (410 Gone).');
        return data;
      }

      if (res.status === 403) {
        setErrorBanner(isRtl ? 'غير مصرح لك بتنفيذ هذا الإجراء في الوقت الحالي.' : 'Forbidden: You do not have the required permission at execution time.');
        return;
      }

      if (!res.ok || !data.success) {
        setErrorBanner(data.errorMessage || data.error || 'Failed to execute proposal.');
        return;
      }

      // Success: update proposal status to Completed
      setProposals(prev => prev.map(p => p.id === proposalId ? { ...p, status: 'Completed', completedAtUtc: new Date().toISOString() } : p));
      return data;
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : String(err);
      setErrorBanner(`Network error: ${msg}`);
    }
  };

  const handleCancelProposal = async (proposalId: string, reason?: string) => {
    setErrorBanner(null);
    try {
      const res = await fetch(`${apiBaseUrl}/proposals/${proposalId}/cancel`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason })
      });

      if (res.ok) {
        const updated = await res.json();
        setProposals(prev => prev.map(p => p.id === proposalId ? { ...p, status: 'Cancelled' } : p));
        return updated;
      } else {
        const err = await res.json();
        setErrorBanner(err.error || 'Failed to cancel proposal.');
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : String(err);
      setErrorBanner(`Network error: ${msg}`);
    }
  };

  const selectConversation = async (convId: string) => {
    setActiveConversationId(convId);
    activeConvRef.current = convId;
    setErrorBanner(null);
    try {
      const res = await fetch(`${apiBaseUrl}/conversations/${convId}`);
      if (res.ok) {
        const data = await res.json();
        if (activeConvRef.current === convId) {
          setMessages(data.messages || []);
        }
      }
    } catch (err) {
      console.error('Failed to load conversation details:', err);
    }
    loadProposals();
  };

  const createNewConversation = async (initialTitle = 'New Analysis') => {
    isCreatingConversationRef.current = true;
    setIsLoading(true);
    setMessages([]);
    setActiveConversationId(null);
    activeConvRef.current = null;
    try {
      const res = await fetch(`${apiBaseUrl}/conversations`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title: initialTitle })
      });
      if (res.ok) {
        const created = await res.json();
        setActiveConversationId(created.id);
        activeConvRef.current = created.id;
        setConversations(prev => [created, ...prev.filter(c => c.id !== created.id)]);
        setMessages([]);
      }
    } catch (err) {
      setErrorBanner('Failed to create new AI conversation session.');
    } finally {
      isCreatingConversationRef.current = false;
      setIsLoading(false);
    }
  };

  const handleSendMessage = async (promptToSend?: string) => {
    const text = (promptToSend || promptInput).trim();
    if (!text || isLoading) return;

    setErrorBanner(null);
    setIsLoading(true);

    let targetConvId = activeConversationId;
    if (!targetConvId) {
      try {
        const createRes = await fetch(`${apiBaseUrl}/conversations`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ title: text.slice(0, 30) })
        });
        if (createRes.ok) {
          const newConv = await createRes.json();
          targetConvId = newConv.id;
          setActiveConversationId(newConv.id);
          setConversations(prev => [newConv, ...prev]);
        }
      } catch (err) {
        setErrorBanner('Failed to initialize session.');
        setIsLoading(false);
        return;
      }
    }

    // Optimistic user message
    const tempUserMsg: AiMessage = {
      messageId: `temp-${Date.now()}`,
      senderRole: 'User',
      content: text,
      sourceCategory: 'CompanyData',
      tokensUsed: 0,
      createdAtUtc: new Date().toISOString(),
      sources: [],
      toolExecutions: []
    };
    setMessages(prev => [...prev, tempUserMsg]);
    setPromptInput('');

    try {
      const res = await fetch(`${apiBaseUrl}/conversations/${targetConvId}/messages`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ prompt: text })
      });

      if (res.ok) {
        const assistantMsg: AiMessage = await res.json();
        setMessages(prev => [...prev, assistantMsg]);
        loadConversations();
      } else {
        const errData = await res.json();
        setErrorBanner(errData.error || 'Failed to generate AI response.');
      }
    } catch (err: any) {
      setErrorBanner(`Network error: ${err.message || 'Unable to communicate with AI API.'}`);
    } finally {
      setIsLoading(false);
    }
  };

  const getCategoryBadgeClass = (category: string) => {
    switch (category) {
      case 'PayrollTrace':
        return 'bg-warning-subtle text-warning-subtle-text border-warning';
      case 'CompanyPolicy':
        return 'bg-primary-subtle text-primary-subtle-text border-primary';
      case 'ProductKnowledge':
        return 'bg-info-subtle text-info-subtle-text border-info';
      case 'CompanyData':
      default:
        return 'bg-success-subtle text-success-subtle-text border-success';
    }
  };

  const getCategoryLabel = (category: string) => {
    if (isRtl) {
      switch (category) {
        case 'PayrollTrace': return 'مسار احتساب مسير الرواتب';
        case 'CompanyPolicy': return 'لائحة وسياسة الشركة';
        case 'ProductKnowledge': return 'دليل استخدام المنظومة';
        case 'CompanyData': return 'بيانات المنظومة المعتمدة';
        default: return category;
      }
    }
    switch (category) {
      case 'PayrollTrace': return 'Payroll Calculation Trace';
      case 'CompanyPolicy': return 'Company Policy (Temporal)';
      case 'ProductKnowledge': return 'Platform Knowledge';
      case 'CompanyData': return 'Enterprise Read Model';
      default: return category;
    }
  };

  const quickPrompts = isRtl ? [
    { label: 'شرح احتساب مسير الرواتب لشهر مايو', prompt: 'لماذا تغير صافي راتب الموظف في مسير شهر مايو وما هي استقطاعات التأمينات والخصومات؟' },
    { label: 'سياسة العمل عن بعد لشهر أغسطس', prompt: 'ما هي لائحة العمل عن بعد السارية في شهر أغسطس 2026؟' },
    { label: 'آلية اعتماد مسير الرواتب', prompt: 'ماذا يحدث عند اعتماد مسير الرواتب في منظومة زين إكس؟' },
    { label: 'تقرير القوى العاملة المعتمد', prompt: 'استخرج تقرير ملخص القوى العاملة المعتمد حسب الأقسام' }
  ] : [
    { label: 'Explain May Payroll Variance', prompt: 'Why did the net pay change in May payroll run, and what statutory deductions were applied?' },
    { label: 'Remote Work Policy (August 2026)', prompt: 'What is the company remote work policy in effect for August 2026?' },
    { label: 'Payroll Finalization Rules', prompt: 'How does payroll finalization lock data and generate settlement batches in ZainX?' },
    { label: 'Governed Headcount Summary', prompt: 'Run governed headcount summary report by department' }
  ];

  return (
    <div 
      className="flex min-h-[calc(100vh-8rem)] flex-col bg-surface-subtle text-text-primary bg-canvas text-text-primary font-sans lg:h-[calc(100vh-8rem)]"
      dir={isRtl ? 'rtl' : 'ltr'}
      data-testid="ai-workspace-container"
    >
      {/* Top Banner / Invariant Guarantee */}
      <header className="flex flex-col gap-3 border-b border-border-default bg-surface px-4 py-4 shadow-sm border-border-default bg-surface-panel sm:flex-row sm:items-center sm:justify-between sm:px-6 sm:py-3">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-primary flex items-center justify-center text-text-inverse font-bold shadow-sm">
            AI
          </div>
          <div>
            <h1 className="flex flex-wrap items-center gap-2 text-base font-bold">
              <span>{isRtl ? 'مساعد زين إكس الذكي (القراءة والتحليل)' : 'ZainX AI Assistant (Read / Analyze / Explain)'}</span>
              <span className="text-xs px-2 py-0.5 rounded-full bg-success-subtle bg-success-subtle text-success text-success-subtle-text font-semibold border border-success border-success">
                {isRtl ? 'حماية مشددة للقراءة فقط' : 'Governed Read-Only'}
              </span>
            </h1>
            <p className="text-xs text-text-muted text-text-muted">
              {isRtl 
                ? 'استعلام البيانات المعتمدة ولوائح الشركة ومسارات الرواتب مع توثيق المصادر' 
                : 'Enterprise knowledge & calculation traces with full provenance citations'}
            </p>
          </div>
        </div>

        {/* Action Controls */}
        <div className="flex items-center gap-2">
          <button
            onClick={() => createNewConversation()}
            className="px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary hover:bg-primary-hover text-text-inverse flex items-center gap-1.5 transition shadow-sm"
            data-testid="btn-new-conversation"
            aria-label="New Conversation"
          >
            <Icon name="plus" size="sm" aria-hidden="true" />
            <span>{isRtl ? 'محادثة جديدة' : 'New Analysis'}</span>
          </button>
        </div>
      </header>

      {/* Main Workspace Layout (Sidebar + Chat Area) */}
      <div className="flex min-h-0 flex-1 flex-col overflow-hidden lg:flex-row">
        {/* Sidebar: Conversation Sessions & Tool Allowlist */}
        <aside className="flex max-h-64 w-full shrink-0 flex-col border-b border-border-default bg-surface-panel backdrop-blur border-border-default bg-surface-panel lg:max-h-none lg:w-80 lg:border-b-0 lg:border-e">
          <div className="p-3 border-b border-border-default border-border-default">
            <h2 className="text-xs font-semibold uppercase tracking-wider text-text-muted text-text-muted px-2 mb-2">
              {isRtl ? 'جلسات التحليل السابقة' : 'Analysis Sessions'}
            </h2>
            <div className="space-y-1 max-h-48 overflow-y-auto">
              {conversations.map(c => (
                <button
                  key={c.id}
                  onClick={() => selectConversation(c.id)}
                  className={`w-full text-start px-3 py-2 rounded-lg text-xs transition flex items-center justify-between ${
                    c.id === activeConversationId
                      ? 'bg-primary-subtle bg-primary-subtle text-primary text-primary-subtle-text font-semibold border border-primary border-primary'
                      : 'hover:bg-surface-subtle hover:bg-surface-raised text-text-secondary text-text-secondary'
                  }`}
                  data-testid={`conv-item-${c.id}`}
                >
                  <span className="truncate max-w-[190px]">{c.title || 'Untitled Session'}</span>
                  <span className="text-[10px] text-text-muted">
                    {new Date(c.updatedAtUtc).toLocaleDateString(isRtl ? 'ar-EG' : 'en-US', { month: 'short', day: 'numeric' })}
                  </span>
                </button>
              ))}
              {conversations.length === 0 && (
                <p className="text-xs text-text-muted px-2 py-4 text-center">
                  {isRtl ? 'لا توجد جلسات سابقة' : 'No previous sessions'}
                </p>
              )}
            </div>
          </div>

          {/* Allowlisted Tools Inspection Panel */}
          <div className="hidden flex-1 overflow-y-auto p-3 lg:block space-y-4">
            <div>
              <h2 className="text-xs font-semibold uppercase tracking-wider text-text-muted text-text-muted px-2 mb-2 flex items-center justify-between">
                <span>{isRtl ? 'أدوات الاستعلام المصرح بها' : 'Approved AI Tools'}</span>
                <span className="text-[10px] bg-surface-tertiary bg-surface-raised px-1.5 py-0.5 rounded font-mono">{tools.length}</span>
              </h2>
              <div className="space-y-2">
                {tools.map(t => (
                  <div 
                    key={t.toolCode} 
                    className="p-2 rounded-lg border border-border-default border-border-default bg-surface bg-surface-panel text-xs shadow-xs"
                    data-testid={`tool-badge-${t.toolCode}`}
                  >
                    <div className="flex items-center justify-between mb-1">
                      <span className="font-mono font-semibold text-text-primary text-[11px]">{t.toolCode}</span>
                      <span className="text-[9px] px-1.5 py-0.2 rounded bg-surface-subtle bg-surface-raised text-text-secondary text-text-muted border border-border-default border-border-strong">
                        {t.dataClassification}
                      </span>
                    </div>
                    <p className="text-[11px] text-text-muted text-text-muted line-clamp-2">
                      {isRtl ? t.descriptionAr : t.descriptionEn}
                    </p>
                  </div>
                ))}
              </div>
            </div>

            {/* Phase 7B: Authorized Action Handlers */}
            <div>
              <h2 className="text-xs font-semibold uppercase tracking-wider text-primary text-primary-subtle-text px-2 mb-2 flex items-center justify-between">
                <span>{isRtl ? 'إجراءات الأعمال المعتمدة (مقترح/تأكيد)' : 'Governed Action Handlers'}</span>
                <span className="text-[10px] bg-primary-subtle bg-primary-subtle text-primary text-primary-subtle-text px-1.5 py-0.5 rounded font-mono">{actions.length}</span>
              </h2>
              <div className="space-y-2">
                {(actions || []).map(a => (
                  <div 
                    key={a.actionCode} 
                    className="p-2 rounded-lg border border-primary border-primary bg-primary-subtle bg-primary-subtle text-xs shadow-xs"
                    data-testid={`action-badge-${a.actionCode}`}
                  >
                    <div className="flex items-center justify-between mb-1">
                      <span className="font-mono font-semibold text-primary text-primary-subtle-text text-[11px]">{a.actionCode}</span>
                      <span className="text-[9px] px-1.5 py-0.2 rounded bg-primary-subtle bg-primary-subtle text-primary text-primary-subtle-text">
                        {a.targetModule}
                      </span>
                    </div>
                    <p className="text-[11px] text-text-secondary text-text-muted line-clamp-2">
                      {a.description}
                    </p>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Invariant Footer Badge */}
          <div className="hidden border-t border-border-default bg-surface-subtle p-3 text-[11px] text-text-muted border-border-default bg-surface-panel lg:block">
            <span className="inline-flex items-center gap-1.5">
              <Icon name="shield-alert" size="xs" aria-hidden="true" />
              <span>{isRtl ? 'إلزامية التأكيد الصريح ومنع التعديل المباشر' : 'Governed Confirmation & Zero Direct DB Writes'}</span>
            </span>
          </div>
        </aside>

        {/* Chat / Timeline Area */}
        <div className="flex min-h-[34rem] min-w-0 flex-1 flex-col overflow-hidden bg-surface-panel-subtle backdrop-blur bg-surface-panel">
          {/* Navigation Sub-Tabs */}
          <div className="flex items-center justify-between border-b border-border-default border-border-default bg-surface bg-surface-panel px-4 py-2 text-xs">
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => setActiveTab('chat')}
                className={`px-3 py-1.5 rounded-lg font-semibold transition ${
                  activeTab === 'chat'
                    ? 'bg-primary text-text-inverse'
                    : 'text-text-secondary text-text-muted hover:bg-surface-subtle hover:bg-surface-raised'
                }`}
                data-testid="tab-chat"
              >
                {isRtl ? 'المحادثة والتحليل' : 'Chat & Analysis'}
              </button>
              <button
                type="button"
                onClick={() => setActiveTab('proposals')}
                className={`px-3 py-1.5 rounded-lg font-semibold transition flex items-center gap-1.5 ${
                  activeTab === 'proposals'
                    ? 'bg-primary text-text-inverse'
                    : 'text-text-secondary text-text-muted hover:bg-surface-subtle hover:bg-surface-raised'
                }`}
                data-testid="tab-proposals"
              >
                <span>{isRtl ? 'الإجراءات المقترحة' : 'Action Proposals'}</span>
                {proposals.length > 0 && (
                  <span className="px-1.5 py-0.2 rounded-full bg-warning text-text-inverse text-[10px] font-bold">
                    {proposals.filter(p => p.status === 'ReadyForConfirmation').length}
                  </span>
                )}
              </button>
            </div>
          </div>

          {/* Error Banner */}
          {errorBanner && (
            <div className="p-3 bg-danger-subtle bg-danger-subtle border-b border-danger border-danger text-danger-subtle-text text-danger-subtle-text text-xs flex items-center justify-between" role="alert">
              <span className="inline-flex items-center gap-1.5"><Icon name="alert-triangle" size="sm" aria-hidden="true" />{errorBanner}</span>
              <button onClick={() => setErrorBanner(null)} className="text-danger hover:text-danger-subtle-text font-bold" aria-label="Dismiss error"><Icon name="x" size="sm" aria-hidden="true" /></button>
            </div>
          )}

          {/* Tab Content: Proposals List View */}
          {activeTab === 'proposals' ? (
            <div className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4" data-testid="proposals-tab-container">
              <div className="flex items-center justify-between mb-2">
                <h3 className="text-sm font-bold text-text-primary">
                  {isRtl ? 'قائمة المقترحات الإدارية بانتظار التأكيد' : 'Governed Business Action Proposals'}
                </h3>
                <button
                  type="button"
                  onClick={() => loadProposals()}
                  className="text-xs text-primary text-primary-subtle-text hover:underline flex items-center gap-1"
                >
                  <Icon name="refresh" size="xs" />
                  <span>{isRtl ? 'تحديث' : 'Refresh'}</span>
                </button>
              </div>

              {proposals.length === 0 ? (
                <div className="p-8 text-center bg-surface-subtle bg-surface-panel rounded-xl border border-border-default border-border-default text-text-muted text-xs">
                  {isRtl ? 'لا توجد مقترحات حالياً.' : 'No action proposals generated yet.'}
                </div>
              ) : (
                proposals.map(p => (
                  <ProposalCard
                    key={p.id}
                    proposal={p}
                    onConfirm={handleConfirmProposal}
                    onCancel={handleCancelProposal}
                  />
                ))
              )}
            </div>
          ) : (
            /* Tab Content: Chat Message Thread */
            <div className="flex-1 space-y-6 overflow-y-auto p-4 sm:p-6" data-testid="chat-messages-container">
              {messages.length === 0 && (
                <div className="h-full flex flex-col items-center justify-center text-center max-w-xl mx-auto py-12" data-testid="ai-empty-state">
                  <div className="w-16 h-16 rounded-xl bg-primary-subtle bg-primary-subtle text-primary text-primary-subtle-text flex items-center justify-center text-3xl mb-4 shadow-sm">
                    <Icon name="sparkles" size="xl" aria-hidden="true" />
                  </div>
                  <h3 className="text-lg font-bold text-text-primary text-text-primary mb-2">
                    {isRtl ? 'كيف يمكنني مساعدتك في تحليل بيانات المنظومة واقتراح الإجراءات؟' : 'How can I assist your enterprise analysis & governed proposals?'}
                  </h3>
                  <p className="text-xs text-text-muted text-text-muted mb-6">
                    {isRtl
                      ? 'يمكنك الاستفسار عن تفاصيل مسيرات الرواتب، لوائح وسياسات الشركة، أو طلب اقتراح إجراءات إدارية تخضع لتأكيدك الصريح.'
                      : 'Query verified payroll traces, temporal policies, attendance anomalies, or propose governed business mutations with mathematical fidelity.'}
                  </p>

                  {/* Quick Prompts Grid */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 w-full text-start">
                    {quickPrompts.map((qp, idx) => (
                      <button
                        key={idx}
                        onClick={() => {
                          setPromptInput(qp.prompt);
                          handleSendMessage(qp.prompt);
                        }}
                        className="p-3 rounded-xl border border-border-default border-border-default bg-surface bg-surface-panel hover:border-primary hover:border-primary hover:shadow-sm transition text-xs group"
                        data-testid={`quick-prompt-${idx}`}
                      >
                        <div className="font-semibold text-text-primary group-hover:text-primary group-hover:text-primary-subtle-text mb-1">
                          {qp.label}
                        </div>
                        <div className="text-[11px] text-text-muted text-text-muted line-clamp-2">
                          {qp.prompt}
                        </div>
                      </button>
                    ))}
                  </div>
                </div>
              )}

              {messages.map((msg, index) => {
                const isUser = msg.senderRole === 'User';
                return (
                  <div
                    key={msg.messageId || index}
                    className={`flex flex-col ${isUser ? 'items-end' : 'items-start'}`}
                    data-testid={`message-item-${index}`}
                    data-sender-role={msg.senderRole}
                  >
                    <div className="flex items-center gap-2 mb-1 text-[11px] text-text-muted">
                      <span className="font-semibold">{isUser ? (isRtl ? 'المستخدم' : 'You') : (isRtl ? 'مساعد زين إكس' : 'ZainX Workforce AI')}</span>
                      <span>•</span>
                      <span>{new Date(msg.createdAtUtc).toLocaleTimeString(isRtl ? 'ar-EG' : 'en-US', { hour: '2-digit', minute: '2-digit' })}</span>
                      {!isUser && msg.sourceCategory && (
                        <span className={`text-[10px] px-2 py-0.2 rounded-full border font-medium ${getCategoryBadgeClass(msg.sourceCategory)}`}>
                          {getCategoryLabel(msg.sourceCategory)}
                        </span>
                      )}
                    </div>

                    <div
                      className={`max-w-2xl rounded-xl p-4 text-sm shadow-xs ${
                        isUser
                          ? 'bg-primary text-text-inverse rounded-br-xs'
                          : 'bg-surface bg-surface-panel border border-border-default border-border-default text-text-primary rounded-bl-xs'
                      }`}
                    >
                      <div className="whitespace-pre-wrap leading-relaxed">{msg.content}</div>

                      {/* Tool Execution Trail (if assistant) */}
                      {!isUser && msg.toolExecutions && msg.toolExecutions.length > 0 && (
                        <div className="mt-3 pt-3 border-t border-border-subtle border-border-default">
                          <div className="text-[11px] font-semibold text-text-muted text-text-muted mb-1.5 flex items-center gap-1.5">
                            <Icon name="settings" size="xs" aria-hidden="true" />
                            <span>{isRtl ? 'سجل استدعاء الأدوات المصرحة' : 'Executed Tool Invocations'}</span>
                          </div>
                          <div className="flex flex-wrap gap-1.5">
                            {msg.toolExecutions.map(te => (
                              <span
                                key={te.id}
                                className={`text-[10px] px-2 py-0.5 rounded font-mono border flex items-center gap-1 ${
                                  te.status === 'Success'
                                    ? 'bg-success-subtle bg-success-subtle text-success text-success-subtle-text border-success border-success'
                                    : te.status === 'Denied'
                                      ? 'bg-warning-subtle bg-warning-subtle text-warning-subtle-text text-warning-subtle-text border-warning border-warning'
                                      : 'bg-danger-subtle bg-danger-subtle text-danger-subtle-text text-danger-subtle-text border-danger border-danger'
                                }`}
                                data-testid={`exec-chip-${te.toolCode}`}
                              >
                                <span>{te.toolCode}</span>
                                <span className="text-[9px] text-text-muted">({te.durationMs}ms)</span>
                              </span>
                            ))}
                          </div>
                        </div>
                      )}

                      {/* Phase 7B Proposals attached to message */}
                      {!isUser && msg.proposals && msg.proposals.length > 0 && (
                        <div className="mt-4">
                          {msg.proposals.map(p => (
                            <ProposalCard
                              key={p.id}
                              proposal={p}
                              onConfirm={handleConfirmProposal}
                              onCancel={handleCancelProposal}
                            />
                          ))}
                        </div>
                      )}

                      {/* Provenance Citations & Source References */}
                      {!isUser && msg.sources && msg.sources.length > 0 && (
                        <div className="mt-3 pt-3 border-t border-border-subtle border-border-default">
                          <div className="text-[11px] font-semibold text-text-muted text-text-muted mb-1.5 flex items-center gap-1.5">
                            <Icon name="info" size="xs" aria-hidden="true" />
                            <span>{isRtl ? 'المصادر والوثائق المعتمدة (Provenance)' : 'Verified Evidence & Provenance'}</span>
                          </div>
                          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                            {msg.sources.map(src => (
                              <button
                                key={src.id}
                                onClick={() => setSelectedSource(src)}
                                className="text-start p-2 rounded-lg border border-border-default border-border-default hover:border-primary hover:border-primary bg-surface-subtle bg-surface-panel transition group"
                                data-testid={`source-card-${src.id}`}
                              >
                                <div className="text-[11px] font-semibold text-text-primary group-hover:text-primary group-hover:text-primary-subtle-text truncate">
                                  {src.title}
                                </div>
                                <div className="text-[10px] text-text-muted text-text-muted flex items-center gap-1.5 mt-0.5">
                                  <span className="font-medium">{src.entityType || 'Policy'}</span>
                                  {src.policyVersion && <span>• v{src.policyVersion}</span>}
                                </div>
                              </button>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                );
              })}

              {/* Loading Indicator */}
              {isLoading && (
                <div className="flex items-center gap-3 p-4 rounded-xl bg-surface bg-surface-panel border border-border-default border-border-default text-text-secondary text-text-muted text-xs w-fit">
                  <div className="w-4 h-4 border-2 border-primary border-t-transparent rounded-full animate-spin"></div>
                  <span>{isRtl ? 'جاري استدعاء العقود المعتمدة وتحليل مسار الإثبات...' : 'Querying authorized read models & calculation traces...'}</span>
                </div>
              )}
            </div>
          )}

          {/* Input Form Bar */}
          <footer className="border-t border-border-default bg-surface p-3 border-border-default bg-surface-panel sm:p-4">
            <form
              onSubmit={e => {
                e.preventDefault();
                handleSendMessage();
              }}
              className="flex items-center gap-2 max-w-4xl mx-auto"
            >
              <input
                type="text"
                value={promptInput}
                onChange={e => setPromptInput(e.target.value)}
                placeholder={
                  isRtl
                    ? 'اطرح سؤالاً عن مسير الرواتب، اللوائح، أو تقارير القوى العاملة...'
                    : 'Ask about payroll calculations, company policies, attendance, or headcount...'
                }
                className="flex-1 px-4 py-2.5 rounded-xl border border-border-strong border-border-strong bg-surface-subtle bg-canvas text-sm focus:outline-hidden focus:ring-2 focus:ring-primary transition"
                disabled={isLoading}
                data-testid="input-ai-prompt"
                aria-label="AI Prompt Input"
              />
              <button
                type="submit"
                disabled={isLoading || !promptInput.trim()}
                className="px-5 py-2.5 rounded-xl bg-primary hover:bg-primary-hover disabled:opacity-50 text-text-inverse font-semibold text-sm transition shadow-sm flex items-center gap-1.5"
                data-testid="btn-submit-prompt"
                aria-label="Submit Prompt"
              >
                <span>{isRtl ? 'إرسال' : 'Send'}</span>
                <Icon name="arrow-right" size="sm" aria-hidden="true" />
              </button>
            </form>
          </footer>
        </div>
      </div>

      {/* Source Citation Modal / Drawer */}
      {selectedSource && (
        <div
          className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4"
          role="dialog"
          aria-modal="true"
          aria-labelledby="source-dialog-title"
          data-testid="source-citation-modal"
        >
          <div className="bg-surface bg-surface-panel rounded-xl max-w-lg w-full border border-border-default border-border-default shadow-overlay p-6 relative">
            <button
              onClick={() => setSelectedSource(null)}
              className="absolute top-4 end-4 text-text-muted hover:text-text-secondary hover:text-text-primary text-lg font-bold"
              aria-label="Close modal"
            >
              <Icon name="x" size="sm" aria-hidden="true" />
            </button>
            <div className="flex items-center gap-2 mb-3">
              <span className={`text-xs px-2.5 py-0.5 rounded-full border font-semibold ${getCategoryBadgeClass(selectedSource.sourceCategory)}`}>
                {getCategoryLabel(selectedSource.sourceCategory)}
              </span>
            </div>
            <h3 id="source-dialog-title" className="text-base font-bold text-text-primary text-text-primary mb-2">
              {selectedSource.title}
            </h3>

            <div className="space-y-2 text-xs text-text-secondary text-text-muted bg-surface-subtle bg-canvas p-4 rounded-xl border border-border-default border-border-default mb-4 font-mono">
              <div><strong className="text-text-primary">Entity Type:</strong> {selectedSource.entityType || 'N/A'}</div>
              <div><strong className="text-text-primary">Entity ID:</strong> {selectedSource.entityId || 'N/A'}</div>
              {selectedSource.policyCode && (
                <div><strong className="text-text-primary">Policy Code:</strong> {selectedSource.policyCode} (v{selectedSource.policyVersion})</div>
              )}
              {selectedSource.payrollRunId && (
                <div><strong className="text-text-primary">Payroll Run ID:</strong> {selectedSource.payrollRunId}</div>
              )}
              <div><strong className="text-text-primary">Retrieved At:</strong> {new Date(selectedSource.retrievedAtUtc).toUTCString()}</div>
              {selectedSource.metadataJson && (
                <div className="mt-2 pt-2 border-t border-border-default border-border-default">
                  <div className="font-bold mb-1 text-text-primary">Metadata Details:</div>
                  <pre className="text-[11px] whitespace-pre-wrap overflow-x-auto text-primary text-primary-subtle-text">{selectedSource.metadataJson}</pre>
                </div>
              )}
            </div>

            <div className="flex justify-end">
              <button
                onClick={() => setSelectedSource(null)}
                className="px-4 py-2 text-xs font-semibold rounded-lg bg-surface-tertiary bg-surface-raised hover:bg-surface-tertiary hover:bg-surface-tertiary transition"
              >
                {isRtl ? 'إغلاق' : 'Close'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};




