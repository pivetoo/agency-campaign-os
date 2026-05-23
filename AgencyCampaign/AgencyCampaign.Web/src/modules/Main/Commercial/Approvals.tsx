import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, PageLayout, useApi, useAuth, useI18n } from 'archon-ui'
import { ArrowUpRight, CheckCircle2, ChevronRight, Clock, ExternalLink, Eye, History, MessageSquare, MoreHorizontal, Plus, Search, ShieldCheck, ThumbsDown, ThumbsUp, Users, XCircle, Zap } from 'lucide-react'
import { opportunityService, OpportunityApprovalStatus, type OpportunityApprovalRequest } from '../../../services/opportunityService'
import { formatDate } from '../../../lib/format'

const approvalTypeKeys: Record<number, string> = {
  1: 'approvals.type.discount',
  2: 'approvals.type.margin',
  3: 'approvals.type.deadline',
  4: 'approvals.type.exception',
}

type FilterTab = 'pending' | 'approved' | 'rejected' | 'all'

function hoursSince(iso: string): number {
  return Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 3600_000))
}

function ageLabel(hours: number): string {
  if (hours < 1) return 'agora'
  if (hours < 24) return `${hours}h`
  return `${Math.round(hours / 24)}d`
}

function statusBadgeConfig(status: number): { bg: string; text: string; label: string } {
  if (status === OpportunityApprovalStatus.Pending) return { bg: 'bg-amber-100', text: 'text-amber-800', label: 'Pendente' }
  if (status === OpportunityApprovalStatus.InReview) return { bg: 'bg-blue-100', text: 'text-blue-800', label: 'Em revisão' }
  if (status === OpportunityApprovalStatus.ChangesRequested) return { bg: 'bg-amber-100', text: 'text-amber-800', label: 'Ajustes pedidos' }
  if (status === OpportunityApprovalStatus.Approved) return { bg: 'bg-emerald-100', text: 'text-emerald-800', label: 'Aprovada' }
  if (status === OpportunityApprovalStatus.Merged) return { bg: 'bg-purple-100', text: 'text-purple-800', label: 'Mesclada' }
  if (status === OpportunityApprovalStatus.Rejected) return { bg: 'bg-rose-100', text: 'text-rose-800', label: 'Rejeitada' }
  return { bg: 'bg-muted', text: 'text-muted-foreground', label: 'Cancelada' }
}

function isPendingDecision(status: number): boolean {
  return status === OpportunityApprovalStatus.Pending || status === OpportunityApprovalStatus.InReview || status === OpportunityApprovalStatus.ChangesRequested
}

