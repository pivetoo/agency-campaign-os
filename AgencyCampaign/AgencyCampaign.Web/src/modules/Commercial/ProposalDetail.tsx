import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  DataTable,
  PageLayout,
  SearchableSelect,
  useApi,
  useI18n,
} from 'archon-ui'
import type { DataTableColumn, PageAction } from 'archon-ui'
import { CalendarClock, CheckCircle, Eye, FileCheck, FileDown, Pencil, Send, Trash2, XCircle } from 'lucide-react'
import ProposalFormModal from '../../components/modals/ProposalFormModal'
import ProposalItemFormModal from '../../components/modals/ProposalItemFormModal'
import ApplyProposalTemplateModal from '../../components/modals/ApplyProposalTemplateModal'
import ProposalShareTab from './ProposalShareTab'
import { campaignService } from '../../services/campaignService'
import { proposalService, ProposalStatus, type Proposal, type ProposalItem, type ProposalStatusValue } from '../../services/proposalService'
import type { Campaign } from '../../types/campaign'

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

function formatCurrency(value: number) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
}

function formatDate(value?: string) {
  return value ? new Date(value).toLocaleDateString('pt-BR') : '-'
}

export default function CommercialProposalDetail() {
  const { t } = useI18n()
  const { id } = useParams()
  const navigate = useNavigate()
  const proposalId = Number(id)
  const [proposal, setProposal] = useState<Proposal | null>(null)
  const [items, setItems] = useState<ProposalItem[]>([])
  const [campaigns, setCampaigns] = useState<Campaign[]>([])
  const [selectedItem, setSelectedItem] = useState<ProposalItem | null>(null)
  const [isProposalFormOpen, setIsProposalFormOpen] = useState(false)
  const [isItemFormOpen, setIsItemFormOpen] = useState(false)
  const [isApplyTemplateOpen, setIsApplyTemplateOpen] = useState(false)
  const [campaignId, setCampaignId] = useState<string>('')

  const { execute: fetchProposal, loading } = useApi<Proposal | undefined>({ showErrorMessage: true })
  const { execute: fetchItems } = useApi<ProposalItem[]>({ showErrorMessage: true })
  const { execute: executeAction, loading: actionLoading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadProposal = async () => {
    const result = await fetchProposal(() => proposalService.getById(proposalId))
    if (result) setProposal(result)

    const proposalItems = await fetchItems(() => proposalService.getItems(proposalId))
    if (proposalItems) setItems(proposalItems)
  }

  useEffect(() => {
    if (!proposalId) return
    void loadProposal()
    void campaignService.getAll({ pageSize: 10 }).then((r) => setCampaigns(r.data ?? []))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [proposalId])

  const total = useMemo(() => items.reduce((sum, item) => sum + item.total, 0), [items])
  const campaignOptions = campaigns.map((campaign) => ({
    value: String(campaign.id),
    label: campaign.name,
  }))

  const runProposalAction = async (action: () => Promise<unknown>) => {
    const result = await executeAction(action)
    if (result !== null) await loadProposal()
  }

  const headerActions: PageAction[] = useMemo(() => {
    if (!proposal) return []
    const status = proposal.status

    const actions: PageAction[] = []

    if (status === ProposalStatus.Draft) {
      actions.push({
        key: 'send',
        label: t('proposals.action.send'),
        icon: <Send className="h-4 w-4" />,
        variant: 'outline-primary',
        disabled: actionLoading,
        onClick: () => void runProposalAction(() => proposalService.send(proposalId)),
      })
    }
    if (status === ProposalStatus.Sent) {
      actions.push({
        key: 'viewed',
        label: t('proposals.action.markViewedLong'),
        icon: <Eye className="h-4 w-4" />,
        variant: 'outline',
        disabled: actionLoading,
        onClick: () => void runProposalAction(() => proposalService.markAsViewed(proposalId)),
      })
    }
    if (status === ProposalStatus.Sent || status === ProposalStatus.Viewed) {
      actions.push({
        key: 'approve',
        label: t('proposals.action.approve'),
        icon: <CheckCircle className="h-4 w-4" />,
        variant: 'outline-success',
        disabled: actionLoading,
        onClick: () => void runProposalAction(() => proposalService.approve(proposalId)),
      })
      actions.push({
        key: 'reject',
        label: t('proposals.action.reject'),
        icon: <XCircle className="h-4 w-4" />,
        variant: 'outline-danger',
        disabled: actionLoading,
        onClick: () => void runProposalAction(() => proposalService.reject(proposalId)),
      })
    }
    if (status !== ProposalStatus.Converted && status !== ProposalStatus.Cancelled) {
      actions.push({
        key: 'cancel',
        label: t('proposals.action.cancel'),
        variant: 'outline-danger',
        disabled: actionLoading,
        onClick: () => void runProposalAction(() => proposalService.cancel(proposalId)),
      })
    }

    actions.push({
      key: 'pdf',
      label: t('proposals.action.downloadPdf'),
      icon: <FileDown className="h-4 w-4" />,
      variant: 'outline',
      onClick: () => proposalService.downloadPdf(proposalId),
    })

    return actions
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [proposal, actionLoading, proposalId])

  const columns: DataTableColumn<ProposalItem>[] = [
    { key: 'description', title: t('proposalDetail.item.field.item'), dataIndex: 'description' },
    { key: 'creator', title: t('nav.item.creators'), dataIndex: 'creator', render: (value?: ProposalItem['creator']) => value?.name || '-' },
    { key: 'quantity', title: t('proposalDetail.item.field.qty'), dataIndex: 'quantity' },
    { key: 'unitPrice', title: t('proposalDetail.item.field.unitPrice'), dataIndex: 'unitPrice', render: (value: number) => formatCurrency(value) },
    { key: 'deliveryDeadline', title: t('proposalDetail.item.field.deadline'), dataIndex: 'deliveryDeadline', render: (value?: string) => formatDate(value) },
    { key: 'total', title: t('proposalDetail.item.field.total'), dataIndex: 'total', render: (value: number) => formatCurrency(value) },
  ]

  if (!proposal && !loading) {
    return (
      <PageLayout title={t('proposalDetail.notFound')} showDefaultActions={false}>
        <Button variant="outline" onClick={() => navigate('/comercial/propostas')}>{t('proposalDetail.backToList')}</Button>
      </PageLayout>
    )
  }

  const isApproved = proposal?.status === ProposalStatus.Approved
  const subtitleParts: string[] = []
  if (proposal?.brand?.name) subtitleParts.push(proposal.brand.name)
  if (proposal?.opportunity?.name) subtitleParts.push(t('proposalDetail.linkedTo').replace('{0}', proposal.opportunity.name))
  const subtitle = subtitleParts.length > 0 ? subtitleParts.join(' · ') : t('proposalDetail.fallbackSubtitle')

  return (
    <PageLayout
      title={proposal?.name ?? t('proposalDetail.fallbackTitle')}
      subtitle={subtitle}
      onEdit={() => setIsProposalFormOpen(true)}
      selectedRowsCount={proposal ? 1 : 0}
      onRefresh={() => void loadProposal()}
      actions={headerActions}
    >
      {proposal && (
        <div className="space-y-6">
          <div className="flex flex-wrap items-center gap-4 rounded-md border border-border/70 bg-muted/20 px-4 py-3">
            <div className="flex items-center gap-2">
              <span className="text-xs uppercase tracking-wide text-muted-foreground">{t('common.field.status')}</span>
              <Badge variant={proposalStatusVariant[proposal.status] || 'default'}>
                {proposalStatusKeys[proposal.status] ? t(proposalStatusKeys[proposal.status]) : '-'}
              </Badge>
            </div>
            <span className="hidden text-border md:inline">·</span>
            <div className="flex items-center gap-2">
              <span className="text-xs uppercase tracking-wide text-muted-foreground">{t('proposalDetail.item.field.total')}</span>
              <strong className="text-base text-foreground">{formatCurrency(total || proposal.totalValue)}</strong>
            </div>
            <span className="hidden text-border md:inline">·</span>
            <div className="flex items-center gap-2">
              <CalendarClock className="h-3.5 w-3.5 text-muted-foreground" />
              <span className="text-xs uppercase tracking-wide text-muted-foreground">{t('common.field.validity')}</span>
              <span className="text-sm text-foreground">{formatDate(proposal.validityUntil)}</span>
            </div>
            {proposal.campaign?.name ? (
              <>
                <span className="hidden text-border md:inline">·</span>
                <div className="flex items-center gap-2">
                  <FileCheck className="h-3.5 w-3.5 text-emerald-600" />
                  <span className="text-xs uppercase tracking-wide text-muted-foreground">{t('proposals.field.campaign')}</span>
                  <span className="text-sm text-foreground">{proposal.campaign.name}</span>
                </div>
              </>
            ) : null}
          </div>

          <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
            <div className="space-y-6">
              <Card>
                <CardHeader className="border-b bg-muted/20 py-3">
                  <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                    <CardTitle className="text-sm">{t('proposalDetail.items.title')}</CardTitle>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        size="sm"
                        onClick={() => { setSelectedItem(null); setIsItemFormOpen(true) }}
                      >
                        {t('common.action.add')}
                      </Button>
                      <Button size="sm" variant="outline" onClick={() => setIsApplyTemplateOpen(true)}>
                        {t('proposalDetail.items.applyTemplate')}
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        icon={<Pencil className="h-3.5 w-3.5" />}
                        disabled={!selectedItem}
                        onClick={() => selectedItem && setIsItemFormOpen(true)}
                      >
                        {t('common.action.edit')}
                      </Button>
                      <Button
                        size="sm"
                        variant="outline-danger"
                        icon={<Trash2 className="h-3.5 w-3.5" />}
                        disabled={!selectedItem}
                        onClick={() => selectedItem && void runProposalAction(() => proposalService.deleteItem(selectedItem.id))}
                      >
                        {t('common.action.delete')}
                      </Button>
                    </div>
                  </div>
                </CardHeader>
                <CardContent className="p-4">
                  <DataTable
                    columns={columns}
                    data={items}
                    rowKey="id"
                    selectedRows={selectedItem ? [selectedItem] : []}
                    onSelectionChange={(rows) => setSelectedItem(rows[0] ?? null)}
                    emptyText={t('proposalDetail.items.empty')}
                    pageSize={10}
                    pageSizeOptions={[5, 10, 20, 50]}
                  />
                </CardContent>
              </Card>

              {isApproved ? (
                <Card>
                  <CardHeader className="border-b bg-muted/20 py-3">
                    <CardTitle className="flex items-center gap-2 text-sm">
                      <FileCheck className="h-4 w-4 text-emerald-600" />
                      {t('proposalDetail.convert.title')}
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="p-4">
                    <p className="mb-3 text-xs text-muted-foreground">
                      {t('proposalDetail.convert.description')}
                    </p>
                    <div className="flex flex-col gap-3 md:flex-row md:items-end">
                      <div className="w-full md:max-w-md">
                        <label className="mb-1 block text-xs font-medium text-muted-foreground">{t('proposals.field.campaign')}</label>
                        <SearchableSelect
                          value={campaignId}
                          onValueChange={setCampaignId}
                          options={campaignOptions}
                          placeholder={t('proposalDetail.convert.campaignPlaceholder')}
                          searchPlaceholder={t('proposalDetail.convert.campaignSearch')}
                          onSearch={async (term) => {
                            const r = await campaignService.getAll({ search: term, pageSize: 10 })
                            return (r.data ?? []).map((campaign) => ({ value: String(campaign.id), label: campaign.name }))
                          }}
                        />
                      </div>
                      <Button
                        disabled={!campaignId || actionLoading}
                        onClick={() => void runProposalAction(() => proposalService.convertToCampaign(proposalId, Number(campaignId)))}
                      >
                        <FileCheck className="mr-2 h-4 w-4" /> {t('proposalDetail.convert.button')}
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ) : null}
            </div>

            <aside className="min-w-0 space-y-4">
              <ProposalShareTab proposalId={proposalId} />
            </aside>
          </div>
        </div>
      )}

      <ProposalFormModal
        open={isProposalFormOpen}
        onOpenChange={setIsProposalFormOpen}
        proposal={proposal}
        onSuccess={() => {
          setIsProposalFormOpen(false)
          void loadProposal()
        }}
      />
      <ProposalItemFormModal
        open={isItemFormOpen}
        onOpenChange={setIsItemFormOpen}
        proposalId={proposalId}
        item={selectedItem}
        onSuccess={() => {
          setIsItemFormOpen(false)
          setSelectedItem(null)
          void loadProposal()
        }}
      />
      <ApplyProposalTemplateModal
        open={isApplyTemplateOpen}
        onOpenChange={setIsApplyTemplateOpen}
        proposalId={proposalId}
        onApplied={() => void loadProposal()}
      />
    </PageLayout>
  )
}
