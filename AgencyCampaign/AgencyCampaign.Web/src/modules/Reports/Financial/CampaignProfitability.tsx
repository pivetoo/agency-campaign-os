import { useEffect, useState } from 'react'
import { useApi, DataTable, Badge, type DataTableColumn } from 'archon-ui'
import { financialReportService, type CampaignProfitabilityReport, type CampaignProfitabilityLine } from '../../../services/financialReportService'
import { formatCurrency, formatPercent } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'

export default function CampaignProfitability() {
  const [data, setData] = useState<CampaignProfitabilityReport | null>(null)
  const { execute } = useApi<CampaignProfitabilityReport | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => financialReportService.getCampaignProfitability())
    setData(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const columns: DataTableColumn<CampaignProfitabilityLine>[] = [
    { key: 'campaignName', title: 'Campanha', dataIndex: 'campaignName', primary: true, render: (value?: string | null) => value || '—' },
    { key: 'revenue', title: 'Receita', dataIndex: 'revenue', render: (value: number) => formatCurrency(value) },
    { key: 'creatorCost', title: 'Custo creator', dataIndex: 'creatorCost', hiddenBelow: 'md', render: (value: number) => formatCurrency(value) },
    { key: 'otherCost', title: 'Outros custos', dataIndex: 'otherCost', hiddenBelow: 'lg', render: (value: number) => formatCurrency(value) },
    { key: 'margin', title: 'Margem', dataIndex: 'margin', render: (value: number) => formatCurrency(value) },
    { key: 'marginPercent', title: 'Margem %', dataIndex: 'marginPercent', cardTag: true, render: (value: number) => <Badge variant={value >= 0 ? 'success' : 'destructive'}>{formatPercent(value)}</Badge> },
  ]

  return (
    <ReportLayout title="Rentabilidade por Campanha" subtitle="Critério: lançamentos financeiros vinculados à campanha (receita da marca menos custos/repasses). Pode incluir valores ainda não pagos." onRefresh={() => void load()} onExportCsv={() => financialReportService.exportCampaignProfitability()} onExportPdf={() => financialReportService.exportCampaignProfitabilityPdf()}>
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Receita total</p>
          <p className="text-lg font-semibold text-emerald-600">{formatCurrency(data?.totalRevenue ?? 0)}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Custo creators</p>
          <p className="text-lg font-semibold text-destructive">{formatCurrency(data?.totalCreatorCost ?? 0)}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Outros custos</p>
          <p className="text-lg font-semibold text-destructive">{formatCurrency(data?.totalOtherCost ?? 0)}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Margem total</p>
          <p className="text-lg font-semibold text-primary">{formatCurrency(data?.totalMargin ?? 0)}</p>
        </div>
      </div>
      <DataTable columns={columns} data={data?.lines ?? []} rowKey="campaignId" emptyText="Nenhuma campanha com lançamentos." />
    </ReportLayout>
  )
}