export default function CommercialApprovals() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [approvals, setApprovals] = useState<OpportunityApprovalRequest[]>([])
  const [filter, setFilter] = useState<FilterTab>('pending')
  const [search, setSearch] = useState('')
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const { execute: fetchApprovals, loading } = useApi<OpportunityApprovalRequest[]>({ showErrorMessage: true })
  const { execute: executeAction, loading: actionLoading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadData = async () => {
    const result = await fetchApprovals(() => opportunityService.getAllApprovals({ page: 1, pageSize: 200 }))
    if (result) setApprovals(result)
  }

  useEffect(() => {
    void loadData()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const counts = useMemo(() => ({
    pending: approvals.filter((a) => a.status === OpportunityApprovalStatus.Pending).length,
    approved: approvals.filter((a) => a.status === OpportunityApprovalStatus.Approved).length,
    rejected: approvals.filter((a) => a.status === OpportunityApprovalStatus.Rejected).length,
    all: approvals.length,
  }), [approvals])

  const filtered = useMemo(() => {
    let list = approvals
    if (filter === 'pending') list = list.filter((a) => a.status === OpportunityApprovalStatus.Pending)
    else if (filter === 'approved') list = list.filter((a) => a.status === OpportunityApprovalStatus.Approved)
    else if (filter === 'rejected') list = list.filter((a) => a.status === OpportunityApprovalStatus.Rejected)

    if (search.trim()) {
      const term = search.trim().toLowerCase()
      list = list.filter((a) =>
        (approvalTypeKeys[a.approvalType] && t(approvalTypeKeys[a.approvalType]).toLowerCase().includes(term))
        || (a.opportunityName ?? '').toLowerCase().includes(term)
        || (a.negotiationTitle ?? '').toLowerCase().includes(term)
        || (a.requestedByUserName ?? '').toLowerCase().includes(term)
        || String(a.id).includes(term))
    }

    return list.slice().sort((a, b) => {
      if (a.status === OpportunityApprovalStatus.Pending && b.status !== OpportunityApprovalStatus.Pending) return -1
      if (b.status === OpportunityApprovalStatus.Pending && a.status !== OpportunityApprovalStatus.Pending) return 1
      return new Date(b.requestedAt).getTime() - new Date(a.requestedAt).getTime()
    })
  }, [approvals, filter, search, t])

  useEffect(() => {
    if (selectedId && !filtered.some((a) => a.id === selectedId)) {
      setSelectedId(filtered[0]?.id ?? null)
      return
    }
    if (!selectedId && filtered.length > 0) {
      setSelectedId(filtered[0].id)
    }
  }, [filtered, selectedId])

  const selected = useMemo(() => approvals.find((a) => a.id === selectedId) ?? null, [approvals, selectedId])

  const decideApproval = async (status: 'approve' | 'reject') => {
    if (!selected) return
    const payload = {
      approvedByUserName: user?.name || t('approvals.user.fallback'),
      decisionNotes: status === 'approve' ? t('approvals.decision.approved') : t('approvals.decision.rejected'),
    }
    const result = await executeAction(() => (
      status === 'approve'
        ? opportunityService.approveRequest(selected.id, payload)
        : opportunityService.rejectRequest(selected.id, payload)
    ))
    if (result !== null) await loadData()
  }

  const requestChanges = async () => {
    if (!selected) return
    const notes = window.prompt('O que precisa ser ajustado?')
    if (notes === null) return
    const result = await executeAction(() => opportunityService.requestApprovalChanges(selected.id, {
      approvedByUserName: user?.name || t('approvals.user.fallback'),
      decisionNotes: notes.trim() || 'Por favor, ajuste a solicitação.',
    }))
    if (result !== null) await loadData()
  }

  const resubmitApproval = async () => {
    if (!selected) return
    const result = await executeAction(() => opportunityService.resubmitApproval(selected.id, {
      requestedByUserName: user?.name || t('approvals.user.fallback'),
    }))
    if (result !== null) await loadData()
  }

  const markMerged = async () => {
    if (!selected) return
    const result = await executeAction(() => opportunityService.markApprovalMerged(selected.id))
    if (result !== null) await loadData()
  }

  return (
    <PageLayout
      title={t('approvals.title')}
      subtitle={t('approvals.subtitle')}
      onRefresh={() => void loadData()}
      showDefaultActions={false}
    >
      <div className="overflow-hidden rounded-xl border border-border bg-card">
        <div className="grid h-[calc(100vh-200px)] min-h-[600px] grid-cols-1 md:grid-cols-[360px_1fr]">
          <InboxColumn
            counts={counts}
            filter={filter}
            onFilterChange={setFilter}
            search={search}
            onSearchChange={setSearch}
            items={filtered}
            selectedId={selectedId}
            onSelect={setSelectedId}
            loading={loading}
            t={t}
          />

          <div className="hidden overflow-y-auto bg-muted/20 md:block">
            {selected ? (
              <ApprovalDetail
                approval={selected}
                actionLoading={actionLoading}
                onApprove={() => void decideApproval('approve')}
                onReject={() => void decideApproval('reject')}
                onRequestChanges={() => void requestChanges()}
                onResubmit={() => void resubmitApproval()}
                onMarkMerged={() => void markMerged()}
                onOpenOpportunity={() => selected.opportunityId && navigate(`/comercial/oportunidades/${selected.opportunityId}?tab=approvals`)}
                t={t}
              />
            ) : (
              <div className="flex h-full items-center justify-center px-8 text-center">
                <div className="max-w-xs text-muted-foreground">
                  <ShieldCheck className="mx-auto mb-2 h-8 w-8 opacity-40" />
                  <p className="text-sm font-medium">Nenhuma solicitação aberta</p>
                  <p className="mt-1 text-xs">Selecione um item da lista para revisar a solicitação de aprovação.</p>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </PageLayout>
  )
}

interface InboxColumnProps {
  counts: { pending: number; approved: number; rejected: number; all: number }
  filter: FilterTab
  onFilterChange: (filter: FilterTab) => void
  search: string
  onSearchChange: (value: string) => void
  items: OpportunityApprovalRequest[]
  selectedId: number | null
  onSelect: (id: number) => void
  loading: boolean
  t: (key: string) => string
}

function InboxColumn({ counts, filter, onFilterChange, search, onSearchChange, items, selectedId, onSelect, loading, t }: InboxColumnProps) {
  return (
    <aside className="flex min-h-0 flex-col border-r border-border bg-card">
      <div className="space-y-3 border-b border-border/60 p-4">
        <div className="flex items-baseline justify-between">
          <h2 className="text-base font-semibold text-foreground">Inbox</h2>
          <span className="text-xs text-muted-foreground">{counts.all} total</span>
        </div>
        <div className="flex flex-wrap items-center gap-1">
          <InboxTab label="Pendentes" count={counts.pending} active={filter === 'pending'} tone="amber" onClick={() => onFilterChange('pending')} />
          <InboxTab label="Aprovadas" count={counts.approved} active={filter === 'approved'} tone="emerald" onClick={() => onFilterChange('approved')} />
          <InboxTab label="Rejeitadas" count={counts.rejected} active={filter === 'rejected'} tone="rose" onClick={() => onFilterChange('rejected')} />
          <InboxTab label="Todas" count={counts.all} active={filter === 'all'} onClick={() => onFilterChange('all')} />
        </div>
        <div className="relative">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
          <input
            value={search}
            onChange={(e) => onSearchChange(e.target.value)}
            placeholder="Buscar tipo, oportunidade…"
            className="w-full rounded-md border border-input bg-background py-1.5 pl-8 pr-3 text-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          />
        </div>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto">
        {loading && items.length === 0 ? (
          <p className="px-4 py-6 text-center text-xs text-muted-foreground">Carregando…</p>
        ) : items.length === 0 ? (
          <p className="px-4 py-8 text-center text-xs text-muted-foreground">Nenhuma solicitação neste filtro.</p>
        ) : (
          <ul>
            {items.map((item) => (
              <li key={item.id}>
                <InboxRow approval={item} selected={selectedId === item.id} onClick={() => onSelect(item.id)} t={t} />
              </li>
            ))}
          </ul>
        )}
      </div>
    </aside>
  )
}

function InboxTab({ label, count, active, tone, onClick }: { label: string; count: number; active: boolean; tone?: 'amber' | 'emerald' | 'rose'; onClick: () => void }) {
  const dot = tone === 'amber' ? 'bg-amber-500' : tone === 'emerald' ? 'bg-emerald-500' : tone === 'rose' ? 'bg-rose-500' : ''
  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        'inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-[12px] font-semibold transition-colors',
        active ? 'bg-foreground text-background' : 'text-muted-foreground hover:bg-muted/60 hover:text-foreground',
      ].join(' ')}
    >
      {tone && <span className={`h-1.5 w-1.5 rounded-full ${dot}`} />}
      {label}
      <span className={`rounded px-1 py-0.5 text-[10px] font-bold ${active ? 'bg-white/20 text-background' : 'bg-muted text-muted-foreground'}`}>{count}</span>
    </button>
  )
}

function InboxRow({ approval, selected, onClick, t }: { approval: OpportunityApprovalRequest; selected: boolean; onClick: () => void; t: (key: string) => string }) {
  const isOpen = isPendingDecision(approval.status)
  const hours = hoursSince(approval.requestedAt)
  const ageTone = hours >= 24 ? 'text-rose-700' : hours >= 2 ? 'text-amber-700' : 'text-muted-foreground'
  const statusBadge = statusBadgeConfig(approval.status)

  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        'group flex w-full flex-col gap-1.5 border-b border-border/60 px-4 py-3 text-left transition-colors',
        selected ? 'bg-primary/8 [box-shadow:inset_3px_0_0_0_hsl(var(--primary))]' : 'hover:bg-muted/40',
      ].join(' ')}
    >
      <div className="flex items-center justify-between">
        <span className="font-mono text-[11px] font-semibold text-muted-foreground">#{approval.id}</span>
        {isOpen && (
          <span className={`inline-flex items-center gap-1 text-[10px] font-bold ${ageTone}`}>
            <Clock className="h-2.5 w-2.5" /> {ageLabel(hours)}
          </span>
        )}
      </div>
      <p className="line-clamp-2 text-[13px] font-semibold leading-tight text-foreground">
        {approvalTypeKeys[approval.approvalType] ? t(approvalTypeKeys[approval.approvalType]) : 'Solicitação'}
        {approval.opportunityName && (
          <span className="font-normal text-muted-foreground"> · {approval.opportunityName}</span>
        )}
      </p>
      <p className="line-clamp-1 text-[11px] text-muted-foreground">
        {approval.negotiationTitle || 'Sem negociação vinculada'} · por {approval.requestedByUserName}
      </p>
      <div className="flex items-center justify-between pt-0.5">
        <span className={`inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wider ${statusBadge.bg} ${statusBadge.text}`}>{statusBadge.label}</span>
        <ChevronRight className="h-3 w-3 text-muted-foreground/40 group-hover:text-foreground" />
      </div>
    </button>
  )
}

