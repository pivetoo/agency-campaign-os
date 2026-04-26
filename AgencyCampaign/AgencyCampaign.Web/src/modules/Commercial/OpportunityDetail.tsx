import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageLayout, Button, Card, CardContent, CardHeader, CardTitle, DataTable, useApi, Badge, Tabs, TabsList, TabsTrigger, TabsContent, Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Building2, Calendar, CheckCircle, CircleDollarSign, Clock, FileText, MapPin, MessageSquare, Pencil, Tag, ThumbsDown, ThumbsUp, Trash2, TrendingUp, User, UserCheck, XCircle } from 'lucide-react'
import { commercialPipelineStageService } from '../../services/commercialPipelineStageService'
import { opportunityService, type Opportunity, type OpportunityApprovalRequest, type OpportunityFollowUp, type OpportunityNegotiation } from '../../services/opportunityService'
import OpportunityFormModal from '../../components/modals/OpportunityFormModal'
import OpportunityNegotiationFormModal from '../../components/modals/OpportunityNegotiationFormModal'
import OpportunityFollowUpFormModal from '../../components/modals/OpportunityFollowUpFormModal'
import OpportunityApprovalRequestFormModal from '../../components/modals/OpportunityApprovalRequestFormModal'

const proposalStatusLabels: Record<number, string> = {
  1: 'Rascunho',
  2: 'Enviada',
  3: 'Visualizada',
  4: 'Aprovada',
  5: 'Rejeitada',
  6: 'Convertida',
  7: 'Expirada',
  8: 'Cancelada',
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

const negotiationStatusLabels: Record<number, string> = {
  1: 'Rascunho',
  2: 'Pendente aprovação',
  3: 'Aprovada',
  4: 'Rejeitada',
  5: 'Enviada ao cliente',
  6: 'Aceita pelo cliente',
  7: 'Cancelada',
}

const approvalTypeLabels: Record<number, string> = {
  1: 'Desconto',
  2: 'Margem',
  3: 'Prazo',
  4: 'Exceção',
}

const approvalStatusLabels: Record<number, string> = {
  1: 'Pendente',
  2: 'Aprovada',
  3: 'Rejeitada',
  4: 'Cancelada',
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
  const { id } = useParams<{ id: string }>()
  const opportunityId = Number(id || 0)

  const [opportunity, setOpportunity] = useState<Opportunity | null>(null)
  const [stages, setStages] = useState<Array<{ id: number; name: string; finalBehavior: number }>>([])
  const [selectedNegotiation, setSelectedNegotiation] = useState<OpportunityNegotiation | null>(null)
  const [selectedApprovalRequest, setSelectedApprovalRequest] = useState<OpportunityApprovalRequest | null>(null)
  const [selectedFollowUp, setSelectedFollowUp] = useState<OpportunityFollowUp | null>(null)
  const [isOpportunityFormOpen, setIsOpportunityFormOpen] = useState(false)
  const [isNegotiationFormOpen, setIsNegotiationFormOpen] = useState(false)
  const [isApprovalRequestFormOpen, setIsApprovalRequestFormOpen] = useState(false)
  const [isFollowUpFormOpen, setIsFollowUpFormOpen] = useState(false)
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
    { key: 'title', title: 'Título', dataIndex: 'title' },
    { key: 'amount', title: 'Valor', dataIndex: 'amount', render: (value: number) => formatCurrency(value) },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      render: (value: number) => (
        <Badge variant={value === 3 || value === 6 ? 'success' : value === 4 || value === 7 ? 'destructive' : 'warning'}>
          {negotiationStatusLabels[value] || '-'}
        </Badge>
      ),
    },
    { key: 'negotiatedAt', title: 'Data', dataIndex: 'negotiatedAt', render: (value: string) => formatDate(value) },
    { key: 'notes', title: 'Observações', dataIndex: 'notes', render: (value?: string) => value || '-' },
  ]

  const approvalColumns: DataTableColumn<OpportunityApprovalRequest>[] = [
    { key: 'approvalType', title: 'Tipo', dataIndex: 'approvalType', render: (value: number) => approvalTypeLabels[value] || '-' },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      render: (value: number) => (
        <Badge variant={value === 2 ? 'success' : value === 3 ? 'destructive' : 'warning'}>
          {approvalStatusLabels[value] || '-'}
        </Badge>
      ),
    },
    { key: 'requestedByUserName', title: 'Solicitado por', dataIndex: 'requestedByUserName' },
    { key: 'reason', title: 'Motivo', dataIndex: 'reason' },
    { key: 'requestedAt', title: 'Solicitado em', dataIndex: 'requestedAt', render: (value: string) => formatDate(value) },
  ]

  const followUpColumns: DataTableColumn<OpportunityFollowUp>[] = [
    { key: 'subject', title: 'Assunto', dataIndex: 'subject' },
    { key: 'dueAt', title: 'Prazo', dataIndex: 'dueAt', render: (value: string) => formatDate(value) },
    {
      key: 'isCompleted',
      title: 'Status',
      dataIndex: 'isCompleted',
      render: (value: boolean, record: OpportunityFollowUp) => {
        const isOverdue = !value && new Date(record.dueAt) < new Date()
        return (
          <Badge variant={value ? 'success' : isOverdue ? 'destructive' : 'warning'}>
            {value ? 'Concluído' : isOverdue ? 'Atrasado' : 'Pendente'}
          </Badge>
        )
      },
    },
    { key: 'notes', title: 'Observações', dataIndex: 'notes', render: (value?: string) => value || '-' },
  ]

  const proposalColumns: DataTableColumn<NonNullable<Opportunity['proposals']>[number]>[] = [
    {
      key: 'name',
      title: 'Proposta',
      dataIndex: 'name',
      render: (value: string) => <span className="font-medium">{value}</span>,
    },
    { key: 'totalValue', title: 'Valor', dataIndex: 'totalValue', render: (value: number) => formatCurrency(value) },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      render: (value: number) => (
        <Badge variant={proposalStatusVariant[value] || 'default'}>
          {proposalStatusLabels[value] || '-'}
        </Badge>
      ),
    },
    { key: 'validityUntil', title: 'Validade', dataIndex: 'validityUntil', render: (value?: string) => formatDate(value) },
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
      approvedByUserName: 'Aprovador interno',
      decisionNotes: 'Aprovação realizada no fluxo comercial.',
    }))
    if (result !== null) {
      setSelectedApprovalRequest(null)
      await loadOpportunity()
    }
  }

  const handleRejectRequest = async () => {
    if (!selectedApprovalRequest) return
    const result = await executeAction(() => opportunityService.rejectRequest(selectedApprovalRequest.id, {
      approvedByUserName: 'Aprovador interno',
      decisionNotes: 'Rejeitada no fluxo comercial.',
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
        title={opportunity?.name || 'Oportunidade'}
        subtitle={opportunity?.brand?.name ? `${opportunity.brand.name} · detalhe comercial` : 'Detalhe comercial'}
        onRefresh={() => void loadOpportunity()}
        showDefaultActions={false}
      >
        <Card className="overflow-hidden border-0 shadow-lg">
          <div
            className="h-2 w-full"
            style={{ backgroundColor: opportunity?.commercialPipelineStage?.color || '#6366f1' }}
          />
          <CardContent className="p-8">
            <div className="mb-6 flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
              <div className="flex items-center gap-4">
                <div
                  className="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl text-xl font-bold text-white shadow-md"
                  style={{ backgroundColor: opportunity?.commercialPipelineStage?.color || '#6366f1' }}
                >
                  {opportunity?.brand?.name?.charAt(0).toUpperCase() || 'O'}
                </div>
                <div>
                  <h2 className="text-2xl font-bold">{opportunity?.name}</h2>
                  <div className="mt-1 flex items-center gap-2 text-sm text-muted-foreground">
                    <Building2 className="h-4 w-4" />
                    {opportunity?.brand?.name || 'Marca não informada'}
                  </div>
                </div>
              </div>
              <Badge variant={stageBadgeVariant} className="self-start px-3 py-1 text-sm md:self-auto">
                {opportunity?.commercialPipelineStage?.name || 'Sem estágio'}
              </Badge>
            </div>

            <div className="my-6 h-px w-full bg-border" />

            <div className="grid grid-cols-2 gap-6 md:grid-cols-4">
              <div className="space-y-2">
                <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wider text-muted-foreground">
                  <CircleDollarSign className="h-4 w-4" />
                  Valor estimado
                </div>
                <p className="text-xl font-bold">{formatCurrency(opportunity?.estimatedValue ?? 0)}</p>
              </div>
              <div className="space-y-2">
                <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wider text-muted-foreground">
                  <Calendar className="h-4 w-4" />
                  Previsão de fechamento
                </div>
                <p className="text-xl font-bold">{formatDate(opportunity?.expectedCloseAt)}</p>
              </div>
              <div className="space-y-2">
                <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wider text-muted-foreground">
                  <UserCheck className="h-4 w-4" />
                  Responsável
                </div>
                <p className="text-xl font-bold">{opportunity?.commercialResponsible?.name || '-'}</p>
              </div>
              <div className="space-y-2">
                <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wider text-muted-foreground">
                  <Clock className="h-4 w-4" />
                  Follow-ups
                </div>
                <div className="flex items-center gap-2">
                  <p className="text-xl font-bold">{pendingFollowUpsCount}</p>
                  {overdueFollowUpsCount > 0 && (
                    <Badge variant="destructive" className="text-[10px]">{overdueFollowUpsCount} atrasado{overdueFollowUpsCount > 1 ? 's' : ''}</Badge>
                  )}
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="flex flex-wrap items-center gap-2 py-2">
          <Button variant="outline" onClick={() => setIsOpportunityFormOpen(true)}>
            <Pencil className="mr-2 h-4 w-4" /> Editar
          </Button>
          <Select value={selectedStage} onValueChange={(value) => void handleChangeStage(Number(value))}>
            <SelectTrigger className="w-[220px]">
              <MapPin className="mr-2 h-4 w-4 text-muted-foreground" />
              <SelectValue placeholder="Mudar estágio" />
            </SelectTrigger>
            <SelectContent>
              {stages.map((stage) => (
                <SelectItem key={stage.id} value={String(stage.id)}>{stage.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <Tabs defaultValue="summary" className="mt-4">
          <TabsList className="mb-6 h-12 w-full justify-start gap-1 rounded-xl bg-muted/50 p-1.5">
            <TabsTrigger value="summary" className="gap-2 rounded-lg px-5 py-2.5 text-sm font-medium data-[state=active]:bg-primary data-[state=active]:text-primary-foreground data-[state=active]:shadow-sm">
              <FileText className="h-4 w-4" /> Resumo
            </TabsTrigger>
            <TabsTrigger value="proposals" className="gap-2 rounded-lg px-5 py-2.5 text-sm font-medium data-[state=active]:bg-primary data-[state=active]:text-primary-foreground data-[state=active]:shadow-sm">
              <TrendingUp className="h-4 w-4" /> Propostas
              {opportunity?.proposals?.length ? <span className="ml-1 flex h-5 w-5 items-center justify-center rounded-full bg-primary-foreground/20 text-[10px] text-primary-foreground">{opportunity.proposals.length}</span> : null}
            </TabsTrigger>
            <TabsTrigger value="negotiations" className="gap-2 rounded-lg px-5 py-2.5 text-sm font-medium data-[state=active]:bg-primary data-[state=active]:text-primary-foreground data-[state=active]:shadow-sm">
              <MessageSquare className="h-4 w-4" /> Negociações
              {opportunity?.negotiations?.length ? <span className="ml-1 flex h-5 w-5 items-center justify-center rounded-full bg-primary-foreground/20 text-[10px] text-primary-foreground">{opportunity.negotiations.length}</span> : null}
            </TabsTrigger>
            <TabsTrigger value="approvals" className="gap-2 rounded-lg px-5 py-2.5 text-sm font-medium data-[state=active]:bg-primary data-[state=active]:text-primary-foreground data-[state=active]:shadow-sm">
              <CheckCircle className="h-4 w-4" /> Aprovações
              {approvalRequests.length ? <span className="ml-1 flex h-5 w-5 items-center justify-center rounded-full bg-primary-foreground/20 text-[10px] text-primary-foreground">{approvalRequests.length}</span> : null}
            </TabsTrigger>
            <TabsTrigger value="followups" className="gap-2 rounded-lg px-5 py-2.5 text-sm font-medium data-[state=active]:bg-primary data-[state=active]:text-primary-foreground data-[state=active]:shadow-sm">
              <Clock className="h-4 w-4" /> Follow-ups
              {opportunity?.followUps?.length ? <span className="ml-1 flex h-5 w-5 items-center justify-center rounded-full bg-primary-foreground/20 text-[10px] text-primary-foreground">{opportunity.followUps.length}</span> : null}
            </TabsTrigger>
          </TabsList>

          <TabsContent value="summary" className="mt-0">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <FileText className="h-5 w-5 text-muted-foreground" /> Informações da oportunidade
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
                  <div className="space-y-1">
                    <p className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                      <FileText className="h-3.5 w-3.5" /> Descrição
                    </p>
                    <p className="text-sm">{opportunity?.description || '-'}</p>
                  </div>
                  <div className="space-y-1">
                    <p className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                      <User className="h-3.5 w-3.5" /> Contato
                    </p>
                    <p className="text-sm">{opportunity?.contactName || '-'}</p>
                  </div>
                  <div className="space-y-1">
                    <p className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                      <Tag className="h-3.5 w-3.5" /> Observações
                    </p>
                    <p className="text-sm">{opportunity?.notes || '-'}</p>
                  </div>
                  <div className="space-y-1">
                    <p className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                      <Calendar className="h-3.5 w-3.5" /> Fechada em
                    </p>
                    <p className="text-sm">{formatDate(opportunity?.closedAt)}</p>
                  </div>
                  {opportunity?.lossReason && (
                    <div className="space-y-1">
                      <p className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                        <XCircle className="h-3.5 w-3.5" /> Motivo de perda
                      </p>
                      <p className="text-sm text-destructive">{opportunity.lossReason}</p>
                    </div>
                  )}
                  {opportunity?.wonNotes && (
                    <div className="space-y-1">
                      <p className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                        <CheckCircle className="h-3.5 w-3.5" /> Notas de ganho
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
                <CardTitle className="flex items-center gap-2">
                  <TrendingUp className="h-5 w-5 text-muted-foreground" /> Propostas vinculadas
                </CardTitle>
              </CardHeader>
              <CardContent>
                <DataTable
                  columns={proposalColumns}
                  data={opportunity?.proposals || []}
                  rowKey="id"
                  emptyText="Nenhuma proposta vinculada a esta oportunidade"
                  loading={loading}
                  pageSize={5}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="negotiations" className="mt-0">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle className="flex items-center gap-2">
                  <MessageSquare className="h-5 w-5 text-muted-foreground" /> Negociações
                </CardTitle>
                <Button size="sm" onClick={() => { setSelectedNegotiation(null); setIsNegotiationFormOpen(true) }}>
                  <TrendingUp className="mr-2 h-4 w-4" /> Nova negociação
                </Button>
              </CardHeader>
              <CardContent>
                <div className="mb-3 flex flex-wrap gap-2">
                  <Button size="sm" variant="outline" onClick={() => selectedNegotiation && setIsNegotiationFormOpen(true)} disabled={!selectedNegotiation}>
                    <Pencil className="mr-2 h-4 w-4" /> Editar
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => setIsApprovalRequestFormOpen(true)} disabled={!selectedNegotiation}>
                    <CheckCircle className="mr-2 h-4 w-4" /> Solicitar aprovação
                  </Button>
                  <Button size="sm" variant="outline-danger" onClick={() => void handleDeleteNegotiation()} disabled={!selectedNegotiation || actionLoading}>
                    <Trash2 className="mr-2 h-4 w-4" /> Excluir
                  </Button>
                </div>
                <DataTable
                  columns={negotiationColumns}
                  data={opportunity?.negotiations || []}
                  rowKey="id"
                  selectedRows={selectedNegotiation ? [selectedNegotiation] : []}
                  onSelectionChange={(rows) => setSelectedNegotiation(rows[0] ?? null)}
                  emptyText="Nenhuma negociação cadastrada"
                  loading={loading}
                  pageSize={5}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="approvals" className="mt-0">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle className="flex items-center gap-2">
                  <CheckCircle className="h-5 w-5 text-muted-foreground" /> Aprovações
                </CardTitle>
                <Button size="sm" onClick={() => setIsApprovalRequestFormOpen(true)} disabled={!selectedNegotiation}>
                  <CheckCircle className="mr-2 h-4 w-4" /> Nova aprovação
                </Button>
              </CardHeader>
              <CardContent>
                <div className="mb-3 flex flex-wrap gap-2">
                  <Button size="sm" variant="outline-success" onClick={() => void handleApproveRequest()} disabled={!selectedApprovalRequest || selectedApprovalRequest.status !== 1 || actionLoading}>
                    <ThumbsUp className="mr-2 h-4 w-4" /> Aprovar
                  </Button>
                  <Button size="sm" variant="outline-danger" onClick={() => void handleRejectRequest()} disabled={!selectedApprovalRequest || selectedApprovalRequest.status !== 1 || actionLoading}>
                    <ThumbsDown className="mr-2 h-4 w-4" /> Rejeitar
                  </Button>
                </div>
                <DataTable
                  columns={approvalColumns}
                  data={approvalRequests}
                  rowKey="id"
                  selectedRows={selectedApprovalRequest ? [selectedApprovalRequest] : []}
                  onSelectionChange={(rows) => setSelectedApprovalRequest(rows[0] ?? null)}
                  emptyText="Nenhuma aprovação cadastrada"
                  loading={loading}
                  pageSize={5}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="followups" className="mt-0">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle className="flex items-center gap-2">
                  <Clock className="h-5 w-5 text-muted-foreground" /> Follow-ups
                </CardTitle>
                <Button size="sm" onClick={() => { setSelectedFollowUp(null); setIsFollowUpFormOpen(true) }}>
                  <Clock className="mr-2 h-4 w-4" /> Novo follow-up
                </Button>
              </CardHeader>
              <CardContent>
                <div className="mb-3 flex flex-wrap gap-2">
                  <Button size="sm" variant="outline" onClick={() => selectedFollowUp && setIsFollowUpFormOpen(true)} disabled={!selectedFollowUp}>
                    <Pencil className="mr-2 h-4 w-4" /> Editar
                  </Button>
                  <Button size="sm" variant="outline-success" onClick={() => void handleCompleteFollowUp()} disabled={!selectedFollowUp || selectedFollowUp?.isCompleted || actionLoading}>
                    <CheckCircle className="mr-2 h-4 w-4" /> Concluir
                  </Button>
                  <Button size="sm" variant="outline-danger" onClick={() => void handleDeleteFollowUp()} disabled={!selectedFollowUp || actionLoading}>
                    <Trash2 className="mr-2 h-4 w-4" /> Excluir
                  </Button>
                </div>
                <DataTable
                  columns={followUpColumns}
                  data={opportunity?.followUps || []}
                  rowKey="id"
                  selectedRows={selectedFollowUp ? [selectedFollowUp] : []}
                  onSelectionChange={(rows) => setSelectedFollowUp(rows[0] ?? null)}
                  emptyText="Nenhum follow-up cadastrado"
                  loading={loading}
                  pageSize={5}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
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
    </div>
  )
}
