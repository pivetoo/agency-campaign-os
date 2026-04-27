import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, Button, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Eye } from 'lucide-react'
import { opportunityService, type Opportunity } from '../../services/opportunityService'
import OpportunityFormModal from '../../components/modals/OpportunityFormModal'

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
      dataIndex: 'commercialPipelineStage',
      render: (value?: Opportunity['commercialPipelineStage']) => (
        <Badge variant={value?.finalBehavior === 1 ? 'success' : value?.finalBehavior === 2 ? 'destructive' : 'warning'}>
          {value?.name || '-'}
        </Badge>
      ),
    },
    { key: 'followUps', title: 'Follow-ups', dataIndex: 'followUps', render: (value?: Opportunity['followUps']) => value?.filter((item) => !item.isCompleted).length ?? 0 },
    { key: 'proposals', title: 'Propostas', dataIndex: 'proposals', render: (value?: Opportunity['proposals']) => value?.length ?? 0 },
    {
      key: 'actions',
      title: '',
      dataIndex: undefined,
      width: 56,
      render: (_: any, record: Opportunity) => (
        <Button variant="ghost" size="icon" onClick={(e) => { e.stopPropagation(); navigate(`/comercial/oportunidades/${record.id}`) }}>
          <Eye size={16} />
        </Button>
      ),
    },
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
          emptyText="Nenhuma oportunidade cadastrada"
          loading={loading}
          pageSize={5}
          pageSizeOptions={[5, 10, 20, 50]}
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