interface DetailProps {
  approval: OpportunityApprovalRequest
  actionLoading: boolean
  onApprove: () => void
  onReject: () => void
  onRequestChanges: () => void
  onResubmit: () => void
  onMarkMerged: () => void
  onOpenOpportunity: () => void
  t: (key: string) => string
}

function ApprovalDetail({ approval, actionLoading, onApprove, onReject, onRequestChanges, onResubmit, onMarkMerged, onOpenOpportunity, t }: DetailProps) {
  const isApproved = approval.status === OpportunityApprovalStatus.Approved
  const isRejected = approval.status === OpportunityApprovalStatus.Rejected
  const isMerged = approval.status === OpportunityApprovalStatus.Merged
  const isChangesRequested = approval.status === OpportunityApprovalStatus.ChangesRequested
  const canDecide = approval.status === OpportunityApprovalStatus.Pending || approval.status === OpportunityApprovalStatus.InReview
  const hours = hoursSince(approval.requestedAt)
  const typeLabel = approvalTypeKeys[approval.approvalType] ? t(approvalTypeKeys[approval.approvalType]) : 'Solicitação'
  const statusInfo = statusBadgeConfig(approval.status)
  const statusIcon = isApproved || isMerged ? <CheckCircle2 className="h-3 w-3" /> : isRejected ? <XCircle className="h-3 w-3" /> : <Clock className="h-3 w-3" />
  const isOpen = isPendingDecision(approval.status)

  return (
    <div className="px-7 py-6">
      {/* PR-style header */}
      <div className="mb-2 flex items-center gap-2 text-xs text-muted-foreground">
        <span className="font-mono font-semibold">#{approval.id}</span>
        <span>·</span>
        <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wider text-foreground">{typeLabel}</span>
        <div className="ml-auto flex items-center gap-1">
          <button type="button" title="Histórico" className="flex h-7 w-7 items-center justify-center rounded-md border border-border text-muted-foreground hover:bg-muted">
            <History className="h-3.5 w-3.5" />
          </button>
          <button type="button" title="Mais" className="flex h-7 w-7 items-center justify-center rounded-md border border-border text-muted-foreground hover:bg-muted">
            <MoreHorizontal className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>
      <h1 className="text-[26px] font-semibold leading-tight tracking-tight text-foreground">
        {typeLabel}
        {approval.opportunityName && <span className="text-muted-foreground"> · {approval.opportunityName}</span>}
      </h1>
      <div className="mt-3 flex flex-wrap items-center gap-3 text-[13px] text-muted-foreground">
        <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11.5px] font-bold ${statusInfo.bg} ${statusInfo.text}`}>
          {statusIcon} {statusInfo.label}
        </span>
        {isOpen && hours >= 24 && (
          <span className="inline-flex items-center gap-1 rounded bg-rose-100 px-2 py-0.5 text-[10.5px] font-bold uppercase tracking-wider text-rose-800">
            <Zap className="h-3 w-3" /> URGENTE · há {Math.round(hours / 24)}d
          </span>
        )}
        <span className="flex items-center gap-2">
          <Avatar name={approval.requestedByUserName} size={22} />
          <span><strong className="text-foreground">{approval.requestedByUserName}</strong> abriu esta solicitação em {formatDate(approval.requestedAt)}</span>
        </span>
      </div>

      {/* Grid principal + sidebar */}
      <div className="mt-6 grid gap-5 lg:grid-cols-[1fr_280px]">
        {/* Main column */}
        <div className="space-y-4 min-w-0">
          <Panel title="Justificativa" accent="primary">
            <div className="flex gap-3">
              <Avatar name={approval.requestedByUserName} size={32} />
              <div className="min-w-0 flex-1">
                <div className="text-xs text-muted-foreground">
                  <strong className="text-foreground">{approval.requestedByUserName}</strong> · Solicitante
                </div>
                <div className="mt-1.5 whitespace-pre-wrap rounded-md border-l-[3px] border-primary bg-muted/30 px-3.5 py-2.5 text-sm leading-relaxed text-foreground">
                  {approval.reason || 'Sem justificativa fornecida.'}
                </div>
              </div>
            </div>
          </Panel>

          <Panel title={`Conversa · ${approval.decisionNotes ? '1 comentário' : '0 comentários'}`} accent="blue">
            {approval.decisionNotes ? (
              <div className="flex gap-2.5">
                <Avatar name={approval.approvedByUserName || 'Aprovador'} size={32} tone={isApproved ? 'emerald' : 'rose'} />
                <div className="min-w-0 flex-1 overflow-hidden rounded-lg border border-border bg-card">
                  <div className="flex items-center gap-2 border-b border-border/60 bg-muted/30 px-3.5 py-2 text-xs">
                    <strong className="text-foreground">{approval.approvedByUserName || 'Aprovador'}</strong>
                    <span className={`rounded px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wider ${isApproved ? 'bg-emerald-100 text-emerald-800' : 'bg-rose-100 text-rose-800'}`}>aprovador</span>
                    <span className="text-muted-foreground">comentou {approval.decidedAt ? `em ${formatDate(approval.decidedAt)}` : ''}</span>
                  </div>
                  <div className="px-3.5 py-2.5 text-sm leading-relaxed text-foreground">
                    {approval.decisionNotes}
                  </div>
                  {isApproved && (
                    <div className="flex items-center gap-1.5 border-t border-emerald-200 bg-emerald-50 px-3.5 py-1.5 text-xs font-semibold text-emerald-800">
                      <CheckCircle2 className="h-3.5 w-3.5" /> Aprovou esta solicitação
                    </div>
                  )}
                  {isRejected && (
                    <div className="flex items-center gap-1.5 border-t border-rose-200 bg-rose-50 px-3.5 py-1.5 text-xs font-semibold text-rose-800">
                      <XCircle className="h-3.5 w-3.5" /> Rejeitou esta solicitação
                    </div>
                  )}
                </div>
              </div>
            ) : (
              <p className="text-xs text-muted-foreground">Sem comentários nesta solicitação.</p>
            )}
          </Panel>
        </div>

        {/* Sidebar */}
        <aside className="space-y-3.5">
          <ActionPanel
            approval={approval}
            actionLoading={actionLoading}
            onApprove={onApprove}
            onReject={onReject}
            onRequestChanges={onRequestChanges}
            onResubmit={onResubmit}
            onMarkMerged={onMarkMerged}
            canDecide={canDecide}
            isChangesRequested={isChangesRequested}
            isApproved={isApproved}
            isRejected={isRejected}
            isMerged={isMerged}
          />

          <SidebarBlock title="Aprovadores" icon={<Users className="h-3.5 w-3.5" />} action={(
            <button type="button" title="Adicionar" className="flex h-5 w-5 items-center justify-center rounded border border-border text-muted-foreground hover:bg-muted">
              <Plus className="h-3 w-3" />
            </button>
          )}>
            <ReviewerRow
              name={approval.requestedByUserName}
              role="Solicitante"
              status="comentou"
            />
            {approval.approvedByUserName ? (
              <ReviewerRow
                name={approval.approvedByUserName}
                role="Aprovador"
                status={isApproved ? 'aprovou' : isRejected ? 'rejeitou' : 'pendente'}
                decidedAt={approval.decidedAt ? formatDate(approval.decidedAt) : undefined}
                required
              />
            ) : (
              <ReviewerRow
                name="Aprovador"
                role="Aguardando designação"
                status="pendente"
                required
              />
            )}
          </SidebarBlock>

          <SidebarBlock title="Vinculado a" icon={<ShieldCheck className="h-3.5 w-3.5" />}>
            <LinkRow icon={<MessageSquare className="h-3 w-3" />} label="Negociação" value={approval.negotiationTitle || 'Sem título'} tone="purple" />
            <LinkRow icon={<ArrowUpRight className="h-3 w-3" />} label="Oportunidade" value={approval.opportunityName || `#${approval.opportunityNegotiationId}`} tone="primary" onClick={onOpenOpportunity} />
          </SidebarBlock>
        </aside>
      </div>
    </div>
  )
}

