import { useEffect, useState } from 'react'
import { PageLayout, Card, CardContent, CardHeader, CardTitle, DataTable, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { financialEntryService } from '../../services/financialEntryService'
import type { FinancialEntry, FinancialSummary } from '../../types/financialEntry'

export default function Financial() {
  const { t } = useI18n()
  const [entries, setEntries] = useState<FinancialEntry[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [summaryReceivable, setSummaryReceivable] = useState<FinancialSummary | null>(null)
  const [summaryPayable, setSummaryPayable] = useState<FinancialSummary | null>(null)
  const { execute: fetchEntries, loading, pagination } = useApi<FinancialEntry[]>({ showErrorMessage: true })

  const loadEntries = async () => {
    const result = await fetchEntries(() => financialEntryService.getAll({ page, pageSize }))
    if (result) setEntries(result)
  }

  const loadSummaries = async () => {
    const [r, p] = await Promise.all([
      financialEntryService.getSummary(1),
      financialEntryService.getSummary(2),
    ])
    setSummaryReceivable(r)
    setSummaryPayable(p)
  }

  useEffect(() => {
    void loadSummaries()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    void loadEntries()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize])

  const receivable = summaryReceivable?.totalPending ?? 0
  const payable = summaryPayable?.totalPending ?? 0
  const settled = (summaryReceivable?.totalSettledThisMonth ?? 0)
  const overdue = (summaryReceivable?.totalOverdue ?? 0) + (summaryPayable?.totalOverdue ?? 0)
  const balance = receivable - payable

  const statusMap: Record<number, string> = {
    1: t('financial.entries.status.pending'),
    2: t('financial.entries.status.paid'),
    3: t('financial.entries.status.overdue'),
    4: t('financial.entries.status.cancelled'),
  }

  const columns: DataTableColumn<FinancialEntry>[] = [
    { key: 'type', title: t('common.field.type'), dataIndex: 'type', render: (value: number) => value === 1 ? t('financial.kpi.receivable') : t('financial.kpi.payable') },
    { key: 'description', title: t('common.field.description'), dataIndex: 'description' },
    { key: 'counterpartyName', title: t('financial.entries.field.counterparty'), dataIndex: 'counterpartyName' },
    { key: 'amount', title: t('common.field.value'), dataIndex: 'amount', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'dueAt', title: t('financial.entries.field.dueDate'), dataIndex: 'dueAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    { key: 'status', title: t('common.field.status'), dataIndex: 'status', render: (value: number) => statusMap[value] || '-' },
  ]

  return (
    <div className="space-y-4">
      <PageLayout title={t('financial.title')} onRefresh={() => { void loadEntries(); void loadSummaries() }}>
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
          <Card><CardHeader><CardTitle className="text-sm">{t('financial.kpi.receivable')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {receivable.toFixed(2)}</CardContent></Card>
          <Card><CardHeader><CardTitle className="text-sm">{t('financial.kpi.payable')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {payable.toFixed(2)}</CardContent></Card>
          <Card><CardHeader><CardTitle className="text-sm">{t('financial.kpi.settledThisMonth')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {settled.toFixed(2)}</CardContent></Card>
          <Card><CardHeader><CardTitle className="text-sm">{t('financial.kpi.overdue')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {overdue.toFixed(2)}</CardContent></Card>
          <Card><CardHeader><CardTitle className="text-sm">{t('financial.kpi.projectedBalance')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {balance.toFixed(2)}</CardContent></Card>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>{t('financial.entries.title')}</CardTitle>
          </CardHeader>
          <CardContent>
            <DataTable
              columns={columns}
              data={entries}
              rowKey="id"
              selectedRows={[]}
              onSelectionChange={() => {}}
              emptyText={t('financial.entries.empty')}
              loading={loading}
              pageSize={pageSize}
              pageSizeOptions={[10, 20, 50]}
              totalCount={pagination?.totalCount}
              page={page}
              onPageChange={setPage}
              onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
            />
          </CardContent>
        </Card>
      </PageLayout>
    </div>
  )
}
