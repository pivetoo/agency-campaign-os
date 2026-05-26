import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, Button, FilterPanel, TableToolbar, Dropdown, DropdownContent, DropdownItem, DropdownTrigger, DropdownSeparator, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn, FilterSection } from 'archon-ui'
import { CheckCircle, Clock, Eye, MoreHorizontal, Pencil, Plus, Send, XCircle } from 'lucide-react'
import { proposalService, ProposalStatus, type Proposal, type ProposalStatusValue, type ProposalListFilters } from '../../../services/proposalService'
import { commercialResponsibleService } from '../../../services/commercialResponsibleService'
import type { CommercialResponsible } from '../../../types/commercialResponsible'
import ProposalFormModal from '../../../components/modals/ProposalFormModal'
import AuditUtilityBar from '../../../components/buttons/AuditUtilityBar'

const STATUS_ALL = ''

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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, search, statusFilter, responsibleFilter])

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
      options: Object.entries(proposalStatusKeys).map(([key, labelKey]) => ({
        value: key,
        label: t(labelKey),
      })),
    },
    {
      key: 'responsible',
      label: t('common.field.responsible'),
      value: responsibleFilter,
      onChange: setResponsibleFilter,
      options: responsibleOptions,
    },
  ], [statusFilter, responsibleFilter, responsibleOptions, t])

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
    { key: 'id', title: t('common.field.code'), dataIndex: 'id', width: 72, render: (value: number) => <code className="rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground">{value}</code> },
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
        showDefaultActions={false}
        onRefresh={() => void loadProposals()}
        actionsSlot={(
          <ProposalsToolbar
            selected={selectedProposal}
            actionLoading={actionLoading}
            t={t}
            onEdit={() => selectedProposal && setIsFormOpen(true)}
            onSend={() => selectedProposal && navigate(`/comercial/propostas/${selectedProposal.id}`)}
            onMarkViewed={() => selectedProposal && void runProposalAction(() => proposalService.markAsViewed(selectedProposal.id))}
            onApprove={() => selectedProposal && void runProposalAction(() => proposalService.approve(selectedProposal.id))}
            onReject={() => selectedProposal && void runProposalAction(() => proposalService.reject(selectedProposal.id))}
            onCancel={() => selectedProposal && void runProposalAction(() => proposalService.cancel(selectedProposal.id))}
            onNew={() => { setSelectedProposal(null); setIsFormOpen(true) }}
          />
        )}
      >
        <TableToolbar
          searchValue={searchInput}
          onSearchChange={setSearchInput}
          searchPlaceholder={t('proposals.search.placeholder')}
          rightSlot={<FilterPanel sections={filterSections} onClearAll={clearFilters} />}
          className="mb-3"
        />
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
            navigate(`/comercial/propostas/${savedProposal.id}`, {
              state: {
                from: 'opportunity',
                opportunityId: savedProposal.opportunityId,
                opportunityName: savedProposal.opportunity?.name,
              },
            })
          }
        }}
      />
    </>
  )
}

interface ProposalsToolbarProps {
  selected: Proposal | null
  actionLoading: boolean
  t: (key: string) => string
  onEdit: () => void
  onSend: () => void
  onMarkViewed: () => void
  onApprove: () => void
  onReject: () => void
  onCancel: () => void
  onNew: () => void
}

