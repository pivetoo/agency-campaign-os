import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  PageLayout,
  DataTable,
  Badge,
  Input,
  SearchableSelect,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Button,
  useApi,
  useI18n,
} from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Eye, Search, X } from 'lucide-react'
import { opportunityService, type Opportunity, type OpportunityListFilters } from '../../services/opportunityService'
import { commercialPipelineStageService } from '../../services/commercialPipelineStageService'
import { commercialResponsibleService } from '../../services/commercialResponsibleService'
import type { CommercialPipelineStage } from '../../types/commercialPipelineStage'
import type { CommercialResponsible } from '../../types/commercialResponsible'
import OpportunityFormModal from '../../components/modals/OpportunityFormModal'

const STATUS_ALL = '__all__'

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
      >
        <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center">
          <div className="relative flex-1 min-w-[240px]">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder={t('opportunities.search.placeholder')}
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              className="pl-9"
            />
          </div>
          <div className="w-full lg:w-[200px]">
            <SearchableSelect
              value={stageFilter}
              onValueChange={setStageFilter}
              options={stageOptions}
              placeholder={t('opportunities.filter.allStages')}
              searchPlaceholder={t('opportunities.filter.searchStage')}
            />
          </div>
          <div className="w-full lg:w-[200px]">
            <SearchableSelect
              value={responsibleFilter}
              onValueChange={setResponsibleFilter}
              options={responsibleOptions}
              placeholder={t('proposals.filter.allResponsibles')}
              searchPlaceholder={t('proposals.filter.searchResponsible')}
            />
          </div>
          <div className="w-full lg:w-[160px]">
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger>
                <SelectValue placeholder={t('common.field.status')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={STATUS_ALL}>{t('opportunities.status.all')}</SelectItem>
                <SelectItem value="open">{t('opportunities.status.open')}</SelectItem>
                <SelectItem value="won">{t('opportunities.status.won')}</SelectItem>
                <SelectItem value="lost">{t('opportunities.status.lost')}</SelectItem>
              </SelectContent>
            </Select>
          </div>
          {hasActiveFilters ? (
            <Button variant="outline" size="sm" icon={<X className="h-4 w-4" />} onClick={clearFilters}>
              {t('common.action.clear')}
            </Button>
          ) : null}
        </div>

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
