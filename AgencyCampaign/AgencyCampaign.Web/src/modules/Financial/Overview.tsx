import { useEffect, useMemo, useState } from 'react'
import { PageLayout, Card, CardContent, CardHeader, CardTitle, DataTable, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { campaignFinancialEntryService } from '../../services/campaignFinancialEntryService'
import type { CampaignFinancialEntry } from '../../types/campaignFinancialEntry'

export default function FinancialOverview() {
  const [entries, setEntries] = useState<CampaignFinancialEntry[]>([])
  const { execute: fetchEntries, loading } = useApi<CampaignFinancialEntry[]>({ showErrorMessage: true })

  const loadEntries = async () => {
    const result = await fetchEntries(() => campaignFinancialEntryService.getAll())
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

  const columns: DataTableColumn<CampaignFinancialEntry>[] = [
    { key: 'type', title: 'Tipo', dataIndex: 'type', render: (value: number) => value === 1 ? 'A receber' : 'A pagar' },
    { key: 'description', title: 'Descrição', dataIndex: 'description' },
    { key: 'counterpartyName', title: 'Contraparte', dataIndex: 'counterpartyName' },
    { key: 'amount', title: 'Valor', dataIndex: 'amount', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'dueAt', title: 'Vencimento', dataIndex: 'dueAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    { key: 'status', title: 'Status', dataIndex: 'status', render: (value: number) => ({ 1: 'Pendente', 2: 'Pago', 3: 'Vencido', 4: 'Cancelado' }[value] || '-') },
  ]

  return (
    <div className="space-y-4">
      <PageLayout title="Visão geral financeira" onRefresh={() => void loadEntries()}>
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
          <Card><CardHeader><CardTitle className="text-sm">A receber</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {totals.receivable.toFixed(2)}</CardContent></Card>
          <Card><CardHeader><CardTitle className="text-sm">A pagar</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {totals.payable.toFixed(2)}</CardContent></Card>
          <Card><CardHeader><CardTitle className="text-sm">Pago/Recebido</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {totals.paid.toFixed(2)}</CardContent></Card>
          <Card><CardHeader><CardTitle className="text-sm">Vencido</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {totals.overdue.toFixed(2)}</CardContent></Card>
          <Card><CardHeader><CardTitle className="text-sm">Saldo previsto</CardTitle></CardHeader><CardContent className="text-2xl font-bold">R$ {totals.balance.toFixed(2)}</CardContent></Card>
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
              pageSize={5}
              pageSizeOptions={[5, 10, 20, 50]}
            />
          </CardContent>
        </Card>
      </PageLayout>
    </div>
  )
}
