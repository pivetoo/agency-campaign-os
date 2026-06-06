import { useEffect, useState } from 'react'
import { useApi, DataTable, type DataTableColumn } from 'archon-ui'
import { opportunityService } from '../../../services/opportunityService'
import { commercialReportService } from '../../../services/commercialReportService'
import type { CommercialAnalytics, StageConversion, Performer, StageTime } from '../../../types/commercialAnalytics'
import { formatCurrency } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

export default function Funil() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<CommercialAnalytics | null>(null)
  const { execute } = useApi<CommercialAnalytics | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => opportunityService.getAnalytics({ periodStart: new Date(range.from).toISOString(), periodEnd: new Date(range.to).toISOString() }))
    setData(result ?? null)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const stageColumns: DataTableColumn<StageConversion>[] = [
    { key: 'stageName', title: 'Estágio', dataIndex: 'stageName', primary: true },
    { key: 'entered', title: 'Entraram', dataIndex: 'entered' },
    { key: 'advanced', title: 'Avançaram', dataIndex: 'advanced' },
    { key: 'stuck', title: 'Parados', dataIndex: 'stuck' },
    { key: 'lost', title: 'Perdidos', dataIndex: 'lost' },
    { key: 'conversionRate', title: 'Conversão', dataIndex: 'conversionRate', render: (value: number) => `${value.toFixed(0)}%` },
  ]

  const performerColumns: DataTableColumn<Performer>[] = [
    { key: 'userName', title: 'Vendedor', dataIndex: 'userName', primary: true },
    { key: 'wonCount', title: 'Ganhos', dataIndex: 'wonCount' },
    { key: 'wonTotal', title: 'Valor', dataIndex: 'wonTotal', render: (value: number) => formatCurrency(value) },
  ]

  const stageTimeColumns: DataTableColumn<StageTime>[] = [
    { key: 'stageName', title: 'Estágio', dataIndex: 'stageName', primary: true },
    { key: 'averageDays', title: 'Dias médios', dataIndex: 'averageDays', render: (v: number) => v.toFixed(1) },
    { key: 'samples', title: 'Amostras', dataIndex: 'samples' },
  ]

  const inProgress = data?.conversionByStage.reduce((acc, s) => acc + s.stuck, 0) ?? 0

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Funil de Conversão" subtitle="Conversão por estágio do pipeline" filters={filters} onRefresh={() => void load()} onExportPdf={() => commercialReportService.exportFunilPdf(new Date(range.from).toISOString(), new Date(range.to).toISOString())}>
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Negócios fechados</p>
          <p className="text-2xl font-semibold">{data?.closedCount ?? 0}</p>
        </div>
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Win rate</p>
          <p className="text-2xl font-semibold">{data ? `${data.winRate.toFixed(1)}%` : '—'}</p>
        </div>
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Ciclo médio</p>
          <p className="text-2xl font-semibold">{data ? `${data.averageCycleDays.toFixed(0)} dias` : '—'}</p>
        </div>
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Em andamento</p>
          <p className="text-2xl font-semibold">{inProgress}</p>
        </div>
      </div>
      <DataTable columns={stageColumns} data={data?.conversionByStage ?? []} rowKey="stageId" emptyText="Nenhum dado no período." />
      <DataTable columns={performerColumns} data={data?.topPerformers ?? []} rowKey={(r) => String(r.userId ?? r.userName)} emptyText="Nenhum desempenho registrado." />
      <p className="text-sm font-semibold">Tempo médio por estágio</p>
      <DataTable columns={stageTimeColumns} data={data?.averageTimeInStage ?? []} rowKey="stageId" emptyText="Sem dados de tempo por estágio." />
    </ReportLayout>
  )
}
