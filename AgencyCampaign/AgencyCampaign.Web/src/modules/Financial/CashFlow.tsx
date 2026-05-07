import { useEffect, useMemo, useState } from 'react'
import { PageLayout, Card, CardContent, useApi, SearchableSelect, Input } from 'archon-ui'
import { ResponsiveLine } from '@nivo/line'
import {
  CashFlowGranularity,
  financialReportService,
  type CashFlowGranularityValue,
  type CashFlowSeries,
} from '../../services/financialReportService'

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function defaultRange(granularity: CashFlowGranularityValue): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now)
  if (granularity === CashFlowGranularity.Month) {
    from.setMonth(from.getMonth() - 5)
    from.setDate(1)
  } else if (granularity === CashFlowGranularity.Week) {
    from.setDate(from.getDate() - 56)
  } else {
    from.setDate(from.getDate() - 30)
  }
  const to = new Date(now)
  to.setMonth(to.getMonth() + 1)
  return {
    from: from.toISOString().slice(0, 10),
    to: to.toISOString().slice(0, 10),
  }
}

export default function CashFlow() {
  const [granularity, setGranularity] = useState<CashFlowGranularityValue>(CashFlowGranularity.Month)
  const [range, setRange] = useState(defaultRange(CashFlowGranularity.Month))
  const [data, setData] = useState<CashFlowSeries | null>(null)
  const { execute, loading } = useApi<CashFlowSeries | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() =>
      financialReportService.getCashFlow(
        new Date(range.from).toISOString(),
        new Date(range.to).toISOString(),
        granularity,
      ),
    )
    setData(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [granularity, range.from, range.to])

  const chartData = useMemo(() => {
    if (!data) return []
    const buckets = new Map<string, { inflow: number; outflow: number; settledIn: number; settledOut: number }>()

    for (const point of data.pending) {
      const key = point.bucket
      const slot = buckets.get(key) ?? { inflow: 0, outflow: 0, settledIn: 0, settledOut: 0 }
      slot.inflow += point.inflow
      slot.outflow += point.outflow
      buckets.set(key, slot)
    }
    for (const point of data.settled) {
      const key = point.bucket
      const slot = buckets.get(key) ?? { inflow: 0, outflow: 0, settledIn: 0, settledOut: 0 }
      slot.settledIn += point.inflow
      slot.settledOut += point.outflow
      buckets.set(key, slot)
    }

    const sortedKeys = Array.from(buckets.keys()).sort()

    const inflowSeries = {
      id: 'A receber',
      color: '#10b981',
      data: sortedKeys.map((key) => ({ x: key.slice(0, 10), y: buckets.get(key)!.inflow })),
    }
    const outflowSeries = {
      id: 'A pagar',
      color: '#ef4444',
      data: sortedKeys.map((key) => ({ x: key.slice(0, 10), y: buckets.get(key)!.outflow })),
    }
    const settledInSeries = {
      id: 'Recebido',
      color: '#0ea5e9',
      data: sortedKeys.map((key) => ({ x: key.slice(0, 10), y: buckets.get(key)!.settledIn })),
    }
    const settledOutSeries = {
      id: 'Pago',
      color: '#a855f7',
      data: sortedKeys.map((key) => ({ x: key.slice(0, 10), y: buckets.get(key)!.settledOut })),
    }

    return [inflowSeries, outflowSeries, settledInSeries, settledOutSeries]
  }, [data])

  const totals = useMemo(() => {
    if (!data) return { pendingIn: 0, pendingOut: 0, settledIn: 0, settledOut: 0 }
    const pendingIn = data.pending.reduce((sum, p) => sum + p.inflow, 0)
    const pendingOut = data.pending.reduce((sum, p) => sum + p.outflow, 0)
    const settledIn = data.settled.reduce((sum, p) => sum + p.inflow, 0)
    const settledOut = data.settled.reduce((sum, p) => sum + p.outflow, 0)
    return { pendingIn, pendingOut, settledIn, settledOut }
  }, [data])

  return (
    <PageLayout
      title="Fluxo de caixa"
      subtitle="Entradas e saídas projetadas e realizadas no período"
      onRefresh={() => void load()}
      showDefaultActions={false}
    >
      <Card>
        <CardContent className="pt-4 space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-4">
            <div className="space-y-1">
              <label className="text-xs text-muted-foreground">De</label>
              <Input type="date" value={range.from} onChange={(e) => setRange((prev) => ({ ...prev, from: e.target.value }))} />
            </div>
            <div className="space-y-1">
              <label className="text-xs text-muted-foreground">Até</label>
              <Input type="date" value={range.to} onChange={(e) => setRange((prev) => ({ ...prev, to: e.target.value }))} />
            </div>
            <div className="space-y-1">
              <label className="text-xs text-muted-foreground">Granularidade</label>
              <SearchableSelect
                value={String(granularity)}
                onValueChange={(value) => {
                  const newGranularity = Number(value) as CashFlowGranularityValue
                  setGranularity(newGranularity)
                  setRange(defaultRange(newGranularity))
                }}
                options={[
                  { value: '0', label: 'Diária' },
                  { value: '1', label: 'Semanal' },
                  { value: '2', label: 'Mensal' },
                ]}
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
            <div className="rounded-md border p-3">
              <p className="text-xs text-muted-foreground">A receber (período)</p>
              <p className="text-lg font-semibold text-emerald-600">{formatCurrency(totals.pendingIn)}</p>
            </div>
            <div className="rounded-md border p-3">
              <p className="text-xs text-muted-foreground">A pagar (período)</p>
              <p className="text-lg font-semibold text-destructive">{formatCurrency(totals.pendingOut)}</p>
            </div>
            <div className="rounded-md border p-3">
              <p className="text-xs text-muted-foreground">Recebido (período)</p>
              <p className="text-lg font-semibold text-sky-600">{formatCurrency(totals.settledIn)}</p>
            </div>
            <div className="rounded-md border p-3">
              <p className="text-xs text-muted-foreground">Pago (período)</p>
              <p className="text-lg font-semibold text-violet-600">{formatCurrency(totals.settledOut)}</p>
            </div>
          </div>

          <div style={{ height: 360 }}>
            {loading ? (
              <p className="flex h-full items-center justify-center text-sm text-muted-foreground">Carregando fluxo de caixa...</p>
            ) : chartData.length === 0 || chartData[0].data.length === 0 ? (
              <p className="flex h-full items-center justify-center text-sm text-muted-foreground">Nenhum dado no período selecionado.</p>
            ) : (
              <ResponsiveLine
                data={chartData}
                margin={{ top: 20, right: 110, bottom: 50, left: 70 }}
                xScale={{ type: 'point' }}
                yScale={{ type: 'linear', min: 'auto', max: 'auto', stacked: false }}
                axisBottom={{
                  tickRotation: -30,
                  format: (value) => {
                    const date = new Date(String(value))
                    return Number.isNaN(date.getTime())
                      ? String(value)
                      : date.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })
                  },
                }}
                axisLeft={{
                  format: (value: number) => `R$ ${(value / 1000).toFixed(0)}k`,
                }}
                pointSize={6}
                pointBorderWidth={1}
                pointBorderColor={{ from: 'serieColor' }}
                useMesh
                colors={(serie) => (serie.color as string) ?? '#6366f1'}
                legends={[
                  {
                    anchor: 'bottom-right',
                    direction: 'column',
                    translateX: 100,
                    itemWidth: 90,
                    itemHeight: 18,
                    symbolSize: 10,
                    symbolShape: 'circle',
                  },
                ]}
              />
            )}
          </div>
        </CardContent>
      </Card>
    </PageLayout>
  )
}
