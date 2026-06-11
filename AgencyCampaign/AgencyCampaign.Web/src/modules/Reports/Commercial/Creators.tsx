import { useEffect, useState } from 'react'
import { useApi, DataTable, type DataTableColumn } from 'archon-ui'
import { commercialReportService, type CreatorRevenue, type CreatorRevenueLine } from '../../../services/commercialReportService'
import { formatCurrency } from '../../../lib/format'
import { periodStartIso, periodEndExclusiveIso } from '../../../lib/reportPeriod'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

export default function CreatorRevenueReport() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<CreatorRevenue | null>(null)
  const { execute } = useApi<CreatorRevenue | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => commercialReportService.getCreatorRevenue(periodStartIso(range.from), periodEndExclusiveIso(range.to)))
    setData(result ?? null)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const columns: DataTableColumn<CreatorRevenueLine>[] = [
    { key: 'creatorName', title: 'Creator', dataIndex: 'creatorName', primary: true },
    { key: 'dealCount', title: 'Negócios', dataIndex: 'dealCount', hiddenBelow: 'sm' },
    { key: 'itemCount', title: 'Itens', dataIndex: 'itemCount', hiddenBelow: 'sm' },
    { key: 'totalValue', title: 'Valor vendido', dataIndex: 'totalValue', cardTag: true, render: (value: number) => formatCurrency(value) },
  ]

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Receita por Creator" subtitle="Quanto cada creator gerou em negócios fechados no período" filters={filters} onRefresh={() => void load()}>
      <DataTable columns={columns} data={data?.lines ?? []} rowKey="creatorId" emptyText="Nenhum creator com receita no período." />
    </ReportLayout>
  )
}
