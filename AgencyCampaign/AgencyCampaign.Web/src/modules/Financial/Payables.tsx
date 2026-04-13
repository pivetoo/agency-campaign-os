import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { campaignFinancialEntryService } from '../../services/campaignFinancialEntryService'
import type { CampaignFinancialEntry } from '../../types/campaignFinancialEntry'

export default function FinancialPayables() {
  const [entries, setEntries] = useState<CampaignFinancialEntry[]>([])
  const { execute: fetchEntries, loading } = useApi<CampaignFinancialEntry[]>({ showErrorMessage: true })

  const loadEntries = async () => {
    const result = await fetchEntries(() => campaignFinancialEntryService.getAll())
    if (result) {
      setEntries(result.filter((item) => item.type === 2))
    }
  }

  useEffect(() => {
    void loadEntries()
  }, [])

  const columns: DataTableColumn<CampaignFinancialEntry>[] = [
    { key: 'description', title: 'Descrição', dataIndex: 'description' },
    { key: 'counterpartyName', title: 'Contraparte', dataIndex: 'counterpartyName' },
    { key: 'amount', title: 'Valor', dataIndex: 'amount', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'dueAt', title: 'Vencimento', dataIndex: 'dueAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      render: (value: number) => (
        <Badge variant={value === 2 ? 'success' : value === 3 ? 'destructive' : 'warning'}>
          {{ 1: 'Pendente', 2: 'Pago', 3: 'Vencido', 4: 'Cancelado' }[value] || '-'}
        </Badge>
      ),
    },
  ]

  return (
    <PageLayout
      title="Contas a pagar"
      subtitle="Visão consolidada dos lançamentos de saída da agência"
      onRefresh={() => void loadEntries()}
    >
      <DataTable
        columns={columns}
        data={entries}
        rowKey="id"
        selectedRows={[]}
        onSelectionChange={() => {}}
        emptyText="Nenhuma conta a pagar cadastrada"
        loading={loading}
      />
    </PageLayout>
  )
}
