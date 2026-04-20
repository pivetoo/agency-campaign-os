import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Button, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { opportunityService, type Opportunity, type OpportunityNegotiation } from '../../services/opportunityService'

interface NegotiationRow extends OpportunityNegotiation {
  opportunityName: string
  brandName: string
}

export default function CommercialNegotiations() {
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

  const negotiations = useMemo<NegotiationRow[]>(() => (
    opportunities.flatMap((opportunity) =>
      opportunity.negotiations.map((negotiation) => ({
        ...negotiation,
        opportunityName: opportunity.name,
        brandName: opportunity.brand?.name || '-',
      })),
    )
      .sort((a, b) => new Date(b.negotiatedAt).getTime() - new Date(a.negotiatedAt).getTime())
  ), [opportunities])

  const columns: DataTableColumn<NegotiationRow>[] = [
    { key: 'title', title: 'Negociação', dataIndex: 'title' },
    { key: 'opportunityName', title: 'Oportunidade', dataIndex: 'opportunityName' },
    { key: 'brandName', title: 'Marca', dataIndex: 'brandName' },
    { key: 'amount', title: 'Valor', dataIndex: 'amount', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'negotiatedAt', title: 'Data', dataIndex: 'negotiatedAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
  ]

  return (
    <PageLayout
      title="Negociações"
      subtitle="Acompanhe todas as negociações comerciais abertas e históricas"
      onRefresh={() => void loadData()}
      showDefaultActions={false}
    >
      <div className="mb-3 flex gap-2">
        <Button variant="outline" onClick={() => navigate('/comercial/oportunidades')}>Ir para oportunidades</Button>
      </div>
      <DataTable
        columns={columns}
        data={negotiations}
        rowKey="id"
        onRowDoubleClick={(row) => navigate(`/comercial/oportunidades/${row.opportunityId}`)}
        emptyText="Nenhuma negociação cadastrada"
        loading={loading}
      />
    </PageLayout>
  )
}
