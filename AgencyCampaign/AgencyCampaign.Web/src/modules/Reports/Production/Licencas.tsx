import { useEffect, useState } from 'react'
import { useApi, DataTable, Badge, SearchableSelect, type DataTableColumn } from 'archon-ui'
import { productionReportService, type ContentLicenseReport, type ContentLicenseReportLine } from '../../../services/productionReportService'
import { formatDate } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'

const DAYS_OPTIONS = [{ value: '15', label: '15 dias' }, { value: '30', label: '30 dias' }, { value: '60', label: '60 dias' }, { value: '90', label: '90 dias' }]

const TYPE_LABELS: Record<number, string> = { 1: 'Reuso UGC', 2: 'Whitelisting pago', 3: 'Exclusividade', 4: 'Outro' }
const STATUS_LABELS: Record<number, string> = { 1: 'Ativa', 2: 'Expira em breve', 3: 'Expirada' }

export default function Licencas() {
  const [expiringSoonDays, setExpiringSoonDays] = useState(30)
  const [data, setData] = useState<ContentLicenseReport | null>(null)
  const { execute } = useApi<ContentLicenseReport | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => productionReportService.getContentLicenses(expiringSoonDays))
    setData(result ?? null)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [expiringSoonDays])

  const columns: DataTableColumn<ContentLicenseReportLine>[] = [
    { key: 'deliverableTitle', title: 'Entregável', dataIndex: 'deliverableTitle', primary: true },
    { key: 'campaignName', title: 'Campanha', dataIndex: 'campaignName', hiddenBelow: 'md', render: (value?: string | null) => value || '-' },
    { key: 'type', title: 'Tipo', dataIndex: 'type', hiddenBelow: 'sm', render: (v: number) => TYPE_LABELS[v] ?? '-' },
    { key: 'channels', title: 'Canais', dataIndex: 'channels', hiddenBelow: 'lg', render: (value?: string | null) => value || '-' },
    { key: 'expiresAt', title: 'Expira', dataIndex: 'expiresAt', render: (v?: string | null) => (v ? formatDate(v) : '-') },
    { key: 'daysUntilExpiry', title: 'Dias', dataIndex: 'daysUntilExpiry', render: (v?: number | null) => (v != null ? String(v) : '-') },
    { key: 'status', title: 'Status', dataIndex: 'status', cardTag: true, render: (value: number) => <Badge variant={value === 1 ? 'success' : value === 2 ? 'warning' : 'destructive'}>{STATUS_LABELS[value] ?? '-'}</Badge> },
  ]

  const filters = (
    <div className="space-y-1">
      <label className="text-xs text-muted-foreground">Vencendo em</label>
      <SearchableSelect value={String(expiringSoonDays)} onValueChange={(v) => setExpiringSoonDays(Number(v))} options={DAYS_OPTIONS} />
    </div>
  )

  return (
    <ReportLayout title="Licenças de Conteúdo" subtitle="Status de expiração das licenças" filters={filters} onRefresh={() => void load()} onExportCsv={() => productionReportService.exportContentLicenses(expiringSoonDays)} onExportPdf={() => productionReportService.exportContentLicensesPdf(expiringSoonDays)}>
      <div className="grid grid-cols-3 gap-3">
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Ativas</p>
          <p className="text-lg font-semibold text-emerald-600">{data?.activeCount ?? 0}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Expirando</p>
          <p className="text-lg font-semibold text-amber-600">{data?.expiringSoonCount ?? 0}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Expiradas</p>
          <p className="text-lg font-semibold text-destructive">{data?.expiredCount ?? 0}</p>
        </div>
      </div>
      <DataTable columns={columns} data={data?.lines ?? []} rowKey="licenseId" emptyText="Nenhuma licença cadastrada." />
    </ReportLayout>
  )
}
