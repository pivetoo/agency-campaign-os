import { useEffect, useState } from 'react'
import { useApi, DataTable, type DataTableColumn } from 'archon-ui'
import { productionReportService, type DeliverableSla, type DeliverableSlaCampaignLine } from '../../../services/productionReportService'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

export default function SlaEntregaveis() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<DeliverableSla | null>(null)
  const { execute } = useApi<DeliverableSla | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => productionReportService.getDeliverableSla(new Date(range.from).toISOString(), new Date(range.to).toISOString()))
    setData(result ?? null)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const columns: DataTableColumn<DeliverableSlaCampaignLine>[] = [
    { key: 'campaignName', title: 'Campanha', dataIndex: 'campaignName', primary: true },
    { key: 'total', title: 'Total', dataIndex: 'total' },
    { key: 'publishedOnTime', title: 'No prazo', dataIndex: 'publishedOnTime' },
    { key: 'publishedLate', title: 'Atrasados', dataIndex: 'publishedLate' },
    { key: 'overdue', title: 'Vencidos', dataIndex: 'overdue' },
    { key: 'upcoming', title: 'A vencer', dataIndex: 'upcoming' },
  ]

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Entregáveis: Prazo × Atraso" subtitle="SLA dos entregáveis por vencimento no período" filters={filters} onRefresh={() => void load()} onExportCsv={() => productionReportService.exportDeliverableSla(new Date(range.from).toISOString(), new Date(range.to).toISOString())}>
      <div className="grid grid-cols-2 gap-3 md:grid-cols-5">
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">No prazo</p>
          <p className="text-lg font-semibold text-emerald-600">{data?.publishedOnTime ?? 0}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Atrasados</p>
          <p className="text-lg font-semibold text-amber-600">{data?.publishedLate ?? 0}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Vencidos</p>
          <p className="text-lg font-semibold text-destructive">{data?.overdue ?? 0}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">A vencer</p>
          <p className="text-lg font-semibold">{data?.upcoming ?? 0}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Taxa no prazo</p>
          <p className="text-lg font-semibold text-primary">{`${(data?.onTimeRate ?? 0).toFixed(1)}%`}</p>
        </div>
      </div>
      <DataTable columns={columns} data={data?.byCampaign ?? []} rowKey="campaignId" emptyText="Nenhum entregável no período." />
    </ReportLayout>
  )
}
