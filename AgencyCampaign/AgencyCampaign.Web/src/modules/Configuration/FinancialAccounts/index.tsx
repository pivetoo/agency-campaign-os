import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, ConfirmModal, TableToolbar, useApi, useI18n } from 'archon-ui'
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
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [selected, setSelected] = useState<FinancialAccount | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)
  const { execute: fetchAll, loading, pagination } = useApi<FinancialAccount[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchAll(() => financialAccountService.getAll({ page, pageSize, search: debouncedSearch || undefined, includeInactive: true }))
    if (result) setItems(result)
  }

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(timeout)
  }, [search])

  useEffect(() => {
    setPage(1)
  }, [debouncedSearch])

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch])

  const handleDelete = async () => {
    if (!selected) return
    const result = await runDelete(() => financialAccountService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      setIsConfirmOpen(false)
      void load()
    }
  }

  const columns: DataTableColumn<FinancialAccount>[] = [
    {
      key: 'name',
      title: t('common.field.account'),
      dataIndex: 'name',
      render: (value: string, record) => (
        <span className="inline-flex items-center gap-2">
          <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ backgroundColor: record.color }} />
          <span className="font-medium">{value}</span>
        </span>
      ),
    },
    { key: 'type', title: t('common.field.type'), dataIndex: 'type', render: (value: number) => financialAccountTypeLabels[value] || '-' },
    { key: 'bank', title: t('configuration.bankAccounts.field.bank'), dataIndex: 'bank', render: (value?: string | null) => value || '-' },
    { key: 'currentBalance', title: t('configuration.bankAccounts.field.currentBalance'), dataIndex: 'currentBalance', render: (value: number) => <span className={value < 0 ? 'text-destructive font-medium' : 'font-medium'}>{formatCurrency(value)}</span> },
    {
      key: 'isActive',
      title: t('common.field.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? t('common.status.activeFemale') : t('common.status.inactiveFemale')}</Badge>,
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
            label: t('common.action.delete'),
            testId: 'crud-delete-button',
            icon: <Trash2 className="h-4 w-4" />,
            variant: 'outline-danger',
            disabled: !selected || deleting,
            onClick: () => setIsConfirmOpen(true),
          },
        ]}
        filtersSlot={
          <TableToolbar
            searchValue={search}
            onSearchChange={setSearch}
            searchPlaceholder={t('common.action.search')}
          />
        }
      >
        <DataTable
          columns={columns}
          data={items}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          emptyText={t('configuration.bankAccounts.empty')}
          loading={loading}
          pageSize={pageSize}
          pageSizeOptions={[5, 10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
        />
      </PageLayout>

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        description={t('configuration.bankAccounts.confirm.delete').replace('{0}', selected?.name ?? '')}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />

      <FinancialAccountFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        account={selected}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelected(null)
          if (page === 1) {
            void load()
          } else {
            setPage(1)
          }
        }}
      />
    </>
  )
}
