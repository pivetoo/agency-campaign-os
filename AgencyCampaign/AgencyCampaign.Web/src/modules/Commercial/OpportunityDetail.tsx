import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageLayout, Button, Card, CardContent, CardHeader, CardTitle, DataTable, useApi, Badge, Tabs, TabsList, TabsTrigger, TabsContent, Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { opportunityService, type Opportunity, type OpportunityApprovalRequest, type OpportunityFollowUp, type OpportunityNegotiation } from '../../services/opportunityService'
import OpportunityFormModal from '../../components/modals/OpportunityFormModal'
import OpportunityNegotiationFormModal from '../../components/modals/OpportunityNegotiationFormModal'
import OpportunityFollowUpFormModal from '../../components/modals/OpportunityFollowUpFormModal'
import OpportunityApprovalRequestFormModal from '../../components/modals/OpportunityApprovalRequestFormModal'

const stageLabels: Record<number, string> = {
  1: 'Lead',
  2: 'Qualificada',
  3: 'Proposta',
  4: 'Negociação',
  5: 'Ganha',
  6: 'Perdida',
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

export default function OpportunityDetail() {
  const { id } = useParams<{ id: string }>()
  const opportunityId = Number(id || 0)

  const [opportunity, setOpportunity] = useState<Opportunity | null>(null)
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
      setSelectedStage(String(result.stage))
    }
  }

  useEffect(() => {
    if (!opportunityId) {
      return
    }

    void loadOpportunity()
  }, [opportunityId])

  const pendingFollowUpsCount = useMemo(() => opportunity?.followUps.filter((item) => !item.isCompleted).length ?? 0, [opportunity])

  const negotiationColumns: DataTableColumn<OpportunityNegotiation>[] = [
    { key: 'title', title: 'Título', dataIndex: 'title' },
    { key: 'amount', title: 'Valor', dataIndex: 'amount', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'status', title: 'Status', dataIndex: 'status', render: (value: number) => negotiationStatusLabels[value] || '-' },
    { key: 'negotiatedAt', title: 'Data', dataIndex: 'negotiatedAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    { key: 'notes', title: 'Observações', dataIndex: 'notes', render: (value?: string) => value || '-' },
  ]

  const approvalColumns: DataTableColumn<OpportunityApprovalRequest>[] = [
    { key: 'approvalType', title: 'Tipo', dataIndex: 'approvalType', render: (value: number) => approvalTypeLabels[value] || '-' },
    { key: 'status', title: 'Status', dataIndex: 'status', render: (value: number) => <Badge variant={value === 2 ? 'success' : value === 3 ? 'destructive' : 'warning'}>{approvalStatusLabels[value] || '-'}</Badge> },
    { key: 'requestedByUserName', title: 'Solicitado por', dataIndex: 'requestedByUserName' },
    { key: 'reason', title: 'Motivo', dataIndex: 'reason' },
    { key: 'requestedAt', title: 'Solicitado em', dataIndex: 'requestedAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
  ]

  const followUpColumns: DataTableColumn<OpportunityFollowUp>[] = [
    { key: 'subject', title: 'Assunto', dataIndex: 'subject' },
    { key: 'dueAt', title: 'Prazo', dataIndex: 'dueAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    {
      key: 'isCompleted',
      title: 'Status',
      dataIndex: 'isCompleted',
      render: (value: boolean) => <Badge variant={value ? 'success' : 'warning'}>{value ? 'Concluído' : 'Pendente'}</Badge>,
    },
    { key: 'notes', title: 'Observações', dataIndex: 'notes', render: (value?: string) => value || '-' },
  ]

  const proposalColumns: DataTableColumn<NonNullable<Opportunity['proposals']>[number]>[] = [
    { key: 'name', title: 'Proposta', dataIndex: 'name' },
    { key: 'totalValue', title: 'Valor', dataIndex: 'totalValue', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'status', title: 'Status', dataIndex: 'status', render: (value: number) => value },
    { key: 'validityUntil', title: 'Validade', dataIndex: 'validityUntil', render: (value?: string) => value ? new Date(value).toLocaleDateString('pt-BR') : '-' },
  ]

  const handleChangeStage = async () => {
    if (!opportunity) {
      return
    }

    const result = await executeAction(() => opportunityService.changeStage(opportunity.id, { stage: Number(selectedStage) }))
    if (result !== null) {
      await loadOpportunity()
    }
  }

  const handleCloseAsWon = async () => {
    if (!opportunity) {
      return
    }

    const result = await executeAction(() => opportunityService.closeAsWon(opportunity.id, { wonNotes: 'Oportunidade marcada como ganha.' }))
    if (result !== null) {
      await loadOpportunity()
    }
  }

  const handleCloseAsLost = async () => {
    if (!opportunity) {
      return
    }

    const result = await executeAction(() => opportunityService.closeAsLost(opportunity.id, { lossReason: 'Encerrada manualmente pela operação comercial.' }))
    if (result !== null) {
      await loadOpportunity()
    }
  }

  const handleDeleteNegotiation = async () => {
    if (!selectedNegotiation) {
      return
    }

    const result = await executeAction(() => opportunityService.deleteNegotiation(selectedNegotiation.id))
    if (result !== null) {
      setSelectedNegotiation(null)
      await loadOpportunity()
    }
  }

  const handleApproveRequest = async () => {
    if (!selectedApprovalRequest) {
      return
    }

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
    if (!selectedApprovalRequest) {
      return
    }

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
    if (!selectedFollowUp) {
      return
    }

    const result = await executeAction(() => opportunityService.completeFollowUp(selectedFollowUp.id))
    if (result !== null) {
      await loadOpportunity()
    }
  }

  const handleDeleteFollowUp = async () => {
    if (!selectedFollowUp) {
      return
    }

    const result = await executeAction(() => opportunityService.deleteFollowUp(selectedFollowUp.id))
    if (result !== null) {
      setSelectedFollowUp(null)
      await loadOpportunity()
    }
  }

  return (
    <div className="space-y-4">
      <PageLayout
        title={opportunity?.name || 'Oportunidade'}
        subtitle={opportunity?.brand?.name ? `${opportunity.brand.name} · detalhe comercial` : 'Detalhe comercial'}
        onRefresh={() => void loadOpportunity()}
        showDefaultActions={false}
      >
        <Card className="border-0 bg-transparent shadow-none">
          <CardContent className="grid gap-4 px-0 pt-0 pb-0 md:grid-cols-2 lg:grid-cols-4">
            <div>
              <p className="text-sm font-medium">Estágio</p>
              <div className="mt-1">
                <Badge variant={opportunity?.stage === 5 ? 'success' : opportunity?.stage === 6 ? 'destructive' : 'warning'}>
                  {opportunity ? stageLabels[opportunity.stage] : '-'}
                </Badge>
              </div>
            </div>
            <div>
              <p className="text-sm font-medium">Valor estimado</p>
              <p className="mt-1 text-lg font-semibold">R$ {(opportunity?.estimatedValue ?? 0).toFixed(2)}</p>
            </div>
            <div>
              <p className="text-sm font-medium">Previsão de fechamento</p>
              <p className="mt-1 text-lg font-semibold">{opportunity?.expectedCloseAt ? new Date(opportunity.expectedCloseAt).toLocaleDateString('pt-BR') : '-'}</p>
            </div>
            <div>
              <p className="text-sm font-medium">Follow-ups pendentes</p>
              <p className="mt-1 text-lg font-semibold">{pendingFollowUpsCount}</p>
            </div>
          </CardContent>
        </Card>

        <div className="flex flex-wrap gap-2">
          <Button variant="outline" onClick={() => setIsOpportunityFormOpen(true)}>Editar oportunidade</Button>
          <Select value={selectedStage} onValueChange={setSelectedStage}>
            <SelectTrigger style={{ width: 220 }}><SelectValue /></SelectTrigger>
            <SelectContent>
              {Object.entries(stageLabels).map(([value, label]) => (
                <SelectItem key={value} value={value}>{label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button variant="outline" onClick={() => void handleChangeStage()} disabled={actionLoading}>Atualizar estágio</Button>
          <Button variant="secondary" onClick={() => void handleCloseAsWon()} disabled={actionLoading}>Marcar como ganha</Button>
          <Button variant="danger" onClick={() => void handleCloseAsLost()} disabled={actionLoading}>Marcar como perdida</Button>
        </div>

        <Tabs defaultValue="summary">
          <TabsList>
            <TabsTrigger value="summary">Resumo</TabsTrigger>
            <TabsTrigger value="proposals">Propostas</TabsTrigger>
            <TabsTrigger value="negotiations">Negociações</TabsTrigger>
            <TabsTrigger value="approvals">Aprovações</TabsTrigger>
            <TabsTrigger value="followups">Follow-ups</TabsTrigger>
          </TabsList>

          <TabsContent value="summary">
            <Card>
              <CardHeader><CardTitle>Resumo</CardTitle></CardHeader>
              <CardContent className="space-y-2">
                <div><strong>Descrição:</strong> {opportunity?.description || '-'}</div>
                <div><strong>Contato:</strong> {opportunity?.contactName || '-'}</div>
                <div><strong>E-mail:</strong> {opportunity?.contactEmail || '-'}</div>
                <div><strong>Observações:</strong> {opportunity?.notes || '-'}</div>
                <div><strong>Fechada em:</strong> {opportunity?.closedAt ? new Date(opportunity.closedAt).toLocaleDateString('pt-BR') : '-'}</div>
                <div><strong>Motivo de perda:</strong> {opportunity?.lossReason || '-'}</div>
                <div><strong>Notas de ganho:</strong> {opportunity?.wonNotes || '-'}</div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="proposals">
            <Card>
              <CardHeader><CardTitle>Propostas vinculadas</CardTitle></CardHeader>
              <CardContent>
                <DataTable
                  columns={proposalColumns}
                  data={opportunity?.proposals || []}
                  rowKey="id"
                  emptyText="Nenhuma proposta vinculada"
                  loading={loading}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="negotiations">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle>Negociações</CardTitle>
                <Button size="sm" onClick={() => { setSelectedNegotiation(null); setIsNegotiationFormOpen(true) }}>Nova negociação</Button>
              </CardHeader>
              <CardContent>
                <div className="mb-2 flex gap-2">
                  <Button size="sm" variant="outline" onClick={() => selectedNegotiation && setIsNegotiationFormOpen(true)} disabled={!selectedNegotiation}>Editar</Button>
                  <Button size="sm" variant="secondary" onClick={() => setIsApprovalRequestFormOpen(true)} disabled={!selectedNegotiation}>Solicitar aprovação</Button>
                  <Button size="sm" variant="danger" onClick={() => void handleDeleteNegotiation()} disabled={!selectedNegotiation || actionLoading}>Excluir</Button>
                </div>
                <DataTable
                  columns={negotiationColumns}
                  data={opportunity?.negotiations || []}
                  rowKey="id"
                  selectedRows={selectedNegotiation ? [selectedNegotiation] : []}
                  onSelectionChange={(rows) => setSelectedNegotiation(rows[0] ?? null)}
                  emptyText="Nenhuma negociação cadastrada"
                  loading={loading}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="approvals">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle>Aprovações</CardTitle>
                <Button size="sm" onClick={() => setIsApprovalRequestFormOpen(true)} disabled={!selectedNegotiation}>Nova aprovação</Button>
              </CardHeader>
              <CardContent>
                <div className="mb-2 flex gap-2">
                  <Button size="sm" variant="secondary" onClick={() => void handleApproveRequest()} disabled={!selectedApprovalRequest || selectedApprovalRequest.status !== 1 || actionLoading}>Aprovar</Button>
                  <Button size="sm" variant="danger" onClick={() => void handleRejectRequest()} disabled={!selectedApprovalRequest || selectedApprovalRequest.status !== 1 || actionLoading}>Rejeitar</Button>
                </div>
                <DataTable
                  columns={approvalColumns}
                  data={approvalRequests}
                  rowKey="id"
                  selectedRows={selectedApprovalRequest ? [selectedApprovalRequest] : []}
                  onSelectionChange={(rows) => setSelectedApprovalRequest(rows[0] ?? null)}
                  emptyText="Nenhuma aprovação cadastrada"
                  loading={loading}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="followups">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle>Follow-ups</CardTitle>
                <Button size="sm" onClick={() => { setSelectedFollowUp(null); setIsFollowUpFormOpen(true) }}>Novo follow-up</Button>
              </CardHeader>
              <CardContent>
                <div className="mb-2 flex gap-2">
                  <Button size="sm" variant="outline" onClick={() => selectedFollowUp && setIsFollowUpFormOpen(true)} disabled={!selectedFollowUp}>Editar</Button>
                  <Button size="sm" variant="secondary" onClick={() => void handleCompleteFollowUp()} disabled={!selectedFollowUp || selectedFollowUp?.isCompleted || actionLoading}>Concluir</Button>
                  <Button size="sm" variant="danger" onClick={() => void handleDeleteFollowUp()} disabled={!selectedFollowUp || actionLoading}>Excluir</Button>
                </div>
                <DataTable
                  columns={followUpColumns}
                  data={opportunity?.followUps || []}
                  rowKey="id"
                  selectedRows={selectedFollowUp ? [selectedFollowUp] : []}
                  onSelectionChange={(rows) => setSelectedFollowUp(rows[0] ?? null)}
                  emptyText="Nenhum follow-up cadastrado"
                  loading={loading}
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
