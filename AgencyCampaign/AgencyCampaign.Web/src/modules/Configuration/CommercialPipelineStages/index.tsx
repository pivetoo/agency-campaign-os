import { useEffect, useMemo, useState } from 'react'
import { Badge, DataTable, FilterPanel, PageLayout, TableToolbar, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn, FilterSection } from 'archon-ui'
import CommercialPipelineStageFormModal from '../../../components/modals/CommercialPipelineStageFormModal'
import { commercialPipelineStageService } from '../../../services/commercialPipelineStageService'
import type { CommercialPipelineStage } from '../../../types/commercialPipelineStage'

const finalBehaviorLabels: Record<number, string> = {
  0: 'Aberto',
  1: 'Ganha',
  2: 'Perdida',
}

export default function CommercialPipelineStages() {
  const { t } = useI18n()
  const [stages, setStages] = useState<CommercialPipelineStage[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [includeInactiveFilter, setIncludeInactiveFilter] = useState('')
  const [selectedStage, setSelectedStage] = useState<CommercialPipelineStage | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchStages, loading, pagination } = useApi<CommercialPipelineStage[]>({ showErrorMessage: true })

  const loadStages = async () => {
    const result = await fetchStages(() => commercialPipelineStageService.getAll({ page, pageSize, search: debouncedSearch || undefined, includeInactive: includeInactiveFilter === 'all' }))
    if (result) {
      setStages(result)
    }
  }

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(timeout)
  }, [search])

  useEffect(() => {
    setPage(1)
  }, [debouncedSearch, includeInactiveFilter])

  useEffect(() => {
    void loadStages()
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

  const columns: DataTableColumn<CommercialPipelineStage>[] = [
    { key: 'name', title: t('configuration.commercialFunnel.field.stage'), dataIndex: 'name' },
    { key: 'displayOrder', title: t('common.field.order'), dataIndex: 'displayOrder' },
    { key: 'color', title: t('common.field.color'), dataIndex: 'color', render: (value: string) => <div className="flex items-center gap-2"><span className="h-4 w-4 rounded-full border" style={{ backgroundColor: value }} /><span>{value}</span></div> },
    { key: 'finalBehavior', title: t('configuration.commercialFunnel.field.closing'), dataIndex: 'finalBehavior', render: (value: number) => finalBehaviorLabels[value] || 'Aberto' },
    { key: 'isInitial', title: t('configuration.commercialFunnel.field.initial'), dataIndex: 'isInitial', render: (value: boolean) => <Badge variant={value ? 'success' : 'outline'}>{value ? t('common.status.yes') : t('common.status.no')}</Badge> },
    { key: 'isActive', title: t('common.field.status'), dataIndex: 'isActive', render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? t('common.status.active') : t('common.status.inactive')}</Badge> },
  ]

  return (
    <>
      <PageLayout
        title={t('configuration.commercialFunnel.title')}
        subtitle={t('configuration.commercialFunnel.subtitle')}
        onAdd={() => { setSelectedStage(null); setIsFormOpen(true) }}
        onEdit={() => selectedStage && setIsFormOpen(true)}
        onRefresh={() => void loadStages()}
        selectedRowsCount={selectedStage ? 1 : 0}
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
          data={stages}
          rowKey="id"
          selectedRows={selectedStage ? [selectedStage] : []}
          onSelectionChange={(rows) => setSelectedStage(rows[0] ?? null)}
          emptyText={t('configuration.commercialFunnel.empty')}
          loading={loading}
          pageSize={pageSize}
          pageSizeOptions={[5, 10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
        />
      </PageLayout>

      <CommercialPipelineStageFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        stage={selectedStage}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedStage(null)
          if (page === 1) {
            void loadStages()
          } else {
            setPage(1)
          }
        }}
      />
    </>
  )
}
