import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  PageLayout,
  DataTable,
  Badge,
  SearchableSelect,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Button,
  TableToolbar,
  useApi,
  useI18n,
} from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { CheckCircle, Clock, Eye, Send, X, XCircle } from 'lucide-react'
import { proposalService, ProposalStatus, type Proposal, type ProposalStatusValue, type ProposalListFilters } from '../../services/proposalService'
import { commercialResponsibleService } from '../../services/commercialResponsibleService'
import type { CommercialResponsible } from '../../types/commercialResponsible'
import ProposalFormModal from '../../components/modals/ProposalFormModal'

const STATUS_ALL = '__all__'

const proposalStatusKeys: Record<ProposalStatusValue, string> = {
  [ProposalStatus.Draft]: 'proposal.status.draft',
  [ProposalStatus.Sent]: 'proposal.status.sent',
  [ProposalStatus.Viewed]: 'proposal.status.viewed',
  [ProposalStatus.Approved]: 'proposal.status.approved',
  [ProposalStatus.Rejected]: 'proposal.status.rejected',
  [ProposalStatus.Converted]: 'proposal.status.converted',
  [ProposalStatus.Expired]: 'proposal.status.expired',
  [ProposalStatus.Cancelled]: 'proposal.status.cancelled',
}

const proposalStatusVariant: Record<ProposalStatusValue, 'default' | 'warning' | 'success' | 'destructive'> = {
  [ProposalStatus.Draft]: 'default',
  [ProposalStatus.Sent]: 'warning',
  [ProposalStatus.Viewed]: 'warning',
  [ProposalStatus.Approved]: 'success',
  [ProposalStatus.Rejected]: 'destructive',
  [ProposalStatus.Converted]: 'success',
  [ProposalStatus.Expired]: 'destructive',
  [ProposalStatus.Cancelled]: 'destructive',
}

function isExpired(proposal: Proposal): boolean {
  if (proposal.status === ProposalStatus.Converted || proposal.status === ProposalStatus.Cancelled) {
    return false
  }
  if (!proposal.validityUntil) {
    return false
  }
  return new Date(proposal.validityUntil) < new Date()
}

