import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, ConfirmModal, Switch, TableToolbar, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Power, PowerOff, Trash2 } from 'lucide-react'
import { financialAccountService } from '../../../services/financialAccountService'
import { financialAccountTypeLabels, type FinancialAccount } from '../../../types/financialAccount'
import FinancialAccountFormModal from '../../../components/modals/FinancialAccountFormModal'
import AuditUtilityBar from '../../../components/buttons/AuditUtilityBar'

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
  const [includeInactive, setIncludeInactive] = useState(false)
  const [selected, setSelected] = useState<FinancialAccount | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)
  const [isToggleConfirmOpen, setIsToggleConfirmOpen] = useState(false)
  const { execute: fetchAll, loading, pagination } = useApi<FinancialAccount[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: runToggle, loading: toggling } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchAll(() => financialAccountService.getAll({ page, pageSize, search: debouncedSearch || undefined, includeInactive }))
    if (result) setItems(result)
  }

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(timeout)
  }, [search])

  useEffect(() => {
    setPage(1)
  }, [debouncedSearch, includeInactive])

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch, includeInactive])

  const handleDelete = async () => {
    if (!selected) return
    const result = await runDelete(() => financialAccountService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      setIsConfirmOpen(false)
      void load()
    }
  }

  const handleToggleActive = async () => {
    if (!selected) return
    const result = await runToggle(() => financialAccountService.update(selected.id, {
      id: selected.id,
      name: selected.name,
      type: selected.type,
      bank: selected.bank ?? undefined,
      agency: selected.agency ?? undefined,
      number: selected.number ?? undefined,
      initialBalance: selected.initialBalance,
      color: selected.color,
      isActive: !selected.isActive,
    }))
    if (result !== null) {
      setSelected(null)
      setIsToggleConfirmOpen(false)
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

  const toggleLabel = selected?.isActive
    ? t('configuration.bankAccounts.action.deactivate')
    : t('configuration.bankAccounts.action.activate')

  const toggleConfirmTemplate = selected?.isActive
    ? t('configuration.bankAccounts.confirm.deactivate')
    : t('configuration.bankAccounts.confirm.activate')

  return (
    <>
      <PageLayout
        title={t('configuration.bankAccounts.title')}
        subtitle={t('configuration.bankAccounts.subtitle')}
        onAdd={() => { setSelected(null); setIsFormOpen(true) }}
        onEdit={() => selected && setIsFormOpen(true)}
        onRefresh={() => void load()}
        actionsSlot={<AuditUtilityBar entityName="FinancialAccount" entityLabel={t('configuration.bankAccounts.audit.entityLabel')} entityId={selected?.id ?? null} />}
        addLabel={t('configuration.bankAccounts.addLabel')}
        selectedRowsCount={selected ? 1 : 0}
        actions={[
          {
            key: 'toggle',
            label: toggleLabel,
            testId: 'crud-toggle-active-button',
            icon: selected?.isActive ? <PowerOff className="h-4 w-4" /> : <Power className="h-4 w-4" />,
            variant: 'ghost',
            disabled: !selected || toggling,
            onClick: () => setIsToggleConfirmOpen(true),
          },
          {
            key: 'delete',
            label: t('common.action.delete'),
            testId: 'crud-delete-button',
            icon: <Trash2 className="h-4 w-4" />,
            variant: 'ghost',
            disabled: !selected || deleting,
            onClick: () => setIsConfirmOpen(true),
          },
        ]}
      >
        <TableToolbar
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder={t('common.action.search')}
          rightSlot={
            <label className="flex items-center gap-2 text-sm">
              <Switch checked={includeInactive} onCheckedChange={(checked) => setIncludeInactive(!!checked)} />
              <span>{t('configuration.bankAccounts.filter.includeInactive')}</span>
            </label>
          }
          className="mb-3"
        />
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

      <ConfirmModal
        open={isToggleConfirmOpen}
        onOpenChange={setIsToggleConfirmOpen}
        description={toggleConfirmTemplate.replace('{0}', selected?.name ?? '')}
        variant={selected?.isActive ? 'warning' : 'primary'}
        onConfirm={() => void handleToggleActive()}
        loading={toggling}
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
