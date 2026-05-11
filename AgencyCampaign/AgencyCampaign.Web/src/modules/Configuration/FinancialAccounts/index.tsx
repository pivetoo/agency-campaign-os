import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import { financialAccountService } from '../../../services/financialAccountService'
import { financialAccountTypeLabels, type FinancialAccount } from '../../../types/financialAccount'
import FinancialAccountFormModal from '../../../components/modals/FinancialAccountFormModal'

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function FinancialAccounts() {
  const { t } = useI18n()
  const [items, setItems] = useState<FinancialAccount[]>([])
  const [selected, setSelected] = useState<FinancialAccount | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchAll, loading } = useApi<FinancialAccount[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchAll(() => financialAccountService.getAll(true))
    if (result) setItems(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleDelete = async () => {
    if (!selected) return
    if (!window.confirm(`Excluir a conta "${selected.name}"? Se houver lançamentos, prefira inativar.`)) return
    const result = await runDelete(() => financialAccountService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      void load()
    }
  }

  const columns: DataTableColumn<FinancialAccount>[] = [
    {
      key: 'name',
      title: 'Conta',
      dataIndex: 'name',
      render: (value: string, record) => (
        <span className="inline-flex items-center gap-2">
          <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ backgroundColor: record.color }} />
          <span className="font-medium">{value}</span>
        </span>
      ),
    },
    { key: 'type', title: 'Tipo', dataIndex: 'type', render: (value: number) => financialAccountTypeLabels[value] || '-' },
    { key: 'bank', title: 'Banco', dataIndex: 'bank', render: (value?: string | null) => value || '-' },
    { key: 'currentBalance', title: 'Saldo atual', dataIndex: 'currentBalance', render: (value: number) => <span className={value < 0 ? 'text-destructive font-medium' : 'font-medium'}>{formatCurrency(value)}</span> },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? 'Ativa' : 'Inativa'}</Badge>,
    },
  ]

  return (
    <>
      <PageLayout
        title={t('configuration.bankAccounts.title')}
        subtitle={t('configuration.bankAccounts.subtitle')}
        onAdd={() => { setSelected(null); setIsFormOpen(true) }}
        onEdit={() => selected && setIsFormOpen(true)}
        onRefresh={() => void load()}
        selectedRowsCount={selected ? 1 : 0}
        actions={[
          {
            key: 'delete',
            label: 'Excluir',
            icon: <Trash2 className="h-4 w-4" />,
            variant: 'outline-danger',
            disabled: !selected || deleting,
            onClick: () => void handleDelete(),
          },
        ]}
      >
        <DataTable
          columns={columns}
          data={items}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          emptyText="Nenhuma conta cadastrada"
          loading={loading}
          pageSize={10}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <FinancialAccountFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        account={selected}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelected(null)
          void load()
        }}
      />
    </>
  )
}