interface ActionPanelProps {
  approval: OpportunityApprovalRequest
  actionLoading: boolean
  canDecide: boolean
  isChangesRequested: boolean
  isApproved: boolean
  isRejected: boolean
  isMerged: boolean
  onApprove: () => void
  onReject: () => void
  onRequestChanges: () => void
  onResubmit: () => void
  onMarkMerged: () => void
}

function ActionPanel({ approval, actionLoading, canDecide, isChangesRequested, isApproved, isRejected, isMerged, onApprove, onReject, onRequestChanges, onResubmit, onMarkMerged }: ActionPanelProps) {
  if (canDecide) {
    return (
      <div className="rounded-xl border-2 border-primary bg-card p-4 shadow-[0_4px_14px_rgba(11,165,164,0.08)]">
        <div className="text-[10.5px] font-bold uppercase tracking-[0.14em] text-muted-foreground">Sua decisão</div>
        <p className="mt-1.5 text-xs leading-relaxed text-foreground">
          Sua aprovação é necessária pra essa exceção avançar.
        </p>
        <div className="mt-3 flex flex-col gap-2">
          <Button size="sm" variant="success" fullWidth disabled={actionLoading} onClick={onApprove} icon={<ThumbsUp />}>
            Aprovar
          </Button>
          <Button size="sm" variant="outline-warning" fullWidth disabled={actionLoading} onClick={onRequestChanges} icon={<MessageSquare />}>
            Pedir ajustes
          </Button>
          <Button size="sm" variant="outline-danger" fullWidth disabled={actionLoading} onClick={onReject} icon={<ThumbsDown />}>
            Rejeitar
          </Button>
        </div>
        <div className="mt-3 flex items-start gap-2 rounded-md border border-amber-200 bg-amber-50 px-2.5 py-1.5 text-[11px] leading-snug text-amber-800">
          <Eye className="mt-0.5 h-3 w-3 shrink-0" />
          Sua decisão é registrada com timestamp e fica no histórico da oportunidade.
        </div>
      </div>
    )
  }

  if (isChangesRequested) {
    return (
      <div className="rounded-xl border-2 border-amber-300 bg-amber-50 p-4">
        <div className="text-[10.5px] font-bold uppercase tracking-[0.14em] text-amber-800">Ajustes pedidos</div>
        <p className="mt-1.5 text-xs leading-relaxed text-amber-800">
          {approval.approvedByUserName ? <><strong>{approval.approvedByUserName}</strong> pediu ajustes</> : 'Aguardando ajustes do solicitante'}
          {approval.decidedAt && <span> em {formatDate(approval.decidedAt)}</span>}.
        </p>
        {approval.decisionNotes && (
          <p className="mt-2 rounded-md border-l-2 border-amber-500 bg-card/50 px-2.5 py-1.5 text-xs italic text-amber-900">
            "{approval.decisionNotes}"
          </p>
        )}
        <Button size="sm" variant="primary" fullWidth disabled={actionLoading} onClick={onResubmit} icon={<ThumbsUp />} className="mt-3">
          Reenviar para aprovação
        </Button>
      </div>
    )
  }

  if (isApproved) {
    return (
      <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4">
        <div className="text-[10.5px] font-bold uppercase tracking-[0.14em] text-emerald-800">Aprovada</div>
        <p className="mt-2 text-xs text-emerald-900">
          Por <strong>{approval.approvedByUserName}</strong>
          {approval.decidedAt && <span> em {formatDate(approval.decidedAt)}</span>}.
        </p>
        <Button size="sm" variant="outline-success" fullWidth disabled={actionLoading} onClick={onMarkMerged} icon={<CheckCircle2 />} className="mt-3">
          Marcar como aplicada
        </Button>
        <p className="mt-2 text-[10.5px] text-emerald-700">Use quando a exceção for refletida na negociação real.</p>
      </div>
    )
  }

  if (isMerged) {
    return (
      <div className="rounded-xl border border-purple-200 bg-purple-50 p-4">
        <div className="text-[10.5px] font-bold uppercase tracking-[0.14em] text-purple-800">Mesclada</div>
        <p className="mt-2 text-xs text-purple-900">
          Aprovada por <strong>{approval.approvedByUserName}</strong>
          {approval.decidedAt && <span> em {formatDate(approval.decidedAt)}</span>} e aplicada na negociação.
        </p>
      </div>
    )
  }

  return (
    <div className="rounded-xl border border-border bg-card p-4">
      <div className="text-[10.5px] font-bold uppercase tracking-[0.14em] text-muted-foreground">Decisão final</div>
      <p className="mt-2 text-xs text-foreground">
        {isRejected ? <strong className="text-rose-700">Rejeitada</strong> : <strong>Encerrada</strong>}
        {approval.approvedByUserName && <span> por <strong className="text-foreground">{approval.approvedByUserName}</strong></span>}
        {approval.decidedAt && <span> em {formatDate(approval.decidedAt)}</span>}
      </p>
    </div>
  )
}

