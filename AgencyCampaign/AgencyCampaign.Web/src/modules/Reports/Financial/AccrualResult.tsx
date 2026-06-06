import { useEffect, useState } from 'react'
import { useApi } from 'archon-ui'
import { financialReportService, type AccrualResult as AccrualResultModel } from '../../../services/financialReportService'
import { formatCurrency } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

export default function AccrualResult() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<AccrualResultModel | null>(null)
  const { execute } = useApi<AccrualResultModel | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => financialReportService.getAccrualResult(new Date(range.from).toISOString(), new Date(range.to).toISOString()))
    setData(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Resultado (Competência)" subtitle="Receita menos despesa no regime de competência" filters={filters} onRefresh={() => void load()} onExportCsv={() => financialReportService.exportAccrualResult(new Date(range.from).toISOString(), new Date(range.to).toISOString())}>
      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Receita</p>
          <p className="text-2xl font-semibold text-emerald-600">{formatCurrency(data?.revenue ?? 0)}</p>
        </div>
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Despesa</p>
          <p className="text-2xl font-semibold text-destructive">{formatCurrency(data?.expense ?? 0)}</p>
        </div>
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Resultado</p>
          <p className="text-2xl font-semibold text-primary">{formatCurrency(data?.result ?? 0)}</p>
        </div>
      </div>
    </ReportLayout>
  )
}
