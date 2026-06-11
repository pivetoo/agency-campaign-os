import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { Button, Card, CardContent, CardHeader, CardTitle, ConfirmModal, DataTable, Dropdown, DropdownTrigger, DropdownContent, DropdownItem, DropdownLabel, DropdownSeparator, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, useApi, useAuth, Badge, Tabs, TabsList, TabsTrigger, TabsContent, useI18n, useToast } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Activity, ArrowRight, Building2, Calendar, CheckCircle, CircleDollarSign, Clock, Compass, FileText, History, MoreHorizontal, Pencil, Plus, Tag, Tags, ThumbsDown, ThumbsUp, Trash2, TrendingUp, User, UserCheck, XCircle } from 'lucide-react'
import { commercialPipelineStageService } from '../../../services/commercialPipelineStageService'
import { opportunityWinReasonService, opportunityLossReasonService } from '../../../services/opportunityOutcomeReasonService'
import type { OpportunityWinReason, OpportunityLossReason } from '../../../types/opportunityOutcomeReason'
import { opportunityService, OpportunityApprovalStatus, type Opportunity, type OpportunityApprovalRequest, type OpportunityFollowUp } from '../../../services/opportunityService'
import { proposalService } from '../../../services/proposalService'
import { OpportunityApprovalReviewerStatus, type OpportunityApprovalReviewer } from '../../../types/opportunityApprovalReviewer'
import OpportunityFormModal from '../../../components/modals/OpportunityFormModal'
import OpportunityFollowUpFormModal from '../../../components/modals/OpportunityFollowUpFormModal'
import OpportunityActivityTab from './OpportunityActivityTab'
import ProposalFormModal from '../../../components/modals/ProposalFormModal'
import { resolveAssetUrl } from '../../../lib/assetUrl'
import { formatDate } from '../../../lib/format'
import { formatCurrency } from '../../../lib/format'

