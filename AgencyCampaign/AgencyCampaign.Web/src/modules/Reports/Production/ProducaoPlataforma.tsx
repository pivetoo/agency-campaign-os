import { useEffect, useState } from 'react'
import { useApi, DataTable, type DataTableColumn } from 'archon-ui'
import { productionReportService, type PlatformProduction, type PlatformProductionLine } from '../../../services/productionReportService'
import { formatNumber } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

const renderRate = (v?: number | null) => (v != null ? `${v.toFixed(2)}%` : '-')

export default function ProducaoPlataforma() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<PlatformProduction | null>(null)
  const { execute } = useApi<PlatformProduction | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => productionReportService.getPlatformProduction(new Date(range.from).toISOString(), new Date(range.to).toISOString()))
    setData(result ?? null)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const lines = data?.lines ?? []
  const totalReach = lines.reduce((acc, l) => acc + l.totalReach, 0)
  const totalEngagement = lines.reduce((acc, l) => acc + l.totalEngagement, 0)

  const columns: DataTableColumn<PlatformProductionLine>[] = [
    { key: 'platformName', title: 'Plataforma', dataIndex: 'platformName', primary: true },
    { key: 'deliverables', title: 'Entregas', dataIndex: 'deliverables' },
    { key: 'totalReach', title: 'Alcance', dataIndex: 'totalReach', render: (value: number) => formatNumber(value) },
    { key: 'totalImpressions', title: 'Impressões', dataIndex: 'totalImpressions', hiddenBelow: 'md', render: (value: number) => formatNumber(value) },
    { key: 'totalEngagement', title: 'Engajamento', dataIndex: 'totalEngagement', render: (value: number) => formatNumber(value) },
    { key: 'avgEngagementRate', title: 'Taxa eng.', dataIndex: 'avgEngagementRate', render: renderRate },
  ]

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Produção por Plataforma" subtitle="Entregas e métricas por plataforma (publicadas no período)" filters={filters} onRefresh={() => void load()} onExportCsv={() => productionReportService.exportPlatformProduction(new Date(range.from).toISOString(), new Date(range.to).toISOString())}>
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Plataformas</p>
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
      </div>
      <DataTable columns={columns} data={lines} rowKey="platformId" emptyText="Nenhuma entrega publicada no período." />
    </ReportLayout>
  )
}
