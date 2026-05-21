import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { Button, Card, CardContent, CardHeader, CardTitle, DataTable, Dropdown, DropdownTrigger, DropdownContent, DropdownItem, DropdownLabel, DropdownSeparator, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useAuth, Badge, Tabs, TabsList, TabsTrigger, TabsContent, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Activity, ArrowRight, Building2, Calendar, CheckCircle, CircleDollarSign, Clock, Compass, FileText, History, MessageSquare, MoreHorizontal, Pencil, Plus, Tag, Tags, ThumbsDown, ThumbsUp, Trash2, TrendingUp, User, UserCheck, XCircle } from 'lucide-react'
import { commercialPipelineStageService } from '../../../services/commercialPipelineStageService'
import { opportunityService, OpportunityNegotiationStatus, OpportunityApprovalStatus, type OpportunityNegotiationStatusValue, type Opportunity, type OpportunityApprovalRequest, type OpportunityFollowUp, type OpportunityNegotiation } from '../../../services/opportunityService'
import OpportunityFormModal from '../../../components/modals/OpportunityFormModal'
import OpportunityNegotiationFormModal from '../../../components/modals/OpportunityNegotiationFormModal'
import OpportunityFollowUpFormModal from '../../../components/modals/OpportunityFollowUpFormModal'
import OpportunityApprovalRequestFormModal from '../../../components/modals/OpportunityApprovalRequestFormModal'
import OpportunityActivityTab from './OpportunityActivityTab'
import ProposalFormModal from '../../../components/modals/ProposalFormModal'
import { resolveAssetUrl } from '../../../lib/assetUrl'
import { formatDate } from '../../../lib/format'
import { formatCurrency } from '../../../lib/format'

const negotiationStatusKeys: Record<OpportunityNegotiationStatusValue, string> = {
  [OpportunityNegotiationStatus.Draft]: 'negotiation.status.draft',
  [OpportunityNegotiationStatus.PendingApproval]: 'negotiation.status.pendingApproval',
  [OpportunityNegotiationStatus.Approved]: 'negotiation.status.approved',
  [OpportunityNegotiationStatus.Rejected]: 'negotiation.status.rejected',
  [OpportunityNegotiationStatus.SentToClient]: 'negotiation.status.sentToClient',
  [OpportunityNegotiationStatus.AcceptedByClient]: 'negotiation.status.acceptedByClient',
  [OpportunityNegotiationStatus.Cancelled]: 'negotiation.status.cancelled',
}

