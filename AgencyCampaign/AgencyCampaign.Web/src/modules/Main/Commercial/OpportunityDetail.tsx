import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { Button, Card, CardContent, CardHeader, CardTitle, DataTable, Dropdown, DropdownTrigger, DropdownContent, DropdownItem, DropdownLabel, DropdownSeparator, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useAuth, Badge, Tabs, TabsList, TabsTrigger, TabsContent, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Activity, ArrowRight, Building2, Calendar, CheckCircle, CircleDollarSign, Clock, Compass, FileText, History, MessageSquare, MoreHorizontal, Pencil, Plus, Tag, Tags, ThumbsDown, ThumbsUp, Trash2, TrendingUp, User, UserCheck, XCircle } from 'lucide-react'
import { commercialPipelineStageService } from '../../../services/commercialPipelineStageService'
import { opportunityService, OpportunityNegotiationStatus, OpportunityApprovalStatus, type OpportunityNegotiationStatusValue, type OpportunityApprovalStatusValue, type Opportunity, type OpportunityApprovalRequest, type OpportunityFollowUp, type OpportunityNegotiation } from '../../../services/opportunityService'
import OpportunityFormModal from '../../../components/modals/OpportunityFormModal'
import OpportunityNegotiationFormModal from '../../../components/modals/OpportunityNegotiationFormModal'
import OpportunityFollowUpFormModal from '../../../components/modals/OpportunityFollowUpFormModal'
import OpportunityApprovalRequestFormModal from '../../../components/modals/OpportunityApprovalRequestFormModal'
import OpportunityActivityTab from './OpportunityActivityTab'
import ProposalFormModal from '../../../components/modals/ProposalFormModal'
import { resolveAssetUrl } from '../../../lib/assetUrl'
import { formatDate } from '../../../lib/format'
import { formatCurrency } from '../../../lib/format'

const proposalStatusKeys: Record<number, string> = {
  1: 'proposal.status.draft',
  2: 'proposal.status.sent',
  3: 'proposal.status.viewed',
  4: 'proposal.status.approved',
  5: 'proposal.status.rejected',
  6: 'proposal.status.converted',
  7: 'proposal.status.expired',
  8: 'proposal.status.cancelled',
}

const proposalStatusVariant: Record<number, 'default' | 'warning' | 'success' | 'destructive'> = {
  1: 'default',
  2: 'warning',
  3: 'warning',
  4: 'success',
  5: 'destructive',
  6: 'success',
  7: 'destructive',
  8: 'destructive',
}

const negotiationStatusKeys: Record<OpportunityNegotiationStatusValue, string> = {
  [OpportunityNegotiationStatus.Draft]: 'negotiation.status.draft',
  [OpportunityNegotiationStatus.PendingApproval]: 'negotiation.status.pendingApproval',
  [OpportunityNegotiationStatus.Approved]: 'negotiation.status.approved',
  [OpportunityNegotiationStatus.Rejected]: 'negotiation.status.rejected',
  [OpportunityNegotiationStatus.SentToClient]: 'negotiation.status.sentToClient',
  [OpportunityNegotiationStatus.AcceptedByClient]: 'negotiation.status.acceptedByClient',
  [OpportunityNegotiationStatus.Cancelled]: 'negotiation.status.cancelled',
}

const approvalTypeKeys: Record<number, string> = {
  1: 'approvals.type.discount',
  2: 'approvals.type.margin',
  3: 'approvals.type.deadline',
  4: 'approvals.type.exception',
}

