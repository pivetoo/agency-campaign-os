import { useEffect, useState } from 'react'
import { useApi, DataTable, type DataTableColumn } from 'archon-ui'
import { financialReportService, type TaxWithholdingReport, type TaxWithholdingLine } from '../../../services/financialReportService'
import { formatCurrency } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

const TAX_REGIME_LABELS: Record<number, string> = { 1: 'Pessoa Física', 2: 'MEI', 3: 'Simples Nacional', 4: 'Lucro Presumido/Real' }

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

export default function TaxWithholding() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<TaxWithholdingReport | null>(null)
  const { execute } = useApi<TaxWithholdingReport | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => financialReportService.getTaxWithholding(new Date(range.from).toISOString(), new Date(range.to).toISOString()))
    setData(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const columns: DataTableColumn<TaxWithholdingLine>[] = [
    { key: 'creatorName', title: 'Creator', dataIndex: 'creatorName', primary: true, render: (value?: string | null) => value || '—' },
    { key: 'document', title: 'Documento', dataIndex: 'document', hiddenBelow: 'md', render: (value?: string | null) => value || '—' },
    { key: 'taxRegime', title: 'Regime', dataIndex: 'taxRegime', hiddenBelow: 'lg', render: (value?: number | null) => (value != null ? TAX_REGIME_LABELS[value] ?? '—' : '—') },
    { key: 'grossAmount', title: 'Bruto', dataIndex: 'grossAmount', render: (value: number) => formatCurrency(value) },
    { key: 'taxWithheld', title: 'Retido', dataIndex: 'taxWithheld', render: (value: number) => formatCurrency(value) },
    { key: 'netAmount', title: 'Líquido', dataIndex: 'netAmount', render: (value: number) => formatCurrency(value) },
    { key: 'paymentCount', title: 'Qtd', dataIndex: 'paymentCount', hiddenBelow: 'sm' },
  ]

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Retenções Fiscais" subtitle="Imposto retido na fonte por creator" filters={filters} onRefresh={() => void load()} onExportCsv={() => financialReportService.exportTaxWithholding(new Date(range.from).toISOString(), new Date(range.to).toISOString())} onExportPdf={() => financialReportService.exportTaxWithholdingPdf(new Date(range.from).toISOString(), new Date(range.to).toISOString())}>
      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Bruto total</p>
          <p className="text-lg font-semibold">{formatCurrency(data?.totalGross ?? 0)}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Retido total</p>
          <p className="text-lg font-semibold text-destructive">{formatCurrency(data?.totalWithheld ?? 0)}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Líquido total</p>
          <p className="text-lg font-semibold text-emerald-600">{formatCurrency(data?.totalNet ?? 0)}</p>
        </div>
      </div>
      <DataTable columns={columns} data={data?.lines ?? []} rowKey="creatorId" emptyText="Nenhuma retenção no período." />
    </ReportLayout>
  )
}
