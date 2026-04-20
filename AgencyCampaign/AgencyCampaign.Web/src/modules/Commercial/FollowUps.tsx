import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, Button, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { opportunityService, type Opportunity, type OpportunityFollowUp } from '../../services/opportunityService'

interface FollowUpRow extends OpportunityFollowUp {
  opportunityName: string
  brandName: string
}

export default function CommercialFollowUps() {
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

  const followUps = useMemo<FollowUpRow[]>(() => (
    opportunities.flatMap((opportunity) =>
      opportunity.followUps.map((followUp) => ({
        ...followUp,
        opportunityName: opportunity.name,
        brandName: opportunity.brand?.name || '-',
      })),
    )
      .sort((a, b) => new Date(a.dueAt).getTime() - new Date(b.dueAt).getTime())
  ), [opportunities])

  const columns: DataTableColumn<FollowUpRow>[] = [
    { key: 'subject', title: 'Follow-up', dataIndex: 'subject' },
    { key: 'opportunityName', title: 'Oportunidade', dataIndex: 'opportunityName' },
    { key: 'brandName', title: 'Marca', dataIndex: 'brandName' },
    { key: 'dueAt', title: 'Prazo', dataIndex: 'dueAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    {
      key: 'isCompleted',
      title: 'Status',
      dataIndex: 'isCompleted',
      render: (value: boolean) => <Badge variant={value ? 'success' : 'warning'}>{value ? 'Concluído' : 'Pendente'}</Badge>,
    },
  ]

  return (
    <PageLayout
      title="Follow-ups"
      subtitle="Acompanhe próximos passos, pendências e follow-ups vencidos"
      onRefresh={() => void loadData()}
      showDefaultActions={false}
    >
      <div className="mb-3 flex gap-2">
        <Button variant="outline" onClick={() => navigate('/comercial/oportunidades')}>Ir para oportunidades</Button>
      </div>
      <DataTable
        columns={columns}
        data={followUps}
        rowKey="id"
        onRowDoubleClick={(row) => navigate(`/comercial/oportunidades/${row.opportunityId}`)}
        emptyText="Nenhum follow-up cadastrado"
        loading={loading}
      />
    </PageLayout>
  )
}
