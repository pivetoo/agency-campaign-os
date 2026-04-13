import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { campaignFinancialEntryService } from '../../services/campaignFinancialEntryService'
import type { CampaignFinancialEntry } from '../../types/campaignFinancialEntry'

const categoryLabels: Record<number, string> = {
  1: 'Recebível da marca',
  2: 'Repasse creator',
  3: 'Fee da agência',
  4: 'Custo operacional',
  5: 'Bônus',
  6: 'Ajuste',
  7: 'Reembolso',
  8: 'Imposto',
}

export default function FinancialReceivables() {
  const [entries, setEntries] = useState<CampaignFinancialEntry[]>([])
  const { execute: fetchEntries, loading } = useApi<CampaignFinancialEntry[]>({ showErrorMessage: true })

  const loadEntries = async () => {
    const result = await fetchEntries(() => campaignFinancialEntryService.getAll())
    if (result) {
      setEntries(result.filter((item) => item.type === 1))
    }
  }

  useEffect(() => {
    void loadEntries()
  }, [])

  const columns: DataTableColumn<CampaignFinancialEntry>[] = [
    { key: 'description', title: 'Descrição', dataIndex: 'description' },
    { key: 'category', title: 'Categoria', dataIndex: 'category', render: (value: number) => categoryLabels[value] || '-' },
    { key: 'counterpartyName', title: 'Contraparte', dataIndex: 'counterpartyName' },
    { key: 'amount', title: 'Valor', dataIndex: 'amount', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'dueAt', title: 'Vencimento', dataIndex: 'dueAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      render: (value: number) => (
        <Badge variant={value === 2 ? 'success' : value === 3 ? 'destructive' : 'warning'}>
          {{ 1: 'Pendente', 2: 'Recebido', 3: 'Vencido', 4: 'Cancelado' }[value] || '-'}
        </Badge>
      ),
    },
  ]

  return (
    <PageLayout
      title="Contas a receber"
      subtitle="Visão consolidada dos lançamentos de entrada da agência"
      onRefresh={() => void loadEntries()}
    >
      <DataTable
        columns={columns}
        data={entries}
        rowKey="id"
        selectedRows={[]}
        onSelectionChange={() => {}}
        emptyText="Nenhuma conta a receber cadastrada"
        loading={loading}
      />
    </PageLayout>
  )
}