const approvalStatusKeys: Record<OpportunityApprovalStatusValue, string> = {
  [OpportunityApprovalStatus.Pending]: 'approvals.status.pending',
  [OpportunityApprovalStatus.Approved]: 'approvals.status.approved',
  [OpportunityApprovalStatus.Rejected]: 'approvals.status.rejected',
  [OpportunityApprovalStatus.Cancelled]: 'approvals.status.cancelled',
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
  const [selectedApprovalRequest, setSelectedApprovalRequest] = useState<OpportunityApprovalRequest | null>(null)
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

  const negotiationColumns: DataTableColumn<OpportunityNegotiation>[] = [
    { key: 'title', title: t('negotiation.field.title'), dataIndex: 'title' },
    { key: 'amount', title: t('negotiation.field.amount'), dataIndex: 'amount', render: (value: number) => formatCurrency(value) },
    {
      key: 'status',
      title: t('common.field.status'),
      dataIndex: 'status',
      render: (value: number) => (
        <Badge variant={value === 3 || value === 6 ? 'success' : value === 4 || value === 7 ? 'destructive' : 'warning'}>
          {negotiationStatusKeys[value] ? t(negotiationStatusKeys[value]) : '-'}
        </Badge>
      ),
    },
    { key: 'negotiatedAt', title: t('negotiation.field.date'), dataIndex: 'negotiatedAt', render: (value: string) => formatDate(value) },
    { key: 'notes', title: t('common.field.notes'), dataIndex: 'notes', render: (value?: string) => value || '-' },
  ]

  const approvalColumns: DataTableColumn<OpportunityApprovalRequest>[] = [
    { key: 'approvalType', title: t('common.field.type'), dataIndex: 'approvalType', render: (value: number) => approvalTypeKeys[value] ? t(approvalTypeKeys[value]) : '-' },
    {
      key: 'status',
      title: t('common.field.status'),
      dataIndex: 'status',
      render: (value: number) => (
        <Badge variant={value === 2 ? 'success' : value === 3 ? 'destructive' : 'warning'}>
          {approvalStatusKeys[value] ? t(approvalStatusKeys[value]) : '-'}
        </Badge>
      ),
    },
    { key: 'requestedByUserName', title: t('approvals.field.requestedBy'), dataIndex: 'requestedByUserName' },
    { key: 'reason', title: t('approvals.field.reason'), dataIndex: 'reason' },
    { key: 'requestedAt', title: t('approvals.field.requestedAt'), dataIndex: 'requestedAt', render: (value: string) => formatDate(value) },
  ]

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

  const proposalColumns: DataTableColumn<NonNullable<Opportunity['proposals']>[number]>[] = [
    {
      key: 'name',
      title: t('proposals.field.proposal'),
      dataIndex: 'name',
      render: (value: string) => <span className="font-medium">{value}</span>,
    },
    { key: 'totalValue', title: t('negotiation.field.amount'), dataIndex: 'totalValue', render: (value: number) => formatCurrency(value) },
    {
      key: 'status',
      title: t('common.field.status'),
      dataIndex: 'status',
      render: (value: number) => (
        <Badge variant={proposalStatusVariant[value] || 'default'}>
          {proposalStatusKeys[value] ? t(proposalStatusKeys[value]) : '-'}
        </Badge>
      ),
    },
    { key: 'validityUntil', title: t('common.field.validity'), dataIndex: 'validityUntil', render: (value?: string) => formatDate(value) },
    {
      key: 'actions',
      title: '',
      dataIndex: 'id',
      width: 110,
      render: (_value: number, record: NonNullable<Opportunity['proposals']>[number]) => (
        <Button
          size="sm"
          variant="outline"
          onClick={(e) => {
            e.stopPropagation()
            navigate(`/comercial/propostas/${record.id}`)
          }}
        >
          {t('common.action.open')} <FileText className="ml-1.5 h-3.5 w-3.5" />
        </Button>
      ),
    },
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

  const openStatusModal = () => {
    if (!selectedNegotiation) return
    setStatusToSet(String(selectedNegotiation.status))
    setIsStatusModalOpen(true)
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


  const handleDeleteNegotiation = async () => {
    if (!selectedNegotiation) return
    const result = await executeAction(() => opportunityService.deleteNegotiation(selectedNegotiation.id))
    if (result !== null) {
      setSelectedNegotiation(null)
      await loadOpportunity()
    }
  }

  const handleApproveRequest = async () => {
    if (!selectedApprovalRequest) return
    const result = await executeAction(() => opportunityService.approveRequest(selectedApprovalRequest.id, {
      approvedByUserName: t('opportunityDetail.approvals.userFallback'),
      decisionNotes: t('opportunityDetail.approvals.decision.approved'),
    }))
    if (result !== null) {
      setSelectedApprovalRequest(null)
      await loadOpportunity()
    }
  }

  const handleRejectRequest = async () => {
    if (!selectedApprovalRequest) return
    const result = await executeAction(() => opportunityService.rejectRequest(selectedApprovalRequest.id, {
      approvedByUserName: t('opportunityDetail.approvals.userFallback'),
      decisionNotes: t('opportunityDetail.approvals.decision.rejected'),
    }))
    if (result !== null) {
      setSelectedApprovalRequest(null)
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

                <div>
                  <h3 className="mb-3 text-base font-semibold text-foreground">O que está em jogo</h3>
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
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between gap-3">
                  <CardTitle className="flex items-center gap-2">
                    <TrendingUp className="h-5 w-5 text-muted-foreground" /> {t('opportunityDetail.proposals.title')}
                  </CardTitle>
                  <Button size="sm" onClick={() => setIsProposalFormOpen(true)}>
                    <Plus className="mr-1.5 h-4 w-4" /> {t('opportunityDetail.proposals.new')}
                  </Button>
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  {t('opportunityDetail.proposals.description')}
                </p>
              </CardHeader>
              <CardContent>
                <DataTable
                  columns={proposalColumns}
                  data={opportunity?.proposals || []}
                  rowKey="id"
                  emptyText={t('opportunityDetail.proposals.empty')}
                  loading={loading}
                  pageSize={10}
                  pageSizeOptions={[5, 10, 20, 50]}
                  onRowClick={(record) => navigate(`/comercial/propostas/${record.id}`)}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="negotiations" className="mt-0">
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between gap-3">
                  <CardTitle className="flex items-center gap-2">
                    <MessageSquare className="h-5 w-5 text-muted-foreground" /> {t('opportunityDetail.tab.negotiations')}
                  </CardTitle>
                  <Button size="sm" onClick={() => { setSelectedNegotiation(null); setIsNegotiationFormOpen(true) }}>
                    <TrendingUp className="mr-1.5 h-4 w-4" /> {t('opportunityDetail.negotiations.new')}
                  </Button>
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  {t('opportunityDetail.negotiations.description')}
                </p>
              </CardHeader>
              <CardContent>
                <div className="mb-3 flex flex-wrap gap-2">
                  <Button size="sm" variant="ghost" onClick={() => selectedNegotiation && setIsNegotiationFormOpen(true)} disabled={!selectedNegotiation}>
                    <Pencil className="mr-2 h-4 w-4" /> {t('common.action.edit')}
                  </Button>
                  <Button size="sm" variant="ghost" onClick={() => void handleDeleteNegotiation()} disabled={!selectedNegotiation || actionLoading}>
                    <Trash2 className="mr-2 h-4 w-4" /> {t('common.action.delete')}
                  </Button>
                  <Button size="sm" variant="outline" onClick={openStatusModal} disabled={!selectedNegotiation || actionLoading}>
                    <Activity className="mr-2 h-4 w-4" /> {t('opportunityDetail.negotiations.changeStatus')}
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => setIsApprovalRequestFormOpen(true)}
                    disabled={!selectedNegotiation || selectedNegotiation.status !== OpportunityNegotiationStatus.Draft}
                    title={
                      !selectedNegotiation
                        ? t('opportunityDetail.negotiations.title.selectFirst')
                        : selectedNegotiation.status !== OpportunityNegotiationStatus.Draft
                          ? t('opportunityDetail.negotiations.title.onlyDraft').replace('{0}', negotiationStatusKeys[selectedNegotiation.status] ? t(negotiationStatusKeys[selectedNegotiation.status]) : '-')
                          : t('opportunityDetail.negotiations.title.request')
                    }
                  >
                    <CheckCircle className="mr-2 h-4 w-4" /> {t('opportunityDetail.negotiations.requestApproval')}
                  </Button>
                </div>
                <DataTable
                  columns={negotiationColumns}
                  data={opportunity?.negotiations || []}
                  rowKey="id"
                  selectedRows={selectedNegotiation ? [selectedNegotiation] : []}
                  onSelectionChange={(rows) => setSelectedNegotiation(rows[0] ?? null)}
                  emptyText={t('opportunityDetail.negotiations.empty')}
                  loading={loading}
                  pageSize={10}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="approvals" className="mt-0">
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between gap-3">
                  <CardTitle className="flex items-center gap-2">
                    <CheckCircle className="h-5 w-5 text-muted-foreground" /> {t('opportunityDetail.tab.approvals')}
                  </CardTitle>
                  <div className="flex flex-wrap gap-2">
                    <Button size="sm" variant="outline-success" onClick={() => void handleApproveRequest()} disabled={!selectedApprovalRequest || selectedApprovalRequest.status !== OpportunityApprovalStatus.Pending || actionLoading}>
                      <ThumbsUp className="mr-1.5 h-4 w-4" /> {t('proposals.action.approve')}
                    </Button>
                    <Button size="sm" variant="outline-danger" onClick={() => void handleRejectRequest()} disabled={!selectedApprovalRequest || selectedApprovalRequest.status !== OpportunityApprovalStatus.Pending || actionLoading}>
                      <ThumbsDown className="mr-1.5 h-4 w-4" /> {t('proposals.action.reject')}
                    </Button>
                  </div>
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  {t('opportunityDetail.approvals.description')}
                </p>
              </CardHeader>
              <CardContent>
                <DataTable
                  columns={approvalColumns}
                  data={approvalRequests}
                  rowKey="id"
                  selectedRows={selectedApprovalRequest ? [selectedApprovalRequest] : []}
                  onSelectionChange={(rows) => setSelectedApprovalRequest(rows[0] ?? null)}
                  emptyText={t('opportunityDetail.approvals.empty')}
                  loading={loading}
                  pageSize={10}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
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