export default function OpportunityDetail() {
  const { t } = useI18n()
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
  const [selectedNegotiation, setSelectedNegotiation] = useState<OpportunityNegotiation | null>(null)
  const [, setSelectedApprovalRequest] = useState<OpportunityApprovalRequest | null>(null)
  const [selectedFollowUp, setSelectedFollowUp] = useState<OpportunityFollowUp | null>(null)
  const [isOpportunityFormOpen, setIsOpportunityFormOpen] = useState(false)
  const [isNegotiationFormOpen, setIsNegotiationFormOpen] = useState(false)
  const [isApprovalRequestFormOpen, setIsApprovalRequestFormOpen] = useState(false)
  const [isFollowUpFormOpen, setIsFollowUpFormOpen] = useState(false)
  const [isProposalFormOpen, setIsProposalFormOpen] = useState(false)
  const [, setSelectedStage] = useState<string>('1')
  const [pendingFinalStage, setPendingFinalStage] = useState<{ id: number; name: string; kind: 'won' | 'lost' } | null>(null)
  const [finalNotes, setFinalNotes] = useState('')
  const [isStatusModalOpen, setIsStatusModalOpen] = useState(false)
  const [statusToSet, setStatusToSet] = useState<string>('')

  const { execute: fetchOpportunity, loading } = useApi<Opportunity | null>({ showErrorMessage: true })
  const { execute: executeAction, loading: actionLoading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadOpportunity = async () => {
    const result = await fetchOpportunity(() => opportunityService.getById(opportunityId))
    if (result) {
      setOpportunity(result)
      setSelectedStage(String(result.commercialPipelineStageId))
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

  const handleChangeStage = async (stageId: number) => {
    if (!opportunity) return
    const targetStage = stages.find((stage) => stage.id === stageId)

    if (targetStage && targetStage.finalBehavior === 1) {
      setPendingFinalStage({ id: targetStage.id, name: targetStage.name, kind: 'won' })
      setFinalNotes('')
      return
    }

    if (targetStage && targetStage.finalBehavior === 2) {
      setPendingFinalStage({ id: targetStage.id, name: targetStage.name, kind: 'lost' })
      setFinalNotes('')
      return
    }

    setSelectedStage(String(stageId))
    const result = await executeAction(() => opportunityService.changeStage(opportunity.id, { commercialPipelineStageId: stageId }))
    if (result !== null) await loadOpportunity()
  }

  const confirmFinalChange = async () => {
    if (!opportunity || !pendingFinalStage) return
    const trimmedNotes = finalNotes.trim()
    if (pendingFinalStage.kind === 'lost' && trimmedNotes.length === 0) return

    const result = await executeAction(() =>
      pendingFinalStage.kind === 'won'
        ? opportunityService.closeAsWon(opportunity.id, trimmedNotes ? { wonNotes: trimmedNotes } : {})
        : opportunityService.closeAsLost(opportunity.id, { lossReason: trimmedNotes }),
    )
    if (result !== null) {
      setPendingFinalStage(null)
      setFinalNotes('')
      await loadOpportunity()
    }
  }

  const cancelFinalChange = () => {
    setPendingFinalStage(null)
    setFinalNotes('')
    if (opportunity) setSelectedStage(String(opportunity.commercialPipelineStageId))
  }

  const submitStatusChange = async () => {
    if (!selectedNegotiation || !statusToSet) return
    const result = await executeAction(() => opportunityService.changeNegotiationStatus(selectedNegotiation.id, { status: Number(statusToSet) }))
    if (result !== null) {
      setIsStatusModalOpen(false)
      setSelectedNegotiation(null)
      await loadOpportunity()
    }
  }


  const approvalRequests = useMemo(() => opportunity?.negotiations.flatMap((item) => item.approvalRequests ?? []) ?? [], [opportunity])

  const handleCompleteFollowUp = async () => {
    if (!selectedFollowUp) return
    const result = await executeAction(() => opportunityService.completeFollowUp(selectedFollowUp.id))
    if (result !== null) await loadOpportunity()
  }

  const handleDeleteFollowUp = async () => {
    if (!selectedFollowUp) return
    const result = await executeAction(() => opportunityService.deleteFollowUp(selectedFollowUp.id))
    if (result !== null) {
      setSelectedFollowUp(null)
      await loadOpportunity()
    }
  }

  const pendingApprovalsCount = approvalRequests.filter((item) => item.status === OpportunityApprovalStatus.Pending).length
  const hasNegotiationPendingApproval = (opportunity?.negotiations ?? []).some((item) => item.status === OpportunityNegotiationStatus.PendingApproval)

  const sortedStages = useMemo(() => [...stages].sort((a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0)), [stages])
  const currentStageIndex = useMemo(() => sortedStages.findIndex((stage) => stage.id === opportunity?.commercialPipelineStage?.id), [sortedStages, opportunity])
  const nextStage = currentStageIndex >= 0 && currentStageIndex < sortedStages.length - 1 ? sortedStages[currentStageIndex + 1] : null
  const createdMeta = opportunity?.createdAt ? `Criada em ${formatDate(opportunity.createdAt)}` : null

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
                  <span
                    className="inline-flex items-center rounded px-2.5 py-1 text-[11px] font-bold uppercase tracking-wider text-white"
                    style={{ backgroundColor: opportunity?.commercialPipelineStage?.color || 'hsl(var(--primary))' }}
                  >
                    {opportunity?.commercialPipelineStage?.name || t('opportunityDetail.stage.none')}
                  </span>
                  {pendingApprovalsCount > 0 && (
                    <span className="inline-flex items-center gap-1 rounded border border-amber-300 bg-amber-100 px-2 py-0.5 text-[11px] font-bold uppercase tracking-wider text-amber-800">
                      <Clock className="h-3 w-3" /> {pendingApprovalsCount === 1 ? '1 aprovação pendente' : `${pendingApprovalsCount} aprovações pendentes`}
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
                      <span>Oportunidade #{opportunity.id}</span>
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
                      <span>por <strong className="text-foreground">{opportunity.commercialResponsible.name}</strong></span>
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
                    <DropdownLabel>Mover para estágio</DropdownLabel>
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
                        {stage.id === opportunity?.commercialPipelineStage?.id && <span className="ml-auto text-[10px] uppercase tracking-wider text-muted-foreground">atual</span>}
                      </DropdownItem>
                    ))}
                    <DropdownSeparator />
                    <DropdownItem onSelect={() => void loadOpportunity()}>
                      <History className="mr-2 h-3.5 w-3.5" /> Atualizar
                    </DropdownItem>
                  </DropdownContent>
                </Dropdown>
                <Button size="sm" variant="outline" onClick={() => setIsOpportunityFormOpen(true)}>
                  <Pencil className="mr-2 h-4 w-4" /> {t('common.action.edit')}
                </Button>
                {nextStage ? (
                  <Button size="sm" onClick={() => void handleChangeStage(nextStage.id)}>
                    Avançar para {nextStage.name}
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
            <TabsTrigger value="negotiations" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <MessageSquare className="h-4 w-4" /> {t('opportunityDetail.tab.negotiations')}
              {opportunity?.negotiations?.length ? (
                <span className="rounded-full bg-muted px-1.5 py-0.5 text-[11px] font-medium text-muted-foreground group-data-[state=active]:bg-primary/10 group-data-[state=active]:text-primary">
                  {opportunity.negotiations.length}
                </span>
              ) : null}
              {hasNegotiationPendingApproval && <span className="h-1.5 w-1.5 rounded-full bg-amber-500" aria-label="Negociação pendente de aprovação" />}
            </TabsTrigger>
            <TabsTrigger value="approvals" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <CheckCircle className="h-4 w-4" /> {t('opportunityDetail.tab.approvals')}
              {approvalRequests.length ? (
                <span className="rounded-full bg-muted px-1.5 py-0.5 text-[11px] font-medium text-muted-foreground group-data-[state=active]:bg-primary/10 group-data-[state=active]:text-primary">
                  {approvalRequests.length}
                </span>
              ) : null}
              {pendingApprovalsCount > 0 && <span className="h-1.5 w-1.5 rounded-full bg-destructive" aria-label={`${pendingApprovalsCount} aprovações pendentes`} />}
            </TabsTrigger>
            <TabsTrigger value="proposals" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <TrendingUp className="h-4 w-4" /> {t('opportunityDetail.tab.proposals')}
              {opportunity?.proposals?.length ? (
                <span className="rounded-full bg-muted px-1.5 py-0.5 text-[11px] font-medium text-muted-foreground group-data-[state=active]:bg-primary/10 group-data-[state=active]:text-primary">
                  {opportunity.proposals.length}
                </span>
              ) : null}
            </TabsTrigger>
            <TabsTrigger value="followups" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <Clock className="h-4 w-4" /> {t('opportunityDetail.tab.followups')}
              {opportunity?.followUps?.length ? (
                <span className="rounded-full bg-muted px-1.5 py-0.5 text-[11px] font-medium text-muted-foreground group-data-[state=active]:bg-primary/10 group-data-[state=active]:text-primary">
                  {opportunity.followUps.length}
                </span>
              ) : null}
              {overdueFollowUpsCount > 0 && <span className="h-1.5 w-1.5 rounded-full bg-amber-500" aria-label={`${overdueFollowUpsCount} follow-ups vencidos`} />}
            </TabsTrigger>
            <TabsTrigger value="activity" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <Activity className="h-4 w-4" /> {t('opportunityDetail.tab.activity')}
            </TabsTrigger>
          </TabsList>

          <TabsContent value="summary" className="mt-0">
            <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
              <div className="space-y-5">
                {pendingApprovalsCount > 0 && (
                  <div className="flex items-center gap-5 rounded-xl bg-gradient-to-br from-primary to-primary/80 p-6 text-primary-foreground shadow-lg shadow-primary/20">
                    <div className="flex h-14 w-14 shrink-0 items-center justify-center rounded-xl bg-white/15">
                      <ThumbsUp className="h-7 w-7" />
                    </div>
                    <div className="flex-1">
                      <div className="text-[11px] font-bold uppercase tracking-widest opacity-80">Próximo passo</div>
                      <div className="mt-1 text-lg font-bold leading-snug">
                        {pendingApprovalsCount === 1
                          ? 'Há uma aprovação esperando sua decisão'
                          : `Há ${pendingApprovalsCount} aprovações esperando sua decisão`}
                      </div>
                      <p className="mt-0.5 text-sm opacity-90">
                        Negociação{pendingApprovalsCount === 1 ? '' : 'ões'} pausada{pendingApprovalsCount === 1 ? '' : 's'} até você responder.
                      </p>
                    </div>
                    <Button
                      size="sm"
                      variant="outline"
                      className="border-white/30 bg-white text-primary hover:bg-white/95"
                      onClick={() => handleTabChange('approvals')}
                    >
                      Revisar agora <ArrowRight className="ml-1.5 h-3.5 w-3.5" />
                    </Button>
                  </div>
                )}

                <div className="grid grid-cols-2 gap-0 overflow-hidden rounded-xl border border-border bg-card md:grid-cols-4">
                  <KpiCell
                    icon={<CircleDollarSign className="h-3.5 w-3.5 text-indigo-600" />}
                    label={t('opportunityDetail.kpi.estimatedValue')}
                    value={formatCurrency(opportunity?.estimatedValue ?? 0)}
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
                    <h3 className="mb-3 text-base font-semibold text-foreground">Próximo passo</h3>
                    <div className={`flex items-center gap-4 rounded-xl border p-4 ${isNextFollowUpOverdue ? 'border-amber-300 bg-amber-50' : 'border-border bg-card'}`}>
                      <div className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-lg ${isNextFollowUpOverdue ? 'bg-amber-100 text-amber-700' : 'bg-primary/10 text-primary'}`}>
                        <Calendar className="h-5 w-5" />
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-semibold text-foreground">{nextFollowUp.subject}</p>
                        <p className={`mt-0.5 text-xs ${isNextFollowUpOverdue ? 'text-amber-800' : 'text-muted-foreground'}`}>
                          {isNextFollowUpOverdue ? 'Venceu em ' : 'Agendado para '}
                          <strong>{formatDate(nextFollowUp.dueAt)}</strong>
                        </p>
                        {nextFollowUp.notes && (
                          <p className="mt-1 line-clamp-1 text-xs italic text-muted-foreground">"{nextFollowUp.notes}"</p>
                        )}
                      </div>
                      <div className="flex flex-shrink-0 gap-1.5">
                        <Button size="sm" variant="outline" onClick={() => { setSelectedFollowUp(nextFollowUp); setIsFollowUpFormOpen(true) }}>
                          Reagendar
                        </Button>
                        <Button size="sm" onClick={() => void handleCompleteNextFollowUp()}>
                          <CheckCircle className="mr-1.5 h-3.5 w-3.5" /> Concluir
                        </Button>
                      </div>
                    </div>
                  </div>
                )}

                <div>
                  <h3 className="mb-3 text-base font-semibold text-foreground">Andamento</h3>
                  <div className="grid gap-3 md:grid-cols-3">
                    <SubflowCard
                      icon={<MessageSquare className="h-4 w-4" />}
                      iconClassName="bg-purple-100 text-purple-700"
                      label="Negociações"
                      count={opportunity?.negotiations?.length ?? 0}
                      statusLabel={hasNegotiationPendingApproval
                        ? `${(opportunity?.negotiations ?? []).filter((n) => n.status === OpportunityNegotiationStatus.PendingApproval).length} pendente${(opportunity?.negotiations ?? []).filter((n) => n.status === OpportunityNegotiationStatus.PendingApproval).length === 1 ? '' : 's'} aprovação`
                        : 'sem pendências'}
                      statusTone={hasNegotiationPendingApproval ? 'amber' : 'muted'}
                      onClick={() => handleTabChange('negotiations')}
                    />
                    <SubflowCard
                      icon={<CheckCircle className="h-4 w-4" />}
                      iconClassName="bg-emerald-100 text-emerald-700"
                      label="Aprovações"
                      count={approvalRequests.length}
                      statusLabel={pendingApprovalsCount > 0
                        ? `${pendingApprovalsCount} esperando você`
                        : 'sem pendências'}
                      statusTone={pendingApprovalsCount > 0 ? 'red' : 'muted'}
                      onClick={() => handleTabChange('approvals')}
                    />
                    <SubflowCard
                      icon={<TrendingUp className="h-4 w-4" />}
                      iconClassName="bg-cyan-100 text-cyan-700"
                      label="Propostas"
                      count={opportunity?.proposals?.length ?? 0}
                      statusLabel={(opportunity?.proposals ?? []).length > 0
                        ? `${(opportunity?.proposals ?? []).length} versão${(opportunity?.proposals ?? []).length === 1 ? '' : 'ões'}`
                        : 'nenhuma enviada'}
                      statusTone="muted"
                      onClick={() => handleTabChange('proposals')}
                    />
                  </div>
                </div>

                <div>
                  <h3 className="mb-3 text-base font-semibold text-foreground">Detalhes</h3>
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
                    <User className="h-3.5 w-3.5" /> Responsável
                  </div>
                  {opportunity?.commercialResponsible?.name ? (
                    <div className="flex items-center gap-3">
                      <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-xs font-bold uppercase text-primary">
                        {opportunity.commercialResponsible.name.split(' ').slice(0, 2).map((s) => s[0]).join('')}
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-semibold text-foreground">{opportunity.commercialResponsible.name}</p>
                        <p className="text-xs text-muted-foreground">Agência · Comercial</p>
                      </div>
                    </div>
                  ) : (
                    <p className="text-xs text-muted-foreground">Sem responsável atribuído.</p>
                  )}
                </div>

                <div className="rounded-xl border border-border bg-card p-4">
                  <div className="mb-3 flex items-center justify-between">
                    <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                      <Clock className="h-3.5 w-3.5" /> Follow-ups
                    </div>
                    <Button size="sm" variant="ghost" className="h-7 px-2 text-xs" onClick={() => setIsFollowUpFormOpen(true)}>
                      <Plus className="mr-1 h-3 w-3" /> Novo
                    </Button>
                  </div>
                  {pendingFollowUpsCount === 0 ? (
                    <p className="text-xs text-muted-foreground">Sem follow-ups agendados. Bom momento para combinar o próximo passo.</p>
                  ) : overdueFollowUpsCount > 0 ? (
                    <div className="rounded-lg border border-amber-300 bg-amber-50 px-3 py-2 text-xs text-amber-800">
                      <strong>{overdueFollowUpsCount}</strong> follow-up{overdueFollowUpsCount === 1 ? '' : 's'} vencido{overdueFollowUpsCount === 1 ? '' : 's'} esperando ação.
                    </div>
                  ) : (
                    <p className="text-xs text-muted-foreground">{pendingFollowUpsCount} follow-up{pendingFollowUpsCount === 1 ? '' : 's'} agendado{pendingFollowUpsCount === 1 ? '' : 's'} no prazo.</p>
                  )}
                </div>
              </aside>
            </div>
          </TabsContent>

          <TabsContent value="proposals" className="mt-0">
            <ProposalsTab
              proposals={opportunity?.proposals ?? []}
              opportunityCreatedAt={opportunity?.createdAt}
              loading={loading}
              onNew={() => setIsProposalFormOpen(true)}
              onOpen={(id) => navigate(`/comercial/propostas/${id}`, {
                state: opportunity
                  ? { from: 'opportunity', opportunityId: opportunity.id, opportunityName: opportunity.name, tab: 'proposals' }
                  : undefined,
              })}
            />
          </TabsContent>

          <TabsContent value="negotiations" className="mt-0">
            <NegotiationsTab
              negotiations={opportunity?.negotiations ?? []}
              loading={loading}
              actionLoading={actionLoading}
              onNew={() => { setSelectedNegotiation(null); setIsNegotiationFormOpen(true) }}
              onEdit={(item) => { setSelectedNegotiation(item); setIsNegotiationFormOpen(true) }}
              onDelete={async (item) => {
                const result = await executeAction(() => opportunityService.deleteNegotiation(item.id))
                if (result !== null) {
                  setSelectedNegotiation(null)
                  await loadOpportunity()
                }
              }}
              onChangeStatus={(item) => {
                setSelectedNegotiation(item)
                setStatusToSet(String(item.status))
                setIsStatusModalOpen(true)
              }}
              onRequestApproval={(item) => { setSelectedNegotiation(item); setIsApprovalRequestFormOpen(true) }}
            />
          </TabsContent>

          <TabsContent value="approvals" className="mt-0">
            <ApprovalsTab
              approvals={approvalRequests}
              negotiations={opportunity?.negotiations ?? []}
              loading={loading}
              actionLoading={actionLoading}
              t={t}
              onApprove={async (item) => {
                setSelectedApprovalRequest(item)
                const result = await executeAction(() => opportunityService.approveRequest(item.id, {
                  approvedByUserName: t('opportunityDetail.approvals.userFallback'),
                  decisionNotes: t('opportunityDetail.approvals.decision.approved'),
                }))
                if (result !== null) {
                  setSelectedApprovalRequest(null)
                  await loadOpportunity()
                }
              }}
              onReject={async (item) => {
                setSelectedApprovalRequest(item)
                const result = await executeAction(() => opportunityService.rejectRequest(item.id, {
                  approvedByUserName: t('opportunityDetail.approvals.userFallback'),
                  decisionNotes: t('opportunityDetail.approvals.decision.rejected'),
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
                  <Button size="sm" variant="ghost" onClick={() => void handleDeleteFollowUp()} disabled={!selectedFollowUp || actionLoading}>
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

      <OpportunityNegotiationFormModal
        open={isNegotiationFormOpen}
        onOpenChange={setIsNegotiationFormOpen}
        opportunityId={opportunityId}
        negotiation={selectedNegotiation}
        onSuccess={() => {
          setIsNegotiationFormOpen(false)
          setSelectedNegotiation(null)
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

      <OpportunityApprovalRequestFormModal
        open={isApprovalRequestFormOpen}
        onOpenChange={setIsApprovalRequestFormOpen}
        negotiation={selectedNegotiation}
        onSuccess={() => {
          setIsApprovalRequestFormOpen(false)
          void loadOpportunity()
        }}
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
            navigate(`/comercial/propostas/${created.id}`)
          }
        }}
      />

      <Modal open={isStatusModalOpen} onOpenChange={(open) => { if (!open) setIsStatusModalOpen(false) }}>
        <ModalContent size="form">
          <ModalHeader>
            <ModalTitle>{t('opportunityDetail.negotiations.changeStatus')}</ModalTitle>
          </ModalHeader>
          <div className="space-y-3">
            <p className="text-sm text-muted-foreground">{t('opportunityDetail.negotiations.changeStatus.description')}</p>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.status')}</label>
              <Select value={statusToSet} onValueChange={setStatusToSet}>
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(negotiationStatusKeys).map(([value, key]) => (
                    <SelectItem key={value} value={value}>{t(key)}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => setIsStatusModalOpen(false)} disabled={actionLoading}>{t('common.action.cancel')}</Button>
            <Button type="button" variant="primary" onClick={() => void submitStatusChange()} disabled={actionLoading || !statusToSet || Number(statusToSet) === selectedNegotiation?.status}>
              {actionLoading ? t('common.action.saving') : t('common.action.save')}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>

      <Modal open={!!pendingFinalStage} onOpenChange={(open) => { if (!open) cancelFinalChange() }}>
        <ModalContent size="form">
          <ModalHeader>
            <ModalTitle>
              {pendingFinalStage?.kind === 'won' ? 'Encerrar como ganha' : 'Encerrar como perdida'}
            </ModalTitle>
          </ModalHeader>
          <div className="space-y-3">
            <p className="text-sm text-muted-foreground">
              Você está movendo a oportunidade para <strong>{pendingFinalStage?.name}</strong>.
              {pendingFinalStage?.kind === 'lost' ? ' Informe o motivo da perda.' : ' Deixe uma observação se quiser.'}
            </p>
            <div className="space-y-2">
              <label className="text-sm font-medium">
                {pendingFinalStage?.kind === 'won' ? 'Notas de fechamento (opcional)' : 'Motivo da perda'}
                {pendingFinalStage?.kind === 'lost' && <span className="text-destructive"> *</span>}
              </label>
              <textarea
                className="min-h-[100px] w-full rounded-md border bg-background p-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                value={finalNotes}
                onChange={(e) => setFinalNotes(e.target.value)}
                placeholder={pendingFinalStage?.kind === 'won' ? 'Ex.: Cliente aprovou orçamento completo.' : 'Ex.: Cliente escolheu concorrente.'}
                autoFocus
              />
            </div>
          </div>
          <ModalFooter>
            <Button type="button" variant="outline" onClick={cancelFinalChange} disabled={actionLoading}>Cancelar</Button>
            <Button
              type="button"
              variant={pendingFinalStage?.kind === 'lost' ? 'danger' : 'primary'}
              onClick={() => void confirmFinalChange()}
              disabled={actionLoading || (pendingFinalStage?.kind === 'lost' && finalNotes.trim().length === 0)}
            >
              {actionLoading ? 'Salvando...' : 'Confirmar'}
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

type NegotiationFilter = 'all' | 'pendingApproval' | 'approved' | 'draft' | 'sent' | 'closed'

interface NegotiationsTabProps {
  negotiations: OpportunityNegotiation[]
  loading: boolean
  actionLoading: boolean
  onNew: () => void
  onEdit: (item: OpportunityNegotiation) => void
  onDelete: (item: OpportunityNegotiation) => Promise<void> | void
  onChangeStatus: (item: OpportunityNegotiation) => void
  onRequestApproval: (item: OpportunityNegotiation) => void
}

function NegotiationsTab({ negotiations, loading, actionLoading, onNew, onEdit, onDelete, onChangeStatus, onRequestApproval }: NegotiationsTabProps) {
  const [filter, setFilter] = useState<NegotiationFilter>('all')

  const counts = useMemo(() => ({
    all: negotiations.length,
    pendingApproval: negotiations.filter((n) => n.status === OpportunityNegotiationStatus.PendingApproval).length,
    approved: negotiations.filter((n) => n.status === OpportunityNegotiationStatus.Approved).length,
    draft: negotiations.filter((n) => n.status === OpportunityNegotiationStatus.Draft).length,
    sent: negotiations.filter((n) => n.status === OpportunityNegotiationStatus.SentToClient).length,
    closed: negotiations.filter((n) => n.status === OpportunityNegotiationStatus.AcceptedByClient || n.status === OpportunityNegotiationStatus.Rejected || n.status === OpportunityNegotiationStatus.Cancelled).length,
  }), [negotiations])

  const filtered = useMemo(() => {
    if (filter === 'all') return negotiations
    if (filter === 'pendingApproval') return negotiations.filter((n) => n.status === OpportunityNegotiationStatus.PendingApproval)
    if (filter === 'approved') return negotiations.filter((n) => n.status === OpportunityNegotiationStatus.Approved)
    if (filter === 'draft') return negotiations.filter((n) => n.status === OpportunityNegotiationStatus.Draft)
    if (filter === 'sent') return negotiations.filter((n) => n.status === OpportunityNegotiationStatus.SentToClient)
    return negotiations.filter((n) => n.status === OpportunityNegotiationStatus.AcceptedByClient || n.status === OpportunityNegotiationStatus.Rejected || n.status === OpportunityNegotiationStatus.Cancelled)
  }, [negotiations, filter])

  const sorted = useMemo(() => [...filtered].sort((a, b) => new Date(b.negotiatedAt).getTime() - new Date(a.negotiatedAt).getTime()), [filtered])
  const firstNegotiationId = useMemo(() => {
    if (negotiations.length === 0) return null
    return [...negotiations].sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())[0]?.id ?? null
  }, [negotiations])

  const pendingTopBanner = counts.pendingApproval > 0

  return (
    <div className="space-y-4">
      <div className="flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-end">
        <div>
          <h2 className="text-xl font-semibold text-foreground">Negociações</h2>
          <p className="mt-0.5 text-sm text-muted-foreground">Cada negociação com valor, motivo e desfecho. {counts.all} {counts.all === 1 ? 'registrada' : 'registradas'}.</p>
        </div>
        <Button size="sm" onClick={onNew}>
          <Plus className="mr-1.5 h-4 w-4" /> Nova negociação
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <FilterChip label="Todas" count={counts.all} active={filter === 'all'} onClick={() => setFilter('all')} />
        <FilterChip label="Pendente aprovação" count={counts.pendingApproval} active={filter === 'pendingApproval'} dotColor="amber" onClick={() => setFilter('pendingApproval')} />
        <FilterChip label="Aprovadas" count={counts.approved} active={filter === 'approved'} onClick={() => setFilter('approved')} />
        <FilterChip label="Rascunhos" count={counts.draft} active={filter === 'draft'} onClick={() => setFilter('draft')} />
        <FilterChip label="Enviadas" count={counts.sent} active={filter === 'sent'} onClick={() => setFilter('sent')} />
        <FilterChip label="Fechadas" count={counts.closed} active={filter === 'closed'} onClick={() => setFilter('closed')} />
      </div>

      {pendingTopBanner && (
        <div className="flex items-center gap-3 rounded-xl border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-900">
          <Clock className="h-4 w-4 shrink-0" />
          <span className="flex-1">
            <strong>{counts.pendingApproval}</strong> negociaç{counts.pendingApproval === 1 ? 'ão esperando' : 'ões esperando'} aprovação interna.
          </span>
        </div>
      )}

      {loading && negotiations.length === 0 ? (
        <Card className="border-dashed">
          <CardContent className="flex items-center justify-center py-12 text-sm text-muted-foreground">Carregando…</CardContent>
        </Card>
      ) : sorted.length === 0 ? (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center py-12 text-center text-muted-foreground">
            <MessageSquare className="mb-3 h-9 w-9 opacity-50" />
            <p className="text-sm font-medium">{filter === 'all' ? 'Nenhuma negociação registrada' : 'Nenhuma negociação neste filtro'}</p>
            {filter === 'all' && (
              <Button size="sm" variant="outline" className="mt-3" onClick={onNew}>
                <Plus className="mr-1.5 h-4 w-4" /> Registrar primeira negociação
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {sorted.map((n) => (
            <NegotiationCard
              key={n.id}
              negotiation={n}
              isFirstRound={n.id === firstNegotiationId}
              actionLoading={actionLoading}
              onEdit={() => onEdit(n)}
              onDelete={() => void onDelete(n)}
              onChangeStatus={() => onChangeStatus(n)}
              onRequestApproval={() => onRequestApproval(n)}
            />
          ))}
        </div>
      )}
    </div>
  )
}

function FilterChip({ label, count, active, dotColor, onClick }: { label: string; count: number; active: boolean; dotColor?: 'amber' | 'green' | 'muted'; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        'inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-semibold transition-colors',
        active ? 'border-foreground bg-foreground text-background' : 'border-border bg-card text-foreground hover:border-foreground/40',
      ].join(' ')}
    >
      {dotColor && (
        <span className={`h-1.5 w-1.5 rounded-full ${dotColor === 'amber' ? 'bg-amber-500' : dotColor === 'green' ? 'bg-emerald-500' : 'bg-muted-foreground'}`} />
      )}
      {label}
      <span className={`rounded-full px-1.5 py-0.5 text-[10px] font-bold ${active ? 'bg-white/20 text-background' : 'bg-muted text-muted-foreground'}`}>
        {count}
      </span>
    </button>
  )
}

interface NegotiationCardProps {
  negotiation: OpportunityNegotiation
  isFirstRound: boolean
  actionLoading: boolean
  onEdit: () => void
  onDelete: () => void
  onChangeStatus: () => void
  onRequestApproval: () => void
}

function NegotiationCard({ negotiation, isFirstRound, actionLoading, onEdit, onDelete, onChangeStatus, onRequestApproval }: NegotiationCardProps) {
  const isPending = negotiation.status === OpportunityNegotiationStatus.PendingApproval
  const isDraft = negotiation.status === OpportunityNegotiationStatus.Draft
  const isApproved = negotiation.status === OpportunityNegotiationStatus.Approved
  const isSent = negotiation.status === OpportunityNegotiationStatus.SentToClient
  const isRejected = negotiation.status === OpportunityNegotiationStatus.Rejected
  const isAccepted = negotiation.status === OpportunityNegotiationStatus.AcceptedByClient
  const isCancelled = negotiation.status === OpportunityNegotiationStatus.Cancelled

  const statusStyle = (() => {
    if (isPending) return { bg: 'bg-amber-500', text: 'text-white', label: 'Pendente aprovação' }
    if (isApproved) return { bg: 'bg-emerald-600', text: 'text-white', label: 'Aprovada' }
    if (isDraft) return { bg: 'bg-muted', text: 'text-muted-foreground', label: 'Rascunho' }
    if (isSent) return { bg: 'bg-blue-600', text: 'text-white', label: 'Enviada ao cliente' }
    if (isAccepted) return { bg: 'bg-emerald-700', text: 'text-white', label: 'Aceita pelo cliente' }
    if (isRejected) return { bg: 'bg-rose-600', text: 'text-white', label: 'Rejeitada' }
    if (isCancelled) return { bg: 'bg-slate-400', text: 'text-white', label: 'Cancelada' }
    return { bg: 'bg-slate-400', text: 'text-white', label: 'Outro' }
  })()

  return (
    <div className={`relative overflow-hidden rounded-xl border bg-card ${isPending ? 'border-amber-300 shadow-sm shadow-amber-100' : 'border-border'}`}>
      {isPending && <div className="absolute inset-x-0 top-0 h-0.5 bg-gradient-to-r from-amber-500 to-amber-300" />}
      <div className="px-5 py-4">
        <div className="flex items-start justify-between gap-4">
          <div className="min-w-0 flex-1">
            <div className="mb-1.5 flex flex-wrap items-center gap-2">
              <strong className="text-base text-foreground">{negotiation.title}</strong>
              <span className={`inline-flex items-center rounded px-2 py-0.5 text-[10.5px] font-bold uppercase tracking-wider ${statusStyle.bg} ${statusStyle.text}`}>
                {statusStyle.label}
              </span>
              {isFirstRound && (
                <span className="inline-flex items-center rounded bg-muted px-2 py-0.5 text-[10.5px] font-bold uppercase tracking-wider text-muted-foreground">
                  Primeira
                </span>
              )}
            </div>
            <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
              <span className="font-mono text-2xl font-bold tracking-tight text-foreground">{formatCurrency(negotiation.amount)}</span>
              <span className="text-xs text-muted-foreground">· {formatDate(negotiation.negotiatedAt)}</span>
            </div>
            {negotiation.notes && (
              <p className="mt-2 text-sm leading-relaxed text-muted-foreground">{negotiation.notes}</p>
            )}
          </div>
        </div>

        <div className="mt-4 flex flex-wrap items-center gap-2 border-t border-border/60 pt-3">
          {isPending && (
            <>
              <span className="mr-auto flex items-center gap-1.5 text-xs font-semibold text-amber-700">
                <Clock className="h-3.5 w-3.5" /> Aguardando aprovação interna
              </span>
              <Button size="sm" variant="outline" onClick={onChangeStatus} disabled={actionLoading}>
                <Activity className="mr-1.5 h-3.5 w-3.5" /> Alterar status
              </Button>
            </>
          )}
          {isDraft && (
            <>
              <span className="mr-auto text-xs text-muted-foreground">Rascunho — não enviada</span>
              <Button size="sm" variant="outline" onClick={onEdit}>
                <Pencil className="mr-1.5 h-3.5 w-3.5" /> Editar
              </Button>
              <Button size="sm" onClick={onRequestApproval}>
                <CheckCircle className="mr-1.5 h-3.5 w-3.5" /> Solicitar aprovação
              </Button>
            </>
          )}
          {isApproved && (
            <>
              <span className="mr-auto flex items-center gap-1.5 text-xs font-semibold text-emerald-700">
                <CheckCircle className="h-3.5 w-3.5" /> Aprovada internamente
              </span>
              <Button size="sm" variant="outline" onClick={onChangeStatus} disabled={actionLoading}>
                <Activity className="mr-1.5 h-3.5 w-3.5" /> Alterar status
              </Button>
            </>
          )}
          {(isSent || isAccepted || isRejected || isCancelled) && (
            <>
              <span className="mr-auto text-xs text-muted-foreground">{statusStyle.label}</span>
              <Button size="sm" variant="outline" onClick={onChangeStatus} disabled={actionLoading}>
                <Activity className="mr-1.5 h-3.5 w-3.5" /> Alterar status
              </Button>
            </>
          )}
          {(isDraft || isCancelled) && (
            <Button size="sm" variant="ghost" onClick={onDelete} disabled={actionLoading} className="text-muted-foreground hover:text-destructive">
              <Trash2 className="h-3.5 w-3.5" />
            </Button>
          )}
        </div>
      </div>
    </div>
  )
}

type OpportunityProposalReference = NonNullable<Opportunity['proposals']>[number]

interface ProposalsTabProps {
  proposals: OpportunityProposalReference[]
  opportunityCreatedAt?: string
  loading: boolean
  onNew: () => void
  onOpen: (id: number) => void
}

const proposalStatusInline: Record<number, { label: string; bg: string; text: string; dot: string }> = {
  1: { label: 'Rascunho', bg: 'bg-slate-200', text: 'text-slate-700', dot: 'bg-slate-400' },
  2: { label: 'Enviada', bg: 'bg-amber-100', text: 'text-amber-800', dot: 'bg-amber-500' },
  3: { label: 'Visualizada', bg: 'bg-blue-100', text: 'text-blue-800', dot: 'bg-blue-500' },
  4: { label: 'Aprovada', bg: 'bg-emerald-100', text: 'text-emerald-800', dot: 'bg-emerald-500' },
  5: { label: 'Rejeitada', bg: 'bg-rose-100', text: 'text-rose-800', dot: 'bg-rose-500' },
  6: { label: 'Convertida', bg: 'bg-emerald-100', text: 'text-emerald-800', dot: 'bg-emerald-500' },
  7: { label: 'Expirada', bg: 'bg-rose-100', text: 'text-rose-800', dot: 'bg-rose-500' },
  8: { label: 'Cancelada', bg: 'bg-slate-200', text: 'text-slate-700', dot: 'bg-slate-400' },
}

function ProposalsTab({ proposals, opportunityCreatedAt, loading, onNew, onOpen }: ProposalsTabProps) {
  const ordered = useMemo(() => [...proposals].sort((a, b) => a.id - b.id), [proposals])
  const total = ordered.length
  const currentProposal = ordered[ordered.length - 1] ?? null
  const approved = ordered.filter((p) => p.status === 4 || p.status === 6).length
  const sent = ordered.filter((p) => p.status === 2 || p.status === 3).length
  const baselineValue = ordered[0]?.totalValue ?? 0
  const lastValue = currentProposal?.totalValue ?? 0
  const valueDelta = lastValue - baselineValue
  const sortedDesc = useMemo(() => [...ordered].reverse(), [ordered])

  if (loading && total === 0) {
    return (
      <Card className="border-dashed">
        <CardContent className="flex items-center justify-center py-12 text-sm text-muted-foreground">Carregando…</CardContent>
      </Card>
    )
  }

  if (total === 0) {
    return (
      <Card className="border-dashed">
        <CardContent className="flex flex-col items-center justify-center py-12 text-center text-muted-foreground">
          <TrendingUp className="mb-3 h-9 w-9 opacity-50" />
          <p className="text-sm font-medium">Nenhuma proposta criada</p>
          <p className="mt-1 max-w-md text-xs">Crie a primeira proposta pra começar o histórico. Cada nova versão entra como um nó da timeline.</p>
          <Button size="sm" className="mt-4" onClick={onNew}>
            <Plus className="mr-1.5 h-4 w-4" /> Nova proposta
          </Button>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-end">
        <div>
          <h2 className="text-xl font-semibold text-foreground">Histórico de propostas</h2>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {total} {total === 1 ? 'versão criada' : 'versões criadas'} · {approved} aprovada{approved === 1 ? '' : 's'} · {sent} aguardando cliente
          </p>
        </div>
        <Button size="sm" onClick={onNew}>
          <Plus className="mr-1.5 h-4 w-4" /> Nova proposta
        </Button>
      </div>

      <div className="grid grid-cols-2 gap-0 overflow-hidden rounded-xl border border-border bg-card md:grid-cols-4">
        <ProposalSummaryCell label="Versões criadas" value={String(total)} sub={total === 1 ? 'só a inicial' : 'evolução de oferta'} />
        <ProposalSummaryCell
          label={`Diferencial v1 → v${total}`}
          value={total > 1 ? (valueDelta >= 0 ? `+${formatCurrency(valueDelta)}` : formatCurrency(valueDelta)) : '—'}
          sub={total > 1 ? 'comparado à primeira oferta' : 'apenas uma versão'}
          valueColor={total > 1 ? (valueDelta > 0 ? 'text-emerald-700' : valueDelta < 0 ? 'text-rose-700' : 'text-foreground') : 'text-muted-foreground'}
        />
        <ProposalSummaryCell label="Valor da versão atual" value={formatCurrency(lastValue)} sub={currentProposal?.name} mono />
        <ProposalSummaryCell
          label="Status atual"
          value={currentProposal ? (proposalStatusInline[currentProposal.status]?.label ?? '—') : '—'}
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
                onOpen={() => onOpen(proposal.id)}
              />
            )
          })}
          {opportunityCreatedAt && (
            <div className="relative flex items-center gap-4 pt-2">
              <div className="z-10 flex h-10 w-10 shrink-0 items-center justify-center rounded-full border-2 border-dashed border-muted-foreground/40 bg-card text-muted-foreground">
                <Compass className="h-4 w-4" />
              </div>
              <div className="text-sm text-muted-foreground">
                <strong className="text-foreground">Oportunidade criada</strong> em {formatDate(opportunityCreatedAt)}
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
                  {status.label}
                </span>
              )}
              {isCurrent && !isFinalApproved && (
                <span className="inline-flex items-center rounded bg-primary/10 px-2 py-0.5 text-[10.5px] font-bold uppercase tracking-wider text-primary">
                  Versão atual
                </span>
              )}
            </div>
            <div className="mt-2 flex flex-wrap items-baseline gap-x-3 gap-y-1 text-sm text-muted-foreground">
              <span className="font-mono text-lg font-bold text-foreground">{formatCurrency(proposal.totalValue)}</span>
              {proposal.validityUntil && (
                <>
                  <span>·</span>
                  <span>Validade {formatDate(proposal.validityUntil)}</span>
                </>
              )}
              {proposal.campaignId && (
                <>
                  <span>·</span>
                  <span className="inline-flex items-center gap-1 text-emerald-700">
                    <CheckCircle className="h-3.5 w-3.5" /> Convertida em campanha
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
  negotiations: OpportunityNegotiation[]
  loading: boolean
  actionLoading: boolean
  t: (key: string) => string
  onApprove: (item: OpportunityApprovalRequest) => Promise<void> | void
  onReject: (item: OpportunityApprovalRequest) => Promise<void> | void
}

const approvalTypeLabel: Record<number, string> = {
  1: 'Exceção de desconto',
  2: 'Exceção de margem',
  3: 'Exceção de prazo',
  4: 'Exceção (outra)',
}

function hoursSince(iso: string): number {
  const diff = Date.now() - new Date(iso).getTime()
  return Math.max(0, Math.floor(diff / (1000 * 60 * 60)))
}

function ApprovalsTab({ approvals, negotiations, loading, actionLoading, t: _t, onApprove, onReject }: ApprovalsTabProps) {
  const pendingApprovals = useMemo(() => approvals.filter((a) => a.status === OpportunityApprovalStatus.Pending), [approvals])
  const decidedApprovals = useMemo(() => approvals.filter((a) => a.status !== OpportunityApprovalStatus.Pending), [approvals])

  const negotiationsById = useMemo(() => {
    const map = new Map<number, OpportunityNegotiation>()
    negotiations.forEach((n) => map.set(n.id, n))
    return map
  }, [negotiations])

  const pendingTotal = pendingApprovals.length

  return (
    <div className="space-y-4">
      <div className="flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-end">
        <div>
          <h2 className="text-xl font-semibold text-foreground">Aprovações internas</h2>
          <p className="mt-0.5 text-sm text-muted-foreground">Solicitações de exceção (margem, prazo, desconto) ligadas à negociação que motivou.</p>
        </div>
      </div>

      {pendingTotal > 0 && (
        <div className="flex items-center gap-3 rounded-xl border border-amber-200 bg-gradient-to-br from-amber-50 to-amber-100 px-4 py-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-amber-500 text-white">
            <Clock className="h-4 w-4" />
          </div>
          <div className="flex-1">
            <p className="text-sm font-bold text-amber-900">{pendingTotal === 1 ? '1 aprovação esperando você' : `${pendingTotal} aprovações esperando você`}</p>
            <p className="mt-0.5 text-xs text-amber-800">Negociaç{pendingTotal === 1 ? 'ão pausada' : 'ões pausadas'} até sua decisão.</p>
          </div>
        </div>
      )}

      {loading && approvals.length === 0 ? (
        <Card className="border-dashed">
          <CardContent className="flex items-center justify-center py-12 text-sm text-muted-foreground">Carregando…</CardContent>
        </Card>
      ) : approvals.length === 0 ? (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center py-12 text-center text-muted-foreground">
            <CheckCircle className="mb-3 h-9 w-9 opacity-50" />
            <p className="text-sm font-medium">Nenhuma aprovação solicitada</p>
            <p className="mt-1 max-w-md text-xs">Aprovações são criadas quando uma negociação tem desvio de política. Para criar uma, abra a negociação em "Negociações" e clique em "Solicitar aprovação".</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {[...pendingApprovals, ...decidedApprovals].map((a) => (
            <ApprovalCard
              key={a.id}
              approval={a}
              negotiation={negotiationsById.get(a.opportunityNegotiationId) ?? null}
              actionLoading={actionLoading}
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
  negotiation: OpportunityNegotiation | null
  actionLoading: boolean
  onApprove: () => void
  onReject: () => void
}

function ApprovalCard({ approval, negotiation, actionLoading, onApprove, onReject }: ApprovalCardProps) {
  const isPending = approval.status === OpportunityApprovalStatus.Pending
  const isApproved = approval.status === OpportunityApprovalStatus.Approved
  const isRejected = approval.status === OpportunityApprovalStatus.Rejected
  const isCancelled = approval.status === OpportunityApprovalStatus.Cancelled

  const config = isPending
    ? { stripBg: 'bg-amber-50', stripBorder: 'border-amber-200', stripText: 'text-amber-800', accent: 'border-amber-300', commentBorder: 'border-l-amber-500', icon: <Clock className="h-3.5 w-3.5" />, label: 'Pendente' }
    : isApproved
      ? { stripBg: 'bg-emerald-50', stripBorder: 'border-emerald-200', stripText: 'text-emerald-800', accent: 'border-border', commentBorder: 'border-l-emerald-500', icon: <CheckCircle className="h-3.5 w-3.5" />, label: 'Aprovada' }
      : isRejected
        ? { stripBg: 'bg-rose-50', stripBorder: 'border-rose-200', stripText: 'text-rose-800', accent: 'border-border', commentBorder: 'border-l-rose-500', icon: <XCircle className="h-3.5 w-3.5" />, label: 'Rejeitada' }
        : { stripBg: 'bg-muted', stripBorder: 'border-border', stripText: 'text-muted-foreground', accent: 'border-border', commentBorder: 'border-l-slate-400', icon: <XCircle className="h-3.5 w-3.5" />, label: 'Cancelada' }

  const requestedHours = hoursSince(approval.requestedAt)

  return (
    <div className={`overflow-hidden rounded-xl border bg-card ${isPending ? 'border-amber-300 shadow-sm shadow-amber-100' : config.accent}`}>
      <div className={`flex items-center gap-2 border-b ${config.stripBorder} ${config.stripBg} px-5 py-2 text-[11.5px] font-bold uppercase tracking-wider ${config.stripText}`}>
        {config.icon}
        {config.label}
        <span className="ml-auto text-[11px] font-medium normal-case tracking-normal">
          {isPending ? (
            requestedHours < 1 ? 'aguardando há menos de 1h' : `aguardando há ${requestedHours}h`
          ) : approval.decidedAt ? (
            `decidida em ${formatDate(approval.decidedAt)}`
          ) : null}
        </span>
      </div>

      <div className="grid gap-5 px-5 py-4 md:grid-cols-[1fr_auto]">
        <div className="min-w-0">
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <strong className="text-base text-foreground">{approvalTypeLabel[approval.approvalType] ?? 'Exceção'}</strong>
          </div>
          {approval.reason && (
            <p className="mb-3 text-sm leading-relaxed text-muted-foreground">{approval.reason}</p>
          )}

          {negotiation && (
            <div className="mb-3 inline-flex items-center gap-2 rounded-lg border border-border bg-muted/30 px-3 py-1.5 text-xs">
              <MessageSquare className="h-3.5 w-3.5 text-muted-foreground" />
              <span className="text-muted-foreground">Vinculada a</span>
              <strong className="text-foreground">{negotiation.title}</strong>
              <span className="text-muted-foreground">·</span>
              <span className="font-mono font-semibold text-foreground">{formatCurrency(negotiation.amount)}</span>
            </div>
          )}

          <div className="flex flex-wrap gap-x-6 gap-y-2 text-xs">
            <div>
              <p className="font-bold uppercase tracking-wider text-muted-foreground">Solicitado por</p>
              <p className="mt-0.5 text-foreground">{approval.requestedByUserName}</p>
              <p className="text-[11px] text-muted-foreground">em {formatDate(approval.requestedAt)}</p>
            </div>
            {!isPending && approval.approvedByUserName && (
              <div className="border-l border-border pl-6">
                <p className="font-bold uppercase tracking-wider text-muted-foreground">
                  {isApproved ? 'Aprovado por' : isRejected ? 'Rejeitado por' : 'Decidido por'}
                </p>
                <p className={`mt-0.5 font-semibold ${isApproved ? 'text-emerald-700' : isRejected ? 'text-rose-700' : 'text-foreground'}`}>
                  {approval.approvedByUserName}
                </p>
                {approval.decidedAt && (
                  <p className="text-[11px] text-muted-foreground">em {formatDate(approval.decidedAt)}</p>
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

        {isPending && !isCancelled && (
          <div className="flex flex-row gap-2 md:flex-col md:gap-2">
            <Button size="sm" variant="outline-success" onClick={onApprove} disabled={actionLoading} className="justify-center md:w-44">
              <ThumbsUp className="mr-1.5 h-3.5 w-3.5" /> Aprovar
            </Button>
            <Button size="sm" variant="outline-danger" onClick={onReject} disabled={actionLoading} className="justify-center md:w-44">
              <ThumbsDown className="mr-1.5 h-3.5 w-3.5" /> Rejeitar
            </Button>
          </div>
        )}
      </div>
    </div>
  )
}

function FunnelStagesCard({ stages, currentStageId }: { stages: Array<{ id: number; name: string; color?: string; displayOrder?: number }>; currentStageId?: number }) {
  const currentIndex = stages.findIndex((s) => s.id === currentStageId)
  return (
    <div className="rounded-xl border border-border bg-card p-4">
      <div className="mb-3 text-[11px] font-bold uppercase tracking-wider text-muted-foreground">Estágio do funil</div>
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
              {isNow && <span className="ml-auto text-[10px] font-semibold uppercase tracking-wider text-primary">aqui</span>}
            </li>
          )
        })}
      </ol>
    </div>
  )
}
