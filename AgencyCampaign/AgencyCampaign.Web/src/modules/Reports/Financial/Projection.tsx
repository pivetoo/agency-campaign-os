import { useEffect, useMemo, useState } from 'react'
import { useApi, DataTable, SearchableSelect, type DataTableColumn } from 'archon-ui'
import { ResponsiveLine } from '@nivo/line'
import { financialReportService, type CashFlowProjection, type CashFlowProjectionWeek } from '../../../services/financialReportService'
import { formatCurrency, formatDate } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'

const WEEK_OPTIONS = [{ value: '8', label: '8 semanas' }, { value: '12', label: '12 semanas' }, { value: '26', label: '26 semanas' }]

export default function Projection() {
  const [weeks, setWeeks] = useState(12)
  const [data, setData] = useState<CashFlowProjection | null>(null)
  const { execute } = useApi<CashFlowProjection | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => financialReportService.getCashFlowProjection(weeks))
    setData(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [weeks])

  const chartData = useMemo(() => {
    if (!data || data.series.length === 0) {
      return []
    }
    return [{
      id: 'Saldo projetado',
      color: '#1F3B61',
      data: data.series.map((week) => ({ x: formatDate(week.weekStart), y: week.projectedBalance })),
    }]
  }, [data])

  const columns: DataTableColumn<CashFlowProjectionWeek>[] = [
    { key: 'weekStart', title: 'Semana', dataIndex: 'weekStart', primary: true, render: (value: string) => formatDate(value) },
    { key: 'inflow', title: 'Entrada', dataIndex: 'inflow', render: (value: number) => formatCurrency(value) },
    { key: 'outflow', title: 'Saída', dataIndex: 'outflow', render: (value: number) => formatCurrency(value) },
    { key: 'net', title: 'Líquido', dataIndex: 'net', render: (value: number) => formatCurrency(value) },
    { key: 'projectedBalance', title: 'Saldo projetado', dataIndex: 'projectedBalance', render: (value: number) => formatCurrency(value) },
  ]

  const filters = (
    <div className="space-y-1">
      <label className="text-xs text-muted-foreground">Horizonte</label>
      <SearchableSelect value={String(weeks)} onValueChange={(v) => setWeeks(Number(v))} options={WEEK_OPTIONS} />
    </div>
  )

  return (
    <ReportLayout title="Projeção de Fluxo" subtitle="Saldo projetado semana a semana" filters={filters} onRefresh={() => void load()} onExportCsv={() => financialReportService.exportCashFlowProjection(weeks)}>
      <div className="rounded-md border p-3">
        <p className="text-xs text-muted-foreground">Saldo de abertura</p>
        <p className="text-lg font-semibold text-primary">{formatCurrency(data?.openingBalance ?? 0)}</p>
      </div>
      <div style={{ height: 320 }}>
        {chartData.length === 0 ? (
          <p className="flex h-full items-center justify-center text-sm text-muted-foreground">Nenhum dado projetado.</p>
        ) : (
          <ResponsiveLine
            data={chartData}
            margin={{ top: 20, right: 30, bottom: 50, left: 70 }}
            xScale={{ type: 'point' }}
            yScale={{ type: 'linear', min: 'auto', max: 'auto' }}
            axisBottom={{ tickRotation: -30 }}
            axisLeft={{ format: (value: number) => `R$ ${(value / 1000).toFixed(0)}k` }}
            colors={(serie) => (serie.color as string) ?? '#1F3B61'}
            pointSize={6}
            useMesh
            enableArea
            areaOpacity={0.1}
          />
        )}
      </div>
      <DataTable columns={columns} data={data?.series ?? []} rowKey="weekStart" emptyText="Nenhum dado projetado." />
    </ReportLayout>
  )
}
