import { useEffect, useState } from 'react'
import { useApi, DataTable, type DataTableColumn } from 'archon-ui'
import { productionReportService, type CampaignPerformance, type CampaignPerformanceLine } from '../../../services/productionReportService'
import { formatNumber, formatCurrency } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

const renderRate = (v?: number | null) => (v != null ? `${v.toFixed(2)}%` : '-')
const renderEmv = (v?: number | null) => (v != null ? formatCurrency(v) : '-')

export default function PerformanceCampanhas() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<CampaignPerformance | null>(null)
  const { execute } = useApi<CampaignPerformance | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => productionReportService.getCampaignPerformance(new Date(range.from).toISOString(), new Date(range.to).toISOString()))
    setData(result ?? null)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const lines = data?.lines ?? []
  const totalReach = lines.reduce((acc, l) => acc + l.totalReach, 0)
  const totalEngagement = lines.reduce((acc, l) => acc + l.totalEngagement, 0)
  const totalEmv = lines.reduce((acc, l) => acc + (l.emv ?? 0), 0)

  const columns: DataTableColumn<CampaignPerformanceLine>[] = [
    { key: 'campaignName', title: 'Campanha', dataIndex: 'campaignName', primary: true },
    { key: 'brandName', title: 'Marca', dataIndex: 'brandName', hiddenBelow: 'md', render: (value?: string | null) => value || '-' },
    { key: 'deliverables', title: 'Entregas', dataIndex: 'deliverables' },
    { key: 'totalReach', title: 'Alcance', dataIndex: 'totalReach', render: (value: number) => formatNumber(value) },
    { key: 'totalImpressions', title: 'Impressões', dataIndex: 'totalImpressions', hiddenBelow: 'lg', render: (value: number) => formatNumber(value) },
    { key: 'totalEngagement', title: 'Engajamento', dataIndex: 'totalEngagement', render: (value: number) => formatNumber(value) },
    { key: 'avgEngagementRate', title: 'Taxa eng.', dataIndex: 'avgEngagementRate', hiddenBelow: 'sm', render: renderRate },
    { key: 'emv', title: 'EMV', dataIndex: 'emv', render: renderEmv },
  ]

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Performance de Campanhas" subtitle="Alcance, engajamento e EMV por campanha (entregas publicadas no período)" filters={filters} onRefresh={() => void load()} onExportCsv={() => productionReportService.exportCampaignPerformance(new Date(range.from).toISOString(), new Date(range.to).toISOString())} onExportPdf={() => productionReportService.exportCampaignPerformancePdf(new Date(range.from).toISOString(), new Date(range.to).toISOString())}>
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Campanhas</p>
          <p className="text-lg font-semibold">{lines.length}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Alcance total</p>
          <p className="text-lg font-semibold">{formatNumber(totalReach)}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Engajamento total</p>
          <p className="text-lg font-semibold">{formatNumber(totalEngagement)}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">EMV total</p>
          <p className="text-lg font-semibold">{formatCurrency(totalEmv)}</p>
        </div>
      </div>
      <DataTable columns={columns} data={lines} rowKey="campaignId" emptyText="Nenhuma entrega publicada no período." />
    </ReportLayout>
  )
}
