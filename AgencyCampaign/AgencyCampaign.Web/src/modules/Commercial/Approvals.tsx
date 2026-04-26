import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, Button, useApi, useAuth } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { opportunityService, type Opportunity, type OpportunityApprovalRequest } from '../../services/opportunityService'

interface ApprovalRow extends OpportunityApprovalRequest {
  opportunityId: number
  opportunityName: string
  negotiationTitle: string
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

export default function CommercialApprovals() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const [opportunities, setOpportunities] = useState<Opportunity[]>([])
  const [selectedApproval, setSelectedApproval] = useState<ApprovalRow | null>(null)
  const { execute: fetchOpportunities, loading } = useApi<Opportunity[]>({ showErrorMessage: true })
  const { execute: executeAction, loading: actionLoading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadData = async () => {
    const result = await fetchOpportunities(() => opportunityService.getAll())
    if (result) {
      setOpportunities(result)
    }
  }

  useEffect(() => {
    void loadData()
  }, [])

  const approvals = useMemo<ApprovalRow[]>(() => (
    opportunities.flatMap((opportunity) =>
      opportunity.negotiations.flatMap((negotiation) =>
        (negotiation.approvalRequests ?? []).map((approval) => ({
          ...approval,
          opportunityId: opportunity.id,
          opportunityName: opportunity.name,
          negotiationTitle: negotiation.title,
        })),
      ),
    )
      .sort((a, b) => new Date(b.requestedAt).getTime() - new Date(a.requestedAt).getTime())
  ), [opportunities])

  const pendingApprovals = approvals.filter((approval) => approval.status === 1)
  const approvedApprovals = approvals.filter((approval) => approval.status === 2)
  const rejectedApprovals = approvals.filter((approval) => approval.status === 3)

  const decideApproval = async (status: 'approve' | 'reject') => {
    if (!selectedApproval) {
      return
    }

    const payload = {
      approvedByUserName: user?.name || 'Gestor comercial',
      decisionNotes: status === 'approve' ? 'Aprovação realizada pela Central de Aprovações.' : 'Rejeição realizada pela Central de Aprovações.',
    }

    const result = await executeAction(() => (
      status === 'approve'
        ? opportunityService.approveRequest(selectedApproval.id, payload)
        : opportunityService.rejectRequest(selectedApproval.id, payload)
    ))

    if (result !== null) {
      setSelectedApproval(null)
      await loadData()
    }
  }

  const columns: DataTableColumn<ApprovalRow>[] = [
    { key: 'approvalType', title: 'Tipo', dataIndex: 'approvalType', render: (value: number) => approvalTypeLabels[value] || '-' },
    { key: 'opportunityName', title: 'Oportunidade', dataIndex: 'opportunityName' },
    { key: 'negotiationTitle', title: 'Negociação', dataIndex: 'negotiationTitle' },
    { key: 'requestedByUserName', title: 'Solicitado por', dataIndex: 'requestedByUserName' },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      render: (value: number) => <Badge variant={value === 2 ? 'success' : value === 3 ? 'destructive' : 'warning'}>{approvalStatusLabels[value] || '-'}</Badge>,
    },
    { key: 'requestedAt', title: 'Solicitado em', dataIndex: 'requestedAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
  ]

  return (
    <PageLayout
      title="Aprovações"
      subtitle="Central para aprovar ou rejeitar exceções comerciais de desconto, margem, prazo e condições especiais"
      onRefresh={() => void loadData()}
      showDefaultActions={false}
    >
      <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-3">
        <div className="rounded-xl border border-border bg-card p-4"><div className="text-sm text-muted-foreground">Pendentes</div><div className="text-2xl font-bold">{pendingApprovals.length}</div></div>
        <div className="rounded-xl border border-border bg-card p-4"><div className="text-sm text-muted-foreground">Aprovadas</div><div className="text-2xl font-bold text-emerald-600">{approvedApprovals.length}</div></div>
        <div className="rounded-xl border border-border bg-card p-4"><div className="text-sm text-muted-foreground">Rejeitadas</div><div className="text-2xl font-bold text-destructive">{rejectedApprovals.length}</div></div>
      </div>

      <div className="mb-3 flex flex-wrap gap-2">
        <Button variant="outline" onClick={() => navigate('/comercial/pipeline')}>Ir para pipeline</Button>
        <Button variant="outline-success" disabled={!selectedApproval || selectedApproval.status !== 1 || actionLoading} onClick={() => void decideApproval('approve')}>Aprovar selecionada</Button>
        <Button variant="outline-danger" disabled={!selectedApproval || selectedApproval.status !== 1 || actionLoading} onClick={() => void decideApproval('reject')}>Rejeitar selecionada</Button>
      </div>
      <DataTable
        columns={columns}
        data={approvals}
        rowKey="id"
        selectedRows={selectedApproval ? [selectedApproval] : []}
        onSelectionChange={(rows) => setSelectedApproval(rows[0] ?? null)}
        onRowDoubleClick={(row) => navigate(`/comercial/oportunidades/${row.opportunityId}`)}
        emptyText="Nenhuma aprovação cadastrada"
        loading={loading}
        pageSize={5}
        pageSizeOptions={[5, 10, 20, 50]}
      />
    </PageLayout>
  )
}
