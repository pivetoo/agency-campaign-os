import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { opportunityService, type Opportunity } from '../../services/opportunityService'
import OpportunityFormModal from '../../components/modals/OpportunityFormModal'

const stageLabels: Record<number, string> = {
  1: 'Lead',
  2: 'Qualificada',
  3: 'Proposta',
  4: 'Negociação',
  5: 'Ganha',
  6: 'Perdida',
}

export default function CommercialOpportunities() {
  const navigate = useNavigate()
  const [opportunities, setOpportunities] = useState<Opportunity[]>([])
  const [selectedOpportunity, setSelectedOpportunity] = useState<Opportunity | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)

  const { execute: fetchOpportunities, loading } = useApi<Opportunity[]>({ showErrorMessage: true })

  const loadOpportunities = async () => {
    const result = await fetchOpportunities(() => opportunityService.getAll())
    if (result) {
      setOpportunities(result)
    }
  }

  useEffect(() => {
    void loadOpportunities()
  }, [])

  const columns: DataTableColumn<Opportunity>[] = [
    { key: 'name', title: 'Oportunidade', dataIndex: 'name' },
    { key: 'brand', title: 'Marca', dataIndex: 'brand', render: (value?: Opportunity['brand']) => value?.name || '-' },
    { key: 'estimatedValue', title: 'Valor estimado', dataIndex: 'estimatedValue', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'expectedCloseAt', title: 'Fechamento', dataIndex: 'expectedCloseAt', render: (value?: string) => value ? new Date(value).toLocaleDateString('pt-BR') : '-' },
    {
      key: 'stage',
      title: 'Estágio',
      dataIndex: 'stage',
      render: (value: number) => (
        <Badge variant={value === 5 ? 'success' : value === 6 ? 'destructive' : 'warning'}>
          {stageLabels[value] || '-'}
        </Badge>
      ),
    },
    { key: 'followUps', title: 'Follow-ups', dataIndex: 'followUps', render: (value?: Opportunity['followUps']) => value?.filter((item) => !item.isCompleted).length ?? 0 },
    { key: 'proposals', title: 'Propostas', dataIndex: 'proposals', render: (value?: Opportunity['proposals']) => value?.length ?? 0 },
  ]

  return (
    <>
      <PageLayout
        title="Oportunidades"
        subtitle="Gerencie o funil comercial e acompanhe propostas, negociações e follow-ups"
        onAdd={() => { setSelectedOpportunity(null); setIsFormOpen(true) }}
        onEdit={() => selectedOpportunity && setIsFormOpen(true)}
        onRefresh={() => void loadOpportunities()}
        selectedRowsCount={selectedOpportunity ? 1 : 0}
      >
        <DataTable
          columns={columns}
          data={opportunities}
          rowKey="id"
          selectedRows={selectedOpportunity ? [selectedOpportunity] : []}
          onSelectionChange={(rows) => setSelectedOpportunity(rows[0] ?? null)}
          onRowDoubleClick={(row) => navigate(`/comercial/oportunidades/${row.id}`)}
          emptyText="Nenhuma oportunidade cadastrada"
          loading={loading}
        />
      </PageLayout>

      <OpportunityFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        opportunity={selectedOpportunity}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedOpportunity(null)
          void loadOpportunities()
        }}
      />
    </>
  )
}
