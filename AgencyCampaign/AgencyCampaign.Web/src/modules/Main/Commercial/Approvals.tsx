import { useCallback, useEffect, useMemo, useState, type ReactElement } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, ConfirmModal, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, PageLayout, SearchableSelect, Sheet, SheetContent, SheetHeader, SheetTitle, UsersManagementService, useApi, useAuth, useI18n } from 'archon-ui'
import { ArrowUpRight, CheckCircle2, ChevronRight, Clock, ExternalLink, Eye, FileText, MessageSquare, Plus, Search, ShieldCheck, ThumbsDown, ThumbsUp, Users, XCircle, Zap } from 'lucide-react'
import { opportunityService, OpportunityApprovalStatus, type OpportunityApprovalRequest } from '../../../services/opportunityService'
import type { OpportunityApprovalComment } from '../../../types/opportunityApprovalComment'
import { OpportunityApprovalReviewerStatus, type OpportunityApprovalReviewer } from '../../../types/opportunityApprovalReviewer'
import type { OpportunityApprovalDiff } from '../../../types/opportunityApprovalDiff'
import { commercialResponsibleService } from '../../../services/commercialResponsibleService'
import CommentInputWithMentions, { type MentionableUser } from '../../../components/comments/CommentInputWithMentions'
import { formatDate } from '../../../lib/format'
import { resolveAssetUrl } from '../../../lib/assetUrl'

const approvalTypeKeys: Record<number, string> = {
  1: 'approvals.type.discount',
  2: 'approvals.type.margin',
  3: 'approvals.type.deadline',
  4: 'approvals.type.exception',
}

function isPolicyDeviation(approvalType: number): boolean {
  return approvalType === 1 || approvalType === 2 || approvalType === 3
}

type FilterTab = 'pending' | 'approved' | 'rejected' | 'all'

function hoursSince(iso: string): number {
  return Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 3600_000))
}

function ageLabel(hours: number, t: (key: string) => string): string {
  if (hours < 1) return t('commercialApprovals.age.now')
  if (hours < 24) return t('commercialApprovals.age.hours').replace('{0}', String(hours))
  return t('commercialApprovals.age.days').replace('{0}', String(Math.round(hours / 24)))
}

function statusBadgeConfig(status: number, t: (key: string) => string): { bg: string; text: string; label: string } {
  if (status === OpportunityApprovalStatus.Pending) return { bg: 'bg-amber-100', text: 'text-amber-800', label: t('commercialApprovals.status.pending') }
  if (status === OpportunityApprovalStatus.InReview) return { bg: 'bg-blue-100', text: 'text-blue-800', label: t('commercialApprovals.status.inReview') }
  if (status === OpportunityApprovalStatus.ChangesRequested) return { bg: 'bg-amber-100', text: 'text-amber-800', label: t('commercialApprovals.status.changesRequested') }
  if (status === OpportunityApprovalStatus.Approved) return { bg: 'bg-emerald-100', text: 'text-emerald-800', label: t('commercialApprovals.status.approved') }
  if (status === OpportunityApprovalStatus.Merged) return { bg: 'bg-purple-100', text: 'text-purple-800', label: t('commercialApprovals.status.merged') }
  if (status === OpportunityApprovalStatus.Rejected) return { bg: 'bg-rose-100', text: 'text-rose-800', label: t('commercialApprovals.status.rejected') }
  return { bg: 'bg-muted', text: 'text-muted-foreground', label: t('commercialApprovals.status.cancelled') }
}

function isPendingDecision(status: number): boolean {
  return status === OpportunityApprovalStatus.Pending || status === OpportunityApprovalStatus.InReview || status === OpportunityApprovalStatus.ChangesRequested
}

function renderBodyWithMentions(body: string, users: MentionableUser[]): Array<string | ReactElement> {
  const sorted = [...users].sort((a, b) => b.name.length - a.name.length)
  const parts: Array<string | ReactElement> = []
  let cursor = 0
  let nodeIndex = 0
  while (cursor < body.length) {
    const atIdx = body.indexOf('@', cursor)
    if (atIdx === -1) {
      parts.push(body.slice(cursor))
      break
    }
    if (atIdx > cursor) {
      parts.push(body.slice(cursor, atIdx))
    }
    const remaining = body.slice(atIdx + 1)
    const found = sorted.find((item) => item.name && remaining.startsWith(item.name))
    if (found) {
      parts.push(<span key={`m-${nodeIndex++}`} className="rounded bg-primary/15 px-1 font-medium text-primary">@{found.name}</span>)
      cursor = atIdx + 1 + found.name.length
    } else {
      parts.push('@')
      cursor = atIdx + 1
    }
  }
  return parts
}

function useIsMobile() {
  const [isMobile, setIsMobile] = useState(() => typeof window !== 'undefined' && window.matchMedia('(max-width: 767px)').matches)
  useEffect(() => {
    const query = window.matchMedia('(max-width: 767px)')
    const handler = (event: MediaQueryListEvent) => setIsMobile(event.matches)
    query.addEventListener('change', handler)
    return () => query.removeEventListener('change', handler)
  }, [])
  return isMobile
}

