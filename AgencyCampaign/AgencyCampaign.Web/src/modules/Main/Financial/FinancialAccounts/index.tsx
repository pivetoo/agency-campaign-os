import { useEffect, useMemo, useState } from 'react'
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn, FilterSection } from 'archon-ui'
import { Link as LinkIcon, Power, PowerOff, RefreshCw, Trash2 } from 'lucide-react'
import { financialAccountService } from '../../../../services/financialAccountService'
import { resolveBankLogoUrl } from '../../../../services/bankService'
import { FinancialAccountSyncStatus, financialAccountTypeLabels, type FinancialAccount, type FinancialAccountSyncStatusValue } from '../../../../types/financialAccount'
import FinancialAccountFormModal from '../../../../components/modals/FinancialAccountFormModal'
import FinancialAccountConnectorBindingModal from '../../../../components/modals/FinancialAccountConnectorBindingModal'
import AuditUtilityBar from '../../../../components/buttons/AuditUtilityBar'

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

type BadgeVariant = 'outline' | 'warning' | 'success' | 'destructive'

const SYNC_STATUS_META: Record<FinancialAccountSyncStatusValue, { variant: BadgeVariant; labelKey: string }> = {
  [FinancialAccountSyncStatus.NotConfigured]: { variant: 'outline', labelKey: 'configuration.bankAccounts.syncStatus.notConfigured' },
  [FinancialAccountSyncStatus.Pending]: { variant: 'warning', labelKey: 'configuration.bankAccounts.syncStatus.pending' },
  [FinancialAccountSyncStatus.Synced]: { variant: 'success', labelKey: 'configuration.bankAccounts.syncStatus.synced' },
  [FinancialAccountSyncStatus.Error]: { variant: 'destructive', labelKey: 'configuration.bankAccounts.syncStatus.error' },
}

