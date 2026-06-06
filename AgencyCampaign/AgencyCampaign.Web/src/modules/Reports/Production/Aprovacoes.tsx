import { useEffect, useState } from 'react'
import { useApi } from 'archon-ui'
import { productionReportService, type ApprovalCycle } from '../../../services/productionReportService'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

const days = (v?: number | null) => (v != null ? `${v.toFixed(1)} dias` : '-')
const pct = (v?: number | null) => (v != null ? `${v.toFixed(1)}%` : '-')
const num = (v?: number | null) => (v != null ? v.toFixed(1) : '-')

export default function Aprovacoes() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<ApprovalCycle | null>(null)
  const { execute } = useApi<ApprovalCycle | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => productionReportService.getApprovalCycle(new Date(range.from).toISOString(), new Date(range.to).toISOString()))
    setData(result ?? null)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Aprovação e Rodadas" subtitle="Tempo de aprovação e rodadas de revisão no período" filters={filters} onRefresh={() => void load()} onExportCsv={() => productionReportService.exportApprovalCycle(new Date(range.from).toISOString(), new Date(range.to).toISOString())}>
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Aprov. interna (médio)</p>
          <p className="text-lg font-semibold">{days(data?.avgInternalApprovalDays)}</p>
          <p className="text-xs text-muted-foreground mt-1">{data?.internalApprovedCount ?? 0} aprovações</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Aprov. marca (médio)</p>
          <p className="text-lg font-semibold">{days(data?.avgBrandApprovalDays)}</p>
          <p className="text-xs text-muted-foreground mt-1">{data?.brandApprovedCount ?? 0} aprovações</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Rodadas médias</p>
          <p className="text-lg font-semibold">{num(data?.avgRounds)}</p>
          <p className="text-xs text-muted-foreground mt-1">{data?.contentApprovedCount ?? 0} conteúdos aprovados</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Aprovado na 1ª rodada</p>
          <p className="text-lg font-semibold">{pct(data?.firstRoundApprovalRate)}</p>
        </div>
      </div>
    </ReportLayout>
  )
}
