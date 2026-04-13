import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { campaignService } from '../../services/campaignService'
import type { Campaign } from '../../types/campaign'
import CampaignFormModal from '../../components/modals/CampaignFormModal'

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
    { key: 'budget', title: 'Budget', dataIndex: 'budget', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'startsAt', title: 'Início', dataIndex: 'startsAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? 'Ativa' : 'Inativa'}
        </Badge>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title="Campanhas"
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
          onRowDoubleClick={(row) => {
            navigate(`/campanhas/${row.id}`)
          }}
          emptyText="Nenhuma campanha cadastrada"
          loading={loading}
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