function Panel({ title, accent, children }: { title: string; accent?: 'primary' | 'emerald' | 'rose' | 'blue'; children: React.ReactNode }) {
  const accentClass = accent === 'emerald' ? 'bg-emerald-500' : accent === 'rose' ? 'bg-rose-500' : accent === 'blue' ? 'bg-blue-500' : accent === 'primary' ? 'bg-primary' : 'bg-muted-foreground'
  return (
    <section className="overflow-hidden rounded-xl border border-border bg-card">
      <header className="flex items-center gap-2 border-b border-border/60 bg-muted/20 px-4 py-2.5 text-[11px] font-bold uppercase tracking-wider text-foreground">
        {accent && <span className={`inline-block h-4 w-1 rounded ${accentClass}`} />}
        {title}
      </header>
      <div className="px-4 py-3.5">{children}</div>
    </section>
  )
}

function SidebarBlock({ title, icon, action, children }: { title: string; icon?: React.ReactNode; action?: React.ReactNode; children: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-border bg-card p-3.5">
      <div className="mb-2.5 flex items-center justify-between">
        <span className="flex items-center gap-1.5 text-[10.5px] font-bold uppercase tracking-wider text-foreground">
          {icon} {title}
        </span>
        {action}
      </div>
      <div className="space-y-2">{children}</div>
    </div>
  )
}