export default function CommercialApprovals() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [approvals, setApprovals] = useState<OpportunityApprovalRequest[]>([])
  const [filter, setFilter] = useState<FilterTab>('pending')
  const [search, setSearch] = useState('')
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [mobileDetailOpen, setMobileDetailOpen] = useState(false)
  const isMobile = useIsMobile()
  const [reviewerRefreshKey, setReviewerRefreshKey] = useState(0)
  const [requestChangesOpen, setRequestChangesOpen] = useState(false)
  const [requestChangesNotes, setRequestChangesNotes] = useState('')
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

  useEffect(() => {
    const refetchOnFocus = () => {
      if (document.visibilityState !== 'visible') return
      void loadData()
      setReviewerRefreshKey((value) => value + 1)
    }
    window.addEventListener('focus', refetchOnFocus)
    document.addEventListener('visibilitychange', refetchOnFocus)
    return () => {
      window.removeEventListener('focus', refetchOnFocus)
      document.removeEventListener('visibilitychange', refetchOnFocus)
    }
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
        || (a.proposalName ?? '').toLowerCase().includes(term)
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
    const result = await executeAction(() => opportunityService.recordReviewerDecision(selected.id, {
      approved: status === 'approve',
      notes: status === 'approve' ? t('approvals.decision.approved') : t('approvals.decision.rejected'),
    }))
    if (result !== null) {
      setReviewerRefreshKey((value) => value + 1)
      await loadData()
    }
  }

  const openRequestChanges = () => {
    if (!selected) return
    setRequestChangesNotes('')
    setRequestChangesOpen(true)
  }

  const confirmRequestChanges = async () => {
    if (!selected) return
    const result = await executeAction(() => opportunityService.requestApprovalChanges(selected.id, {
      approvedByUserName: user?.name || t('approvals.user.fallback'),
      decisionNotes: requestChangesNotes.trim() || t('commercialApprovals.requestChanges.defaultNote'),
    }))
    if (result !== null) {
      setRequestChangesOpen(false)
      await loadData()
    }
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
            onSelect={(id) => { setSelectedId(id); setMobileDetailOpen(true) }}
            loading={loading}
            t={t}
          />

          <div className="hidden overflow-y-auto bg-muted/20 md:block">
            {selected ? (
              <ApprovalDetail
                approval={selected}
                actionLoading={actionLoading}
                currentUserName={user?.name || t('approvals.user.fallback')}
                currentUserId={user?.id}
                reviewerRefreshKey={reviewerRefreshKey}
                onApprove={() => void decideApproval('approve')}
                onReject={() => void decideApproval('reject')}
                onRequestChanges={openRequestChanges}
                onResubmit={() => void resubmitApproval()}
                onMarkMerged={() => void markMerged()}
                onOpenOpportunity={() => selected.opportunityId && navigate(`/comercial/oportunidades/${selected.opportunityId}?tab=approvals`)}
                t={t}
              />
            ) : (
              <div className="flex h-full items-center justify-center px-8 text-center">
                <div className="max-w-xs text-muted-foreground">
                  <ShieldCheck className="mx-auto mb-2 h-8 w-8 opacity-40" />
                  <p className="text-sm font-medium">{t('commercialApprovals.empty.noneOpenTitle')}</p>
                  <p className="mt-1 text-xs">{t('commercialApprovals.empty.noneOpenHint')}</p>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Mobile: detalhe da aprovacao abre como bottom-sheet ao tocar na linha */}
      <Sheet open={isMobile && mobileDetailOpen && !!selected} onOpenChange={setMobileDetailOpen}>
        <SheetContent side="right" className="w-full p-0 sm:max-w-none">
          <SheetHeader className="sr-only">
            <SheetTitle>Detalhe da aprovação</SheetTitle>
          </SheetHeader>
          {selected && (
            <ApprovalDetail
              approval={selected}
              actionLoading={actionLoading}
              currentUserName={user?.name || t('approvals.user.fallback')}
              currentUserId={user?.id}
              reviewerRefreshKey={reviewerRefreshKey}
              onApprove={() => void decideApproval('approve')}
              onReject={() => void decideApproval('reject')}
              onRequestChanges={openRequestChanges}
              onResubmit={() => void resubmitApproval()}
              onMarkMerged={() => void markMerged()}
              onOpenOpportunity={() => selected.opportunityId && navigate(`/comercial/oportunidades/${selected.opportunityId}?tab=approvals`)}
              t={t}
            />
          )}
        </SheetContent>
      </Sheet>

      <Modal open={requestChangesOpen} onOpenChange={setRequestChangesOpen}>
        <ModalContent>
          <ModalHeader>
            <ModalTitle>{t('commercialApprovals.requestChanges.title')}</ModalTitle>
          </ModalHeader>
          <div className="space-y-2 px-1 py-2">
            <label className="text-sm font-medium text-foreground">{t('commercialApprovals.requestChanges.label')}</label>
            <textarea
              value={requestChangesNotes}
              onChange={(e) => setRequestChangesNotes(e.target.value)}
              rows={4}
              placeholder={t('commercialApprovals.requestChanges.placeholder')}
              className="w-full resize-y rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            />
          </div>
          <ModalFooter>
            <Button variant="outline" onClick={() => setRequestChangesOpen(false)}>{t('commercialApprovals.action.cancel')}</Button>
            <Button variant="outline-warning" disabled={actionLoading} onClick={() => void confirmRequestChanges()}>
              {actionLoading ? t('commercialApprovals.action.sending') : t('commercialApprovals.requestChanges.title')}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
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
          <h2 className="text-base font-semibold text-foreground">{t('commercialApprovals.inbox.title')}</h2>
          <span className="text-xs text-muted-foreground">{t('commercialApprovals.inbox.total').replace('{0}', String(counts.all))}</span>
        </div>
        <div className="-mx-1 flex items-center gap-1 overflow-x-auto px-1 [scrollbar-width:thin]">
          <InboxTab label={t('commercialApprovals.filter.pending')} count={counts.pending} active={filter === 'pending'} tone="amber" onClick={() => onFilterChange('pending')} />
          <InboxTab label={t('commercialApprovals.filter.approved')} count={counts.approved} active={filter === 'approved'} tone="emerald" onClick={() => onFilterChange('approved')} />
          <InboxTab label={t('commercialApprovals.filter.rejected')} count={counts.rejected} active={filter === 'rejected'} tone="rose" onClick={() => onFilterChange('rejected')} />
          <InboxTab label={t('commercialApprovals.filter.all')} count={counts.all} active={filter === 'all'} onClick={() => onFilterChange('all')} />
        </div>
        <div className="relative">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
          <input
            value={search}
            onChange={(e) => onSearchChange(e.target.value)}
            placeholder={t('commercialApprovals.inbox.searchPlaceholder')}
            className="w-full rounded-md border border-input bg-background py-1.5 pl-8 pr-3 text-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          />
        </div>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto">
        {loading && items.length === 0 ? (
          <p className="px-4 py-6 text-center text-xs text-muted-foreground">{t('commercialApprovals.common.loading')}</p>
        ) : items.length === 0 ? (
          <p className="px-4 py-8 text-center text-xs text-muted-foreground">{t('commercialApprovals.inbox.emptyFilter')}</p>
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
        'inline-flex shrink-0 items-center gap-1.5 whitespace-nowrap rounded-full px-3 py-1.5 text-[12px] font-semibold transition-colors',
        active ? 'bg-primary/12 text-primary' : 'text-muted-foreground hover:bg-muted/60 hover:text-foreground',
      ].join(' ')}
    >
      {tone && <span className={`h-1.5 w-1.5 rounded-full ${dot}`} />}
      {label}
      <span className={`rounded-full px-1.5 py-0.5 text-[10px] font-bold ${active ? 'bg-primary/20 text-primary' : 'bg-muted text-muted-foreground'}`}>{count}</span>
    </button>
  )
}

function InboxRow({ approval, selected, onClick, t }: { approval: OpportunityApprovalRequest; selected: boolean; onClick: () => void; t: (key: string) => string }) {
  const isOpen = isPendingDecision(approval.status)
  const hours = hoursSince(approval.requestedAt)
  const ageTone = hours >= 24 ? 'text-rose-700' : hours >= 2 ? 'text-amber-700' : 'text-muted-foreground'
  const statusBadge = statusBadgeConfig(approval.status, t)

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
            <Clock className="h-2.5 w-2.5" /> {ageLabel(hours, t)}
          </span>
        )}
      </div>
      <p className="line-clamp-2 text-[13px] font-semibold leading-tight text-foreground">
        {approvalTypeKeys[approval.approvalType] ? t(approvalTypeKeys[approval.approvalType]) : t('commercialApprovals.request.fallbackTitle')}
        {approval.opportunityName && (
          <span className="font-normal text-muted-foreground"> · {approval.opportunityName}</span>
        )}
      </p>
      <p className="line-clamp-1 text-[11px] text-muted-foreground">
        {approval.proposalName || t('commercialApprovals.request.noProposal')} · {t('commercialApprovals.request.byUser').replace('{0}', approval.requestedByUserName)}
      </p>
      <div className="flex items-center justify-between pt-0.5">
        <div className="flex items-center gap-1.5">
          <span className={`inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wider ${statusBadge.bg} ${statusBadge.text}`}>{statusBadge.label}</span>
          {isPolicyDeviation(approval.approvalType) && (
            <span className="inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wider bg-rose-100 text-rose-700 dark:bg-rose-500/15 dark:text-rose-300">{t('commercialApprovals.badge.outsidePolicy')}</span>
          )}
        </div>
        <ChevronRight className="h-3 w-3 text-muted-foreground/40 group-hover:text-foreground" />
      </div>
    </button>
  )
}

