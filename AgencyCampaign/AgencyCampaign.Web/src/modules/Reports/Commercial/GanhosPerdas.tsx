import { useEffect, useMemo, useState } from 'react'
import { useApi } from 'archon-ui'
import { ResponsivePie } from '@nivo/pie'
import { opportunityService } from '../../../services/opportunityService'
import type { CommercialAnalytics, ReasonAggregate } from '../../../types/commercialAnalytics'
import { formatCurrency } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

function ReasonsBlock({ items, emptyLabel }: { items: ReasonAggregate[]; emptyLabel: string }) {
  const pieData = useMemo(() => items.map((item) => ({
    id: `${item.reasonId ?? 'none'}-${item.reasonName}`,
    label: item.reasonName,
    value: item.count,
    color: item.reasonColor ?? '#94A3B8',
  })), [items])

  if (items.length === 0) {
    return <p className="text-xs text-muted-foreground">{emptyLabel}</p>
  }

  return (
    <div className="space-y-3">
      <div className="h-40">
        <ResponsivePie
          data={pieData}
          margin={{ top: 6, right: 6, bottom: 6, left: 6 }}
          innerRadius={0.5}
          padAngle={1}
          cornerRadius={2}
          activeOuterRadiusOffset={4}
          colors={(d) => (d.data as { color: string }).color}
          enableArcLabels={false}
          enableArcLinkLabels={false}
        />
      </div>
      <div className="space-y-1.5">
        {items.map((item) => (
          <div key={`${item.reasonId ?? 'none'}-${item.reasonName}`} className="rounded-md border border-border/60 bg-muted/20 px-2.5 py-1.5">
            <div className="flex items-center justify-between gap-2 text-xs">
              <span className="flex items-center gap-2 truncate text-foreground">
                <span className="inline-block h-2 w-2 rounded-full" style={{ backgroundColor: item.reasonColor ?? '#94A3B8' }} />
                {item.reasonName}
              </span>
              <span className="font-mono text-muted-foreground">{item.count}</span>
            </div>
            <div className="mt-1 flex items-center justify-end text-[11px] text-muted-foreground">
              <span className="font-mono">{formatCurrency(item.totalValue)}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

export default function GanhosPerdas() {
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

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Ganhos × Perdas" subtitle="Motivos de ganho e de perda" filters={filters} onRefresh={() => void load()}>
      <div className="grid gap-5 md:grid-cols-2">
        <div className="rounded-lg border border-border bg-card p-4">
          <h3 className="mb-3 text-sm font-semibold text-foreground">Motivos de Ganho</h3>
          <ReasonsBlock items={data?.winReasons ?? []} emptyLabel="Sem dados de ganho no período." />
        </div>
        <div className="rounded-lg border border-border bg-card p-4">
          <h3 className="mb-3 text-sm font-semibold text-foreground">Motivos de Perda</h3>
          <ReasonsBlock items={data?.lossReasons ?? []} emptyLabel="Sem dados de perda no período." />
        </div>
      </div>
    </ReportLayout>
  )
}