function ReviewerRow({ name, role, status, decidedAt, required }: { name: string; role: string; status: 'pendente' | 'aprovou' | 'rejeitou' | 'comentou'; decidedAt?: string; required?: boolean }) {
  const config = {
    pendente: { color: 'text-amber-700', label: 'aguardando' },
    aprovou: { color: 'text-emerald-700', label: 'aprovou' },
    rejeitou: { color: 'text-rose-700', label: 'rejeitou' },
    comentou: { color: 'text-blue-700', label: 'comentou' },
  }[status]

  const tone = status === 'aprovou' ? 'emerald' : status === 'rejeitou' ? 'rose' : status === 'comentou' ? 'blue' : 'amber'

  return (
    <div className="flex items-center gap-2.5">
      <div className="relative">
        <Avatar name={name} size={30} />
        <span className={`absolute -bottom-0.5 -right-0.5 flex h-4 w-4 items-center justify-center rounded-full border-2 border-card ${tone === 'emerald' ? 'bg-emerald-500' : tone === 'rose' ? 'bg-rose-500' : tone === 'blue' ? 'bg-blue-500' : 'bg-amber-500'}`}>
          {status === 'aprovou' && <CheckCircle2 className="h-2.5 w-2.5 text-white" />}
          {status === 'rejeitou' && <XCircle className="h-2.5 w-2.5 text-white" />}
          {status === 'comentou' && <MessageSquare className="h-2 w-2 text-white" />}
          {status === 'pendente' && <Clock className="h-2 w-2 text-white" />}
        </span>
      </div>
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-1.5 text-xs font-semibold text-foreground">
          {name}
          {required && <span className="rounded bg-rose-100 px-1 py-0.5 text-[9px] font-bold uppercase tracking-wider text-rose-700">Obrig.</span>}
        </div>
        <div className="text-[11px] text-muted-foreground">
          {role} · <span className={`font-semibold ${config.color}`}>{config.label}</span>
          {decidedAt && <span> em {decidedAt}</span>}
        </div>
      </div>
    </div>
  )
}