export default function OpportunityDetail() {
  const { t } = useI18n()
  const { toast } = useToast()
  const { id } = useParams<{ id: string }>()
  const opportunityId = Number(id || 0)
  const navigate = useNavigate()
  const { user: authUser } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const [activeTab, setActiveTab] = useState(() => searchParams.get('tab') || 'summary')

  useEffect(() => {
    const fromUrl = searchParams.get('tab')
    if (fromUrl && fromUrl !== activeTab) setActiveTab(fromUrl)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams])

  const handleTabChange = (next: string) => {
    setActiveTab(next)
    if (next === 'summary') {
      searchParams.delete('tab')
    } else {
      searchParams.set('tab', next)
    }
    setSearchParams(searchParams, { replace: true })
  }

  const [opportunity, setOpportunity] = useState<Opportunity | null>(null)
  const [stages, setStages] = useState<Array<{ id: number; name: string; finalBehavior: number; displayOrder?: number; color?: string }>>([])
  const [approvalRequests, setApprovalRequests] = useState<OpportunityApprovalRequest[]>([])
  const [, setSelectedApprovalRequest] = useState<OpportunityApprovalRequest | null>(null)
  const [selectedFollowUp, setSelectedFollowUp] = useState<OpportunityFollowUp | null>(null)
  const [isOpportunityFormOpen, setIsOpportunityFormOpen] = useState(false)
  const [isFollowUpFormOpen, setIsFollowUpFormOpen] = useState(false)
  const [confirmDeleteFollowUpOpen, setConfirmDeleteFollowUpOpen] = useState(false)
  const [isProposalFormOpen, setIsProposalFormOpen] = useState(false)
  const [, setSelectedStage] = useState<string>('1')
  const [pendingFinalStage, setPendingFinalStage] = useState<{ id: number; name: string; kind: 'won' | 'lost' } | null>(null)
  const [pendingReopen, setPendingReopen] = useState<{ stageId: number; correction: { kind: 'won' | 'lost'; stageId: number; name: string } | null } | null>(null)
  const [finalNotes, setFinalNotes] = useState('')
  const [finalReasonId, setFinalReasonId] = useState<number | null>(null)
  const [winReasons, setWinReasons] = useState<OpportunityWinReason[]>([])
  const [lossReasons, setLossReasons] = useState<OpportunityLossReason[]>([])

  const { execute: fetchOpportunity, loading } = useApi<Opportunity | null>({ showErrorMessage: true })
  const { execute: executeAction, loading: actionLoading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadApprovals = async (proposals: { id: number }[]) => {
    if (proposals.length === 0) {
      setApprovalRequests([])
      return
    }
    const results = await Promise.allSettled(proposals.map((p) => proposalService.getApprovalRequests(p.id)))
    setApprovalRequests(results.flatMap((result) => (result.status === 'fulfilled' ? result.value : [])))
    if (results.some((result) => result.status === 'rejected')) {
      toast({ title: t('opportunityDetail.approvals.loadError'), variant: 'destructive' })
    }
  }

  const loadOpportunity = async () => {
    const result = await fetchOpportunity(() => opportunityService.getById(opportunityId))
    if (result) {
      setOpportunity(result)
      setSelectedStage(String(result.commercialPipelineStageId))
      await loadApprovals(result.proposals ?? [])
    }
  }

  useEffect(() => {
    if (!opportunityId) {
      return
    }

    void loadOpportunity()
    void commercialPipelineStageService.getActive().then(setStages)
  }, [opportunityId])

  const pendingFollowUpsCount = useMemo(() => opportunity?.followUps.filter((item) => !item.isCompleted).length ?? 0, [opportunity])
  const overdueFollowUpsCount = useMemo(() => opportunity?.followUps.filter((item) => !item.isCompleted && new Date(item.dueAt) < new Date()).length ?? 0, [opportunity])

  const followUpColumns: DataTableColumn<OpportunityFollowUp>[] = [
    { key: 'subject', title: t('followup.field.subject'), dataIndex: 'subject' },
    { key: 'dueAt', title: t('proposalDetail.item.field.deadline'), dataIndex: 'dueAt', render: (value: string) => formatDate(value) },
    {
      key: 'isCompleted',
      title: t('common.field.status'),
      dataIndex: 'isCompleted',
      render: (value: boolean, record: OpportunityFollowUp) => {
        const isOverdue = !value && new Date(record.dueAt) < new Date()
        return (
          <Badge variant={value ? 'success' : isOverdue ? 'destructive' : 'warning'}>
            {value ? t('followupStatus.completed') : isOverdue ? t('followupStatus.overdue') : t('followupStatus.pending')}
          </Badge>
        )
      },
    },
    { key: 'notes', title: t('common.field.notes'), dataIndex: 'notes', render: (value?: string) => value || '-' },
  ]

  const openWonModal = async (target: { id: number; name: string }) => {
    setPendingFinalStage({ id: target.id, name: target.name, kind: 'won' })
    setFinalNotes('')
    setFinalReasonId(null)
    try {
      const result = await opportunityWinReasonService.getAll({ pageSize: 200 })
      setWinReasons(result.data ?? [])
    } catch {
      setWinReasons([])
    }
  }

  const openLostModal = async (target: { id: number; name: string }) => {
    setPendingFinalStage({ id: target.id, name: target.name, kind: 'lost' })
    setFinalNotes('')
    setFinalReasonId(null)
    try {
      const result = await opportunityLossReasonService.getAll({ pageSize: 200 })
      setLossReasons(result.data ?? [])
    } catch {
      setLossReasons([])
    }
  }

  const handleChangeStage = async (stageId: number) => {
    if (!opportunity) return
    const targetStage = stages.find((stage) => stage.id === stageId)
    if (!targetStage) return

    const isClosed = !!opportunity.closedAt

    // Oportunidade fechada: mover para etapa aberta exige confirmar reabertura (allowReopen);
    // mover para outra etapa final (corrigir ganho<->perda) reabre primeiro e depois fecha de novo
    if (isClosed) {
      if (targetStage.finalBehavior === 0) {
        setPendingReopen({ stageId, correction: null })
        return
      }
      const kind = targetStage.finalBehavior === 1 ? 'won' : 'lost'
      setPendingReopen({ stageId, correction: { kind, stageId, name: targetStage.name } })
      return
    }

    if (targetStage.finalBehavior === 1) {
      await openWonModal(targetStage)
      return
    }

    if (targetStage.finalBehavior === 2) {
      await openLostModal(targetStage)
      return
    }

    setSelectedStage(String(stageId))
    const result = await executeAction(() => opportunityService.changeStage(opportunity.id, { commercialPipelineStageId: stageId, expectedVersion: opportunity.version }))
    if (result !== null) await loadOpportunity()
  }

  const confirmReopen = async () => {
    if (!opportunity || !pendingReopen) return
    const { correction } = pendingReopen
    // Correcao de resultado: reabre para a primeira etapa aberta e abre o modal de fechamento do alvo
    const reopenStageId = correction
      ? (sortedStages.find((stage) => (stage.finalBehavior ?? 0) === 0)?.id ?? pendingReopen.stageId)
      : pendingReopen.stageId

    const result = await executeAction(() => opportunityService.changeStage(opportunity.id, { commercialPipelineStageId: reopenStageId, allowReopen: true, expectedVersion: opportunity.version }))
    setPendingReopen(null)
    if (result === null) return
    await loadOpportunity()

    if (correction) {
      const target = { id: correction.stageId, name: correction.name }
      await (correction.kind === 'won' ? openWonModal(target) : openLostModal(target))
    }
  }

  const confirmFinalChange = async () => {
    if (!opportunity || !pendingFinalStage) return
    const trimmedNotes = finalNotes.trim()
    if (pendingFinalStage.kind === 'lost' && trimmedNotes.length === 0 && finalReasonId === null) return

    const result = await executeAction(() =>
      pendingFinalStage.kind === 'won'
        ? opportunityService.closeAsWon(opportunity.id, { wonNotes: trimmedNotes || undefined, winReasonId: finalReasonId ?? undefined })
        : opportunityService.closeAsLost(opportunity.id, { lossReason: trimmedNotes || lossReasons.find((r) => r.id === finalReasonId)?.name || t('opportunityDetail.close.defaultLossReason'), lossReasonId: finalReasonId ?? undefined }),
    )
    if (result !== null) {
      setPendingFinalStage(null)
      setFinalNotes('')
      setFinalReasonId(null)
      await loadOpportunity()
    }
  }

  const cancelFinalChange = () => {
    setPendingFinalStage(null)
    setFinalNotes('')
    setFinalReasonId(null)
    if (opportunity) setSelectedStage(String(opportunity.commercialPipelineStageId))
  }

  const handleCompleteFollowUp = async () => {
    if (!selectedFollowUp) return
    const result = await executeAction(() => opportunityService.completeFollowUp(selectedFollowUp.id))
    if (result !== null) await loadOpportunity()
  }

  const handleDeleteFollowUp = async () => {
    if (!selectedFollowUp) return
    const result = await executeAction(() => opportunityService.deleteFollowUp(selectedFollowUp.id))
    setConfirmDeleteFollowUpOpen(false)
    if (result !== null) {
      setSelectedFollowUp(null)
      await loadOpportunity()
    }
  }

  const pendingApprovalsCount = approvalRequests.filter((item) => item.status === OpportunityApprovalStatus.Pending).length

  const reviewersByApproval = useMemo<Record<number, OpportunityApprovalReviewer[]>>(
    () => Object.fromEntries(approvalRequests.map((approval) => [approval.id, approval.reviewers ?? []])),
    [approvalRequests],
  )

  const isMyPendingApproval = (approvalId: number): boolean => (reviewersByApproval[approvalId] ?? []).some((reviewer) => reviewer.status === OpportunityApprovalReviewerStatus.Pending && ((authUser?.id != null && reviewer.userId === authUser.id) || (reviewer.userId == null && reviewer.userName === authUser?.name)))

  const pendingForMe = approvalRequests.filter((item) => item.status === OpportunityApprovalStatus.Pending && isMyPendingApproval(item.id)).length

  const sortedStages = useMemo(() => [...stages].sort((a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0)), [stages])
  const currentStageIndex = useMemo(() => sortedStages.findIndex((stage) => stage.id === opportunity?.commercialPipelineStage?.id), [sortedStages, opportunity])
  const nextStage = currentStageIndex >= 0 && currentStageIndex < sortedStages.length - 1 ? sortedStages[currentStageIndex + 1] : null
  const createdMeta = opportunity?.createdAt ? t('opportunityDetail.createdAt').replace('{0}', formatDate(opportunity.createdAt)) : null

  const nextFollowUp = useMemo(() => {
    const pending = (opportunity?.followUps ?? []).filter((item) => !item.isCompleted)
    if (pending.length === 0) return null
    return [...pending].sort((a, b) => new Date(a.dueAt).getTime() - new Date(b.dueAt).getTime())[0]
  }, [opportunity])

  const nextFollowUpDueDate = nextFollowUp ? new Date(nextFollowUp.dueAt) : null
  const isNextFollowUpOverdue = nextFollowUpDueDate ? nextFollowUpDueDate.getTime() < Date.now() : false

  const handleCompleteNextFollowUp = async () => {
    if (!nextFollowUp) return
    const result = await executeAction(() => opportunityService.completeFollowUp(nextFollowUp.id))
    if (result !== null) await loadOpportunity()
  }

  return (
    <div className="space-y-6">
          <header className="space-y-3 border-b border-border pb-5">
            <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
              <div className="min-w-0 flex-1 space-y-2">
                <div className="flex flex-wrap items-center gap-3">
                  <h1 className="text-3xl font-semibold tracking-tight text-foreground" style={{ letterSpacing: '-0.01em', lineHeight: 1.15 }}>
                    {opportunity?.name || t('opportunityDetail.fallbackTitle')}
                  </h1>
                  {opportunity?.id ? (
                    <span className="inline-flex items-center rounded bg-muted px-2 py-0.5 font-mono text-xs text-muted-foreground">
                      {t('common.field.code')} {opportunity.id}
                    </span>
                  ) : null}
                  <span
                    className="inline-flex items-center rounded px-2.5 py-1 text-[11px] font-bold uppercase tracking-wider text-white"
                    style={{ backgroundColor: opportunity?.commercialPipelineStage?.color || 'hsl(var(--primary))' }}
                  >
                    {opportunity?.commercialPipelineStage?.name || t('opportunityDetail.stage.none')}
                  </span>
                  {pendingApprovalsCount > 0 && (
                    <span className="inline-flex items-center gap-1 rounded border border-amber-300 bg-amber-100 px-2 py-0.5 text-[11px] font-bold uppercase tracking-wider text-amber-800">
                      <Clock className="h-3 w-3" /> {pendingApprovalsCount === 1 ? t('opportunityDetail.pendingApprovals.one') : t('opportunityDetail.pendingApprovals.many').replace('{0}', String(pendingApprovalsCount))}
                    </span>
                  )}
                </div>
                <div className="flex flex-wrap items-center gap-x-3 gap-y-1.5 text-[13px] text-muted-foreground">
                  <span className="flex items-center gap-2">
                    {opportunity?.brand?.logoUrl ? (
                      <img
                        src={resolveAssetUrl(opportunity.brand.logoUrl)}
                        alt={opportunity.brand?.name ?? ''}
                        className="h-5 w-5 rounded-sm border bg-card object-contain p-0.5"
                        onError={(e) => {
                          const img = e.currentTarget as HTMLImageElement
                          img.style.display = 'none'
                          const fallback = img.nextElementSibling as HTMLElement | null
                          if (fallback) fallback.style.display = 'inline-flex'
                        }}
                      />
                    ) : null}
                    <Building2
                      className="h-3.5 w-3.5"
                      style={{ display: opportunity?.brand?.logoUrl ? 'none' : 'inline-flex' }}
                    />
                    <strong className="text-foreground">{opportunity?.brand?.name || t('opportunityDetail.brand.unknown')}</strong>
                  </span>
                  {opportunity?.id ? (
                    <>
                      <span className="text-muted-foreground/40">·</span>
                      <span>{t('opportunityDetail.opportunityNumber').replace('{0}', String(opportunity.id))}</span>
                    </>
                  ) : null}
                  {createdMeta ? (
                    <>
                      <span className="text-muted-foreground/40">·</span>
                      <span>{createdMeta}</span>
                    </>
                  ) : null}
                  {opportunity?.commercialResponsible?.name ? (
                    <>
                      <span className="text-muted-foreground/40">·</span>
                      <span>{t('opportunityDetail.byResponsiblePrefix')} <strong className="text-foreground">{opportunity.commercialResponsible.name}</strong></span>
                    </>
                  ) : null}
                </div>
              </div>
              <div className="flex flex-shrink-0 flex-wrap items-center gap-2">
                <Dropdown>
                  <DropdownTrigger asChild>
                    <Button size="sm" variant="outline" className="px-2.5">
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </DropdownTrigger>
                  <DropdownContent align="end" className="w-56">
                    <DropdownLabel>{t('opportunityDetail.moveToStage')}</DropdownLabel>
                    {sortedStages.map((stage) => (
                      <DropdownItem
                        key={stage.id}
                        onSelect={() => void handleChangeStage(stage.id)}
                        disabled={stage.id === opportunity?.commercialPipelineStage?.id}
                      >
                        <span
                          className="mr-2 inline-block h-2 w-2 rounded-full"
                          style={{ backgroundColor: stage.color || 'hsl(var(--muted-foreground))' }}
                        />
                        {stage.name}
                        {stage.id === opportunity?.commercialPipelineStage?.id && <span className="ml-auto text-[10px] uppercase tracking-wider text-muted-foreground">{t('opportunityDetail.stage.current')}</span>}
                      </DropdownItem>
                    ))}
                    <DropdownSeparator />
                    <DropdownItem onSelect={() => void loadOpportunity()}>
                      <History className="mr-2 h-3.5 w-3.5" /> {t('opportunityDetail.action.refresh')}
                    </DropdownItem>
                  </DropdownContent>
                </Dropdown>
                <Button size="sm" variant="outline" onClick={() => setIsOpportunityFormOpen(true)}>
                  <Pencil className="mr-2 h-4 w-4" /> {t('common.action.edit')}
                </Button>
                {nextStage ? (
                  <Button size="sm" onClick={() => void handleChangeStage(nextStage.id)}>
                    {t('opportunityDetail.action.advanceTo').replace('{0}', nextStage.name)}
                    <ArrowRight className="ml-2 h-4 w-4" />
                  </Button>
                ) : null}
              </div>
            </div>
          </header>

        <Tabs value={activeTab} onValueChange={handleTabChange} className="pt-2">
          <TabsList className="mb-6 h-auto w-full justify-start gap-6 rounded-none border-b border-border bg-transparent p-0">
            <TabsTrigger value="summary" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <FileText className="h-4 w-4" /> {t('opportunityDetail.tab.summary')}
            </TabsTrigger>
            <TabsTrigger value="proposals" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <TrendingUp className="h-4 w-4" /> {t('opportunityDetail.tab.proposals')}
              {opportunity?.proposals?.length ? (
                <span className="rounded-full bg-muted px-1.5 py-0.5 text-[11px] font-medium text-muted-foreground group-data-[state=active]:bg-primary/10 group-data-[state=active]:text-primary">
                  {opportunity.proposals.length}
                </span>
              ) : null}
            </TabsTrigger>
            <TabsTrigger value="approvals" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <CheckCircle className="h-4 w-4" /> {t('opportunityDetail.tab.approvals')}
              {approvalRequests.length ? (
                <span className="rounded-full bg-muted px-1.5 py-0.5 text-[11px] font-medium text-muted-foreground group-data-[state=active]:bg-primary/10 group-data-[state=active]:text-primary">
                  {approvalRequests.length}
                </span>
              ) : null}
              {pendingForMe > 0 && <span className="h-1.5 w-1.5 rounded-full bg-destructive" aria-label={t('opportunityDetail.approval.waitingForYouAria').replace('{0}', String(pendingForMe))} />}
            </TabsTrigger>
            <TabsTrigger value="followups" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <Clock className="h-4 w-4" /> {t('opportunityDetail.tab.followups')}
              {opportunity?.followUps?.length ? (
                <span className="rounded-full bg-muted px-1.5 py-0.5 text-[11px] font-medium text-muted-foreground group-data-[state=active]:bg-primary/10 group-data-[state=active]:text-primary">
                  {opportunity.followUps.length}
                </span>
              ) : null}
              {overdueFollowUpsCount > 0 && <span className="h-1.5 w-1.5 rounded-full bg-amber-500" aria-label={t('opportunityDetail.followups.overdueAria').replace('{0}', String(overdueFollowUpsCount))} />}
            </TabsTrigger>
            <TabsTrigger value="activity" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <Activity className="h-4 w-4" /> {t('opportunityDetail.tab.activity')}
            </TabsTrigger>
          </TabsList>

          <TabsContent value="summary" className="mt-0">
            <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
              <div className="space-y-5">
                {pendingForMe > 0 && (
                  <div className="flex items-center gap-5 rounded-xl bg-gradient-to-br from-primary to-primary/80 p-6 text-primary-foreground shadow-lg shadow-primary/20">
                    <div className="flex h-14 w-14 shrink-0 items-center justify-center rounded-xl bg-white/15">
                      <ThumbsUp className="h-7 w-7" />
                    </div>
                    <div className="flex-1">
                      <div className="text-[11px] font-bold uppercase tracking-widest opacity-80">{t('opportunityDetail.summary.nextStep')}</div>
                      <div className="mt-1 text-lg font-bold leading-snug">
                        {pendingForMe === 1
                          ? t('opportunityDetail.summary.banner.one')
                          : t('opportunityDetail.summary.banner.many').replace('{0}', String(pendingForMe))}
                      </div>
                      <p className="mt-0.5 text-sm opacity-90">
                        {t('opportunityDetail.summary.banner.subtitle')}
                      </p>
                    </div>
                    <Button
                      size="sm"
                      variant="outline"
                      className="border-white/30 bg-white text-primary hover:bg-white/95"
                      onClick={() => handleTabChange('approvals')}
                    >
                      {t('opportunityDetail.summary.banner.review')} <ArrowRight className="ml-1.5 h-3.5 w-3.5" />
                    </Button>
                  </div>
                )}

                <div className="grid grid-cols-2 gap-0 overflow-hidden rounded-xl border border-border bg-card md:grid-cols-4">
                  <KpiCell
                    icon={<CircleDollarSign className={`h-3.5 w-3.5 ${opportunity?.closedValue != null ? 'text-emerald-600' : 'text-indigo-600'}`} />}
                    label={opportunity?.closedValue != null ? t('opportunityDetail.kpi.closedValue') : t('opportunityDetail.kpi.estimatedValue')}
                    value={opportunity?.closedValue != null ? formatCurrency(opportunity.closedValue) : formatCurrency(opportunity?.estimatedValue ?? 0)}
                    sub={opportunity?.closedValue != null ? t('opportunityDetail.kpi.estimatedSub').replace('{0}', formatCurrency(opportunity?.estimatedValue ?? 0)) : undefined}
                  />
                  <KpiCell
                    icon={<Calendar className="h-3.5 w-3.5 text-violet-600" />}
                    label={t('opportunityDetail.kpi.forecast')}
                    value={formatDate(opportunity?.expectedCloseAt) || '—'}
                  />
                  <KpiCell
                    icon={<UserCheck className="h-3.5 w-3.5 text-cyan-600" />}
                    label={t('common.field.responsible')}
                    value={opportunity?.commercialResponsible?.name || '—'}
                  />
                  <KpiCell
                    icon={<Clock className="h-3.5 w-3.5 text-amber-600" />}
                    label={t('opportunityDetail.kpi.pendingFollowUps')}
                    value={String(pendingFollowUpsCount)}
                    sub={overdueFollowUpsCount > 0
                      ? (overdueFollowUpsCount === 1 ? t('opportunityDetail.overdueSingle') : t('opportunityDetail.overdueMany')).replace('{0}', String(overdueFollowUpsCount))
                      : undefined}
                    subTone="destructive"
                    last
                  />
                </div>

                {nextFollowUp && (
                  <div>
                    <h3 className="mb-3 text-base font-semibold text-foreground">{t('opportunityDetail.summary.nextStep')}</h3>
                    <div className={`flex items-center gap-4 rounded-xl border p-4 ${isNextFollowUpOverdue ? 'border-amber-300 bg-amber-50' : 'border-border bg-card'}`}>
                      <div className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-lg ${isNextFollowUpOverdue ? 'bg-amber-100 text-amber-700' : 'bg-primary/10 text-primary'}`}>
                        <Calendar className="h-5 w-5" />
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-semibold text-foreground">{nextFollowUp.subject}</p>
                        <p className={`mt-0.5 text-xs ${isNextFollowUpOverdue ? 'text-amber-800' : 'text-muted-foreground'}`}>
                          {isNextFollowUpOverdue ? t('opportunityDetail.nextFollowUp.overduePrefix') : t('opportunityDetail.nextFollowUp.scheduledPrefix')}
                          <strong>{formatDate(nextFollowUp.dueAt)}</strong>
                        </p>
                        {nextFollowUp.notes && (
                          <p className="mt-1 line-clamp-1 text-xs italic text-muted-foreground">"{nextFollowUp.notes}"</p>
                        )}
                      </div>
                      <div className="flex flex-shrink-0 gap-1.5">
                        <Button size="sm" variant="outline" onClick={() => { setSelectedFollowUp(nextFollowUp); setIsFollowUpFormOpen(true) }}>
                          {t('opportunityDetail.nextFollowUp.reschedule')}
                        </Button>
                        <Button size="sm" onClick={() => void handleCompleteNextFollowUp()}>
                          <CheckCircle className="mr-1.5 h-3.5 w-3.5" /> {t('opportunityDetail.nextFollowUp.complete')}
                        </Button>
                      </div>
                    </div>
                  </div>
                )}

                <div>
                  <h3 className="mb-3 text-base font-semibold text-foreground">{t('opportunityDetail.progress.title')}</h3>
                  <div className="grid gap-3 md:grid-cols-2">
                    <SubflowCard
                      icon={<CheckCircle className="h-4 w-4" />}
                      iconClassName="bg-emerald-100 text-emerald-700"
                      label={t('opportunityDetail.progress.approvals')}
                      count={approvalRequests.length}
                      statusLabel={pendingForMe > 0
                        ? t('opportunityDetail.progress.waitingForYou').replace('{0}', String(pendingForMe))
                        : pendingApprovalsCount > 0
                          ? (pendingApprovalsCount === 1 ? t('opportunityDetail.progress.pending.one') : t('opportunityDetail.progress.pending.many')).replace('{0}', String(pendingApprovalsCount))
                          : t('opportunityDetail.progress.noPending')}
                      statusTone={pendingForMe > 0 ? 'red' : pendingApprovalsCount > 0 ? 'amber' : 'muted'}
                      onClick={() => handleTabChange('approvals')}
                    />
                    <SubflowCard
                      icon={<TrendingUp className="h-4 w-4" />}
                      iconClassName="bg-cyan-100 text-cyan-700"
                      label={t('opportunityDetail.progress.proposals')}
                      count={opportunity?.proposals?.length ?? 0}
                      statusLabel={(opportunity?.proposals ?? []).length > 0
                        ? ((opportunity?.proposals ?? []).length === 1 ? t('opportunityDetail.progress.versions.one') : t('opportunityDetail.progress.versions.many')).replace('{0}', String((opportunity?.proposals ?? []).length))
                        : t('opportunityDetail.progress.noneSent')}
                      statusTone="muted"
                      onClick={() => handleTabChange('proposals')}
                    />
                  </div>
                </div>

                <div>
                  <h3 className="mb-3 text-base font-semibold text-foreground">{t('opportunityDetail.details.title')}</h3>
                  <div className="grid gap-x-7 gap-y-4 rounded-xl border border-border bg-card p-5 md:grid-cols-3">
                    <DetailField icon={<FileText className="h-3.5 w-3.5" />} label={t('opportunityDetail.summary.description')} value={opportunity?.description} />
                    <DetailField icon={<User className="h-3.5 w-3.5" />} label={t('common.field.contact')} value={opportunity?.contactName} />
                    <DetailField icon={<Compass className="h-3.5 w-3.5" />} label={t('opportunityDetail.summary.source')} value={opportunity?.opportunitySource ? (
                      <span className="inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-xs font-medium" style={{ borderColor: opportunity.opportunitySource.color, color: opportunity.opportunitySource.color }}>
                        <span className="inline-block h-1.5 w-1.5 rounded-full" style={{ backgroundColor: opportunity.opportunitySource.color }} />
                        {opportunity.opportunitySource.name}
                      </span>
                    ) : null} />
                    <DetailField icon={<Calendar className="h-3.5 w-3.5" />} label={t('opportunityDetail.summary.closedAt')} value={formatDate(opportunity?.closedAt)} />
                    <DetailField icon={<Tag className="h-3.5 w-3.5" />} label={t('common.field.notes')} value={opportunity?.notes} />
                    <DetailField icon={<Tags className="h-3.5 w-3.5" />} label={t('opportunityDetail.summary.tags')} value={
                      opportunity?.tags && opportunity.tags.length > 0 ? (
                        <div className="flex flex-wrap gap-1.5">
                          {opportunity.tags.map((tag) => (
                            <span key={tag.id} className="inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium" style={{ backgroundColor: `${tag.color}20`, color: tag.color, borderColor: tag.color, borderWidth: 1 }}>
                              {tag.name}
                            </span>
                          ))}
                        </div>
                      ) : null
                    } />
                    {opportunity?.lossReason && (
                      <DetailField icon={<XCircle className="h-3.5 w-3.5 text-destructive" />} label={t('opportunityDetail.summary.lossReason')} value={<span className="text-destructive">{opportunity.lossReason}</span>} />
                    )}
                    {opportunity?.wonNotes && (
                      <DetailField icon={<CheckCircle className="h-3.5 w-3.5 text-emerald-600" />} label={t('opportunityDetail.summary.wonNotes')} value={<span className="text-emerald-700">{opportunity.wonNotes}</span>} />
                    )}
                  </div>
                </div>
              </div>

              <aside className="space-y-4">
                <FunnelStagesCard stages={sortedStages} currentStageId={opportunity?.commercialPipelineStage?.id} />

                <div className="rounded-xl border border-border bg-card p-4">
                  <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-foreground">
                    <User className="h-3.5 w-3.5" /> {t('opportunityDetail.responsible.title')}
                  </div>
                  {opportunity?.commercialResponsible?.name ? (
                    <div className="flex items-center gap-3">
                      <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-xs font-bold uppercase text-primary">
                        {opportunity.commercialResponsible.name.split(' ').slice(0, 2).map((s) => s[0]).join('')}
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-semibold text-foreground">{opportunity.commercialResponsible.name}</p>
                        <p className="text-xs text-muted-foreground">{t('opportunityDetail.responsible.role')}</p>
                      </div>
                    </div>
                  ) : (
                    <p className="text-xs text-muted-foreground">{t('opportunityDetail.responsible.none')}</p>
                  )}
                </div>

                <div className="rounded-xl border border-border bg-card p-4">
                  <div className="mb-3 flex items-center justify-between">
                    <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                      <Clock className="h-3.5 w-3.5" /> {t('opportunityDetail.followups.sidebarTitle')}
                    </div>
                    <Button size="sm" variant="ghost" className="h-7 px-2 text-xs" onClick={() => setIsFollowUpFormOpen(true)}>
                      <Plus className="mr-1 h-3 w-3" /> {t('opportunityDetail.followups.newShort')}
                    </Button>
                  </div>
                  {pendingFollowUpsCount === 0 ? (
                    <p className="text-xs text-muted-foreground">{t('opportunityDetail.followups.sidebarEmpty')}</p>
                  ) : overdueFollowUpsCount > 0 ? (
                    <div className="rounded-lg border border-amber-300 bg-amber-50 px-3 py-2 text-xs text-amber-800">
                      <strong>{overdueFollowUpsCount}</strong> {overdueFollowUpsCount === 1 ? t('opportunityDetail.followups.overdueWaiting.one') : t('opportunityDetail.followups.overdueWaiting.many')}
                    </div>
                  ) : (
                    <p className="text-xs text-muted-foreground">{(pendingFollowUpsCount === 1 ? t('opportunityDetail.followups.scheduledOnTime.one') : t('opportunityDetail.followups.scheduledOnTime.many')).replace('{0}', String(pendingFollowUpsCount))}</p>
                  )}
                </div>
              </aside>
            </div>
          </TabsContent>

          <TabsContent value="proposals" className="mt-0">
            <ProposalsTab
              proposals={opportunity?.proposals ?? []}
              opportunityCreatedAt={opportunity?.createdAt}
              onNew={() => setIsProposalFormOpen(true)}
              onOpen={(id, name) => navigate(`/comercial/propostas/${id}`, {
                state: opportunity
                  ? { from: 'opportunity', opportunityId: opportunity.id, opportunityName: opportunity.name, tab: 'proposals', proposalName: name }
                  : undefined,
              })}
            />
          </TabsContent>

          <TabsContent value="approvals" className="mt-0">
            <ApprovalsTab
              approvals={approvalRequests}
              actionLoading={actionLoading}
              t={t}
              canDecide={(item) => isMyPendingApproval(item.id)}
              onApprove={async (item) => {
                setSelectedApprovalRequest(item)
                const result = await executeAction(() => opportunityService.recordReviewerDecision(item.id, {
                  approved: true,
                  notes: t('opportunityDetail.approvals.decision.approved'),
                }))
                if (result !== null) {
                  setSelectedApprovalRequest(null)
                  await loadOpportunity()
                }
              }}
              onReject={async (item) => {
                setSelectedApprovalRequest(item)
                const result = await executeAction(() => opportunityService.recordReviewerDecision(item.id, {
                  approved: false,
                  notes: t('opportunityDetail.approvals.decision.rejected'),
                }))
                if (result !== null) {
                  setSelectedApprovalRequest(null)
                  await loadOpportunity()
                }
              }}
            />
          </TabsContent>

          <TabsContent value="followups" className="mt-0">
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between gap-3">
                  <CardTitle className="flex items-center gap-2">
                    <Clock className="h-5 w-5 text-muted-foreground" /> {t('opportunityDetail.tab.followups')}
                  </CardTitle>
                  <Button size="sm" onClick={() => { setSelectedFollowUp(null); setIsFollowUpFormOpen(true) }}>
                    <Clock className="mr-1.5 h-4 w-4" /> {t('opportunityDetail.followups.new')}
                  </Button>
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  {t('opportunityDetail.followups.description')}
                </p>
              </CardHeader>
              <CardContent>
                <div className="mb-3 flex flex-wrap gap-2">
                  <Button size="sm" variant="outline" onClick={() => selectedFollowUp && setIsFollowUpFormOpen(true)} disabled={!selectedFollowUp}>
                    <Pencil className="mr-2 h-4 w-4" /> {t('common.action.edit')}
                  </Button>
                  <Button size="sm" variant="outline-success" onClick={() => void handleCompleteFollowUp()} disabled={!selectedFollowUp || selectedFollowUp?.isCompleted || actionLoading}>
                    <CheckCircle className="mr-2 h-4 w-4" /> {t('opportunityDetail.followups.complete')}
                  </Button>
                  <Button size="sm" variant="ghost" onClick={() => setConfirmDeleteFollowUpOpen(true)} disabled={!selectedFollowUp || actionLoading}>
                    <Trash2 className="mr-2 h-4 w-4" /> {t('common.action.delete')}
                  </Button>
                </div>
                <DataTable
                  columns={followUpColumns}
                  data={opportunity?.followUps || []}
                  rowKey="id"
                  selectedRows={selectedFollowUp ? [selectedFollowUp] : []}
                  onSelectionChange={(rows) => setSelectedFollowUp(rows[0] ?? null)}
                  emptyText={t('opportunityDetail.followups.empty')}
                  loading={loading}
                  pageSize={10}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="activity" className="mt-0">
            <OpportunityActivityTab opportunityId={opportunityId} currentUserId={authUser?.id ?? null} />
          </TabsContent>
        </Tabs>

      <OpportunityFormModal
        open={isOpportunityFormOpen}
        onOpenChange={setIsOpportunityFormOpen}
        opportunity={opportunity}
        onSuccess={() => {
          setIsOpportunityFormOpen(false)
          void loadOpportunity()
        }}
      />

      <OpportunityFollowUpFormModal
        open={isFollowUpFormOpen}
        onOpenChange={setIsFollowUpFormOpen}
        opportunityId={opportunityId}
        followUp={selectedFollowUp}
        onSuccess={() => {
          setIsFollowUpFormOpen(false)
          setSelectedFollowUp(null)
          void loadOpportunity()
        }}
      />

      <ConfirmModal
        open={confirmDeleteFollowUpOpen}
        onOpenChange={setConfirmDeleteFollowUpOpen}
        description={t('common.confirm.deleteItem').replace('{0}', selectedFollowUp?.subject ?? '')}
        variant="danger"
        onConfirm={() => void handleDeleteFollowUp()}
        loading={actionLoading}
      />

      <ProposalFormModal
        open={isProposalFormOpen}
        onOpenChange={setIsProposalFormOpen}
        proposal={null}
        presetOpportunityId={opportunity?.id ?? null}
        onSuccess={(created) => {
          setIsProposalFormOpen(false)
          void loadOpportunity()
          if (created?.id) {
            navigate(`/comercial/propostas/${created.id}`, {
              state: opportunity
                ? { from: 'opportunity', opportunityId: opportunity.id, opportunityName: opportunity.name, tab: 'proposals' }
                : undefined,
            })
          }
        }}
      />

      <ConfirmModal
        open={!!pendingReopen}
        onOpenChange={(open) => { if (!open) setPendingReopen(null) }}
        description={pendingReopen?.correction ? t('opportunityDetail.reopen.correctionConfirm') : t('opportunity.reopen.confirmationRequired')}
        variant="warning"
        onConfirm={() => void confirmReopen()}
        loading={actionLoading}
      />

      <Modal open={!!pendingFinalStage} onOpenChange={(open) => { if (!open) cancelFinalChange() }}>
        <ModalContent size="form">
          <ModalHeader>
            <ModalTitle>
              {pendingFinalStage?.kind === 'won' ? t('opportunityDetail.close.wonTitle') : t('opportunityDetail.close.lostTitle')}
            </ModalTitle>
          </ModalHeader>
          <div className="space-y-3">
            <p className="text-sm text-muted-foreground">
              {t('opportunityDetail.close.movingPrefix')}<strong>{pendingFinalStage?.name}</strong>.
              {pendingFinalStage?.kind === 'lost' ? t('opportunityDetail.close.lostSuffix') : t('opportunityDetail.close.wonSuffix')}
            </p>
            {pendingFinalStage?.kind === 'won' ? (
              <div className="space-y-2">
                <label className="text-sm font-medium">{t('opportunityDetail.close.wonReasonLabel')}</label>
                {winReasons.length === 0 ? (
                  <p className="text-xs text-muted-foreground">{t('opportunityDetail.close.wonReasonEmpty')}</p>
                ) : (
                  <div className="flex flex-wrap gap-1.5">
                    {winReasons.map((reason) => {
                      const selected = finalReasonId === reason.id
                      return (
                        <button
                          key={reason.id}
                          type="button"
                          onClick={() => setFinalReasonId(selected ? null : reason.id)}
                          className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-medium transition-colors"
                          style={selected ? { borderColor: reason.color, backgroundColor: `${reason.color}1a`, color: reason.color } : { borderColor: 'hsl(var(--border))', color: 'hsl(var(--muted-foreground))' }}
                        >
                          <span className="h-1.5 w-1.5 rounded-full" style={{ backgroundColor: reason.color }} />
                          {reason.name}
                        </button>
                      )
                    })}
                  </div>
                )}
              </div>
            ) : (
              <div className="space-y-2">
                <label className="text-sm font-medium">
                  {t('opportunityDetail.close.lostReasonLabel')}
                  <span className="text-destructive"> *</span>
                </label>
                {lossReasons.length === 0 ? (
                  <p className="text-xs text-muted-foreground">{t('opportunityDetail.close.lostReasonEmpty')}</p>
                ) : (
                  <div className="flex flex-wrap gap-1.5">
                    {lossReasons.map((reason) => {
                      const selected = finalReasonId === reason.id
                      return (
                        <button
                          key={reason.id}
                          type="button"
                          onClick={() => setFinalReasonId(selected ? null : reason.id)}
                          className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-medium transition-colors"
                          style={selected ? { borderColor: reason.color, backgroundColor: `${reason.color}1a`, color: reason.color } : { borderColor: 'hsl(var(--border))', color: 'hsl(var(--muted-foreground))' }}
                        >
                          <span className="h-1.5 w-1.5 rounded-full" style={{ backgroundColor: reason.color }} />
                          {reason.name}
                        </button>
                      )
                    })}
                  </div>
                )}
              </div>
            )}
            <div className="space-y-2">
              <label className="text-sm font-medium">
                {pendingFinalStage?.kind === 'won' ? t('opportunityDetail.close.wonNotesLabel') : t('opportunityDetail.close.lostNotesLabel')}
              </label>
              <textarea
                className="min-h-[80px] w-full rounded-md border bg-background p-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                value={finalNotes}
                onChange={(e) => setFinalNotes(e.target.value)}
                placeholder={pendingFinalStage?.kind === 'won' ? t('opportunityDetail.close.wonPlaceholder') : t('opportunityDetail.close.lostPlaceholder')}
              />
            </div>
          </div>
          <ModalFooter>
            <Button type="button" variant="outline" onClick={cancelFinalChange} disabled={actionLoading}>{t('common.action.cancel')}</Button>
            <Button
              type="button"
              variant={pendingFinalStage?.kind === 'lost' ? 'danger' : 'primary'}
              onClick={() => void confirmFinalChange()}
              disabled={actionLoading || (pendingFinalStage?.kind === 'lost' && finalNotes.trim().length === 0 && finalReasonId === null)}
            >
              {actionLoading ? t('common.action.saving') : t('opportunityDetail.close.confirm')}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </div>
  )
}

function KpiCell({ icon, label, value, sub, subTone, last }: { icon: React.ReactNode; label: string; value: string; sub?: string; subTone?: 'destructive' | 'muted'; last?: boolean }) {
  return (
    <div className={`px-5 py-4 ${last ? '' : 'border-r border-border/60'}`}>
      <div className="flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wider text-muted-foreground">
        {icon} {label}
      </div>
      <p className="mt-1.5 truncate text-base font-semibold text-foreground">{value}</p>
      {sub && (
        <p className={`mt-0.5 text-[11px] font-medium ${subTone === 'destructive' ? 'text-destructive' : 'text-muted-foreground'}`}>{sub}</p>
      )}
    </div>
  )
}

function SubflowCard({ icon, iconClassName, label, count, statusLabel, statusTone, onClick }: { icon: React.ReactNode; iconClassName: string; label: string; count: number; statusLabel: string; statusTone: 'amber' | 'red' | 'muted'; onClick?: () => void }) {
  const toneClasses: Record<typeof statusTone, string> = {
    amber: 'text-amber-700',
    red: 'text-destructive',
    muted: 'text-muted-foreground',
  }
  return (
    <button type="button" onClick={onClick} className="group flex flex-col gap-3 rounded-xl border border-border bg-card p-4 text-left transition-colors hover:border-primary/40 hover:bg-accent/30">
      <div className="flex items-center justify-between">
        <span className={`flex h-8 w-8 items-center justify-center rounded-md ${iconClassName}`}>
          {icon}
        </span>
        <ArrowRight className="h-3.5 w-3.5 text-muted-foreground/50 transition-colors group-hover:text-primary" />
      </div>
      <div>
        <div className="text-[11px] font-bold uppercase tracking-wider text-muted-foreground">{label}</div>
        <div className="mt-0.5 text-2xl font-bold leading-none text-foreground">{count}</div>
        <div className={`mt-1 text-xs font-medium ${toneClasses[statusTone]}`}>{statusLabel}</div>
      </div>
    </button>
  )
}

function DetailField({ icon, label, value }: { icon: React.ReactNode; label: string; value: React.ReactNode }) {
  const isEmpty = value === null || value === undefined || value === '' || value === '-'
  return (
    <div className="space-y-1">
      <p className="flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wider text-muted-foreground">
        {icon} {label}
      </p>
      {isEmpty ? (
        <p className="text-sm text-muted-foreground/70">—</p>
      ) : typeof value === 'string' ? (
        <p className="text-sm text-foreground">{value}</p>
      ) : (
        <div className="text-sm text-foreground">{value}</div>
      )}
    </div>
  )
}

type OpportunityProposalReference = NonNullable<Opportunity['proposals']>[number]

interface ProposalsTabProps {
  proposals: OpportunityProposalReference[]
  opportunityCreatedAt?: string
  onNew: () => void
  onOpen: (id: number, name: string) => void
}

const proposalStatusInline: Record<number, { labelKey: string; bg: string; text: string; dot: string }> = {
  1: { labelKey: 'opportunityDetail.proposal.statusShort.draft', bg: 'bg-slate-200', text: 'text-slate-700', dot: 'bg-slate-400' },
  2: { labelKey: 'opportunityDetail.proposal.statusShort.sent', bg: 'bg-amber-100', text: 'text-amber-800', dot: 'bg-amber-500' },
  3: { labelKey: 'opportunityDetail.proposal.statusShort.viewed', bg: 'bg-blue-100', text: 'text-blue-800', dot: 'bg-blue-500' },
  4: { labelKey: 'opportunityDetail.proposal.statusShort.approved', bg: 'bg-emerald-100', text: 'text-emerald-800', dot: 'bg-emerald-500' },
  5: { labelKey: 'opportunityDetail.proposal.statusShort.rejected', bg: 'bg-rose-100', text: 'text-rose-800', dot: 'bg-rose-500' },
  6: { labelKey: 'opportunityDetail.proposal.statusShort.converted', bg: 'bg-emerald-100', text: 'text-emerald-800', dot: 'bg-emerald-500' },
  7: { labelKey: 'opportunityDetail.proposal.statusShort.expired', bg: 'bg-rose-100', text: 'text-rose-800', dot: 'bg-rose-500' },
  8: { labelKey: 'opportunityDetail.proposal.statusShort.cancelled', bg: 'bg-slate-200', text: 'text-slate-700', dot: 'bg-slate-400' },
}

function ProposalsTab({ proposals, opportunityCreatedAt, onNew, onOpen }: ProposalsTabProps) {
  const { t } = useI18n()
  const ordered = useMemo(() => [...proposals].sort((a, b) => a.id - b.id), [proposals])
  const total = ordered.length
  const currentProposal = ordered[ordered.length - 1] ?? null
  const approved = ordered.filter((p) => p.status === 4 || p.status === 6).length
  const sent = ordered.filter((p) => p.status === 2 || p.status === 3).length
  const baselineValue = ordered[0]?.totalValue ?? 0
  const lastValue = currentProposal?.totalValue ?? 0
  const valueDelta = lastValue - baselineValue
  const sortedDesc = useMemo(() => [...ordered].reverse(), [ordered])

  if (total === 0) {
    return (
      <Card className="border-dashed">
        <CardContent className="flex flex-col items-center justify-center py-12 text-center text-muted-foreground">
          <TrendingUp className="mb-3 h-9 w-9 opacity-50" />
          <p className="text-sm font-medium">{t('opportunityDetail.proposal.emptyTitle')}</p>
          <p className="mt-1 max-w-md text-xs">{t('opportunityDetail.proposal.emptyHint')}</p>
          <Button size="sm" className="mt-4" onClick={onNew}>
            <Plus className="mr-1.5 h-4 w-4" /> {t('opportunityDetail.proposals.new')}
          </Button>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-end">
        <div>
          <h2 className="text-xl font-semibold text-foreground">{t('opportunityDetail.proposal.historyTitle')}</h2>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {(total === 1 ? t('opportunityDetail.proposal.versionsCreated.one') : t('opportunityDetail.proposal.versionsCreated.many')).replace('{0}', String(total))} · {(approved === 1 ? t('opportunityDetail.proposal.approvedCount.one') : t('opportunityDetail.proposal.approvedCount.many')).replace('{0}', String(approved))} · {t('opportunityDetail.proposal.awaitingClient').replace('{0}', String(sent))}
          </p>
        </div>
        <Button size="sm" onClick={onNew}>
          <Plus className="mr-1.5 h-4 w-4" /> {t('opportunityDetail.proposals.new')}
        </Button>
      </div>

      <div className="grid grid-cols-2 gap-0 overflow-hidden rounded-xl border border-border bg-card md:grid-cols-4">
        <ProposalSummaryCell label={t('opportunityDetail.proposal.cell.versionsCreated')} value={String(total)} sub={total === 1 ? t('opportunityDetail.proposal.cell.onlyInitial') : t('opportunityDetail.proposal.cell.offerEvolution')} />
        <ProposalSummaryCell
          label={t('opportunityDetail.proposal.cell.diff').replace('{0}', String(total))}
          value={total > 1 ? (valueDelta >= 0 ? `+${formatCurrency(valueDelta)}` : formatCurrency(valueDelta)) : '—'}
          sub={total > 1 ? t('opportunityDetail.proposal.cell.diffSub') : t('opportunityDetail.proposal.cell.onlyOneVersion')}
          valueColor={total > 1 ? (valueDelta > 0 ? 'text-emerald-700' : valueDelta < 0 ? 'text-rose-700' : 'text-foreground') : 'text-muted-foreground'}
        />
        <ProposalSummaryCell label={t('opportunityDetail.proposal.cell.currentValue')} value={formatCurrency(lastValue)} sub={currentProposal?.name} mono />
        <ProposalSummaryCell
          label={t('opportunityDetail.proposal.cell.currentStatus')}
          value={currentProposal ? (proposalStatusInline[currentProposal.status]?.labelKey ? t(proposalStatusInline[currentProposal.status].labelKey) : '—') : '—'}
          pillBg={currentProposal ? proposalStatusInline[currentProposal.status]?.bg : undefined}
          pillText={currentProposal ? proposalStatusInline[currentProposal.status]?.text : undefined}
          last
        />
      </div>

      <div className="relative pt-2">
        <div className="absolute bottom-3 left-[19px] top-3 w-0.5 bg-gradient-to-b from-primary/50 via-border to-border/40" aria-hidden />
        <div className="space-y-4">
          {sortedDesc.map((proposal, index) => {
            const versionNumber = ordered.findIndex((p) => p.id === proposal.id) + 1
            const isCurrent = proposal.id === currentProposal?.id
            return (
              <ProposalTimelineNode
                key={proposal.id}
                proposal={proposal}
                versionNumber={versionNumber}
                isCurrent={isCurrent}
                isLast={index === sortedDesc.length - 1}
                onOpen={() => onOpen(proposal.id, proposal.name)}
              />
            )
          })}
          {opportunityCreatedAt && (
            <div className="relative flex items-center gap-4 pt-2">
              <div className="z-10 flex h-10 w-10 shrink-0 items-center justify-center rounded-full border-2 border-dashed border-muted-foreground/40 bg-card text-muted-foreground">
                <Compass className="h-4 w-4" />
              </div>
              <div className="text-sm text-muted-foreground">
                <strong className="text-foreground">{t('opportunityDetail.proposal.opportunityCreated')}</strong> {t('opportunityDetail.proposal.createdOn').replace('{0}', formatDate(opportunityCreatedAt))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

function ProposalSummaryCell({ label, value, sub, valueColor, pillBg, pillText, mono, last }: { label: string; value: string; sub?: string; valueColor?: string; pillBg?: string; pillText?: string; mono?: boolean; last?: boolean }) {
  return (
    <div className={`px-5 py-4 ${last ? '' : 'border-r border-border/60'}`}>
      <div className="text-[11px] font-bold uppercase tracking-wider text-muted-foreground">{label}</div>
      {pillBg ? (
        <div className="mt-1.5">
          <span className={`inline-flex items-center rounded px-2.5 py-0.5 text-xs font-bold uppercase tracking-wider ${pillBg} ${pillText}`}>{value}</span>
        </div>
      ) : (
        <p className={`mt-1.5 truncate text-base font-semibold ${mono ? 'font-mono' : ''} ${valueColor ?? 'text-foreground'}`}>{value}</p>
      )}
      {sub && <p className="mt-0.5 truncate text-[11px] text-muted-foreground">{sub}</p>}
    </div>
  )
}

interface ProposalTimelineNodeProps {
  proposal: OpportunityProposalReference
  versionNumber: number
  isCurrent: boolean
  isLast: boolean
  onOpen: () => void
}

function ProposalTimelineNode({ proposal, versionNumber, isCurrent, onOpen }: ProposalTimelineNodeProps) {
  const { t } = useI18n()
  const status = proposalStatusInline[proposal.status]
  const isFinalApproved = proposal.status === 4 || proposal.status === 6

  return (
    <div className="relative flex gap-4">
      <div
        className={`z-10 flex h-10 w-10 shrink-0 items-center justify-center rounded-full border-2 text-xs font-bold ${
          isFinalApproved
            ? 'border-emerald-500 bg-emerald-500 text-white shadow-[0_0_0_4px_rgba(16,185,129,0.15)]'
            : isCurrent
              ? 'border-primary bg-primary text-primary-foreground shadow-[0_0_0_4px_hsl(var(--primary)/0.15)]'
              : 'border-border bg-card text-muted-foreground'
        }`}
      >
        v{versionNumber}
      </div>
      <button
        type="button"
        onClick={onOpen}
        className={`group flex-1 cursor-pointer rounded-xl border bg-card p-4 text-left transition-shadow hover:shadow-md ${
          isFinalApproved
            ? 'border-emerald-300 shadow-sm shadow-emerald-100'
            : isCurrent
              ? 'border-primary/40'
              : 'border-border'
        }`}
      >
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <strong className="text-base text-foreground">{proposal.name}</strong>
              {status && (
                <span className={`inline-flex items-center gap-1 rounded px-2 py-0.5 text-[10.5px] font-bold uppercase tracking-wider ${status.bg} ${status.text}`}>
                  <span className={`h-1.5 w-1.5 rounded-full ${status.dot}`} />
                  {t(status.labelKey)}
                </span>
              )}
              {isCurrent && !isFinalApproved && (
                <span className="inline-flex items-center rounded bg-primary/10 px-2 py-0.5 text-[10.5px] font-bold uppercase tracking-wider text-primary">
                  {t('opportunityDetail.proposal.currentVersion')}
                </span>
              )}
            </div>
            <div className="mt-2 flex flex-wrap items-baseline gap-x-3 gap-y-1 text-sm text-muted-foreground">
              <span className="font-mono text-lg font-bold text-foreground">{formatCurrency(proposal.totalValue)}</span>
              {proposal.validityUntil && (
                <>
                  <span>·</span>
                  <span>{t('opportunityDetail.proposal.validity').replace('{0}', formatDate(proposal.validityUntil))}</span>
                </>
              )}
              {proposal.campaignId && (
                <>
                  <span>·</span>
                  <span className="inline-flex items-center gap-1 text-emerald-700">
                    <CheckCircle className="h-3.5 w-3.5" /> {t('opportunityDetail.proposal.convertedToCampaign')}
                  </span>
                </>
              )}
            </div>
          </div>
          <ArrowRight className="h-4 w-4 shrink-0 text-muted-foreground/50 transition-colors group-hover:text-primary" />
        </div>
      </button>
    </div>
  )
}

interface ApprovalsTabProps {
  approvals: OpportunityApprovalRequest[]
  actionLoading: boolean
  t: (key: string) => string
  canDecide: (approval: OpportunityApprovalRequest) => boolean
  onApprove: (item: OpportunityApprovalRequest) => Promise<void> | void
  onReject: (item: OpportunityApprovalRequest) => Promise<void> | void
}

const approvalTypeLabelKey: Record<number, string> = {
  1: 'opportunityDetail.approval.type.discount',
  2: 'opportunityDetail.approval.type.margin',
  3: 'opportunityDetail.approval.type.deadline',
  4: 'opportunityDetail.approval.type.other',
}

function hoursSince(iso: string): number {
  const diff = Date.now() - new Date(iso).getTime()
  return Math.max(0, Math.floor(diff / (1000 * 60 * 60)))
}

function ApprovalsTab({ approvals, actionLoading, t, canDecide, onApprove, onReject }: ApprovalsTabProps) {
  const pendingApprovals = useMemo(() => approvals.filter((a) => a.status === OpportunityApprovalStatus.Pending), [approvals])
  const decidedApprovals = useMemo(() => approvals.filter((a) => a.status !== OpportunityApprovalStatus.Pending), [approvals])

  const pendingTotal = pendingApprovals.filter((approval) => canDecide(approval)).length

  return (
    <div className="space-y-4">
      <div className="flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-end">
        <div>
          <h2 className="text-xl font-semibold text-foreground">{t('opportunityDetail.approval.heading')}</h2>
          <p className="mt-0.5 text-sm text-muted-foreground">{t('opportunityDetail.approval.subtitle')}</p>
        </div>
      </div>

      {pendingTotal > 0 && (
        <div className="flex items-center gap-3 rounded-xl border border-amber-200 bg-gradient-to-br from-amber-50 to-amber-100 px-4 py-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-amber-500 text-white">
            <Clock className="h-4 w-4" />
          </div>
          <div className="flex-1">
            <p className="text-sm font-bold text-amber-900">{pendingTotal === 1 ? t('opportunityDetail.approval.waitingBanner.one') : t('opportunityDetail.approval.waitingBanner.many').replace('{0}', String(pendingTotal))}</p>
            <p className="mt-0.5 text-xs text-amber-800">{pendingTotal === 1 ? t('opportunityDetail.approval.waitingBannerSub.one') : t('opportunityDetail.approval.waitingBannerSub.many')}</p>
          </div>
        </div>
      )}

      {approvals.length === 0 ? (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center py-12 text-center text-muted-foreground">
            <CheckCircle className="mb-3 h-9 w-9 opacity-50" />
            <p className="text-sm font-medium">{t('opportunityDetail.approval.emptyTitle')}</p>
            <p className="mt-1 max-w-md text-xs">{t('opportunityDetail.approval.emptyHint')}</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {[...pendingApprovals, ...decidedApprovals].map((a) => (
            <ApprovalCard
              key={a.id}
              approval={a}
              actionLoading={actionLoading}
              canDecide={canDecide(a)}
              onApprove={() => void onApprove(a)}
              onReject={() => void onReject(a)}
            />
          ))}
        </div>
      )}
    </div>
  )
}

interface ApprovalCardProps {
  approval: OpportunityApprovalRequest
  actionLoading: boolean
  canDecide: boolean
  onApprove: () => void
  onReject: () => void
}

function ApprovalCard({ approval, actionLoading, canDecide, onApprove, onReject }: ApprovalCardProps) {
  const { t } = useI18n()
  const isPending = approval.status === OpportunityApprovalStatus.Pending
  const isApproved = approval.status === OpportunityApprovalStatus.Approved
  const isRejected = approval.status === OpportunityApprovalStatus.Rejected
  const isCancelled = approval.status === OpportunityApprovalStatus.Cancelled

  const config = isPending
    ? { stripBg: 'bg-amber-50', stripBorder: 'border-amber-200', stripText: 'text-amber-800', accent: 'border-amber-300', commentBorder: 'border-l-amber-500', icon: <Clock className="h-3.5 w-3.5" />, labelKey: 'opportunityDetail.approval.statusShort.pending' }
    : isApproved
      ? { stripBg: 'bg-emerald-50', stripBorder: 'border-emerald-200', stripText: 'text-emerald-800', accent: 'border-border', commentBorder: 'border-l-emerald-500', icon: <CheckCircle className="h-3.5 w-3.5" />, labelKey: 'opportunityDetail.approval.statusShort.approved' }
      : isRejected
        ? { stripBg: 'bg-rose-50', stripBorder: 'border-rose-200', stripText: 'text-rose-800', accent: 'border-border', commentBorder: 'border-l-rose-500', icon: <XCircle className="h-3.5 w-3.5" />, labelKey: 'opportunityDetail.approval.statusShort.rejected' }
        : { stripBg: 'bg-muted', stripBorder: 'border-border', stripText: 'text-muted-foreground', accent: 'border-border', commentBorder: 'border-l-slate-400', icon: <XCircle className="h-3.5 w-3.5" />, labelKey: 'opportunityDetail.approval.statusShort.cancelled' }

  const requestedHours = hoursSince(approval.requestedAt)

  return (
    <div className={`overflow-hidden rounded-xl border bg-card ${isPending ? 'border-amber-300 shadow-sm shadow-amber-100' : config.accent}`}>
      <div className={`flex items-center gap-2 border-b ${config.stripBorder} ${config.stripBg} px-5 py-2 text-[11.5px] font-bold uppercase tracking-wider ${config.stripText}`}>
        {config.icon}
        {t(config.labelKey)}
        <span className="ml-auto text-[11px] font-medium normal-case tracking-normal">
          {isPending ? (
            requestedHours < 1 ? t('opportunityDetail.approval.waitingLessThanHour') : t('opportunityDetail.approval.waitingHours').replace('{0}', String(requestedHours))
          ) : approval.decidedAt ? (
            t('opportunityDetail.approval.decidedOn').replace('{0}', formatDate(approval.decidedAt))
          ) : null}
        </span>
      </div>

      <div className="grid gap-5 px-5 py-4 md:grid-cols-[1fr_auto]">
        <div className="min-w-0">
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <strong className="text-base text-foreground">{approvalTypeLabelKey[approval.approvalType] ? t(approvalTypeLabelKey[approval.approvalType]) : t('opportunityDetail.approval.type.fallback')}</strong>
          </div>
          {approval.reason && (
            <p className="mb-3 text-sm leading-relaxed text-muted-foreground">{approval.reason}</p>
          )}

          {approval.proposalName && (
            <div className="mb-3 inline-flex items-center gap-2 rounded-lg border border-border bg-muted/30 px-3 py-1.5 text-xs">
              <FileText className="h-3.5 w-3.5 text-muted-foreground" />
              <span className="text-muted-foreground">{t('opportunityDetail.approval.linkedTo')}</span>
              <strong className="text-foreground">{approval.proposalName}</strong>
              {approval.proposalTotalValue != null && (
                <>
                  <span className="text-muted-foreground">·</span>
                  <span className="font-mono font-semibold text-foreground">{formatCurrency(approval.proposalTotalValue)}</span>
                </>
              )}
            </div>
          )}

          <div className="flex flex-wrap gap-x-6 gap-y-2 text-xs">
            <div>
              <p className="font-bold uppercase tracking-wider text-muted-foreground">{t('opportunityDetail.approval.requestedBy')}</p>
              <p className="mt-0.5 text-foreground">{approval.requestedByUserName}</p>
              <p className="text-[11px] text-muted-foreground">{t('opportunityDetail.approval.onDate').replace('{0}', formatDate(approval.requestedAt))}</p>
            </div>
            {!isPending && approval.approvedByUserName && (
              <div className="border-l border-border pl-6">
                <p className="font-bold uppercase tracking-wider text-muted-foreground">
                  {isApproved ? t('opportunityDetail.approval.approvedBy') : isRejected ? t('opportunityDetail.approval.rejectedBy') : t('opportunityDetail.approval.decidedBy')}
                </p>
                <p className={`mt-0.5 font-semibold ${isApproved ? 'text-emerald-700' : isRejected ? 'text-rose-700' : 'text-foreground'}`}>
                  {approval.approvedByUserName}
                </p>
                {approval.decidedAt && (
                  <p className="text-[11px] text-muted-foreground">{t('opportunityDetail.approval.onDate').replace('{0}', formatDate(approval.decidedAt))}</p>
                )}
              </div>
            )}
          </div>

          {approval.decisionNotes && (
            <div className={`mt-3 rounded-lg border border-l-4 ${config.commentBorder} bg-muted/30 px-3 py-2 text-sm italic text-foreground`}>
              "{approval.decisionNotes}"
            </div>
          )}
        </div>

        {isPending && !isCancelled && canDecide && (
          <div className="flex flex-row gap-2 md:flex-col md:gap-2">
            <Button size="sm" variant="outline-success" onClick={onApprove} disabled={actionLoading} className="justify-center md:w-44">
              <ThumbsUp className="mr-1.5 h-3.5 w-3.5" /> {t('opportunityDetail.approval.approveButton')}
            </Button>
            <Button size="sm" variant="outline-danger" onClick={onReject} disabled={actionLoading} className="justify-center md:w-44">
              <ThumbsDown className="mr-1.5 h-3.5 w-3.5" /> {t('opportunityDetail.approval.rejectButton')}
            </Button>
          </div>
        )}
      </div>
    </div>
  )
}

function FunnelStagesCard({ stages, currentStageId }: { stages: Array<{ id: number; name: string; color?: string; displayOrder?: number }>; currentStageId?: number }) {
  const { t } = useI18n()
  const currentIndex = stages.findIndex((s) => s.id === currentStageId)
  return (
    <div className="rounded-xl border border-border bg-card p-4">
      <div className="mb-3 text-[11px] font-bold uppercase tracking-wider text-muted-foreground">{t('opportunityDetail.funnel.title')}</div>
      <ol className="space-y-2">
        {stages.map((stage, index) => {
          const done = currentIndex >= 0 && index < currentIndex
          const isNow = stage.id === currentStageId
          return (
            <li key={stage.id} className="flex items-center gap-2.5">
              <span
                className={`flex h-5 w-5 shrink-0 items-center justify-center rounded-full border-2 text-[10px] font-bold ${
                  done
                    ? 'border-emerald-500 bg-emerald-500 text-white'
                    : isNow
                      ? 'border-primary bg-primary text-primary-foreground'
                      : 'border-border bg-card text-muted-foreground'
                }`}
                style={isNow && stage.color ? { borderColor: stage.color, backgroundColor: stage.color } : undefined}
              >
                {done ? '✓' : isNow ? '●' : ''}
              </span>
              <span className={`text-sm ${isNow ? 'font-semibold text-foreground' : done ? 'text-muted-foreground line-through' : 'text-muted-foreground'}`}>
                {stage.name}
              </span>
              {isNow && <span className="ml-auto text-[10px] font-semibold uppercase tracking-wider text-primary">{t('opportunityDetail.funnel.here')}</span>}
            </li>
          )
        })}
      </ol>
    </div>
  )
}