export default function FinancialAccounts() {
  const { t } = useI18n()
  const [items, setItems] = useState<FinancialAccount[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [includeInactiveFilter, setIncludeInactiveFilter] = useState('')
  const [selected, setSelected] = useState<FinancialAccount | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)
  const [isToggleConfirmOpen, setIsToggleConfirmOpen] = useState(false)
  const [isConnectorModalOpen, setIsConnectorModalOpen] = useState(false)
  const { execute: fetchAll, loading, pagination } = useApi<FinancialAccount[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: runToggle, loading: toggling } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: runSync, loading: syncing } = useApi<{ executionId: number }>({ showSuccessMessage: true, showErrorMessage: true })

  const includeInactive = includeInactiveFilter === 'all'

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
  }, [debouncedSearch, includeInactiveFilter])

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch, includeInactiveFilter])

  const filterSections: FilterSection[] = useMemo(() => [
    {
      key: 'inactiveFilter',
      label: t('common.field.status'),
      value: includeInactiveFilter,
      onChange: setIncludeInactiveFilter,
      options: [
        { value: 'all', label: t('configuration.bankAccounts.filter.includeInactive') },
      ],
      allLabel: 'Somente ativas',
    },
  ], [includeInactiveFilter, t])

  const clearFilters = () => {
    setIncludeInactiveFilter('')
  }

  const refreshList = () => {
    void load()
  }

  const handleDelete = async () => {
    if (!selected) return
    const result = await runDelete(() => financialAccountService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      setIsConfirmOpen(false)
      refreshList()
    }
  }

  const handleSync = async () => {
    if (!selected) return
    const result = await runSync(() => financialAccountService.sync(selected.id))
    if (result !== null) {
      refreshList()
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
      refreshList()
    }
  }

  const renderLogoCell = (_: unknown, record: FinancialAccount) => {
    const url = resolveBankLogoUrl(record.bankLogoUrl)
    return (
      <div className="flex h-9 w-9 items-center justify-center overflow-hidden rounded border bg-muted/30">
        {url ? (
          <img src={url} alt={record.bankShortName ?? ''} className="h-full w-full object-contain p-0.5" />
        ) : (
          <span className="text-xs font-semibold text-muted-foreground">
            {(record.bankShortName ?? record.name)?.charAt(0).toUpperCase() ?? '?'}
          </span>
        )}
      </div>
    )
  }

  const columns: DataTableColumn<FinancialAccount>[] = [
    { key: 'logo', title: '', dataIndex: 'bankLogoUrl', width: 56, render: renderLogoCell },
    {
      key: 'name',
      title: t('common.field.account'),
      dataIndex: 'name',
      render: (value: string) => <span className="font-medium">{value}</span>,
    },
    { key: 'type', title: t('common.field.type'), dataIndex: 'type', render: (value: number) => financialAccountTypeLabels[value] || '-' },
    {
      key: 'bank',
      title: t('configuration.bankAccounts.field.bank'),
      dataIndex: 'bankShortName',
      render: (value?: string | null, record?: FinancialAccount) => value ?? record?.bank ?? '-',
    },
    {
      key: 'currentBalance',
      title: t('configuration.bankAccounts.field.currentBalance'),
      dataIndex: 'currentBalance',
      render: (value: number) => <span className={value < 0 ? 'text-destructive font-medium' : 'font-medium'}>{formatCurrency(value)}</span>,
    },
    {
      key: 'syncStatus',
      title: t('configuration.bankAccounts.field.sync'),
      dataIndex: 'syncStatus',
      render: (value: FinancialAccountSyncStatusValue, record) => {
        const meta = SYNC_STATUS_META[value] ?? SYNC_STATUS_META[FinancialAccountSyncStatus.NotConfigured]
        const tooltipParts: string[] = []
        if (record.lastSyncedAt) {
          tooltipParts.push(t('configuration.bankAccounts.tooltip.lastSync').replace('{0}', new Date(record.lastSyncedAt).toLocaleString('pt-BR')))
        }
        if (record.lastSyncedBalance != null) {
          tooltipParts.push(t('configuration.bankAccounts.tooltip.bankBalance').replace('{0}', formatCurrency(record.lastSyncedBalance)))
        }
        const tooltip = tooltipParts.join(' • ')
        return <span title={tooltip || undefined}><Badge variant={meta.variant}>{t(meta.labelKey)}</Badge></span>
      },
    },
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
        onRefresh={refreshList}
        actionsSlot={<AuditUtilityBar entityName="FinancialAccount" entityLabel={t('configuration.bankAccounts.audit.entityLabel')} entityId={selected?.id ?? null} />}
        addLabel={t('configuration.bankAccounts.addLabel')}
        selectedRowsCount={selected ? 1 : 0}
        actions={[
          {
            key: 'sync',
            label: t('configuration.bankAccounts.action.sync'),
            testId: 'financial-account-sync-button',
            icon: <RefreshCw className={`h-4 w-4 ${syncing ? 'animate-spin' : ''}`} />,
            variant: 'ghost',
            disabled: !selected || syncing || !selected?.integrationConnectorId,
            onClick: () => void handleSync(),
          },
          {
            key: 'connector',
            label: t('configuration.bankAccounts.action.openConnector'),
            testId: 'financial-account-connector-button',
            icon: <LinkIcon className="h-4 w-4" />,
            variant: 'ghost',
            disabled: !selected,
            onClick: () => setIsConnectorModalOpen(true),
          },
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
          rightSlot={<FilterPanel sections={filterSections} onClearAll={clearFilters} />}
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

      <FinancialAccountConnectorBindingModal
        open={isConnectorModalOpen}
        onOpenChange={setIsConnectorModalOpen}
        account={selected}
        onSuccess={() => {
          setIsConnectorModalOpen(false)
          refreshList()
        }}
      />

      <FinancialAccountFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        account={selected}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelected(null)
          if (page === 1) {
            refreshList()
          } else {
            setPage(1)
          }
        }}
      />
    </>
  )
}
