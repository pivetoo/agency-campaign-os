import { useEffect, useMemo, useState } from 'react'
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn, FilterSection } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import { opportunityTagService } from '../../../services/opportunitySourceService'
import type { OpportunityTag } from '../../../types/opportunitySource'
import { OpportunityTagFormModal } from '../../../components/modals/OpportunitySourceFormModal'

export default function OpportunityTags() {
  const { t } = useI18n()
  const [items, setItems] = useState<OpportunityTag[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [includeInactiveFilter, setIncludeInactiveFilter] = useState('')
  const [selected, setSelected] = useState<OpportunityTag | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)
  const { execute: fetchAll, loading, pagination } = useApi<OpportunityTag[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchAll(() => opportunityTagService.getAll({ page, pageSize, search: debouncedSearch || undefined, includeInactive: includeInactiveFilter === 'all' }))
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

  const clearFilters = () => {
    setIncludeInactiveFilter('')
  }

  const handleDelete = async () => {
    if (!selected) return
    const result = await runDelete(() => opportunityTagService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      setIsConfirmOpen(false)
      void load()
    }
  }

  const columns: DataTableColumn<OpportunityTag>[] = [
    {
      key: 'name',
      title: t('configuration.opportunityTags.field.tag'),
      dataIndex: 'name',
      render: (value: string, record: OpportunityTag) => (
        <span
          className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium"
          style={{ backgroundColor: `${record.color}25`, color: record.color }}
        >
          {value}
        </span>
      ),
    },
    {
      key: 'isActive',
      title: t('common.field.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>{value ? t('common.status.activeFemale') : t('common.status.inactiveFemale')}</Badge>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title={t('configuration.opportunityTags.title')}
        subtitle={t('configuration.opportunityTags.subtitle')}
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
          emptyText={t('configuration.opportunityTags.empty')}
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
        description={t('configuration.opportunityTags.confirm.delete').replace('{0}', selected?.name ?? '')}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />

      <OpportunityTagFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        tag={selected}
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
