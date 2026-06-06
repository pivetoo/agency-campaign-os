import { useEffect, useState } from 'react'
import { useApi } from 'archon-ui'
import { commercialReportService, type ProposalsFunnel } from '../../../services/commercialReportService'
import { formatCurrency } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

export default function Propostas() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<ProposalsFunnel | null>(null)
  const { execute } = useApi<ProposalsFunnel | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => commercialReportService.getProposalsFunnel(new Date(range.from).toISOString(), new Date(range.to).toISOString()))
    setData(result ?? null)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  const emitted = data?.emittedCount ?? 0
  const accepted = data?.acceptedCount ?? 0
  const rejected = data?.rejectedCount ?? 0
  const maxCount = Math.max(emitted, 1)

  const bars: { label: string; count: number; color: string }[] = [
    { label: 'Emitidas', count: emitted, color: 'bg-primary/60' },
    { label: 'Aceitas', count: accepted, color: 'bg-emerald-500' },
    { label: 'Rejeitadas', count: rejected, color: 'bg-rose-400' },
  ]

  return (
    <ReportLayout title="Propostas: Emitidas × Aceitas" subtitle="Volume, valor e taxa de aceite no período" filters={filters} onRefresh={() => void load()} onExportCsv={() => commercialReportService.exportProposalsFunnel(new Date(range.from).toISOString(), new Date(range.to).toISOString())} onExportPdf={() => commercialReportService.exportProposalsFunnelPdf(new Date(range.from).toISOString(), new Date(range.to).toISOString())}>
      <div className="grid grid-cols-2 gap-3 md:grid-cols-3">
        <div className="rounded-md border p-4">
          <p className="text-xs uppercase tracking-wide text-muted-foreground">Emitidas</p>
          <p className="mt-1 text-2xl font-semibold text-primary">{emitted}</p>
          <p className="mt-0.5 text-xs text-muted-foreground">{formatCurrency(data?.emittedValue ?? 0)}</p>
        </div>
        <div className="rounded-md border p-4">
          <p className="text-xs uppercase tracking-wide text-muted-foreground">Aceitas</p>
          <p className="mt-1 text-2xl font-semibold text-emerald-600">{accepted}</p>
          <p className="mt-0.5 text-xs text-muted-foreground">{formatCurrency(data?.acceptedValue ?? 0)}</p>
        </div>
        <div className="col-span-2 rounded-md border p-4 md:col-span-1">
          <p className="text-xs uppercase tracking-wide text-muted-foreground">Taxa de aceite</p>
          <p className="mt-1 text-2xl font-semibold text-primary">{data ? `${data.acceptanceRate.toFixed(1)}%` : '—'}</p>
        </div>
      </div>

      <div className="rounded-md border p-4">
        <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-muted-foreground">Comparativo de volume</p>
        <div className="space-y-3">
          {bars.map((bar) => (
            <div key={bar.label}>
              <div className="mb-1 flex items-baseline justify-between text-xs">
                <span className="text-foreground">{bar.label}</span>
                <span className="font-semibold text-foreground">{bar.count}</span>
              </div>
              <div className="h-5 overflow-hidden rounded bg-muted" role="progressbar" aria-label={bar.label} aria-valuenow={Math.round((bar.count / maxCount) * 100)} aria-valuemin={0} aria-valuemax={100}>
                <div className={`h-full ${bar.color}`} style={{ width: `${(bar.count / maxCount) * 100}%` }} />
              </div>
            </div>
          ))}
        </div>
      </div>
    </ReportLayout>
  )
}
