import { useEffect, useMemo, useState } from 'react'
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn, FilterSection } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import {
  proposalTemplateService,
  type ProposalTemplate,
} from '../../../services/proposalTemplateService'
import ProposalTemplateFormModal from '../../../components/modals/ProposalTemplateFormModal'

export default function ProposalTemplates() {
  const { t } = useI18n()
  const [templates, setTemplates] = useState<ProposalTemplate[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [includeInactiveFilter, setIncludeInactiveFilter] = useState('')
  const [selected, setSelected] = useState<ProposalTemplate | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)

  const { execute: fetchTemplates, loading, pagination } = useApi<ProposalTemplate[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchTemplates(() => proposalTemplateService.getAll({ page, pageSize, search: debouncedSearch || undefined, includeInactive: includeInactiveFilter === 'all' }))
    if (result) setTemplates(result)
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
    const result = await runDelete(() => proposalTemplateService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      setIsConfirmOpen(false)
      void load()
    }
  }

  const columns: DataTableColumn<ProposalTemplate>[] = [
    { key: 'name', title: t('configuration.proposalTemplates.field.template'), dataIndex: 'name' },
    {
      key: 'description',
      title: t('common.field.description'),
      dataIndex: 'description',
      render: (value?: string) => value || '-',
    },
    {
      key: 'items',
      title: t('common.field.items'),
      dataIndex: 'items',
      render: (value?: ProposalTemplate['items']) => value?.length ?? 0,
    },
    {
      key: 'isActive',
      title: t('common.field.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>{value ? t('common.status.active') : t('common.status.inactive')}</Badge>
      ),
    },
    {
      key: 'createdByUserName',
      title: t('configuration.proposalTemplates.field.createdBy'),
      dataIndex: 'createdByUserName',
      render: (value?: string) => value || '-',
    },
  ]

  return (
    <>
      <PageLayout
        title={t('configuration.proposalTemplates.title')}
        subtitle={t('configuration.proposalTemplates.subtitle')}
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
          data={templates}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          emptyText={t('configuration.proposalTemplates.empty')}
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
        description={t('configuration.proposalTemplates.confirm.delete').replace('{0}', selected?.name ?? '')}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />

      <ProposalTemplateFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        template={selected}
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