function LinkRow({ icon, label, value, tone, onClick }: { icon: React.ReactNode; label: string; value: string; tone?: 'primary' | 'purple'; onClick?: () => void }) {
  const toneBg = tone === 'purple' ? 'bg-purple-100 text-purple-700' : 'bg-primary/10 text-primary'
  const Wrap = onClick ? 'button' : 'div'
  return (
    <Wrap
      type={onClick ? 'button' : undefined}
      onClick={onClick}
      className={`flex w-full items-center gap-2 rounded-md border border-border/60 bg-muted/20 px-2 py-1.5 text-left transition-colors ${onClick ? 'cursor-pointer hover:border-primary/40 hover:bg-muted/40' : ''}`}
    >
      <span className={`flex h-6 w-6 shrink-0 items-center justify-center rounded ${toneBg}`}>{icon}</span>
      <div className="min-w-0 flex-1">
        <p className="text-[9.5px] font-bold uppercase tracking-wider text-muted-foreground">{label}</p>
        <p className="truncate text-[12px] font-semibold text-foreground">{value}</p>
      </div>
      {onClick && <ExternalLink className="h-3 w-3 shrink-0 text-muted-foreground" />}
    </Wrap>
  )
}

function Avatar({ name, size = 28, tone }: { name: string; size?: number; tone?: 'emerald' | 'rose' }) {
  const initials = name.split(' ').slice(0, 2).map((part) => part[0]).join('').toUpperCase()
  const bg = tone === 'emerald' ? 'bg-emerald-100 text-emerald-700' : tone === 'rose' ? 'bg-rose-100 text-rose-700' : 'bg-primary/10 text-primary'
  return (
    <span
      className={`inline-flex shrink-0 items-center justify-center rounded-full font-bold ${bg}`}
      style={{ width: size, height: size, fontSize: size * 0.38 }}
    >
      {initials}
    </span>
  )
}
