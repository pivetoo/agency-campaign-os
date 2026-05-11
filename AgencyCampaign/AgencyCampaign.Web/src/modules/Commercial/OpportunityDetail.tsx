import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { PageLayout, Button, Card, CardContent, CardHeader, CardTitle, DataTable, useApi, useAuth, Badge, Tabs, TabsList, TabsTrigger, TabsContent, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Activity, Building2, Calendar, CheckCircle, CircleDollarSign, Clock, FileText, MapPin, MessageSquare, Pencil, Plus, Tag, ThumbsDown, ThumbsUp, Trash2, TrendingUp, User, UserCheck, XCircle } from 'lucide-react'
import { commercialPipelineStageService } from '../../services/commercialPipelineStageService'
import { opportunityService, type Opportunity, type OpportunityApprovalRequest, type OpportunityFollowUp, type OpportunityNegotiation } from '../../services/opportunityService'
import OpportunityFormModal from '../../components/modals/OpportunityFormModal'
import OpportunityNegotiationFormModal from '../../components/modals/OpportunityNegotiationFormModal'
import OpportunityFollowUpFormModal from '../../components/modals/OpportunityFollowUpFormModal'
import OpportunityApprovalRequestFormModal from '../../components/modals/OpportunityApprovalRequestFormModal'
import OpportunityActivityTab from './OpportunityActivityTab'
import ProposalFormModal from '../../components/modals/ProposalFormModal'
import { resolveAssetUrl } from '../../lib/assetUrl'

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

const negotiationStatusKeys: Record<number, string> = {
  1: 'negotiation.status.draft',
  2: 'negotiation.status.pendingApproval',
  3: 'negotiation.status.approved',
  4: 'negotiation.status.rejected',
  5: 'negotiation.status.sentToClient',
  6: 'negotiation.status.acceptedByClient',
  7: 'negotiation.status.cancelled',
}

const approvalTypeKeys: Record<number, string> = {
  1: 'approvals.type.discount',
  2: 'approvals.type.margin',
  3: 'approvals.type.deadline',
  4: 'approvals.type.exception',
}

const approvalStatusKeys: Record<number, string> = {
  1: 'approvals.status.pending',
  2: 'approvals.status.approved',
  3: 'approvals.status.rejected',
  4: 'approvals.status.cancelled',
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
}

function formatDate(value?: string) {
  return value ? new Date(value).toLocaleDateString('pt-BR') : '-'
}

function getStageBadgeVariant(finalBehavior?: number): 'default' | 'success' | 'destructive' | 'warning' {
  if (finalBehavior === 1) return 'success'
  if (finalBehavior === 2) return 'destructive'
  return 'warning'
}

