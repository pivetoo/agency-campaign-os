import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, PageLayout, useApi, useAuth, useI18n } from 'archon-ui'
import { ArrowUpRight, CheckCircle2, ChevronRight, Clock, ExternalLink, MessageSquare, Search, ShieldCheck, ThumbsDown, ThumbsUp, XCircle } from 'lucide-react'
import { opportunityService, OpportunityApprovalStatus, type OpportunityApprovalRequest, type ApprovalSummary } from '../../../services/opportunityService'
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

export default function CommercialApprovals() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [approvals, setApprovals] = useState<OpportunityApprovalRequest[]>([])
  const [summary, setSummary] = useState<ApprovalSummary | null>(null)
  const [filter, setFilter] = useState<FilterTab>('pending')
  const [search, setSearch] = useState('')
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const { execute: fetchApprovals, loading } = useApi<OpportunityApprovalRequest[]>({ showErrorMessage: true })
  const { execute: executeAction, loading: actionLoading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadData = async () => {
    const result = await fetchApprovals(() => opportunityService.getAllApprovals({ page: 1, pageSize: 200 }))
    if (result) setApprovals(result)
    const summaryResult = await opportunityService.getApprovalsSummary()
    setSummary(summaryResult)
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

  return (
    <PageLayout
      title={t('approvals.title')}
      subtitle={t('approvals.subtitle')}
      onRefresh={() => void loadData()}
      showDefaultActions={false}
    >
      <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-3">
        <KpiCard label={t('approvals.kpi.pending')} value={summary?.pending ?? 0} tone="amber" icon={<Clock className="h-3.5 w-3.5" />} />
        <KpiCard label={t('approvals.kpi.approved')} value={summary?.approved ?? 0} tone="emerald" icon={<CheckCircle2 className="h-3.5 w-3.5" />} />
        <KpiCard label={t('approvals.kpi.rejected')} value={summary?.rejected ?? 0} tone="rose" icon={<XCircle className="h-3.5 w-3.5" />} />
      </div>

      <div className="overflow-hidden rounded-xl border border-border bg-card">
        <div className="grid h-[calc(100vh-260px)] min-h-[520px] grid-cols-1 md:grid-cols-[360px_1fr]">
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

function KpiCard({ label, value, tone, icon }: { label: string; value: number; tone: 'amber' | 'emerald' | 'rose'; icon: React.ReactNode }) {
  const toneText = tone === 'emerald' ? 'text-emerald-700' : tone === 'rose' ? 'text-rose-700' : 'text-amber-700'
  return (
    <div className="rounded-xl border border-border bg-card p-4">
      <div className={`flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wider ${toneText}`}>{icon} {label}</div>
      <div className="mt-1 text-2xl font-bold text-foreground">{value}</div>
    </div>
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
    <aside className="flex flex-col border-r border-border bg-card">
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
  const isPending = approval.status === OpportunityApprovalStatus.Pending
  const isApproved = approval.status === OpportunityApprovalStatus.Approved
  const isRejected = approval.status === OpportunityApprovalStatus.Rejected
  const hours = hoursSince(approval.requestedAt)
  const ageTone = hours >= 24 ? 'text-rose-700' : hours >= 2 ? 'text-amber-700' : 'text-muted-foreground'
  const statusBadge = isPending
    ? { bg: 'bg-amber-100', text: 'text-amber-800', label: 'Pendente' }
    : isApproved
      ? { bg: 'bg-emerald-100', text: 'text-emerald-800', label: 'Aprovada' }
      : isRejected
        ? { bg: 'bg-rose-100', text: 'text-rose-800', label: 'Rejeitada' }
        : { bg: 'bg-muted', text: 'text-muted-foreground', label: 'Cancelada' }

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
        {isPending && (
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
  onOpenOpportunity: () => void
  t: (key: string) => string
}

function ApprovalDetail({ approval, actionLoading, onApprove, onReject, onOpenOpportunity, t }: DetailProps) {
  const isPending = approval.status === OpportunityApprovalStatus.Pending
  const isApproved = approval.status === OpportunityApprovalStatus.Approved
  const isRejected = approval.status === OpportunityApprovalStatus.Rejected
  const hours = hoursSince(approval.requestedAt)
  const statusBadge = isPending
    ? { bg: 'bg-amber-100', text: 'text-amber-800', label: 'Pendente de decisão' }
    : isApproved
      ? { bg: 'bg-emerald-100', text: 'text-emerald-800', label: 'Aprovada' }
      : isRejected
        ? { bg: 'bg-rose-100', text: 'text-rose-800', label: 'Rejeitada' }
        : { bg: 'bg-muted', text: 'text-muted-foreground', label: 'Cancelada' }

  return (
    <div className="space-y-5 px-6 py-5">
      <div>
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          <span className="font-mono font-semibold">#{approval.id}</span>
          <span>·</span>
          <span className="rounded bg-muted px-1.5 py-0.5 font-bold uppercase tracking-wider text-foreground">{approvalTypeKeys[approval.approvalType] ? t(approvalTypeKeys[approval.approvalType]) : 'Solicitação'}</span>
        </div>
        <h1 className="mt-2 text-xl font-semibold leading-tight text-foreground">
          {approvalTypeKeys[approval.approvalType] ? t(approvalTypeKeys[approval.approvalType]) : 'Solicitação de aprovação'}
          {approval.opportunityName && (
            <span className="text-muted-foreground"> · {approval.opportunityName}</span>
          )}
        </h1>
        <div className="mt-3 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
          <span className={`inline-flex items-center rounded px-2 py-0.5 text-[10.5px] font-bold uppercase tracking-wider ${statusBadge.bg} ${statusBadge.text}`}>{statusBadge.label}</span>
          {isPending && hours >= 24 && (
            <span className="inline-flex items-center gap-1 rounded bg-rose-100 px-2 py-0.5 text-[10.5px] font-bold uppercase tracking-wider text-rose-800">
              <Clock className="h-3 w-3" /> {Math.round(hours / 24)}d aguardando
            </span>
          )}
          <span className="flex items-center gap-2">
            <Avatar name={approval.requestedByUserName} />
            <span><strong className="text-foreground">{approval.requestedByUserName}</strong> abriu em {formatDate(approval.requestedAt)}</span>
          </span>
        </div>
      </div>

      <Panel title="Justificativa" accent="primary">
        <div className="flex gap-3">
          <Avatar name={approval.requestedByUserName} />
          <div className="min-w-0 flex-1">
            <div className="text-xs text-muted-foreground"><strong className="text-foreground">{approval.requestedByUserName}</strong> · Solicitante</div>
            <p className="mt-1.5 whitespace-pre-wrap rounded-md border-l-2 border-primary bg-muted/30 px-3 py-2 text-sm text-foreground">
              {approval.reason || 'Sem justificativa fornecida.'}
            </p>
          </div>
        </div>
      </Panel>

      {!isPending && approval.decisionNotes && (
        <Panel title="Decisão" accent={isApproved ? 'emerald' : 'rose'}>
          <div className="flex gap-3">
            <Avatar name={approval.approvedByUserName || 'Aprovador'} tone={isApproved ? 'emerald' : 'rose'} />
            <div className="min-w-0 flex-1">
              <div className="text-xs text-muted-foreground">
                <strong className={isApproved ? 'text-emerald-700' : 'text-rose-700'}>{approval.approvedByUserName || 'Aprovador'}</strong> · {isApproved ? 'aprovou' : 'rejeitou'}
                {approval.decidedAt && <span> em {formatDate(approval.decidedAt)}</span>}
              </div>
              <p className={`mt-1.5 whitespace-pre-wrap rounded-md border-l-2 ${isApproved ? 'border-emerald-500' : 'border-rose-500'} bg-muted/30 px-3 py-2 text-sm italic text-foreground`}>
                "{approval.decisionNotes}"
              </p>
            </div>
          </div>
        </Panel>
      )}

      <Panel title="Vinculado a">
        <div className="space-y-1.5">
          <LinkRow icon={<MessageSquare className="h-3.5 w-3.5" />} label="Negociação" value={approval.negotiationTitle || 'Sem título'} tone="purple" />
          <LinkRow icon={<ArrowUpRight className="h-3.5 w-3.5" />} label="Oportunidade" value={approval.opportunityName || `#${approval.opportunityNegotiationId}`} tone="primary" onClick={onOpenOpportunity} />
        </div>
      </Panel>

      {isPending && (
        <div className="rounded-xl border border-amber-200 bg-amber-50 p-4">
          <div className="mb-3 flex items-center gap-2 text-[11px] font-bold uppercase tracking-wider text-amber-800">
            <ShieldCheck className="h-3.5 w-3.5" /> Sua decisão
          </div>
          <p className="mb-3 text-xs text-amber-800">A negociação fica pausada até você responder.</p>
          <div className="flex flex-wrap gap-2">
            <Button size="sm" variant="primary" disabled={actionLoading} onClick={onApprove} className="!bg-emerald-600 !border-emerald-600 hover:!bg-emerald-700">
              <ThumbsUp className="mr-1.5 h-3.5 w-3.5" /> Aprovar
            </Button>
            <Button size="sm" variant="outline-danger" disabled={actionLoading} onClick={onReject}>
              <ThumbsDown className="mr-1.5 h-3.5 w-3.5" /> Rejeitar
            </Button>
            <Button size="sm" variant="outline" onClick={onOpenOpportunity} className="ml-auto">
              <ExternalLink className="mr-1.5 h-3.5 w-3.5" /> Abrir oportunidade
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

function Panel({ title, accent, children }: { title: string; accent?: 'primary' | 'emerald' | 'rose'; children: React.ReactNode }) {
  const accentClass = accent === 'emerald' ? 'bg-emerald-500' : accent === 'rose' ? 'bg-rose-500' : accent === 'primary' ? 'bg-primary' : 'bg-muted-foreground'
  return (
    <section className="overflow-hidden rounded-xl border border-border bg-card">
      <header className="flex items-center gap-2 border-b border-border/60 bg-muted/20 px-4 py-2 text-[11px] font-bold uppercase tracking-wider text-foreground">
        {accent && <span className={`inline-block h-3.5 w-1 rounded ${accentClass}`} />}
        {title}
      </header>
      <div className="px-4 py-3">{children}</div>
    </section>
  )
}

function LinkRow({ icon, label, value, tone, onClick }: { icon: React.ReactNode; label: string; value: string; tone?: 'primary' | 'purple'; onClick?: () => void }) {
  const toneBg = tone === 'purple' ? 'bg-purple-100 text-purple-700' : 'bg-primary/10 text-primary'
  const Wrap = onClick ? 'button' : 'div'
  return (
    <Wrap
      type={onClick ? 'button' : undefined}
      onClick={onClick}
      className={`flex w-full items-center gap-2.5 rounded-md border border-border/60 bg-muted/20 px-2.5 py-1.5 text-left transition-colors ${onClick ? 'cursor-pointer hover:border-primary/40 hover:bg-muted/40' : ''}`}
    >
      <span className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-md ${toneBg}`}>{icon}</span>
      <div className="min-w-0 flex-1">
        <p className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">{label}</p>
        <p className="truncate text-xs font-semibold text-foreground">{value}</p>
      </div>
      {onClick && <ExternalLink className="h-3 w-3 shrink-0 text-muted-foreground" />}
    </Wrap>
  )
}

function Avatar({ name, tone }: { name: string; tone?: 'emerald' | 'rose' }) {
  const initials = name.split(' ').slice(0, 2).map((part) => part[0]).join('').toUpperCase()
  const bg = tone === 'emerald' ? 'bg-emerald-100 text-emerald-700' : tone === 'rose' ? 'bg-rose-100 text-rose-700' : 'bg-primary/10 text-primary'
  return (
    <span className={`inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-[10.5px] font-bold ${bg}`}>{initials}</span>
  )
}
