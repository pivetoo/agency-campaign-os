import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Eye } from 'lucide-react'
import { campaignService } from '../../services/campaignService'
import type { Campaign } from '../../types/campaign'
import CampaignFormModal from '../../components/modals/CampaignFormModal'

const campaignStatusLabels: Record<number, string> = {
  1: 'Rascunho',
  2: 'Planejada',
  3: 'Em execução',
  4: 'Em revisão',
  5: 'Concluída',
  6: 'Cancelada',
}

export default function Campaigns() {
  const navigate = useNavigate()
  const [campaigns, setCampaigns] = useState<Campaign[]>([])
  const [selectedCampaign, setSelectedCampaign] = useState<Campaign | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)

  const { execute: fetchCampaigns, loading } = useApi<Campaign[]>({ showErrorMessage: true })
  const loadCampaigns = async () => {
    const result = await fetchCampaigns(() => campaignService.getAll())
    if (result) {
      setCampaigns(result)
    }
  }

  useEffect(() => {
    void loadCampaigns()
  }, [])

  const columns: DataTableColumn<Campaign>[] = [
    { key: 'name', title: 'Campanha', dataIndex: 'name' },
    { key: 'brand', title: 'Marca', dataIndex: 'brand', render: (value: Campaign['brand']) => value?.name || '-' },
    { key: 'objective', title: 'Objetivo', dataIndex: 'objective', render: (value?: string) => value || '-' },
    { key: 'budget', title: 'Budget', dataIndex: 'budget', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'startsAt', title: 'Início', dataIndex: 'startsAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      render: (value: number) => (
        <Badge variant={value === 5 ? 'success' : value === 6 ? 'destructive' : 'warning'}>
          {campaignStatusLabels[value] || '-'}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: '',
      dataIndex: undefined,
      width: 56,
      render: (_: any, record: Campaign) => (
        <button
          className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
          onClick={(e) => { e.stopPropagation(); navigate(`/campanhas/${record.id}`) }}
        >
          <Eye size={16} />
        </button>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title="Campanhas"
        subtitle="Gerencie campanhas, casting, entregas e operação"
        onAdd={() => { setSelectedCampaign(null); setIsFormOpen(true) }}
        onEdit={() => selectedCampaign && setIsFormOpen(true)}
        onRefresh={() => void loadCampaigns()}
        selectedRowsCount={selectedCampaign ? 1 : 0}
      >
        <DataTable
          columns={columns}
          data={campaigns}
          rowKey="id"
          selectedRows={selectedCampaign ? [selectedCampaign] : []}
          onSelectionChange={(rows) => setSelectedCampaign(rows[0] ?? null)}
          emptyText="Nenhuma campanha cadastrada"
          loading={loading}
          pageSize={5}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <CampaignFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        campaign={selectedCampaign}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedCampaign(null)
          void loadCampaigns()
        }}
      />
    </>
  )
}