interface DetailProps {
  approval: OpportunityApprovalRequest
  actionLoading: boolean
  currentUserName: string
  currentUserId?: number
  reviewerRefreshKey: number
  onApprove: () => void
  onReject: () => void
  onRequestChanges: () => void
  onResubmit: () => void
  onMarkMerged: () => void
  onOpenOpportunity: () => void
  t: (key: string) => string
}

function ApprovalDetail({ approval, actionLoading, currentUserName, currentUserId, reviewerRefreshKey, onApprove, onReject, onRequestChanges, onResubmit, onMarkMerged, onOpenOpportunity, t }: DetailProps) {
  const [detailReviewers, setDetailReviewers] = useState<OpportunityApprovalReviewer[]>([])

  useEffect(() => {
    let active = true
    void opportunityService.getApprovalReviewers(approval.id)
      .then((data) => { if (active) setDetailReviewers(data) })
      .catch(() => { if (active) setDetailReviewers([]) })
    return () => { active = false }
  }, [approval.id, approval.status, approval.updatedAt, reviewerRefreshKey])

  const isApproved = approval.status === OpportunityApprovalStatus.Approved
  const isRejected = approval.status === OpportunityApprovalStatus.Rejected
  const isMerged = approval.status === OpportunityApprovalStatus.Merged
  const isChangesRequested = approval.status === OpportunityApprovalStatus.ChangesRequested
  const isOpenPending = approval.status === OpportunityApprovalStatus.Pending || approval.status === OpportunityApprovalStatus.InReview
  const isMyPendingReview = detailReviewers.some((item) => item.status === OpportunityApprovalReviewerStatus.Pending && ((currentUserId != null && item.userId === currentUserId) || (item.userId == null && item.userName === currentUserName)))
  const canDecide = isOpenPending && isMyPendingReview
  const pendingReviewerNames = detailReviewers.filter((item) => item.required && item.status === OpportunityApprovalReviewerStatus.Pending).map((item) => item.userName)
  const hours = hoursSince(approval.requestedAt)
  const typeLabel = approvalTypeKeys[approval.approvalType] ? t(approvalTypeKeys[approval.approvalType]) : t('commercialApprovals.request.fallbackTitle')
  const statusInfo = statusBadgeConfig(approval.status, t)
  const statusIcon = isApproved || isMerged ? <CheckCircle2 className="h-3 w-3" /> : isRejected ? <XCircle className="h-3 w-3" /> : <Clock className="h-3 w-3" />
  const isOpen = isPendingDecision(approval.status)

  return (
    <div className="px-7 py-6">
      {/* PR-style header */}
      <div className="mb-2 flex items-center gap-2 text-xs text-muted-foreground">
        <span className="font-mono font-semibold">#{approval.id}</span>
        <span>·</span>
        <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wider text-foreground">{typeLabel}</span>
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
            <Zap className="h-3 w-3" /> {t('commercialApprovals.urgent.label')} · {t('commercialApprovals.urgent.daysAgo').replace('{0}', String(Math.round(hours / 24)))}
          </span>
        )}
        <span className="flex items-center gap-2">
          <Avatar name={approval.requestedByUserName} size={22} />
          <span><strong className="text-foreground">{approval.requestedByUserName}</strong> {t('commercialApprovals.detail.openedAt').replace('{0}', formatDate(approval.requestedAt))}</span>
        </span>
      </div>

      {/* Grid principal + sidebar */}
      <div className="mt-6 grid gap-5 lg:grid-cols-[1fr_280px]">
        {/* Main column */}
        <div className="space-y-4 min-w-0">
          <DiffPanel approvalId={approval.id} editable={isOpen} />

          <Panel title={t('commercialApprovals.panel.justification')} accent="primary">
            <div className="flex gap-3">
              <Avatar name={approval.requestedByUserName} size={32} />
              <div className="min-w-0 flex-1">
                <div className="text-xs text-muted-foreground">
                  <strong className="text-foreground">{approval.requestedByUserName}</strong> · {t('commercialApprovals.role.requester')}
                </div>
                <div className="mt-1.5 whitespace-pre-wrap rounded-md border-l-[3px] border-primary bg-muted/30 px-3.5 py-2.5 text-sm leading-relaxed text-foreground">
                  {approval.reason || t('commercialApprovals.justification.empty')}
                </div>
              </div>
            </div>
          </Panel>

          <ConversationPanel approvalId={approval.id} currentUserName={currentUserName} />

          {(approval.decisionNotes && (isApproved || isRejected)) && (
            <Panel title={t('commercialApprovals.panel.decision')} accent={isApproved ? 'emerald' : 'rose'}>
              <div className="flex gap-2.5">
                <Avatar name={approval.approvedByUserName || t('commercialApprovals.role.approver')} size={32} tone={isApproved ? 'emerald' : 'rose'} />
                <div className="min-w-0 flex-1">
                  <div className="text-xs text-muted-foreground">
                    <strong className={isApproved ? 'text-emerald-700' : 'text-rose-700'}>{approval.approvedByUserName || t('commercialApprovals.role.approver')}</strong>
                    {approval.decidedAt && <span> · {formatDate(approval.decidedAt)}</span>}
                  </div>
                  <p className={`mt-1.5 whitespace-pre-wrap rounded-md border-l-2 ${isApproved ? 'border-emerald-500' : 'border-rose-500'} bg-muted/30 px-3 py-2 text-sm italic text-foreground`}>
                    "{approval.decisionNotes}"
                  </p>
                </div>
              </div>
            </Panel>
          )}
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
            isOpenPending={isOpenPending}
            pendingReviewerNames={pendingReviewerNames}
            isChangesRequested={isChangesRequested}
            isApproved={isApproved}
            isRejected={isRejected}
            isMerged={isMerged}
          />

          <ReviewersPanel approvalId={approval.id} requesterName={approval.requestedByUserName} currentUserName={currentUserName} currentUserId={currentUserId} refreshKey={reviewerRefreshKey} />

          <SidebarBlock title={t('commercialApprovals.linked.title')} icon={<ShieldCheck className="h-3.5 w-3.5" />}>
            <LinkRow icon={<FileText className="h-3 w-3" />} label={t('commercialApprovals.linked.proposal')} value={approval.proposalName || t('commercialApprovals.linked.noTitle')} tone="purple" />
            <LinkRow icon={<ArrowUpRight className="h-3 w-3" />} label={t('commercialApprovals.linked.opportunity')} value={approval.opportunityName || `#${approval.proposalId}`} tone="primary" onClick={onOpenOpportunity} />
            {approval.brandName && (
              <LinkRow label={t('commercialApprovals.linked.brand')} value={approval.brandName} brandLogoUrl={approval.brandLogoUrl} />
            )}
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
  isOpenPending: boolean
  pendingReviewerNames: string[]
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

