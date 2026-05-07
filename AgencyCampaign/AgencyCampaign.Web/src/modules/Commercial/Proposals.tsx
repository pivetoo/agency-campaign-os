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
import { CheckCircle, Clock, Eye, Search, Send, X, XCircle } from 'lucide-react'
import { proposalService, type Proposal, type ProposalListFilters } from '../../services/proposalService'
import { commercialResponsibleService } from '../../services/commercialResponsibleService'
import type { CommercialResponsible } from '../../types/commercialResponsible'
import ProposalFormModal from '../../components/modals/ProposalFormModal'

const STATUS_ALL = '__all__'

const proposalStatusLabels: Record<number, string> = {
  1: 'Rascunho',
  2: 'Enviada',
  3: 'Visualizada',
  4: 'Aprovada',
  5: 'Rejeitada',
  6: 'Convertida',
  7: 'Expirada',
  8: 'Cancelada',
}

const proposalStatusVariant: Record<number, 'default' | 'warning' | 'success' | 'destructive'> = {
  1: 'default',
  2: 'warning',
  3: 'warning',
  4: 'success',
  5: 'destructive',
  6: 'success',
  7: 'destructive',
  8: 'destructive',
}

function isExpired(proposal: Proposal): boolean {
  if (proposal.status === 6 || proposal.status === 8) {
    return false
  }
  if (!proposal.validityUntil) {
    return false
  }
  return new Date(proposal.validityUntil) < new Date()
}

