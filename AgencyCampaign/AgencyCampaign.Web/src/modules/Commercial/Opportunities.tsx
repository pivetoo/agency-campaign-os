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
  const navigate = useNavigate()
  const [opportunities, setOpportunities] = useState<Opportunity[]>([])
  const [stages, setStages] = useState<CommercialPipelineStage[]>([])
  const [responsibles, setResponsibles] = useState<CommercialResponsible[]>([])
  const [selectedOpportunity, setSelectedOpportunity] = useState<Opportunity | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)

  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [stageFilter, setStageFilter] = useState('')
  const [responsibleFilter, setResponsibleFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>(STATUS_ALL)

  const { execute: fetchOpportunities, loading } = useApi<Opportunity[]>({ showErrorMessage: true })

  useEffect(() => {
    void commercialPipelineStageService.getAll().then(setStages)
    void commercialResponsibleService.getAll().then(setResponsibles)
  }, [])

  useEffect(() => {
    const handle = window.setTimeout(() => setSearch(searchInput.trim()), 300)
    return () => window.clearTimeout(handle)
  }, [searchInput])

  useEffect(() => {
    const filters: OpportunityListFilters = {}
    if (search) filters.search = search
    if (stageFilter) filters.commercialPipelineStageId = Number(stageFilter)
    if (responsibleFilter) filters.commercialResponsibleId = Number(responsibleFilter)
    if (statusFilter !== STATUS_ALL) filters.status = statusFilter as OpportunityListFilters['status']

    void fetchOpportunities(() => opportunityService.getAll({ pageSize: 200, ...filters })).then((result) => {
      if (result) setOpportunities(result)
    })
  }, [search, stageFilter, responsibleFilter, statusFilter, fetchOpportunities])

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
    if (responsibleFilter) filters.commercialResponsibleId = Number(responsibleFilter)
    if (statusFilter !== STATUS_ALL) filters.status = statusFilter as OpportunityListFilters['status']

    void fetchOpportunities(() => opportunityService.getAll({ pageSize: 200, ...filters })).then((result) => {
      if (result) setOpportunities(result)
    })
  }

  const columns: DataTableColumn<Opportunity>[] = [
    { key: 'name', title: 'Oportunidade', dataIndex: 'name' },
    { key: 'brand', title: 'Marca', dataIndex: 'brand', render: (value?: Opportunity['brand']) => value?.name || '-' },
    { key: 'estimatedValue', title: 'Valor estimado', dataIndex: 'estimatedValue', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'expectedCloseAt', title: 'Fechamento', dataIndex: 'expectedCloseAt', render: (value?: string) => value ? new Date(value).toLocaleDateString('pt-BR') : '-' },
    {
      key: 'stage',
      title: 'Estágio',
      dataIndex: 'commercialPipelineStage',
      render: (value?: Opportunity['commercialPipelineStage']) => (
        <Badge variant={value?.finalBehavior === 1 ? 'success' : value?.finalBehavior === 2 ? 'destructive' : 'warning'}>
          {value?.name || '-'}
        </Badge>
      ),
    },
    {
      key: 'responsible',
      title: 'Responsável',
      dataIndex: 'commercialResponsible',
      render: (value?: Opportunity['commercialResponsible']) => value?.name || '-',
    },
    { key: 'followUps', title: 'Follow-ups', dataIndex: 'followUps', render: (value?: Opportunity['followUps']) => value?.filter((item) => !item.isCompleted).length ?? 0 },
    { key: 'proposals', title: 'Propostas', dataIndex: 'proposals', render: (value?: Opportunity['proposals']) => value?.length ?? 0 },
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
        title="Oportunidades"
        subtitle="Gerencie o funil comercial e acompanhe propostas, negociações e follow-ups"
        onAdd={() => { setSelectedOpportunity(null); setIsFormOpen(true) }}
        onEdit={() => selectedOpportunity && setIsFormOpen(true)}
        onRefresh={reload}
        selectedRowsCount={selectedOpportunity ? 1 : 0}
      >
        <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center">
          <div className="relative flex-1 min-w-[240px]">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Buscar por nome, marca, contato ou e-mail..."
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
              placeholder="Todos os estágios"
              searchPlaceholder="Buscar estágio..."
            />
          </div>
          <div className="w-full lg:w-[200px]">
            <SearchableSelect
              value={responsibleFilter}
              onValueChange={setResponsibleFilter}
              options={responsibleOptions}
              placeholder="Todos os responsáveis"
              searchPlaceholder="Buscar responsável..."
            />
          </div>
          <div className="w-full lg:w-[160px]">
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger>
                <SelectValue placeholder="Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={STATUS_ALL}>Todos</SelectItem>
                <SelectItem value="open">Abertas</SelectItem>
                <SelectItem value="won">Ganhas</SelectItem>
                <SelectItem value="lost">Perdidas</SelectItem>
              </SelectContent>
            </Select>
          </div>
          {hasActiveFilters ? (
            <Button variant="outline" size="sm" icon={<X className="h-4 w-4" />} onClick={clearFilters}>
              Limpar
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
          emptyText={hasActiveFilters ? 'Nenhuma oportunidade encontrada com os filtros atuais' : 'Nenhuma oportunidade cadastrada'}
          loading={loading}
          pageSize={10}
          pageSizeOptions={[5, 10, 20, 50]}
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
