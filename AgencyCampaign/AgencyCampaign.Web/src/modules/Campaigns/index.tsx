import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Eye } from 'lucide-react'
import { campaignService } from '../../services/campaignService'
import type { Campaign } from '../../types/campaign'
import CampaignFormModal from '../../components/modals/CampaignFormModal'

const campaignStatusKeys: Record<number, string> = {
  1: 'campaign.status.draft',
  2: 'campaign.status.planned',
  3: 'campaign.status.executing',
  4: 'campaign.status.reviewing',
  5: 'campaign.status.completed',
  6: 'campaign.status.cancelled',
}

export default function Campaigns() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [campaigns, setCampaigns] = useState<Campaign[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [selectedCampaign, setSelectedCampaign] = useState<Campaign | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)

  const { execute: fetchCampaigns, loading, pagination } = useApi<Campaign[]>({ showErrorMessage: true })
  const loadCampaigns = async () => {
    const result = await fetchCampaigns(() => campaignService.getAll({ page, pageSize }))
    if (result) {
      setCampaigns(result)
    }
  }

  useEffect(() => {
    void loadCampaigns()
  }, [page, pageSize])

  const columns: DataTableColumn<Campaign>[] = [
    { key: 'name', title: t('campaign.field.campaign'), dataIndex: 'name' },
    { key: 'brand', title: t('campaign.field.brand'), dataIndex: 'brand', render: (value: Campaign['brand']) => value?.name || '-' },
    { key: 'objective', title: t('campaign.field.objective'), dataIndex: 'objective', hiddenBelow: 'md', render: (value?: string) => value || '-' },
    { key: 'budget', title: t('campaign.field.budget'), dataIndex: 'budget', hiddenBelow: 'md', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'startsAt', title: t('common.field.startDate'), dataIndex: 'startsAt', hiddenBelow: 'lg', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    {
      key: 'status',
      title: t('common.field.status'),
      dataIndex: 'status',
      render: (value: number) => (
        <Badge variant={value === 5 ? 'success' : value === 6 ? 'destructive' : 'warning'}>
          {campaignStatusKeys[value] ? t(campaignStatusKeys[value]) : '-'}
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
        title={t('campaigns.title')}
        subtitle={t('campaigns.subtitle')}
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
          emptyText={t('campaigns.empty')}
          loading={loading}
          pageSize={pageSize}
          pageSizeOptions={[10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
        />
      </PageLayout>

      <CampaignFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        campaign={selectedCampaign}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedCampaign(null)
          if (page === 1) {
            void loadCampaigns()
          } else {
            setPage(1)
          }
        }}
      />
    </>
  )
}
