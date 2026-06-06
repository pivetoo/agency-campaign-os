import { useEffect, useState } from 'react'
import { useApi, DataTable, type DataTableColumn } from 'archon-ui'
import { opportunityService } from '../../../services/opportunityService'
import type { CommercialForecast, CommercialForecastStageBreakdown } from '../../../types/commercialForecast'
import { formatCurrency } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

export default function Forecast() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<CommercialForecast | null>(null)
  const { execute } = useApi<CommercialForecast | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => opportunityService.getForecast({ periodStart: new Date(range.from).toISOString(), periodEnd: new Date(range.to).toISOString() }))
    setData(result ?? null)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const columns: DataTableColumn<CommercialForecastStageBreakdown>[] = [
    { key: 'stageName', title: 'Estágio', dataIndex: 'stageName', primary: true },
    { key: 'count', title: 'Qtd', dataIndex: 'count' },
    { key: 'totalValue', title: 'Valor', dataIndex: 'totalValue', render: (value: number) => formatCurrency(value) },
    { key: 'weightedValue', title: 'Ponderado', dataIndex: 'weightedValue', render: (value: number) => formatCurrency(value) },
    { key: 'averageProbability', title: 'Prob. média', dataIndex: 'averageProbability', render: (value: number) => `${value.toFixed(0)}%` },
  ]

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Previsão (Forecast)" subtitle="Pipeline ponderado por probabilidade" filters={filters} onRefresh={() => void load()}>
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Ponderado</p>
          <p className="text-2xl font-semibold text-primary">{formatCurrency(data?.weightedTotal ?? 0)}</p>
        </div>
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Bruto</p>
          <p className="text-2xl font-semibold">{formatCurrency(data?.unweightedTotal ?? 0)}</p>
        </div>
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Ganho</p>
          <p className="text-2xl font-semibold text-emerald-600">{formatCurrency(data?.wonTotal ?? 0)}</p>
        </div>
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Em aberto</p>
          <p className="text-2xl font-semibold">{data?.openCount ?? 0}</p>
        </div>
      </div>
      <DataTable columns={columns} data={data?.byStage ?? []} rowKey="stageId" emptyText="Nenhum dado no período." />
    </ReportLayout>
  )
}