export default function OpportunityDetail() {
  const { t } = useI18n()
  const { id } = useParams<{ id: string }>()
  const opportunityId = Number(id || 0)
  const navigate = useNavigate()
  const { user: authUser } = useAuth()

  const [opportunity, setOpportunity] = useState<Opportunity | null>(null)
  const [stages, setStages] = useState<Array<{ id: number; name: string; finalBehavior: number }>>([])
  const [selectedNegotiation, setSelectedNegotiation] = useState<OpportunityNegotiation | null>(null)
  const [selectedApprovalRequest, setSelectedApprovalRequest] = useState<OpportunityApprovalRequest | null>(null)
  const [selectedFollowUp, setSelectedFollowUp] = useState<OpportunityFollowUp | null>(null)
  const [isOpportunityFormOpen, setIsOpportunityFormOpen] = useState(false)
  const [isNegotiationFormOpen, setIsNegotiationFormOpen] = useState(false)
  const [isApprovalRequestFormOpen, setIsApprovalRequestFormOpen] = useState(false)
  const [isFollowUpFormOpen, setIsFollowUpFormOpen] = useState(false)
  const [isProposalFormOpen, setIsProposalFormOpen] = useState(false)
  const [selectedStage, setSelectedStage] = useState<string>('1')

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
    setSelectedStage(String(stageId))
    const result = await executeAction(() => opportunityService.changeStage(opportunity.id, { commercialPipelineStageId: stageId }))
    if (result !== null) await loadOpportunity()
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

  const stageBadgeVariant = getStageBadgeVariant(opportunity?.commercialPipelineStage?.finalBehavior)

  return (
    <div className="space-y-6">
      <PageLayout
        title={opportunity?.name || t('opportunityDetail.fallbackTitle')}
        subtitle={opportunity?.brand?.name ? t('opportunityDetail.subtitleWithBrand').replace('{0}', opportunity.brand.name) : t('opportunityDetail.subtitle')}
        onRefresh={() => void loadOpportunity()}
        showDefaultActions={false}
      >
        <div className="space-y-6">
          <div className="flex flex-col gap-3 rounded-md border border-border/70 bg-muted/20 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex flex-wrap items-center gap-3">
              <span
                className="inline-flex h-2.5 w-2.5 rounded-full"
                style={{ backgroundColor: opportunity?.commercialPipelineStage?.color || 'hsl(var(--primary))' }}
                aria-hidden
              />
              <Badge variant={stageBadgeVariant} className="px-2.5 py-0.5 text-xs">
                {opportunity?.commercialPipelineStage?.name || t('opportunityDetail.stage.none')}
              </Badge>
              <span className="hidden text-border sm:inline">·</span>
              <span className="flex items-center gap-2 text-sm text-muted-foreground">
                {opportunity?.brand?.logoUrl ? (
                  <img
                    src={resolveAssetUrl(opportunity.brand.logoUrl)}
                    alt={opportunity.brand?.name ?? ''}
                    className="h-6 w-6 rounded-md border bg-card object-contain p-0.5"
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
                {opportunity?.brand?.name || t('opportunityDetail.brand.unknown')}
              </span>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <Select value={selectedStage} onValueChange={(value) => void handleChangeStage(Number(value))}>
                <SelectTrigger className="w-full sm:w-[200px]">
                  <MapPin className="mr-2 h-4 w-4 text-muted-foreground" />
                  <SelectValue placeholder={t('opportunityDetail.stage.placeholder')} />
                </SelectTrigger>
                <SelectContent>
                  {stages.map((stage) => (
                    <SelectItem key={stage.id} value={String(stage.id)}>{stage.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <Button size="sm" variant="outline" onClick={() => setIsOpportunityFormOpen(true)}>
                <Pencil className="mr-2 h-4 w-4" /> {t('common.action.edit')}
              </Button>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
            <Card className="border border-border/70 shadow-sm">
              <CardContent className="p-4">
                <div className="flex items-center gap-2 text-xs font-medium text-muted-foreground">
                  <CircleDollarSign className="h-3.5 w-3.5 text-indigo-600" />
                  {t('opportunityDetail.kpi.estimatedValue')}
                </div>
                <p className="mt-1.5 text-lg font-semibold text-foreground">{formatCurrency(opportunity?.estimatedValue ?? 0)}</p>
              </CardContent>
            </Card>
            <Card className="border border-border/70 shadow-sm">
              <CardContent className="p-4">
                <div className="flex items-center gap-2 text-xs font-medium text-muted-foreground">
                  <Calendar className="h-3.5 w-3.5 text-violet-600" />
                  {t('opportunityDetail.kpi.forecast')}
                </div>
                <p className="mt-1.5 text-lg font-semibold text-foreground">{formatDate(opportunity?.expectedCloseAt)}</p>
              </CardContent>
            </Card>
            <Card className="border border-border/70 shadow-sm">
              <CardContent className="p-4">
                <div className="flex items-center gap-2 text-xs font-medium text-muted-foreground">
                  <UserCheck className="h-3.5 w-3.5 text-cyan-600" />
                  {t('common.field.responsible')}
                </div>
                <p className="mt-1.5 truncate text-lg font-semibold text-foreground">{opportunity?.commercialResponsible?.name || '-'}</p>
              </CardContent>
            </Card>
            <Card className="border border-border/70 shadow-sm">
              <CardContent className="p-4">
                <div className="flex items-center gap-2 text-xs font-medium text-muted-foreground">
                  <Clock className="h-3.5 w-3.5 text-amber-600" />
                  {t('opportunityDetail.kpi.pendingFollowUps')}
                </div>
                <div className="mt-1.5 flex items-baseline gap-2">
                  <p className="text-lg font-semibold text-foreground">{pendingFollowUpsCount}</p>
                  {overdueFollowUpsCount > 0 ? (
                    <span className="text-[11px] font-medium text-destructive">
                      {(overdueFollowUpsCount === 1 ? t('opportunityDetail.overdueSingle') : t('opportunityDetail.overdueMany')).replace('{0}', String(overdueFollowUpsCount))}
                    </span>
                  ) : null}
                </div>
              </CardContent>
            </Card>
          </div>

        <Tabs defaultValue="summary" className="pt-2">
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
            <TabsTrigger value="negotiations" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <MessageSquare className="h-4 w-4" /> {t('opportunityDetail.tab.negotiations')}
              {opportunity?.negotiations?.length ? (
                <span className="rounded-full bg-muted px-1.5 py-0.5 text-[11px] font-medium text-muted-foreground group-data-[state=active]:bg-primary/10 group-data-[state=active]:text-primary">
                  {opportunity.negotiations.length}
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
            </TabsTrigger>
            <TabsTrigger value="followups" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <Clock className="h-4 w-4" /> {t('opportunityDetail.tab.followups')}
              {opportunity?.followUps?.length ? (
                <span className="rounded-full bg-muted px-1.5 py-0.5 text-[11px] font-medium text-muted-foreground group-data-[state=active]:bg-primary/10 group-data-[state=active]:text-primary">
                  {opportunity.followUps.length}
                </span>
              ) : null}
            </TabsTrigger>
            <TabsTrigger value="activity" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <Activity className="h-4 w-4" /> {t('opportunityDetail.tab.activity')}
            </TabsTrigger>
          </TabsList>

          <TabsContent value="summary" className="mt-0">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <FileText className="h-5 w-5 text-muted-foreground" /> {t('opportunityDetail.summary.title')}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
                  <div className="space-y-1">
                    <p className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                      <FileText className="h-3.5 w-3.5" /> {t('opportunityDetail.summary.description')}
                    </p>
                    <p className="text-sm">{opportunity?.description || '-'}</p>
                  </div>
                  <div className="space-y-1">
                    <p className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                      <User className="h-3.5 w-3.5" /> {t('common.field.contact')}
                    </p>
                    <p className="text-sm">{opportunity?.contactName || '-'}</p>
                  </div>
                  <div className="space-y-1">
                    <p className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                      <Tag className="h-3.5 w-3.5" /> {t('common.field.notes')}
                    </p>
                    <p className="text-sm">{opportunity?.notes || '-'}</p>
                  </div>
                  <div className="space-y-1">
                    <p className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                      <Calendar className="h-3.5 w-3.5" /> {t('opportunityDetail.summary.closedAt')}
                    </p>
                    <p className="text-sm">{formatDate(opportunity?.closedAt)}</p>
                  </div>
                  {opportunity?.lossReason && (
                    <div className="space-y-1">
                      <p className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                        <XCircle className="h-3.5 w-3.5" /> {t('opportunityDetail.summary.lossReason')}
                      </p>
                      <p className="text-sm text-destructive">{opportunity.lossReason}</p>
                    </div>
                  )}
                  {opportunity?.wonNotes && (
                    <div className="space-y-1">
                      <p className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                        <CheckCircle className="h-3.5 w-3.5" /> {t('opportunityDetail.summary.wonNotes')}
                      </p>
                      <p className="text-sm text-success">{opportunity.wonNotes}</p>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
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
                  pageSize={5}
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
                  <Button size="sm" variant="outline" onClick={() => selectedNegotiation && setIsNegotiationFormOpen(true)} disabled={!selectedNegotiation}>
                    <Pencil className="mr-2 h-4 w-4" /> {t('common.action.edit')}
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => setIsApprovalRequestFormOpen(true)}
                    disabled={!selectedNegotiation || selectedNegotiation.status !== 1}
                    title={
                      !selectedNegotiation
                        ? t('opportunityDetail.negotiations.title.selectFirst')
                        : selectedNegotiation.status !== 1
                          ? t('opportunityDetail.negotiations.title.onlyDraft').replace('{0}', negotiationStatusKeys[selectedNegotiation.status] ? t(negotiationStatusKeys[selectedNegotiation.status]) : '-')
                          : t('opportunityDetail.negotiations.title.request')
                    }
                  >
                    <CheckCircle className="mr-2 h-4 w-4" /> {t('opportunityDetail.negotiations.requestApproval')}
                  </Button>
                  <Button size="sm" variant="outline-danger" onClick={() => void handleDeleteNegotiation()} disabled={!selectedNegotiation || actionLoading}>
                    <Trash2 className="mr-2 h-4 w-4" /> {t('common.action.delete')}
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
                  pageSize={5}
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
                    <Button size="sm" variant="outline-success" onClick={() => void handleApproveRequest()} disabled={!selectedApprovalRequest || selectedApprovalRequest.status !== 1 || actionLoading}>
                      <ThumbsUp className="mr-1.5 h-4 w-4" /> {t('proposals.action.approve')}
                    </Button>
                    <Button size="sm" variant="outline-danger" onClick={() => void handleRejectRequest()} disabled={!selectedApprovalRequest || selectedApprovalRequest.status !== 1 || actionLoading}>
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
                  pageSize={5}
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
                  <Button size="sm" variant="outline-danger" onClick={() => void handleDeleteFollowUp()} disabled={!selectedFollowUp || actionLoading}>
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
                  pageSize={5}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="activity" className="mt-0">
            <OpportunityActivityTab opportunityId={opportunityId} currentUserId={authUser?.id ?? null} />
          </TabsContent>
        </Tabs>
        </div>
      </PageLayout>

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
    </div>
  )
}
