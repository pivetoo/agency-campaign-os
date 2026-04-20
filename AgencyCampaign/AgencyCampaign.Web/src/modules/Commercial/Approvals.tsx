import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, Button, useApi } from 'archon-ui'
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
  const [opportunities, setOpportunities] = useState<Opportunity[]>([])
  const { execute: fetchOpportunities, loading } = useApi<Opportunity[]>({ showErrorMessage: true })

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
      subtitle="Acompanhe aprovações pendentes, aprovadas e rejeitadas do Comercial"
      onRefresh={() => void loadData()}
      showDefaultActions={false}
    >
      <div className="mb-3 flex gap-2">
        <Button variant="outline" onClick={() => navigate('/comercial/oportunidades')}>Ir para oportunidades</Button>
      </div>
      <DataTable
        columns={columns}
        data={approvals}
        rowKey="id"
        onRowDoubleClick={(row) => navigate(`/comercial/oportunidades/${row.opportunityId}`)}
        emptyText="Nenhuma aprovação cadastrada"
        loading={loading}
      />
    </PageLayout>
  )
}