function ProposalsToolbar({ selected, actionLoading, t, onEdit, onSend, onMarkViewed, onApprove, onReject, onCancel, onNew }: ProposalsToolbarProps) {
  const status = selected?.status
  const isDraft = status === ProposalStatus.Draft
  const isSent = status === ProposalStatus.Sent
  const isViewed = status === ProposalStatus.Viewed
  const isTerminal = status === ProposalStatus.Approved || status === ProposalStatus.Rejected || status === ProposalStatus.Converted || status === ProposalStatus.Cancelled

  const canSend = !!selected && !actionLoading && isDraft
  const canMarkViewed = !!selected && !actionLoading && isSent
  const canApprove = !!selected && !actionLoading && isViewed
  const canReject = !!selected && !actionLoading && (isSent || isViewed)
  const canCancel = !!selected && !actionLoading && !isTerminal
  const canEdit = !!selected && !actionLoading && !isTerminal

  return (
    <div className="flex flex-wrap items-center gap-2">
      <AuditUtilityBar entityName="Proposal" entityLabel={t('proposals.audit.entityLabel')} entityId={selected?.id ?? null} />

      {/* Desktop: controle segmentado */}
      <div className="hidden overflow-hidden rounded-md border border-border bg-card md:inline-flex">
        <SegmentedAction icon={<Send className="h-3.5 w-3.5" />} label={t('proposals.action.openToSend')} active={canSend} disabled={!canSend} onClick={onSend} />
        <SegmentedAction icon={<Eye className="h-3.5 w-3.5" />} label={t('proposals.action.markViewed')} active={canMarkViewed} disabled={!canMarkViewed} onClick={onMarkViewed} />
        <SegmentedAction icon={<CheckCircle className="h-3.5 w-3.5" />} label={t('proposals.action.approve')} active={canApprove} tone="emerald" disabled={!canApprove} onClick={onApprove} />
        <SegmentedAction icon={<XCircle className="h-3.5 w-3.5" />} label={t('proposals.action.reject')} tone="rose" disabled={!canReject} onClick={onReject} />
        <SegmentedAction icon={<Clock className="h-3.5 w-3.5" />} label={t('proposals.action.cancel')} tone="amber" disabled={!canCancel} onClick={onCancel} last />
      </div>

      <span aria-hidden className="hidden h-6 w-px bg-border md:inline-block" />

      <Button size="sm" variant="ghost" onClick={onEdit} disabled={!canEdit} className="hidden md:inline-flex">
        <Pencil className="mr-1.5 h-3.5 w-3.5" /> {t('common.action.edit')}
      </Button>

      {/* Mobile: acoes de status agrupadas num menu */}
      <Dropdown>
        <DropdownTrigger asChild>
          <Button size="sm" variant="outline" className="md:hidden">
            <MoreHorizontal className="mr-1.5 h-4 w-4" /> Ações
          </Button>
        </DropdownTrigger>
        <DropdownContent align="start" className="min-w-[13rem]">
          <DropdownItem disabled={!canSend} onSelect={onSend} className="gap-2">
            <Send className="h-4 w-4" /> {t('proposals.action.openToSend')}
          </DropdownItem>
          <DropdownItem disabled={!canMarkViewed} onSelect={onMarkViewed} className="gap-2">
            <Eye className="h-4 w-4" /> {t('proposals.action.markViewed')}
          </DropdownItem>
          <DropdownItem disabled={!canApprove} onSelect={onApprove} className="gap-2">
            <CheckCircle className="h-4 w-4" /> {t('proposals.action.approve')}
          </DropdownItem>
          <DropdownItem disabled={!canReject} onSelect={onReject} className="gap-2 text-destructive focus:text-destructive">
            <XCircle className="h-4 w-4" /> {t('proposals.action.reject')}
          </DropdownItem>
          <DropdownItem disabled={!canCancel} onSelect={onCancel} className="gap-2">
            <Clock className="h-4 w-4" /> {t('proposals.action.cancel')}
          </DropdownItem>
          <DropdownSeparator />
          <DropdownItem disabled={!canEdit} onSelect={onEdit} className="gap-2">
            <Pencil className="h-4 w-4" /> {t('common.action.edit')}
          </DropdownItem>
        </DropdownContent>
      </Dropdown>

      <Button size="sm" variant="secondary" onClick={onNew}>
        <Plus className="mr-1.5 h-4 w-4" /> {t('proposals.action.new')}
      </Button>
    </div>
  )
}

interface SegmentedActionProps {
  icon: React.ReactNode
  label: string
  active?: boolean
  disabled?: boolean
  tone?: 'emerald' | 'rose' | 'amber'
  last?: boolean
  onClick: () => void
}

function SegmentedAction({ icon, label, active, disabled, tone, last, onClick }: SegmentedActionProps) {
  const toneColor = tone === 'emerald' ? 'text-emerald-700' : tone === 'rose' ? 'text-rose-700' : tone === 'amber' ? 'text-amber-700' : 'text-foreground'
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={[
        'flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold transition-colors',
        last ? '' : 'border-r border-border',
        disabled
          ? 'cursor-not-allowed text-muted-foreground/60'
          : active
            ? 'bg-primary/12 text-primary'
            : `${toneColor} hover:bg-muted/60`,
      ].join(' ')}
    >
      {icon}
      <span>{label}</span>
    </button>
  )
}
