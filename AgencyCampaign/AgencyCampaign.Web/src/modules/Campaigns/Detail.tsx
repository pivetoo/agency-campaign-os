import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageLayout, Button, Card, CardContent, CardHeader, CardTitle, DataTable, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Plus } from 'lucide-react'
import { campaignService } from '../../services/campaignService'
import { campaignDeliverableService } from '../../services/campaignDeliverableService'
import type { Campaign, CampaignSummary } from '../../types/campaign'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'
import CampaignDeliverableFormModal from '../../components/modals/CampaignDeliverableFormModal'

const statusLabels: Record<number, string> = {
  1: 'Pendente',
  2: 'Em revisão',
  3: 'Aprovada',
  4: 'Publicada',
  5: 'Cancelada',
}

export default function CampaignDetail() {
  const { id } = useParams<{ id: string }>()
  const campaignId = Number(id || 0)

  const [campaign, setCampaign] = useState<Campaign | null>(null)
  const [summary, setSummary] = useState<CampaignSummary | null>(null)
  const [deliverables, setDeliverables] = useState<CampaignDeliverable[]>([])
  const [selectedDeliverable, setSelectedDeliverable] = useState<CampaignDeliverable | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)

  const { execute: fetchCampaign } = useApi<Campaign[]>({ showErrorMessage: true })
  const { execute: fetchSummary } = useApi<CampaignSummary | null>({ showErrorMessage: true })
  const { execute: fetchDeliverables, loading } = useApi<CampaignDeliverable[]>({ showErrorMessage: true })

  const loadCampaign = async () => {
    const result = await fetchCampaign(() => campaignService.getAll())
    if (result) {
      const current = result.find((item) => item.id === campaignId) || null
      setCampaign(current)
    }
  }

  const loadSummary = async () => {
    const result = await fetchSummary(() => campaignService.getSummary(campaignId))
    if (result) {
      setSummary(result)
    }
  }

  const loadDeliverables = async () => {
    const result = await fetchDeliverables(() => campaignDeliverableService.getByCampaign(campaignId))
    if (result) {
      setDeliverables(result)
    }
  }

  useEffect(() => {
    if (!campaignId) {
      return
    }

    void loadCampaign()
    void loadSummary()
    void loadDeliverables()
  }, [campaignId])

  const columns: DataTableColumn<CampaignDeliverable>[] = [
    { key: 'title', title: 'Entrega', dataIndex: 'title' },
    { key: 'creator', title: 'Influenciador', dataIndex: 'creator', render: (value: CampaignDeliverable['creator']) => value?.name || '-' },
    { key: 'status', title: 'Status', dataIndex: 'status', render: (value: number) => statusLabels[value] || '-' },
    { key: 'dueAt', title: 'Prazo', dataIndex: 'dueAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    { key: 'grossAmount', title: 'Valor bruto', dataIndex: 'grossAmount', render: (value: number) => `R$ ${value.toFixed(2)}` },
  ]

  return (
    <div className="space-y-4">
      <PageLayout
        title={campaign?.name || 'Campanha'}
        subtitle={campaign?.brand?.name || ''}
        onRefresh={() => {
          void loadCampaign()
          void loadSummary()
          void loadDeliverables()
        }}
        showDefaultActions={false}
      >
        <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
          <Card>
            <CardHeader><CardTitle className="text-sm">Budget</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold">R$ {(summary?.budget ?? 0).toFixed(2)}</CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle className="text-sm">Entregas</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold">{summary?.deliverablesCount ?? 0}</CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle className="text-sm">Fee da agência</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold">R$ {(summary?.agencyFeeAmountTotal ?? 0).toFixed(2)}</CardContent>
          </Card>
        </div>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle>Entregas da campanha</CardTitle>
            <Button size="sm" onClick={() => { setSelectedDeliverable(null); setIsFormOpen(true) }}>
              <Plus size={16} className="mr-2" />
              Nova entrega
            </Button>
          </CardHeader>
          <CardContent>
            <DataTable
              columns={columns}
              data={deliverables}
              rowKey="id"
              selectedRows={selectedDeliverable ? [selectedDeliverable] : []}
              onSelectionChange={(rows) => setSelectedDeliverable(rows[0] ?? null)}
              onRowDoubleClick={(row) => {
                setSelectedDeliverable(row)
                setIsFormOpen(true)
              }}
              emptyText="Nenhuma entrega cadastrada"
              loading={loading}
            />
          </CardContent>
        </Card>
      </PageLayout>

      <CampaignDeliverableFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        campaignId={campaignId}
        deliverable={selectedDeliverable}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedDeliverable(null)
          void loadSummary()
          void loadDeliverables()
        }}
      />
    </div>
  )
}