function ActionPanel({ approval, actionLoading, canDecide, isOpenPending, pendingReviewerNames, isChangesRequested, isApproved, isRejected, isMerged, onApprove, onReject, onRequestChanges, onResubmit, onMarkMerged }: ActionPanelProps) {
  const { t } = useI18n()
  if (canDecide) {
    return (
      <div className="rounded-xl border-2 border-primary bg-card p-4 shadow-[0_4px_14px_rgba(11,165,164,0.08)]">
        <div className="text-[10.5px] font-bold uppercase tracking-[0.14em] text-muted-foreground">{t('commercialApprovals.action.yourDecision')}</div>
        <p className="mt-1.5 text-xs leading-relaxed text-foreground">
          {t('commercialApprovals.action.yourApprovalNeeded')}
        </p>
        <div className="mt-3 flex flex-col gap-2">
          <Button size="sm" variant="success" fullWidth disabled={actionLoading} onClick={onApprove} icon={<ThumbsUp />}>
            {t('commercialApprovals.action.approve')}
          </Button>
          <Button size="sm" variant="outline-warning" fullWidth disabled={actionLoading} onClick={onRequestChanges} icon={<MessageSquare />}>
            {t('commercialApprovals.requestChanges.title')}
          </Button>
          <Button size="sm" variant="outline-danger" fullWidth disabled={actionLoading} onClick={onReject} icon={<ThumbsDown />}>
            {t('commercialApprovals.action.reject')}
          </Button>
        </div>
        <div className="mt-3 flex items-start gap-2 rounded-md border border-amber-200 bg-amber-50 px-2.5 py-1.5 text-[11px] leading-snug text-amber-800">
          <Eye className="mt-0.5 h-3 w-3 shrink-0" />
          {t('commercialApprovals.action.decisionRecordedNote')}
        </div>
      </div>
    )
  }

  if (isOpenPending) {
    return (
      <div className="rounded-xl border border-border bg-card p-4">
        <div className="text-[10.5px] font-bold uppercase tracking-[0.14em] text-muted-foreground">{t('commercialApprovals.waiting.title')}</div>
        <p className="mt-2 text-xs leading-relaxed text-muted-foreground">
          {pendingReviewerNames.length > 0
            ? <>{t('commercialApprovals.waiting.decisionFrom')} <strong className="text-foreground">{pendingReviewerNames.join(', ')}</strong>.</>
            : t('commercialApprovals.waiting.noRequiredReviewer')}
        </p>
      </div>
    )
  }

  if (isChangesRequested) {
    return (
      <div className="rounded-xl border-2 border-amber-300 bg-amber-50 p-4">
        <div className="text-[10.5px] font-bold uppercase tracking-[0.14em] text-amber-800">{t('commercialApprovals.status.changesRequested')}</div>
        <p className="mt-1.5 text-xs leading-relaxed text-amber-800">
          {approval.approvedByUserName ? <><strong>{approval.approvedByUserName}</strong> {t('commercialApprovals.changes.requestedSuffix')}</> : t('commercialApprovals.changes.awaitingRequester')}
          {approval.decidedAt && <span> {t('commercialApprovals.detail.onDate').replace('{0}', formatDate(approval.decidedAt))}</span>}.
        </p>
        {approval.decisionNotes && (
          <p className="mt-2 rounded-md border-l-2 border-amber-500 bg-card/50 px-2.5 py-1.5 text-xs italic text-amber-900">
            "{approval.decisionNotes}"
          </p>
        )}
        <Button size="sm" variant="primary" fullWidth disabled={actionLoading} onClick={onResubmit} icon={<ThumbsUp />} className="mt-3">
          {t('commercialApprovals.action.resubmit')}
        </Button>
      </div>
    )
  }

  if (isApproved) {
    return (
      <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4">
        <div className="text-[10.5px] font-bold uppercase tracking-[0.14em] text-emerald-800">{t('commercialApprovals.status.approved')}</div>
        <p className="mt-2 text-xs text-emerald-900">
          {t('commercialApprovals.detail.byPrefix')} <strong>{approval.approvedByUserName}</strong>
          {approval.decidedAt && <span> {t('commercialApprovals.detail.onDate').replace('{0}', formatDate(approval.decidedAt))}</span>}.
        </p>
        <Button size="sm" variant="outline-success" fullWidth disabled={actionLoading} onClick={onMarkMerged} icon={<CheckCircle2 />} className="mt-3">
          {t('commercialApprovals.action.markApplied')}
        </Button>
        <p className="mt-2 text-[10.5px] text-emerald-700">{t('commercialApprovals.action.markAppliedHint')}</p>
      </div>
    )
  }

  if (isMerged) {
    return (
      <div className="rounded-xl border border-purple-200 bg-purple-50 p-4">
        <div className="text-[10.5px] font-bold uppercase tracking-[0.14em] text-purple-800">{t('commercialApprovals.status.merged')}</div>
        <p className="mt-2 text-xs text-purple-900">
          {t('commercialApprovals.merged.approvedByPrefix')} <strong>{approval.approvedByUserName}</strong>
          {approval.decidedAt && <span> {t('commercialApprovals.detail.onDate').replace('{0}', formatDate(approval.decidedAt))}</span>} {t('commercialApprovals.merged.appliedSuffix')}
        </p>
      </div>
    )
  }

  return (
    <div className="rounded-xl border border-border bg-card p-4">
      <div className="text-[10.5px] font-bold uppercase tracking-[0.14em] text-muted-foreground">{t('commercialApprovals.finalDecision.title')}</div>
      <p className="mt-2 text-xs text-foreground">
        {isRejected ? <strong className="text-rose-700">{t('commercialApprovals.status.rejected')}</strong> : <strong>{t('commercialApprovals.finalDecision.closed')}</strong>}
        {approval.approvedByUserName && <span> {t('commercialApprovals.detail.byPrefix')} <strong className="text-foreground">{approval.approvedByUserName}</strong></span>}
        {approval.decidedAt && <span> {t('commercialApprovals.detail.onDate').replace('{0}', formatDate(approval.decidedAt))}</span>}
      </p>
    </div>
  )
}

