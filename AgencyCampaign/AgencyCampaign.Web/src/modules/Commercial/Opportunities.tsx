import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  PageLayout,
  DataTable,
  Badge,
  FilterPanel,
  TableToolbar,
  useApi,
  useI18n,
} from 'archon-ui'
import type { DataTableColumn, FilterSection } from 'archon-ui'
import { Eye } from 'lucide-react'
import { opportunityService, type Opportunity, type OpportunityListFilters } from '../../services/opportunityService'
import { commercialPipelineStageService } from '../../services/commercialPipelineStageService'
import { commercialResponsibleService } from '../../services/commercialResponsibleService'
import type { CommercialPipelineStage } from '../../types/commercialPipelineStage'
import type { CommercialResponsible } from '../../types/commercialResponsible'
import OpportunityFormModal from '../../components/modals/OpportunityFormModal'

const STATUS_ALL = ''

export default function CommercialOpportunities() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [opportunities, setOpportunities] = useState<Opportunity[]>([])
  const [stages, setStages] = useState<CommercialPipelineStage[]>([])
  const [responsibles, setResponsibles] = useState<CommercialResponsible[]>([])
  const [selectedOpportunity, setSelectedOpportunity] = useState<Opportunity | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)

  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [stageFilter, setStageFilter] = useState('')
  const [responsibleFilter, setResponsibleFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>(STATUS_ALL)

  const { execute: fetchOpportunities, loading, pagination } = useApi<Opportunity[]>({ showErrorMessage: true })

  useEffect(() => {
    void commercialPipelineStageService.getAll({ pageSize: 200 }).then((r) => setStages(r.data ?? []))
    void commercialResponsibleService.getAll().then(setResponsibles)
  }, [])

  useEffect(() => {
    const handle = window.setTimeout(() => setSearch(searchInput.trim()), 300)
    return () => window.clearTimeout(handle)
  }, [searchInput])

  useEffect(() => {
    setPage(1)
  }, [search, stageFilter, responsibleFilter, statusFilter])

  useEffect(() => {
    const filters: OpportunityListFilters = {}
    if (search) filters.search = search
    if (stageFilter) filters.commercialPipelineStageId = Number(stageFilter)
    if (responsibleFilter) filters.responsibleUserId = Number(responsibleFilter)
    if (statusFilter !== STATUS_ALL) filters.status = statusFilter as OpportunityListFilters['status']

    void fetchOpportunities(() => opportunityService.getAll({ page, pageSize, ...filters })).then((result) => {
      if (result) setOpportunities(result)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, search, stageFilter, responsibleFilter, statusFilter])

  const stageOptions = useMemo(
    () => stages.map((stage) => ({ value: stage.id.toString(), label: stage.name })),
    [stages]
  )

  const responsibleOptions = useMemo(
    () => responsibles.map((item) => ({ value: item.id.toString(), label: item.name })),
    [responsibles]
  )

  const filterSections: FilterSection[] = useMemo(() => [
    {
      key: 'status',
      label: t('common.field.status'),
      value: statusFilter,
      onChange: setStatusFilter,
      options: [
        { value: 'open', label: t('opportunities.status.open') },
        { value: 'won', label: t('opportunities.status.won') },
        { value: 'lost', label: t('opportunities.status.lost') },
      ],
    },
    {
      key: 'stage',
      label: t('opportunities.filter.stage'),
      value: stageFilter,
      onChange: setStageFilter,
      options: stageOptions,
    },
    {
      key: 'responsible',
      label: t('common.field.responsible'),
      value: responsibleFilter,
      onChange: setResponsibleFilter,
      options: responsibleOptions,
    },
  ], [statusFilter, stageFilter, responsibleFilter, stageOptions, responsibleOptions, t])

  const hasActiveFilters = !!search || !!stageFilter || !!responsibleFilter || statusFilter !== STATUS_ALL

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setStageFilter('')
    setResponsibleFilter('')
    setStatusFilter(STATUS_ALL)
  }

  const reload = () => {
    const filters: OpportunityListFilters = {}
    if (search) filters.search = search
    if (stageFilter) filters.commercialPipelineStageId = Number(stageFilter)
    if (responsibleFilter) filters.responsibleUserId = Number(responsibleFilter)
    if (statusFilter !== STATUS_ALL) filters.status = statusFilter as OpportunityListFilters['status']

    void fetchOpportunities(() => opportunityService.getAll({ page, pageSize, ...filters })).then((result) => {
      if (result) setOpportunities(result)
    })
  }

  const columns: DataTableColumn<Opportunity>[] = [
    { key: 'name', title: t('opportunities.field.opportunity'), dataIndex: 'name' },
    { key: 'brand', title: t('campaign.field.brand'), dataIndex: 'brand', render: (value?: Opportunity['brand']) => value?.name || '-' },
    { key: 'estimatedValue', title: t('opportunities.field.estimatedValue'), dataIndex: 'estimatedValue', hiddenBelow: 'md', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'expectedCloseAt', title: t('opportunities.field.expectedCloseAt'), dataIndex: 'expectedCloseAt', hiddenBelow: 'md', render: (value?: string) => value ? new Date(value).toLocaleDateString('pt-BR') : '-' },
    {
      key: 'stage',
      title: t('opportunities.field.stage'),
      dataIndex: 'commercialPipelineStage',
      render: (value?: Opportunity['commercialPipelineStage']) => (
        <Badge variant={value?.finalBehavior === 1 ? 'success' : value?.finalBehavior === 2 ? 'destructive' : 'warning'}>
          {value?.name || '-'}
        </Badge>
      ),
    },
    {
      key: 'responsible',
      title: t('common.field.responsible'),
      dataIndex: 'commercialResponsible',
      hiddenBelow: 'lg',
      render: (value?: Opportunity['commercialResponsible']) => value?.name || '-',
    },
    { key: 'followUps', title: t('opportunities.field.followUps'), dataIndex: 'followUps', hiddenBelow: 'lg', render: (value?: Opportunity['followUps']) => value?.filter((item) => !item.isCompleted).length ?? 0 },
    { key: 'proposals', title: t('opportunities.field.proposals'), dataIndex: 'proposals', hiddenBelow: 'lg', render: (value?: Opportunity['proposals']) => value?.length ?? 0 },
    {
      key: 'actions',
      title: '',
      dataIndex: undefined,
      width: 56,
      render: (_: any, record: Opportunity) => (
        <button
          className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
          onClick={(e) => { e.stopPropagation(); navigate(`/comercial/oportunidades/${record.id}`) }}
        >
          <Eye size={16} />
        </button>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title={t('opportunities.title')}
        subtitle={t('opportunities.subtitle')}
        onAdd={() => { setSelectedOpportunity(null); setIsFormOpen(true) }}
        onEdit={() => selectedOpportunity && setIsFormOpen(true)}
        onRefresh={reload}
        selectedRowsCount={selectedOpportunity ? 1 : 0}
      ><TableToolbar
          searchValue={searchInput}
          onSearchChange={setSearchInput}
          searchPlaceholder={t('opportunities.search.placeholder')}
          rightSlot={<FilterPanel sections={filterSections} onClearAll={clearFilters} />}
          className="mb-3"
        />
        <DataTable
          columns={columns}
          data={opportunities}
          rowKey="id"
          selectedRows={selectedOpportunity ? [selectedOpportunity] : []}
          onSelectionChange={(rows) => setSelectedOpportunity(rows[0] ?? null)}
          onRowDoubleClick={(row) => navigate(`/comercial/oportunidades/${row.id}`)}
          emptyText={hasActiveFilters ? t('opportunities.empty.filtered') : t('opportunities.empty')}
          loading={loading}
          pageSize={pageSize}
          pageSizeOptions={[10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
        />
      </PageLayout>

      <OpportunityFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        opportunity={selectedOpportunity}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedOpportunity(null)
          reload()
        }}
      />
    </>
  )
}
