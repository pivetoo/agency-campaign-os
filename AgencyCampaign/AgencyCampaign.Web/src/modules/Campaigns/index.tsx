import { useEffect, useState } from 'react'
import { PageLayout, DataTable, useApi, Card, CardContent } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { campaignService } from '../../services/campaignService'
import type { Campaign, CampaignSummary } from '../../types/campaign'

export default function Campaigns() {
  const [campaigns, setCampaigns] = useState<Campaign[]>([])
  const [selectedCampaign, setSelectedCampaign] = useState<Campaign | null>(null)
  const [summary, setSummary] = useState<CampaignSummary | null>(null)

  const { execute: fetchCampaigns, loading } = useApi<Campaign[]>({ showErrorMessage: true })
  const { execute: fetchSummary } = useApi<CampaignSummary | null>({ showErrorMessage: true })

  useEffect(() => {
    void fetchCampaigns(() => campaignService.getAll()).then((result) => {
      if (result) {
        setCampaigns(result)
      }
    })
  }, [])

  useEffect(() => {
    if (!selectedCampaign) {
      setSummary(null)
      return
    }

    void fetchSummary(() => campaignService.getSummary(selectedCampaign.id)).then((result) => {
      if (result) {
        setSummary(result)
      }
    })
  }, [selectedCampaign])

  const columns: DataTableColumn<Campaign>[] = [
    { key: 'name', title: 'Campanha', dataIndex: 'name' },
    { key: 'brand', title: 'Marca', dataIndex: 'brand', render: (value: Campaign['brand']) => value?.name || '-' },
    { key: 'budget', title: 'Budget', dataIndex: 'budget', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'startsAt', title: 'Início', dataIndex: 'startsAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
  ]

  return (
    <div className="space-y-4">
      <PageLayout title="Campanhas" onRefresh={() => void fetchCampaigns(() => campaignService.getAll()).then((result) => result && setCampaigns(result))}>
        <DataTable
          columns={columns}
          data={campaigns}
          rowKey="id"
          selectedRows={selectedCampaign ? [selectedCampaign] : []}
          onSelectionChange={(rows) => setSelectedCampaign(rows[0] ?? null)}
          emptyText="Nenhuma campanha cadastrada"
          loading={loading}
        />
      </PageLayout>

      {summary && (
        <Card>
          <CardContent className="grid grid-cols-1 gap-4 p-6 md:grid-cols-3">
            <div>
              <div className="text-sm text-muted-foreground">Budget</div>
              <div className="text-2xl font-bold">R$ {summary.budget.toFixed(2)}</div>
            </div>
            <div>
              <div className="text-sm text-muted-foreground">Entregas</div>
              <div className="text-2xl font-bold">{summary.deliverablesCount}</div>
            </div>
            <div>
              <div className="text-sm text-muted-foreground">Fee da agência</div>
              <div className="text-2xl font-bold">R$ {summary.agencyFeeAmountTotal.toFixed(2)}</div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
