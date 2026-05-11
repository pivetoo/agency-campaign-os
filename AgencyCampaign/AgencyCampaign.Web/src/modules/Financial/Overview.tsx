import { useEffect, useMemo, useState } from 'react'
import { PageLayout, Card, CardContent, CardHeader, CardTitle, DataTable, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { financialEntryService } from '../../services/financialEntryService'
import type { FinancialEntry } from '../../types/financialEntry'

export default function FinancialOverview() {
  const { t } = useI18n()
  const [entries, setEntries] = useState<FinancialEntry[]>([])
  const { execute: fetchEntries, loading } = useApi<FinancialEntry[]>({ showErrorMessage: true })

  const loadEntries = async () => {
    const result = await fetchEntries(() => financialEntryService.getAll())
    if (result) {
      setEntries(result)
    }
  }

  useEffect(() => {
    void loadEntries()
  }, [])

  const totals = useMemo(() => {
    const receivable = entries.filter((item) => item.type === 1).reduce((sum, item) => sum + item.amount, 0)
    const payable = entries.filter((item) => item.type === 2).reduce((sum, item) => sum + item.amount, 0)
    const paid = entries.filter((item) => item.status === 2).reduce((sum, item) => sum + item.amount, 0)
    const overdue = entries.filter((item) => item.status === 3).reduce((sum, item) => sum + item.amount, 0)

    return { receivable, payable, paid, overdue, balance: receivable - payable }
  }, [entries])

  const columns: DataTableColumn<FinancialEntry>[] = [
    { key: 'type', title: 'Tipo', dataIndex: 'type', render: (value: number) => value === 1 ? t('financial.kpi.receivable') : t('financial.kpi.payable') },
    { key: 'description', title: 'Descrição', dataIndex: 'description' },
    { key: 'counterpartyName', title: 'Contraparte', dataIndex: 'counterpartyName' },
    { key: 'amount', title: 'Valor', dataIndex: 'amount', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'dueAt', title: 'Vencimento', dataIndex: 'dueAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    { key: 'status', title: 'Status', dataIndex: 'status', render: (value: number) => ({ 1: 'Pendente', 2: 'Pago', 3: 'Vencido', 4: 'Cancelado' }[value] || '-') },
  ]

  return (
    <div className="space-y-4">
      <PageLayout title={t('financial.overview.title')} onRefresh={() => void loadEntries()}>
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
          <Card><CardHeader><CardTitle className="text-sm">{t('financial.kpi.receivable')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {totals.receivable.toFixed(2)}</CardContent></Card>
          <Card><CardHeader><CardTitle className="text-sm">{t('financial.kpi.payable')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {totals.payable.toFixed(2)}</CardContent></Card>
          <Card><CardHeader><CardTitle className="text-sm">Pago/Recebido</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {totals.paid.toFixed(2)}</CardContent></Card>
          <Card><CardHeader><CardTitle className="text-sm">{t('financial.kpi.overdue')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {totals.overdue.toFixed(2)}</CardContent></Card>
          <Card><CardHeader><CardTitle className="text-sm">{t('financial.kpi.projectedBalance')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {totals.balance.toFixed(2)}</CardContent></Card>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Lançamentos financeiros</CardTitle>
          </CardHeader>
          <CardContent>
            <DataTable
              columns={columns}
              data={entries}
              rowKey="id"
              selectedRows={[]}
              onSelectionChange={() => {}}
              emptyText="Nenhum lançamento financeiro cadastrado"
              loading={loading}
              pageSize={10}
              pageSizeOptions={[5, 10, 20, 50]}
            />
          </CardContent>
        </Card>
      </PageLayout>
    </div>
  )
}