export default function CommercialProposals() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [proposals, setProposals] = useState<Proposal[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [responsibles, setResponsibles] = useState<CommercialResponsible[]>([])
  const [selectedProposal, setSelectedProposal] = useState<Proposal | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>(STATUS_ALL)
  const [responsibleFilter, setResponsibleFilter] = useState('')

  const { execute: fetchProposals, loading, pagination } = useApi<Proposal[]>({ showErrorMessage: true })
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
    if (statusFilter !== STATUS_ALL) filters.status = Number(statusFilter) as ProposalStatusValue
    if (responsibleFilter) filters.internalOwnerId = Number(responsibleFilter)
    return filters
  }

  const loadProposals = async () => {
    const result = await fetchProposals(() => proposalService.getAll({ page, pageSize, ...buildFilters() }))
    if (result) {
      setProposals(result)
    }
  }

  useEffect(() => {
    setPage(1)
  }, [search, statusFilter, responsibleFilter])

  useEffect(() => {
    void loadProposals()
  }, [page, pageSize, search, statusFilter, responsibleFilter])

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
      title: t('proposals.field.proposal'),
      dataIndex: 'name',
      render: (value: string, record: Proposal) => (
        <div className="flex items-center gap-2">
          <span className="font-medium">{value}</span>
          {isExpired(record) && (
            <Badge variant="destructive" className="text-[10px]">{t('proposals.expiredBadge')}</Badge>
          )}
        </div>
      ),
    },
    { key: 'brand', title: t('campaign.field.brand'), dataIndex: 'brand', render: (value?: Proposal['brand']) => value?.name || '-' },
    {
      key: 'opportunity',
      title: t('proposals.field.opportunity'),
      dataIndex: 'opportunity',
      hiddenBelow: 'md',
      render: (value?: Proposal['opportunity']) => value ? `${value.name} (#${value.id})` : '-',
    },
    {
      key: 'totalValue',
      title: t('common.field.totalValue'),
      dataIndex: 'totalValue',
      render: (value?: number) => value != null ? `R$ ${value.toFixed(2)}` : '-',
    },
    {
      key: 'validityUntil',
      title: t('common.field.validity'),
      dataIndex: 'validityUntil',
      hiddenBelow: 'md',
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
      title: t('common.field.status'),
      dataIndex: 'status',
      render: (value?: number) => (
        <Badge variant={proposalStatusVariant[value ?? 0] || 'default'}>
          {proposalStatusKeys[value ?? 0] ? t(proposalStatusKeys[value ?? 0]) : '-'}
        </Badge>
      ),
    },
    {
      key: 'internalOwnerName',
      title: t('common.field.responsible'),
      dataIndex: 'internalOwnerName',
      hiddenBelow: 'lg',
      render: (value?: string) => value || '-',
    },
    {
      key: 'campaign',
      title: t('proposals.field.campaign'),
      dataIndex: 'campaign',
      hiddenBelow: 'lg',
      render: (value?: Proposal['campaign']) => value?.name || '-',
    },
    { key: 'items', title: t('common.field.items'), dataIndex: 'items', hiddenBelow: 'lg', render: (value?: Proposal['items']) => value?.length ?? 0 },
    {
      key: 'actions',
      title: '',
      dataIndex: undefined,
      width: 56,
      render: (_: any, record: Proposal) => (
        <button
          type="button"
          onClick={(e) => { e.stopPropagation(); navigate(`/comercial/propostas/${record.id}`) }}
          className="inline-flex items-center justify-center p-1 rounded hover:bg-accent hover:text-foreground text-muted-foreground transition-colors"
        >
          <Eye size={16} />
        </button>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title={t('proposals.title')}
        subtitle={t('proposals.subtitle')}
        onAdd={() => { setSelectedProposal(null); setIsFormOpen(true) }}
        onEdit={() => selectedProposal && setIsFormOpen(true)}
        onRefresh={() => void loadProposals()}
        selectedRowsCount={selectedProposal ? 1 : 0}
        actions={[
          {
            key: 'send',
            label: t('proposals.action.send'),
            icon: <Send className="h-4 w-4" />,
            variant: 'outline-primary',
            disabled: !selectedProposal || actionLoading || selectedProposal.status !== ProposalStatus.Draft,
            onClick: () => selectedProposal && void runProposalAction(() => proposalService.send(selectedProposal.id)),
          },
          {
            key: 'viewed',
            label: t('proposals.action.markViewed'),
            icon: <Eye className="h-4 w-4" />,
            variant: 'outline',
            disabled: !selectedProposal || actionLoading || selectedProposal.status !== ProposalStatus.Sent,
            onClick: () => selectedProposal && void runProposalAction(() => proposalService.markAsViewed(selectedProposal.id)),
          },
          {
            key: 'approve',
            label: t('proposals.action.approve'),
            icon: <CheckCircle className="h-4 w-4" />,
            variant: 'outline-success',
            disabled: !selectedProposal || actionLoading || selectedProposal.status !== ProposalStatus.Viewed,
            onClick: () => selectedProposal && void runProposalAction(() => proposalService.approve(selectedProposal.id)),
          },
          {
            key: 'reject',
            label: t('proposals.action.reject'),
            icon: <XCircle className="h-4 w-4" />,
            variant: 'outline-danger',
            disabled: !selectedProposal || actionLoading || (selectedProposal.status !== ProposalStatus.Sent && selectedProposal.status !== ProposalStatus.Viewed),
            onClick: () => selectedProposal && void runProposalAction(() => proposalService.reject(selectedProposal.id)),
          },
          {
            key: 'cancel',
            label: t('proposals.action.cancel'),
            icon: <Clock className="h-4 w-4" />,
            variant: 'outline-danger',
            disabled: !selectedProposal || actionLoading || selectedProposal.status === ProposalStatus.Cancelled || selectedProposal.status === ProposalStatus.Converted,
            onClick: () => selectedProposal && void runProposalAction(() => proposalService.cancel(selectedProposal.id)),
          },
        ]}
        filtersSlot={
          <TableToolbar
            searchValue={searchInput}
            onSearchChange={setSearchInput}
            searchPlaceholder={t('proposals.search.placeholder')}
            leftSlot={
              <>
                <div className="w-full lg:w-[200px]">
                  <Select value={statusFilter} onValueChange={setStatusFilter}>
                    <SelectTrigger>
                      <SelectValue placeholder={t('common.field.status')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value={STATUS_ALL}>{t('common.filter.allStatuses')}</SelectItem>
                      {Object.entries(proposalStatusKeys).map(([key, labelKey]) => (
                        <SelectItem key={key} value={key}>{t(labelKey)}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
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
                {hasActiveFilters ? (
                  <Button variant="outline" size="sm" icon={<X className="h-4 w-4" />} onClick={clearFilters}>
                    {t('common.action.clear')}
                  </Button>
                ) : null}
              </>
            }
          />
        }
      >
        <DataTable
          columns={columns}
          data={proposals}
          rowKey="id"
          selectedRows={selectedProposal ? [selectedProposal] : []}
          onSelectionChange={(rows) => setSelectedProposal(rows[0] ?? null)}
          onRowDoubleClick={(row) => navigate(`/comercial/propostas/${row.id}`)}
          emptyText={hasActiveFilters ? t('proposals.empty.filtered') : t('proposals.empty')}
          loading={loading}
          pageSize={pageSize}
          pageSizeOptions={[10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
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
