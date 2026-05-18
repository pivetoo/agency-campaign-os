import { useEffect, useMemo, useState } from 'react'
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn, FilterSection } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import { bankService, resolveBankLogoUrl } from '../../../services/bankService'
import type { Bank } from '../../../types/bank'
import BankFormModal from '../../../components/modals/BankFormModal'

export default function Banks() {
  const { t } = useI18n()
  const [items, setItems] = useState<Bank[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [includeInactiveFilter, setIncludeInactiveFilter] = useState('')
  const [selected, setSelected] = useState<Bank | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)
  const { execute: fetchAll, loading, pagination } = useApi<Bank[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const includeInactive = includeInactiveFilter === 'all'

  const load = async () => {
    const result = await fetchAll(() => bankService.getAll({ page, pageSize, search: debouncedSearch || undefined, includeInactive }))
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
        { value: 'all', label: 'Incluir inativos' },
      ],
      allLabel: 'Somente ativos',
    },
  ], [includeInactiveFilter, t])

  const clearFilters = () => setIncludeInactiveFilter('')

  const handleDelete = async () => {
    if (!selected) return
    const result = await runDelete(() => bankService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      setIsConfirmOpen(false)
      void load()
    }
  }

  const renderLogoCell = (_: unknown, record: Bank) => {
    const url = resolveBankLogoUrl(record.logoUrl)
    return (
      <div className="flex h-9 w-9 items-center justify-center overflow-hidden rounded border bg-muted/30">
        {url ? (
          <img src={url} alt={record.shortName} className="h-full w-full object-contain p-0.5" />
        ) : (
          <span className="text-[10px] font-semibold text-muted-foreground">{record.compe}</span>
        )}
      </div>
    )
  }

  const columns: DataTableColumn<Bank>[] = [
    { key: 'logo', title: t('configuration.banks.field.logo'), dataIndex: 'logoUrl', width: 64, render: renderLogoCell },
    { key: 'compe', title: t('configuration.banks.column.code'), dataIndex: 'compe', width: 100, render: (value: string) => <code className="rounded bg-muted px-1.5 py-0.5 text-xs">{value}</code> },
    {
      key: 'shortName',
      title: t('configuration.banks.field.shortName'),
      dataIndex: 'shortName',
      render: (value: string) => <span className="font-medium">{value}</span>,
    },
    { key: 'name', title: t('common.field.name'), dataIndex: 'name', hiddenBelow: 'md' },
    { key: 'ispb', title: t('configuration.banks.field.ispb'), dataIndex: 'ispb', hiddenBelow: 'lg', render: (value?: string | null) => value || '-' },
    {
      key: 'origin',
      title: t('configuration.banks.column.origin'),
      dataIndex: 'isSystem',
      hiddenBelow: 'sm',
      render: (_: unknown, record: Bank) => record.isSystem
        ? <span className="text-muted-foreground">{t('configuration.banks.origin.system')}</span>
        : <span>{record.createdByUserName ?? '—'}</span>,
    },
    {
      key: 'isActive',
      title: t('common.field.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? t('common.status.active') : t('common.status.inactive')}</Badge>,
    },
  ]

  const canDelete = selected != null && !selected.isSystem

  return (
    <>
      <PageLayout
        title={t('configuration.banks.title')}
        subtitle={t('configuration.banks.subtitle')}
        onAdd={() => { setSelected(null); setIsFormOpen(true) }}
        onEdit={() => selected && setIsFormOpen(true)}
        onRefresh={() => void load()}
        addLabel={t('configuration.banks.addLabel')}
        selectedRowsCount={selected ? 1 : 0}
        actions={[
          {
            key: 'delete',
            label: t('common.action.delete'),
            testId: 'crud-delete-button',
            icon: <Trash2 className="h-4 w-4" />,
            variant: 'ghost',
            disabled: !canDelete || deleting,
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
          emptyText={t('configuration.banks.empty')}
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
        description={t('configuration.banks.confirm.delete').replace('{0}', selected?.shortName ?? '')}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />

      <BankFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        bank={selected}
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