function Panel({ title, accent, children }: { title: string; accent?: 'primary' | 'emerald' | 'rose' | 'blue' | 'amber'; children: React.ReactNode }) {
  const accentClass = accent === 'emerald' ? 'bg-emerald-500' : accent === 'rose' ? 'bg-rose-500' : accent === 'blue' ? 'bg-blue-500' : accent === 'amber' ? 'bg-amber-500' : accent === 'primary' ? 'bg-primary' : 'bg-muted-foreground'
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

function ReviewerRowFromEntity({ reviewer, canRemove, onRemove }: { reviewer: OpportunityApprovalReviewer; canRemove: boolean; onRemove: () => void }) {
  const { t } = useI18n()
  const statusMap: Record<number, 'pendente' | 'aprovou' | 'rejeitou' | 'comentou'> = {
    1: 'pendente',
    2: 'aprovou',
    3: 'rejeitou',
    4: 'comentou',
  }
  return (
    <div className="group/reviewer">
      <ReviewerRow
        name={reviewer.userName}
        role={reviewer.role ?? t('commercialApprovals.role.approver')}
        status={statusMap[reviewer.status] ?? 'pendente'}
        decidedAt={reviewer.decidedAt ? formatDate(reviewer.decidedAt) : undefined}
        required={reviewer.required}
      />
      {canRemove && (
        <button
          type="button"
          onClick={onRemove}
          className="mt-0.5 pl-10 text-[10px] font-semibold text-muted-foreground opacity-0 transition-opacity hover:text-destructive group-hover/reviewer:opacity-100"
        >
          {t('commercialApprovals.action.remove')}
        </button>
      )}
    </div>
  )
}

function ReviewerRow({ name, role, status, decidedAt, required }: { name: string; role: string; status: 'pendente' | 'aprovou' | 'rejeitou' | 'comentou'; decidedAt?: string; required?: boolean }) {
  const { t } = useI18n()
  const config = {
    pendente: { color: 'text-amber-700', label: t('commercialApprovals.reviewerStatus.waiting') },
    aprovou: { color: 'text-emerald-700', label: t('commercialApprovals.reviewerStatus.approved') },
    rejeitou: { color: 'text-rose-700', label: t('commercialApprovals.reviewerStatus.rejected') },
    comentou: { color: 'text-blue-700', label: t('commercialApprovals.reviewerStatus.commented') },
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
          {required && <span className="rounded bg-rose-100 px-1 py-0.5 text-[9px] font-bold uppercase tracking-wider text-rose-700">{t('commercialApprovals.reviewer.requiredShort')}</span>}
        </div>
        <div className="text-[11px] text-muted-foreground">
          {role} · <span className={`font-semibold ${config.color}`}>{config.label}</span>
          {decidedAt && <span> {t('commercialApprovals.detail.onDate').replace('{0}', decidedAt)}</span>}
        </div>
      </div>
    </div>
  )
}

function LinkRow({ icon, label, value, tone, onClick, brandLogoUrl }: { icon?: React.ReactNode; label: string; value: string; tone?: 'primary' | 'purple'; onClick?: () => void; brandLogoUrl?: string }) {
  const toneBg = tone === 'purple' ? 'bg-purple-100 text-purple-700' : 'bg-primary/10 text-primary'
  const Wrap = onClick ? 'button' : 'div'
  const initials = value.split(' ').slice(0, 2).map((part) => part[0]).join('').toUpperCase()
  return (
    <Wrap
      type={onClick ? 'button' : undefined}
      onClick={onClick}
      className={`flex w-full items-center gap-2 rounded-md border border-border/60 bg-muted/20 px-2 py-1.5 text-left transition-colors ${onClick ? 'cursor-pointer hover:border-primary/40 hover:bg-muted/40' : ''}`}
    >
      {brandLogoUrl ? (
        <span className="flex h-6 w-6 shrink-0 items-center justify-center overflow-hidden rounded border border-border bg-white">
          <img src={resolveAssetUrl(brandLogoUrl)} alt={value} className="h-full w-full object-contain" onError={(e) => { const target = e.currentTarget as HTMLImageElement; target.style.display = 'none'; (target.nextElementSibling as HTMLElement | null)?.style.setProperty('display', 'inline-flex') }} />
          <span className="hidden h-6 w-6 items-center justify-center rounded bg-muted text-[10px] font-bold text-muted-foreground">{initials}</span>
        </span>
      ) : icon ? (
        <span className={`flex h-6 w-6 shrink-0 items-center justify-center rounded ${toneBg}`}>{icon}</span>
      ) : (
        <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded bg-muted text-[10px] font-bold text-muted-foreground">{initials}</span>
      )}
      <div className="min-w-0 flex-1">
        <p className="text-[9.5px] font-bold uppercase tracking-wider text-muted-foreground">{label}</p>
        <p className="truncate text-[12px] font-semibold text-foreground">{value}</p>
      </div>
      {onClick && <ExternalLink className="h-3 w-3 shrink-0 text-muted-foreground" />}
    </Wrap>
  )
}

function DiffPanel({ approvalId, editable }: { approvalId: number; editable: boolean }) {
  const { t } = useI18n()
  const [diffs, setDiffs] = useState<OpportunityApprovalDiff[]>([])
  const [loading, setLoading] = useState(true)
  const [autoLoading, setAutoLoading] = useState(false)
  const [adding, setAdding] = useState(false)
  const [draftField, setDraftField] = useState('')
  const [draftPolicy, setDraftPolicy] = useState('')
  const [draftRequested, setDraftRequested] = useState('')
  const [draftDelta, setDraftDelta] = useState('')
  const [draftKind, setDraftKind] = useState<1 | 2 | 3>(1)
  const [posting, setPosting] = useState(false)
  const [removeTargetId, setRemoveTargetId] = useState<number | null>(null)
  const [removing, setRemoving] = useState(false)

  const load = async () => {
    setLoading(true)
    try {
      const data = await opportunityService.getApprovalDiffs(approvalId)
      setDiffs(data)
    } catch {
      setDiffs([])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
    setAdding(false)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [approvalId])

  const reset = () => {
    setDraftField('')
    setDraftPolicy('')
    setDraftRequested('')
    setDraftDelta('')
    setDraftKind(1)
  }

  const submit = async () => {
    const field = draftField.trim()
    if (!field || posting) return
    setPosting(true)
    try {
      await opportunityService.addApprovalDiff(approvalId, {
        field,
        policyValue: draftPolicy.trim() || undefined,
        requestedValue: draftRequested.trim() || undefined,
        delta: draftDelta.trim() || undefined,
        kind: draftKind,
        displayOrder: diffs.length,
      })
      reset()
      setAdding(false)
      await load()
    } finally {
      setPosting(false)
    }
  }

  const confirmRemove = async () => {
    if (removeTargetId == null || removing) return
    setRemoving(true)
    try {
      await opportunityService.removeApprovalDiff(removeTargetId)
      setRemoveTargetId(null)
      await load()
    } finally {
      setRemoving(false)
    }
  }

  const handleAutoPopulate = async () => {
    if (autoLoading) return
    setAutoLoading(true)
    try {
      await opportunityService.populateApprovalFromPolicy(approvalId)
      await load()
    } finally {
      setAutoLoading(false)
    }
  }

  const violations = diffs.filter((d) => d.isAutoGenerated && d.kind !== 1).length

  if (loading) {
    return <Panel title={t('commercialApprovals.diff.title')} accent="primary"><p className="text-xs text-muted-foreground">{t('commercialApprovals.common.loading')}</p></Panel>
  }

  if (diffs.length === 0 && !editable) {
    return null
  }

  return (
    <>
    <Panel
      title={t('commercialApprovals.diff.title')}
      accent="primary"
    >
      {diffs.length > 0 ? (
        <>
          <div className="mb-2 flex items-center gap-3 text-[11px] font-semibold">
            {violations > 0 ? (
              <span className="text-rose-700">{violations === 1 ? t('commercialApprovals.diff.violations.one').replace('{0}', String(violations)) : t('commercialApprovals.diff.violations.many').replace('{0}', String(violations))}</span>
            ) : (
              <span className="text-emerald-700">{t('commercialApprovals.diff.withinPolicy')}</span>
            )}
            <span className="text-muted-foreground">{diffs.length === 1 ? t('commercialApprovals.diff.terms.one').replace('{0}', String(diffs.length)) : t('commercialApprovals.diff.terms.many').replace('{0}', String(diffs.length))}</span>
          </div>
          <div className="overflow-hidden rounded-md border border-border">
            <div className="grid grid-cols-[1fr_1fr_1fr_90px_auto] gap-2 border-b border-border bg-muted/30 px-3 py-1.5 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
              <div>{t('commercialApprovals.diff.column.field')}</div>
              <div>{t('commercialApprovals.diff.column.policy')}</div>
              <div>{t('commercialApprovals.diff.column.negotiated')}</div>
              <div className="text-right">{t('commercialApprovals.diff.column.status')}</div>
              <div />
            </div>
            {diffs.map((d) => {
              const isViolation = d.isAutoGenerated && d.kind !== 1
              const isWithin = d.isAutoGenerated && d.kind === 1
              return (
                <div key={d.id} className="grid grid-cols-[1fr_1fr_1fr_90px_auto] items-center gap-2 border-b border-border/60 px-3 py-2 text-sm last:border-b-0">
                  <div className="flex items-center gap-1.5 truncate font-semibold text-foreground">
                    {d.field}
                    {d.isAutoGenerated && (
                      <span title={t('commercialApprovals.diff.autoDetectedTooltip')} className="rounded bg-primary/10 px-1.5 py-0.5 text-[9px] font-bold uppercase tracking-wider text-primary">{t('commercialApprovals.diff.autoBadge')}</span>
                    )}
                  </div>
                  <div className="rounded bg-muted/50 px-2 py-1 font-mono text-xs text-muted-foreground">{d.policyValue || '—'}</div>
                  <div className={`rounded px-2 py-1 font-mono text-xs font-semibold ${isViolation ? 'bg-rose-50 text-rose-700' : isWithin ? 'bg-emerald-50 text-emerald-700' : 'bg-muted/50 text-foreground'}`}>{d.requestedValue || '—'}</div>
                  <div className={`text-right font-mono text-xs font-bold ${isViolation ? 'text-rose-700' : isWithin ? 'text-emerald-700' : 'text-foreground'}`}>{isWithin ? t('commercialApprovals.diff.within') : (d.delta || '—')}</div>
                  {editable && !d.isAutoGenerated ? (
                    <button type="button" onClick={() => setRemoveTargetId(d.id)} className="text-[10px] font-semibold text-muted-foreground hover:text-destructive">×</button>
                  ) : <div />}
                </div>
              )
            })}
          </div>
        </>
      ) : (
        <div className="space-y-2">
          <p className="text-xs text-muted-foreground">{t('commercialApprovals.diff.emptyChanges')}</p>
          {editable && (
            <div className="rounded-md border border-dashed border-border bg-muted/20 p-3 text-[11.5px] text-muted-foreground">
              <p className="mb-2">{t('commercialApprovals.diff.autoRequirements')}</p>
              <ul className="ml-4 list-disc space-y-0.5">
                <li>{t('commercialApprovals.diff.requirementPolicyPrefix')} <strong>{t('commercialApprovals.diff.requirementPolicySettings')}</strong></li>
                <li>{t('commercialApprovals.diff.requirementNegotiationPrefix')} <strong>{t('commercialApprovals.diff.requirementNegotiationFields')}</strong> {t('commercialApprovals.diff.requirementNegotiationSuffix')}</li>
              </ul>
              <Button size="sm" variant="outline" className="mt-2.5" disabled={autoLoading} onClick={() => void handleAutoPopulate()}>
                {autoLoading ? t('commercialApprovals.diff.detecting') : t('commercialApprovals.diff.detectNow')}
              </Button>
            </div>
          )}
        </div>
      )}

      {editable && (
        <div className="mt-3">
          {adding ? (
            <div className="space-y-1.5 rounded-md border border-dashed border-border bg-muted/20 p-2">
              <div className="grid grid-cols-2 gap-1.5">
                <input value={draftField} onChange={(e) => setDraftField(e.target.value)} placeholder={t('commercialApprovals.diff.fieldPlaceholder')} className="rounded-md border border-input bg-background px-2 py-1 text-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring" />
                <select value={draftKind} onChange={(e) => setDraftKind(Number(e.target.value) as 1 | 2 | 3)} className="rounded-md border border-input bg-background px-2 py-1 text-xs">
                  <option value={1}>{t('commercialApprovals.diff.kind.changed')}</option>
                  <option value={2}>{t('commercialApprovals.diff.kind.added')}</option>
                  <option value={3}>{t('commercialApprovals.diff.kind.removed')}</option>
                </select>
              </div>
              <div className="grid grid-cols-2 gap-1.5">
                <input value={draftPolicy} onChange={(e) => setDraftPolicy(e.target.value)} placeholder={t('commercialApprovals.diff.policyPlaceholder')} className="rounded-md border border-input bg-background px-2 py-1 text-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring" />
                <input value={draftRequested} onChange={(e) => setDraftRequested(e.target.value)} placeholder={t('commercialApprovals.diff.requestedPlaceholder')} className="rounded-md border border-input bg-background px-2 py-1 text-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring" />
              </div>
              <input value={draftDelta} onChange={(e) => setDraftDelta(e.target.value)} placeholder={t('commercialApprovals.diff.deltaPlaceholder')} className="w-full rounded-md border border-input bg-background px-2 py-1 text-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring" />
              <div className="flex gap-1.5">
                <Button size="sm" variant="primary" fullWidth disabled={!draftField.trim() || posting} onClick={() => void submit()}>
                  {posting ? t('commercialApprovals.action.adding') : t('commercialApprovals.action.add')}
                </Button>
                <Button size="sm" variant="outline" onClick={() => { setAdding(false); reset() }}>{t('commercialApprovals.action.cancel')}</Button>
              </div>
            </div>
          ) : (
            <Button size="sm" variant="outline" onClick={() => setAdding(true)} icon={<Plus />}>{t('commercialApprovals.diff.addChange')}</Button>
          )}
        </div>
      )}
    </Panel>
    <ConfirmModal
      open={removeTargetId !== null}
      onOpenChange={(open) => { if (!open) setRemoveTargetId(null) }}
      description={t('commercialApprovals.diff.removeConfirm')}
      variant="danger"
      onConfirm={() => void confirmRemove()}
      loading={removing}
    />
    </>
  )
}

interface ReviewersPanelProps {
  approvalId: number
  requesterName: string
  currentUserName: string
  currentUserId?: number
  refreshKey: number
}

function ReviewersPanel({ approvalId, requesterName, currentUserName, currentUserId, refreshKey }: ReviewersPanelProps) {
  const { t } = useI18n()
  const [reviewers, setReviewers] = useState<OpportunityApprovalReviewer[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const [adding, setAdding] = useState(false)
  const [users, setUsers] = useState<{ userId: number; name: string }[]>([])
  const [draftUserId, setDraftUserId] = useState('')
  const [draftRole, setDraftRole] = useState('')
  const [draftRequired, setDraftRequired] = useState(true)
  const [posting, setPosting] = useState(false)
  const [removeTargetId, setRemoveTargetId] = useState<number | null>(null)
  const [removing, setRemoving] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(false)
    try {
      const data = await opportunityService.getApprovalReviewers(approvalId)
      setReviewers(data)
    } catch {
      setReviewers([])
      setError(true)
    } finally {
      setLoading(false)
    }
  }, [approvalId])

  useEffect(() => {
    void load()
  }, [load, refreshKey])

  useEffect(() => {
    setAdding(false)
    setDraftUserId('')
    setDraftRole('')
    setDraftRequired(true)
  }, [approvalId])

  useEffect(() => {
    let active = true
    void commercialResponsibleService.getAll()
      .then((list) => { if (active) setUsers(list.map((item) => ({ userId: item.userId, name: item.name }))) })
      .catch(() => undefined)
    return () => { active = false }
  }, [])

  const submit = async () => {
    const selectedUser = users.find((item) => String(item.userId) === draftUserId)
    if (!selectedUser || posting) return
    setPosting(true)
    try {
      await opportunityService.addApprovalReviewer(approvalId, { userName: selectedUser.name, userId: selectedUser.userId, role: draftRole.trim() || undefined, required: draftRequired })
      setAdding(false)
      setDraftUserId('')
      setDraftRole('')
      setDraftRequired(true)
      await load()
    } finally {
      setPosting(false)
    }
  }

  const confirmRemove = async () => {
    if (removeTargetId == null || removing) return
    setRemoving(true)
    try {
      await opportunityService.removeApprovalReviewer(removeTargetId)
      setRemoveTargetId(null)
      await load()
    } finally {
      setRemoving(false)
    }
  }

  const requiredReviewers = reviewers.filter((item) => item.required)
  const requiredTotal = requiredReviewers.length
  const approvedRequiredCount = requiredReviewers.filter((item) => item.status === OpportunityApprovalReviewerStatus.Approved).length
  const allRequiredApproved = requiredTotal > 0 && approvedRequiredCount === requiredTotal

  return (
    <>
    <SidebarBlock title={t('commercialApprovals.reviewers.title')} icon={<Users className="h-3.5 w-3.5" />} action={(
      <button type="button" title={adding ? t('commercialApprovals.action.cancel') : t('commercialApprovals.action.add')} onClick={() => setAdding((v) => !v)} className="flex h-5 w-5 items-center justify-center rounded border border-border text-muted-foreground hover:bg-muted">
        <Plus className={`h-3 w-3 transition-transform ${adding ? 'rotate-45' : ''}`} />
      </button>
    )}>
      <div className="space-y-2">
        {!loading && !error && requiredTotal > 0 && (
          <div className="space-y-1 rounded-md bg-muted/30 px-2.5 py-2">
            <div className="flex items-center justify-between text-[11px] font-semibold">
              <span className={allRequiredApproved ? 'text-emerald-700' : 'text-foreground'}>
                {requiredTotal === 1
                  ? t('commercialApprovals.reviewers.progress.one').replace('{0}', String(approvedRequiredCount)).replace('{1}', String(requiredTotal))
                  : t('commercialApprovals.reviewers.progress.many').replace('{0}', String(approvedRequiredCount)).replace('{1}', String(requiredTotal))}
              </span>
              {allRequiredApproved && <span className="text-emerald-700">{t('commercialApprovals.reviewers.done')}</span>}
            </div>
            <div className="h-1.5 overflow-hidden rounded-full bg-border" role="progressbar" aria-label={t('commercialApprovals.reviewers.title')} aria-valuenow={approvedRequiredCount} aria-valuemin={0} aria-valuemax={requiredTotal}>
              <div className="h-full rounded-full bg-emerald-500 transition-all" style={{ width: `${Math.round((approvedRequiredCount / requiredTotal) * 100)}%` }} />
            </div>
          </div>
        )}
        <ReviewerRow name={requesterName} role={t('commercialApprovals.role.requester')} status="comentou" />
        {loading ? (
          <p className="text-[11px] text-muted-foreground">{t('commercialApprovals.common.loading')}</p>
        ) : error ? (
          <button type="button" onClick={() => void load()} className="text-[11px] font-semibold text-destructive hover:underline">{t('commercialApprovals.reviewers.loadError')}</button>
        ) : (
          reviewers.map((r) => (
            <ReviewerRowFromEntity key={r.id} reviewer={r} canRemove={(currentUserId != null && r.userId === currentUserId) || (r.userId == null && r.userName === currentUserName) || r.status === OpportunityApprovalReviewerStatus.Pending} onRemove={() => setRemoveTargetId(r.id)} />
          ))
        )}
        {reviewers.length === 0 && !loading && !error && (
          <p className="text-[11px] text-muted-foreground">{t('commercialApprovals.reviewers.empty')}</p>
        )}

        {adding && (
          <div className="space-y-1.5 rounded-md border border-dashed border-border bg-muted/20 p-2">
            <SearchableSelect
              value={draftUserId}
              onValueChange={setDraftUserId}
              options={users.filter((user) => !reviewers.some((reviewer) => reviewer.userId === user.userId)).map((item) => ({ value: String(item.userId), label: item.name }))}
              placeholder={t('commercialApprovals.reviewers.selectPlaceholder')}
            />
            <input
              value={draftRole}
              onChange={(e) => setDraftRole(e.target.value)}
              placeholder={t('commercialApprovals.reviewers.rolePlaceholder')}
              className="w-full rounded-md border border-input bg-background px-2 py-1 text-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            />
            <label className="flex items-center gap-1.5 text-[11px] text-muted-foreground">
              <input type="checkbox" checked={draftRequired} onChange={(e) => setDraftRequired(e.target.checked)} />
              {t('commercialApprovals.reviewers.requiredApproval')}
            </label>
            <Button size="sm" variant="primary" fullWidth disabled={!draftUserId || posting} onClick={() => void submit()}>
              {posting ? t('commercialApprovals.action.adding') : t('commercialApprovals.reviewers.addReviewer')}
            </Button>
          </div>
        )}
      </div>
    </SidebarBlock>
    <ConfirmModal
      open={removeTargetId !== null}
      onOpenChange={(open) => { if (!open) setRemoveTargetId(null) }}
      description={t('commercialApprovals.reviewers.removeConfirm')}
      variant="danger"
      onConfirm={() => void confirmRemove()}
      loading={removing}
    />
    </>
  )
}

interface ConversationPanelProps {
  approvalId: number
  currentUserName: string
}

function ConversationPanel({ approvalId, currentUserName }: ConversationPanelProps) {
  const { t } = useI18n()
  const [comments, setComments] = useState<OpportunityApprovalComment[]>([])
  const [draft, setDraft] = useState('')
  const [loading, setLoading] = useState(true)
  const [posting, setPosting] = useState(false)
  const [deleteTargetId, setDeleteTargetId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [users, setUsers] = useState<MentionableUser[]>([])
  const [mentionedUserIds, setMentionedUserIds] = useState<number[]>([])

  const load = async () => {
    setLoading(true)
    try {
      const data = await opportunityService.getApprovalComments(approvalId)
      setComments(data)
    } catch {
      setComments([])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [approvalId])

  useEffect(() => {
    void UsersManagementService.listInCurrentContract().then((list) => {
      setUsers(list.filter((item) => item.isActive).map((item) => ({ userId: item.userId, name: item.name, email: item.email })))
    })
  }, [])

  const submit = async () => {
    const body = draft.trim()
    if (!body || posting) return
    setPosting(true)
    try {
      await opportunityService.createApprovalComment(approvalId, { userName: currentUserName, body, role: 'observador' })
      setDraft('')
      setMentionedUserIds([])
      await load()
    } finally {
      setPosting(false)
    }
  }

  const confirmDelete = async () => {
    if (deleteTargetId == null || deleting) return
    setDeleting(true)
    try {
      await opportunityService.deleteApprovalComment(deleteTargetId)
      setDeleteTargetId(null)
      await load()
    } finally {
      setDeleting(false)
    }
  }

  return (
    <>
    <Panel title={loading ? t('commercialApprovals.conversation.titleLoading') : (comments.length === 1 ? t('commercialApprovals.conversation.title.one').replace('{0}', String(comments.length)) : t('commercialApprovals.conversation.title.many').replace('{0}', String(comments.length)))} accent="blue">
      <div className="space-y-3">
        {loading && comments.length === 0 ? (
          <p className="text-xs text-muted-foreground">{t('commercialApprovals.common.loading')}</p>
        ) : comments.length === 0 ? (
          <p className="text-xs text-muted-foreground">{t('commercialApprovals.conversation.empty')}</p>
        ) : (
          comments.map((c) => <CommentRow key={c.id} comment={c} users={users} canDelete={c.userName === currentUserName} onDelete={() => setDeleteTargetId(c.id)} />)
        )}

        <div className="flex gap-2.5 pt-2">
          <Avatar name={currentUserName} size={28} />
          <div className="min-w-0 flex-1">
            <CommentInputWithMentions
              value={draft}
              onChange={setDraft}
              onMentionsChange={setMentionedUserIds}
              mentionedUserIds={mentionedUserIds}
              users={users}
              placeholder={t('commercialApprovals.conversation.placeholder')}
              rows={2}
              disabled={posting}
            />
            <div className="mt-2 flex items-center justify-end gap-2">
              <Button size="sm" variant="primary" disabled={posting || draft.trim().length === 0} onClick={() => void submit()}>
                {posting ? t('commercialApprovals.action.sending') : t('commercialApprovals.conversation.comment')}
              </Button>
            </div>
          </div>
        </div>
      </div>
    </Panel>
    <ConfirmModal
      open={deleteTargetId !== null}
      onOpenChange={(open) => { if (!open) setDeleteTargetId(null) }}
      description={t('commercialApprovals.conversation.deleteConfirm')}
      variant="danger"
      onConfirm={() => void confirmDelete()}
      loading={deleting}
    />
    </>
  )
}

function CommentRow({ comment, users, canDelete, onDelete }: { comment: OpportunityApprovalComment; users: MentionableUser[]; canDelete: boolean; onDelete: () => void }) {
  const { t } = useI18n()
  const roleStyle: Record<string, { bg: string; text: string }> = {
    aprovador: { bg: 'bg-emerald-100', text: 'text-emerald-800' },
    solicitante: { bg: 'bg-blue-100', text: 'text-blue-800' },
    observador: { bg: 'bg-muted', text: 'text-muted-foreground' },
  }
  const style = roleStyle[comment.role] ?? roleStyle.observador

  return (
    <div className="flex gap-2.5">
      <Avatar name={comment.userName} size={28} />
      <div className="min-w-0 flex-1 overflow-hidden rounded-lg border border-border bg-card">
        <div className="flex items-center gap-2 border-b border-border/60 bg-muted/30 px-3 py-1.5 text-[11.5px]">
          <strong className="text-foreground">{comment.userName}</strong>
          <span className={`rounded px-1.5 py-0.5 text-[9.5px] font-bold uppercase tracking-wider ${style.bg} ${style.text}`}>{comment.role}</span>
          <span className="text-muted-foreground">{t('commercialApprovals.conversation.commentedAt').replace('{0}', formatDate(comment.createdAt))}</span>
          {canDelete && (
            <button type="button" onClick={onDelete} className="ml-auto text-[10px] font-semibold text-muted-foreground hover:text-destructive">
              {t('commercialApprovals.action.delete')}
            </button>
          )}
        </div>
        <div className="whitespace-pre-wrap px-3 py-2 text-sm leading-relaxed text-foreground">{renderBodyWithMentions(comment.body, users)}</div>
      </div>
    </div>
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
