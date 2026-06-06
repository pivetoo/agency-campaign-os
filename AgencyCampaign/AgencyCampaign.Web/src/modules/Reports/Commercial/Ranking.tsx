import { useEffect, useState } from 'react'
import { useApi, DataTable, Badge, type DataTableColumn } from 'archon-ui'
import { commercialReportService, type BrandRanking, type BrandRankingLine } from '../../../services/commercialReportService'
import { formatCurrency } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

export default function Ranking() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<BrandRanking | null>(null)
  const { execute } = useApi<BrandRanking | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => commercialReportService.getBrandRanking(new Date(range.from).toISOString(), new Date(range.to).toISOString()))
    setData(result ?? null)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const columns: DataTableColumn<BrandRankingLine>[] = [
    { key: 'brandName', title: 'Marca', dataIndex: 'brandName', primary: true },
    { key: 'wonCount', title: 'Ganhos', dataIndex: 'wonCount' },
    { key: 'lostCount', title: 'Perdas', dataIndex: 'lostCount', hiddenBelow: 'sm' },
    { key: 'wonValue', title: 'Valor ganho', dataIndex: 'wonValue', render: (value: number) => formatCurrency(value) },
    { key: 'winRate', title: 'Win rate', dataIndex: 'winRate', cardTag: true, render: (value: number) => <Badge variant={value >= 50 ? 'success' : 'warning'}>{value.toFixed(1)}%</Badge> },
  ]

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Ranking por Marca" subtitle="Marcas por valor ganho no período" filters={filters} onRefresh={() => void load()} onExportCsv={() => commercialReportService.exportBrandRanking(new Date(range.from).toISOString(), new Date(range.to).toISOString())}>
      <DataTable columns={columns} data={data?.lines ?? []} rowKey="brandId" emptyText="Nenhuma marca com negócios fechados no período." />
    </ReportLayout>
  )
}