export default function CommercialProposals() {
  const navigate = useNavigate()
  const [proposals, setProposals] = useState<Proposal[]>([])
  const [responsibles, setResponsibles] = useState<CommercialResponsible[]>([])
  const [selectedProposal, setSelectedProposal] = useState<Proposal | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>(STATUS_ALL)
  const [responsibleFilter, setResponsibleFilter] = useState('')

  const { execute: fetchProposals, loading } = useApi<Proposal[]>({ showErrorMessage: true })
  const { execute: executeAction, loading: actionLoading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    void commercialResponsibleService.getAll().then(setResponsibles)
  }, [])

  useEffect(() => {
    const handle = window.setTimeout(() => setSearch(searchInput.trim()), 300)
    return () => window.clearTimeout(handle)
  }, [searchInput])

  const buildFilters = (): ProposalListFilters => {
    const filters: ProposalListFilters = {}
    if (search) filters.search = search
    if (statusFilter !== STATUS_ALL) filters.status = Number(statusFilter)
    if (responsibleFilter) filters.internalOwnerId = Number(responsibleFilter)
    return filters
  }

  const loadProposals = async () => {
    const result = await fetchProposals(() => proposalService.getAll({ pageSize: 200, ...buildFilters() }))
    if (result) {
      setProposals(result)
    }
  }

  useEffect(() => {
    void loadProposals()
  }, [search, statusFilter, responsibleFilter])

  const responsibleOptions = useMemo(
    () => responsibles.map((item) => ({ value: item.id.toString(), label: item.name })),
    [responsibles]
  )

  const hasActiveFilters = !!search || statusFilter !== STATUS_ALL || !!responsibleFilter

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setStatusFilter(STATUS_ALL)
    setResponsibleFilter('')
  }

  const runProposalAction = async (action: () => Promise<unknown>) => {
    const result = await executeAction(action)
    if (result !== null) {
      setSelectedProposal(null)
      void loadProposals()
    }
  }

  const columns: DataTableColumn<Proposal>[] = [
    {
      key: 'name',
      title: 'Proposta',
      dataIndex: 'name',
      render: (value: string, record: Proposal) => (
        <div className="flex items-center gap-2">
          <span className="font-medium">{value}</span>
          {isExpired(record) && (
            <Badge variant="destructive" className="text-[10px]">Expirada</Badge>
          )}
        </div>
      ),
    },
    { key: 'brand', title: 'Marca', dataIndex: 'brand', render: (value?: Proposal['brand']) => value?.name || '-' },
    {
      key: 'opportunity',
      title: 'Oportunidade',
      dataIndex: 'opportunity',
      render: (value?: Proposal['opportunity']) => value ? `${value.name} (#${value.id})` : '-',
    },
    {
      key: 'totalValue',
      title: 'Valor Total',
      dataIndex: 'totalValue',
      render: (value?: number) => value != null ? `R$ ${value.toFixed(2)}` : '-',
    },
    {
      key: 'validityUntil',
      title: 'Validade',
      dataIndex: 'validityUntil',
      render: (value?: string, record?: Proposal) => {
        if (!value) return '-'
        const date = new Date(value).toLocaleDateString('pt-BR')
        if (record && isExpired(record)) {
          return <span className="text-destructive font-medium">{date}</span>
        }
        return date
      },
    },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      render: (value?: number) => (
        <Badge variant={proposalStatusVariant[value ?? 0] || 'default'}>
          {proposalStatusLabels[value ?? 0] || '-'}
        </Badge>
      ),
    },
    {
      key: 'internalOwnerName',
      title: 'Responsável',
      dataIndex: 'internalOwnerName',
      render: (value?: string) => value || '-',
    },
    {
      key: 'campaign',
      title: 'Campanha',
      dataIndex: 'campaign',
      render: (value?: Proposal['campaign']) => value?.name || '-',
    },
    { key: 'items', title: 'Itens', dataIndex: 'items', render: (value?: Proposal['items']) => value?.length ?? 0 },
  ]

  return (
    <>
      <PageLayout
        title="Propostas"
        subtitle="Gerencie propostas, vínculos com oportunidades e conversão para campanhas"
        onAdd={() => { setSelectedProposal(null); setIsFormOpen(true) }}
        onEdit={() => selectedProposal && setIsFormOpen(true)}
        onRefresh={() => void loadProposals()}
        selectedRowsCount={selectedProposal ? 1 : 0}
        actions={[
          {
            key: 'send',
            label: 'Enviar',
            icon: <Send className="h-4 w-4" />,
            variant: 'outline-primary',
            disabled: !selectedProposal || actionLoading || selectedProposal.status !== 1,
            onClick: () => selectedProposal && void runProposalAction(() => proposalService.send(selectedProposal.id)),
          },
          {
            key: 'viewed',
            label: 'Visualizada',
            icon: <Eye className="h-4 w-4" />,
            variant: 'outline',
            disabled: !selectedProposal || actionLoading || selectedProposal.status !== 2,
            onClick: () => selectedProposal && void runProposalAction(() => proposalService.markAsViewed(selectedProposal.id)),
          },
          {
            key: 'approve',
            label: 'Aprovar',
            icon: <CheckCircle className="h-4 w-4" />,
            variant: 'outline-success',
            disabled: !selectedProposal || actionLoading || selectedProposal.status !== 3,
            onClick: () => selectedProposal && void runProposalAction(() => proposalService.approve(selectedProposal.id)),
          },
          {
            key: 'reject',
            label: 'Rejeitar',
            icon: <XCircle className="h-4 w-4" />,
            variant: 'outline-danger',
            disabled: !selectedProposal || actionLoading || (selectedProposal.status !== 2 && selectedProposal.status !== 3),
            onClick: () => selectedProposal && void runProposalAction(() => proposalService.reject(selectedProposal.id)),
          },
          {
            key: 'cancel',
            label: 'Cancelar',
            icon: <Clock className="h-4 w-4" />,
            variant: 'outline-danger',
            disabled: !selectedProposal || actionLoading || selectedProposal.status === 8 || selectedProposal.status === 6,
            onClick: () => selectedProposal && void runProposalAction(() => proposalService.cancel(selectedProposal.id)),
          },
        ]}
      >
        <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center">
          <div className="relative flex-1 min-w-[240px]">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Buscar por proposta, marca ou oportunidade..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              className="pl-9"
            />
          </div>
          <div className="w-full lg:w-[200px]">
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger>
                <SelectValue placeholder="Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={STATUS_ALL}>Todos os status</SelectItem>
                {Object.entries(proposalStatusLabels).map(([key, label]) => (
                  <SelectItem key={key} value={key}>{label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
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
          {hasActiveFilters ? (
            <Button variant="outline" size="sm" icon={<X className="h-4 w-4" />} onClick={clearFilters}>
              Limpar
            </Button>
          ) : null}
        </div>

        <DataTable
          columns={columns}
          data={proposals}
          rowKey="id"
          selectedRows={selectedProposal ? [selectedProposal] : []}
          onSelectionChange={(rows) => setSelectedProposal(rows[0] ?? null)}
          onRowDoubleClick={(row) => navigate(`/comercial/propostas/${row.id}`)}
          emptyText={hasActiveFilters ? 'Nenhuma proposta encontrada com os filtros atuais' : 'Nenhuma proposta cadastrada'}
          loading={loading}
          pageSize={10}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <ProposalFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        proposal={selectedProposal}
        onSuccess={(savedProposal) => {
          setIsFormOpen(false)
          setSelectedProposal(null)
          void loadProposals()

          if (!selectedProposal && savedProposal) {
            navigate(`/comercial/propostas/${savedProposal.id}`)
          }
        }}
      />
    </>
  )
}
