import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, DataTable, Input, PageLayout, SearchableSelect, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn, PageAction } from 'archon-ui'
import { AlertTriangle, CalendarClock, CheckCircle, Eye, FileCheck, FileDown, Pencil, Percent, Send, ShieldCheck, Trash2, Wallet, XCircle } from 'lucide-react'
import ProposalFormModal from '../../../components/modals/ProposalFormModal'
import ProposalItemFormModal from '../../../components/modals/ProposalItemFormModal'
import ApplyProposalTemplateModal from '../../../components/modals/ApplyProposalTemplateModal'
import ProposalSendModal from '../../../components/modals/ProposalSendModal'
import OpportunityApprovalRequestFormModal from '../../../components/modals/OpportunityApprovalRequestFormModal'
import ProposalShareTab from './ProposalShareTab'
import { campaignService } from '../../../services/campaignService'
import { proposalService, ProposalStatus, type Proposal, type ProposalItem, type ProposalStatusValue } from '../../../services/proposalService'
import { OpportunityApprovalStatus, type OpportunityApprovalRequest } from '../../../services/opportunityService'
import type { PolicyEvaluation } from '../../../types/policyEvaluation'
import type { Campaign } from '../../../types/campaign'
import { formatDate } from '../../../lib/format'
import { formatCurrency } from '../../../lib/format'

const round2 = (value: number) => Math.round(value * 100) / 100

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
  const [isSendModalOpen, setIsSendModalOpen] = useState(false)
  const [isApprovalRequestOpen, setIsApprovalRequestOpen] = useState(false)
  const [publicLinkUrl, setPublicLinkUrl] = useState<string | undefined>(undefined)
  const [campaignId, setCampaignId] = useState<string>('')
  const [policyEvaluation, setPolicyEvaluation] = useState<PolicyEvaluation | null>(null)
  const [approvals, setApprovals] = useState<OpportunityApprovalRequest[]>([])
  const [discountPercentInput, setDiscountPercentInput] = useState('')
  const [discountValueInput, setDiscountValueInput] = useState('')

  const { execute: fetchProposal, loading } = useApi<Proposal | undefined>({ showErrorMessage: true })
  const { execute: fetchItems } = useApi<ProposalItem[]>({ showErrorMessage: true })
  const { execute: executeAction, loading: actionLoading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadProposal = async () => {
    const result = await fetchProposal(() => proposalService.getById(proposalId))
    if (result) setProposal(result)

    const proposalItems = await fetchItems(() => proposalService.getItems(proposalId))
    if (proposalItems) setItems(proposalItems)

    const loadedGross = proposalItems && proposalItems.length > 0
      ? proposalItems.reduce((sum, item) => sum + item.total, 0)
      : result?.totalValue ?? 0
    const loadedAmount = result?.discountAmount ?? null
    const appliedAmount = loadedAmount != null ? Math.min(loadedGross, Math.max(0, loadedAmount)) : null
    setDiscountValueInput(loadedAmount != null ? String(loadedAmount) : '')
    setDiscountPercentInput(appliedAmount != null && loadedGross > 0 ? String(round2((appliedAmount / loadedGross) * 100)) : '')

    const [evaluation, approvalList] = await Promise.all([
      proposalService.evaluatePolicy(proposalId).catch(() => null),
      proposalService.getApprovalRequests(proposalId).catch(() => []),
    ])
    setPolicyEvaluation(evaluation)
    setApprovals(approvalList)
  }

  const hasApprovedApproval = useMemo(() => approvals.some((a) => a.status === OpportunityApprovalStatus.Approved), [approvals])
  const hasOpenApproval = useMemo(() => approvals.some((a) => a.status === OpportunityApprovalStatus.Pending || a.status === OpportunityApprovalStatus.InReview || a.status === OpportunityApprovalStatus.ChangesRequested), [approvals])
  const needsApproval = !!policyEvaluation?.hasDeviations && !hasApprovedApproval

  useEffect(() => {
    if (!proposalId) return
    void loadProposal()
    void campaignService.getAll({ pageSize: 10 }).then((r) => setCampaigns(r.data ?? []))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [proposalId])

  const total = useMemo(() => items.reduce((sum, item) => sum + item.total, 0), [items])
  const gross = total || proposal?.totalValue || 0
  const discountAmount = proposal?.discountAmount ?? 0
  const discountValue = round2(Math.min(gross, Math.max(0, discountAmount)))
  const discountPercent = gross > 0 ? round2((discountValue / gross) * 100) : 0
  const netTotal = round2(gross - discountValue)
  const hasItems = gross > 0
  const canEditDiscount = hasItems && proposal != null && proposal.status !== ProposalStatus.Converted && proposal.status !== ProposalStatus.Cancelled
  const persistedDiscount = proposal?.discountAmount == null ? null : round2(Math.min(gross, Math.max(0, proposal.discountAmount)))
  const pendingDiscount = discountValueInput === '' ? null : round2(Math.min(gross, Math.max(0, Number(discountValueInput))))
  const discountDirty = canEditDiscount && pendingDiscount !== persistedDiscount

  const persistDiscountAmount = async (amount: number | null) => {
    if (!proposal) return
    await runProposalAction(() => proposalService.update(proposal.id, {
      id: proposal.id,
      opportunityId: proposal.opportunityId,
      description: proposal.description,
      validityUntil: proposal.validityUntil,
      notes: proposal.notes,
      proposalLayoutId: proposal.proposalLayoutId ?? null,
      paymentTermDays: proposal.paymentTermDays ?? null,
      discountAmount: amount,
    }))
  }

  const clampPercent = (value: number) => Math.min(100, Math.max(0, value))

  const handleDiscountPercentChange = (raw: string) => {
    setDiscountPercentInput(raw)
    if (raw === '') {
      setDiscountValueInput('')
      return
    }
    const percent = clampPercent(Number(raw))
    setDiscountValueInput(String(round2((gross * percent) / 100)))
  }

  const handleDiscountValueChange = (raw: string) => {
    setDiscountValueInput(raw)
    if (raw === '' || gross <= 0) {
      setDiscountPercentInput('')
      return
    }
    const value = Math.min(gross, Math.max(0, Number(raw)))
    setDiscountPercentInput(String(round2((value / gross) * 100)))
  }

  const commitDiscount = async () => {
    if (!canEditDiscount) return
    if (discountValueInput === '') {
      if (proposal?.discountAmount != null) await persistDiscountAmount(null)
      return
    }
    const amount = round2(Math.min(gross, Math.max(0, Number(discountValueInput))))
    if (amount === round2(Math.min(gross, Math.max(0, proposal?.discountAmount ?? 0)))) return
    await persistDiscountAmount(amount)
  }

  const handleDeleteItem = async () => {
    if (!selectedItem) return
    const projectedGross = round2(gross - selectedItem.total)
    const discountWillShrink = discountAmount > 0 && projectedGross < discountAmount
    const message = discountWillShrink
      ? t('proposalDetail.item.deleteDiscountWarning').replace('{0}', formatCurrency(Math.max(0, projectedGross)))
      : t('proposalDetail.item.deleteConfirm')
    if (!window.confirm(message)) return
    await runProposalAction(() => proposalService.deleteItem(selectedItem.id))
  }

  const campaignOptions = campaigns.map((campaign) => ({
    value: String(campaign.id),
    label: campaign.name,
  }))

  const openSendModal = async () => {
    setPublicLinkUrl(undefined)
    try {
      const links = await proposalService.getShareLinks(proposalId)
      let active = links.find((link) => link.isActive)
      if (!active) {
        const created = await proposalService.createShareLink(proposalId, {})
        active = created?.data ?? undefined
      }
      if (active) {
        setPublicLinkUrl(`${window.location.origin}/p/${active.token}`)
      }
    } catch {
      // sem link público não inviabiliza envio
    }
    setIsSendModalOpen(true)
  }

  const runProposalAction = async (action: () => Promise<unknown>) => {
    const result = await executeAction(action)
    if (result !== null) await loadProposal()
  }

  const headerActions: PageAction[] = useMemo(() => {
    if (!proposal) return []
    const status = proposal.status

    const actions: PageAction[] = []

    if ((status === ProposalStatus.Draft || status === ProposalStatus.Sent || status === ProposalStatus.Viewed) && !hasOpenApproval && !hasApprovedApproval) {
      actions.push({
        key: 'requestApproval',
        label: t('proposalDetail.approval.requestButton'),
        icon: <ShieldCheck className="h-4 w-4" />,
        variant: 'outline-primary',
        disabled: actionLoading,
        onClick: () => setIsApprovalRequestOpen(true),
      })
    }
    if (status === ProposalStatus.Draft || status === ProposalStatus.Sent || status === ProposalStatus.Viewed) {
      actions.push({
        key: 'send',
        label: t(status === ProposalStatus.Draft ? 'proposals.action.send' : 'proposals.action.resend'),
        icon: <Send className="h-4 w-4" />,
        variant: 'outline-primary',
        disabled: actionLoading,
        onClick: () => { void openSendModal() },
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
        disabled: actionLoading || needsApproval,
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
  }, [proposal, actionLoading, proposalId, needsApproval, hasOpenApproval, hasApprovedApproval])

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
  if (proposal?.id) subtitleParts.push(`${t('common.field.code')} ${proposal.id}`)
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
          {needsApproval && (
            <div className="flex items-start gap-3 rounded-lg border border-amber-300 bg-amber-50 px-4 py-3">
              <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
              <div className="flex-1 text-sm text-amber-900">
                <p className="font-semibold">{t('proposalDetail.approval.deviationTitle')}</p>
                <ul className="mt-1 space-y-0.5 text-[12.5px] text-amber-800">
                  {(policyEvaluation?.deviations ?? []).filter((d) => d.isViolation).map((d) => (
                    <li key={d.field}><strong>{d.field}</strong>: {d.requestedValue} ({d.policyValue} · {d.delta})</li>
                  ))}
                </ul>
                <p className="mt-1.5 text-[12.5px] text-amber-800">{hasOpenApproval ? t('proposalDetail.approval.pendingHint') : t('proposalDetail.approval.requiredHint')}</p>
              </div>
              {!hasOpenApproval && (
                <Button size="sm" variant="primary" onClick={() => setIsApprovalRequestOpen(true)} className="shrink-0">
                  <ShieldCheck className="mr-1.5 h-3.5 w-3.5" /> {t('proposalDetail.approval.requestButton')}
                </Button>
              )}
            </div>
          )}

          <div className="flex flex-wrap items-center gap-4 rounded-md border border-border/70 bg-muted/20 px-4 py-3">
            <div className="flex items-center gap-2">
              <span className="text-xs uppercase tracking-wide text-muted-foreground">{t('common.field.status')}</span>
              <Badge variant={proposalStatusVariant[proposal.status] || 'default'}>
                {proposalStatusKeys[proposal.status] ? t(proposalStatusKeys[proposal.status]) : '-'}
              </Badge>
            </div>
            <span className="hidden text-border md:inline">·</span>
            <div className="flex items-center gap-2">
              <span className="text-xs uppercase tracking-wide text-muted-foreground">{t('proposalDetail.values.gross')}</span>
              <span className="text-sm text-foreground">{formatCurrency(gross)}</span>
            </div>
            {discountValue > 0 ? (
              <>
                <span className="hidden text-border md:inline">·</span>
                <div className="flex items-center gap-2">
                  <Percent className="h-3.5 w-3.5 text-muted-foreground" />
                  <span className="text-xs uppercase tracking-wide text-muted-foreground">{t('modal.proposal.field.discount')}</span>
                  <span className="text-sm text-destructive">- {formatCurrency(discountValue)} ({discountPercent}%)</span>
                </div>
              </>
            ) : null}
            <span className="hidden text-border md:inline">·</span>
            <div className="flex items-center gap-2">
              <span className="text-xs uppercase tracking-wide text-muted-foreground">{t('proposalDetail.values.net')}</span>
              <strong className="text-base text-foreground">{formatCurrency(netTotal)}</strong>
            </div>
            <span className="hidden text-border md:inline">·</span>
            <div className="flex items-center gap-2">
              <CalendarClock className="h-3.5 w-3.5 text-muted-foreground" />
              <span className="text-xs uppercase tracking-wide text-muted-foreground">{t('common.field.validity')}</span>
              <span className="text-sm text-foreground">{formatDate(proposal.validityUntil)}</span>
            </div>
            {proposal.paymentTermDays != null ? (
              <>
                <span className="hidden text-border md:inline">·</span>
                <div className="flex items-center gap-2">
                  <Wallet className="h-3.5 w-3.5 text-muted-foreground" />
                  <span className="text-xs uppercase tracking-wide text-muted-foreground">{t('modal.proposal.field.paymentTerm')}</span>
                  <span className="text-sm text-foreground">{t('proposalDetail.paymentTermValue').replace('{0}', String(proposal.paymentTermDays))}</span>
                </div>
              </>
            ) : null}
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
                        variant="ghost"
                        icon={<Trash2 className="h-3.5 w-3.5" />}
                        disabled={!selectedItem}
                        onClick={() => void handleDeleteItem()}
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

              <Card>
                <CardHeader className="border-b bg-muted/20 py-3">
                  <CardTitle className="flex items-center gap-2 text-sm">
                    <Percent className="h-4 w-4 text-muted-foreground" />
                    {t('proposalDetail.discount.title')}
                  </CardTitle>
                </CardHeader>
                <CardContent className="p-4">
                  {!hasItems ? (
                    <div className="flex items-start gap-2 rounded-md border border-dashed border-border/70 bg-muted/20 px-3 py-2 text-sm text-muted-foreground">
                      <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-amber-500" />
                      {t('proposalDetail.discount.needsItems')}
                    </div>
                  ) : (
                    <div className="space-y-4">
                      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                        <div className="space-y-1.5">
                          <label className="text-xs font-medium text-muted-foreground">{t('proposalDetail.discount.fieldValue')}</label>
                          <Input
                            type="number"
                            min={0}
                            max={gross}
                            step="0.01"
                            value={discountValueInput}
                            disabled={!canEditDiscount || actionLoading}
                            onChange={(e) => handleDiscountValueChange(e.target.value)}
                            placeholder="0,00"
                          />
                        </div>
                        <div className="space-y-1.5">
                          <label className="text-xs font-medium text-muted-foreground">{t('proposalDetail.discount.fieldPercent')}</label>
                          <Input
                            type="number"
                            min={0}
                            max={100}
                            step="0.01"
                            value={discountPercentInput}
                            disabled={!canEditDiscount || actionLoading}
                            onChange={(e) => handleDiscountPercentChange(e.target.value)}
                            placeholder="0"
                          />
                        </div>
                      </div>
                      {canEditDiscount && (
                        <div className="flex items-center justify-between gap-2">
                          <span className={`text-xs font-medium ${discountDirty ? 'text-amber-600' : 'text-emerald-600'}`}>
                            {discountDirty ? t('proposalDetail.discount.unsaved') : t('proposalDetail.discount.saved')}
                          </span>
                          <Button type="button" size="sm" variant="outline" disabled={!discountDirty || actionLoading} onClick={() => void commitDiscount()}>
                            {actionLoading ? t('common.action.saving') : t('proposalDetail.discount.save')}
                          </Button>
                        </div>
                      )}
                      <div className="flex flex-col gap-1 rounded-md border border-border/70 bg-muted/20 px-4 py-3 text-sm">
                        <div className="flex items-center justify-between">
                          <span className="text-muted-foreground">{t('proposalDetail.values.gross')}</span>
                          <span className="text-foreground">{formatCurrency(gross)}</span>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-muted-foreground">{t('modal.proposal.field.discount')}</span>
                          <span className="text-destructive">- {formatCurrency(discountValue)}</span>
                        </div>
                        <div className="mt-1 flex items-center justify-between border-t border-border/70 pt-2">
                          <span className="font-medium text-muted-foreground">{t('proposalDetail.values.net')}</span>
                          <strong className="text-base text-foreground">{formatCurrency(netTotal)}</strong>
                        </div>
                      </div>
                    </div>
                  )}
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
                    <Button
                      disabled={actionLoading}
                      onClick={() => {
                        if (!window.confirm(t('proposalDetail.convert.confirmNewCampaign'))) return
                        void runProposalAction(() => proposalService.convertToNewCampaign(proposalId))
                      }}
                    >
                      <FileCheck className="mr-2 h-4 w-4" /> {t('proposalDetail.convert.newCampaignButton')}
                    </Button>
                    <div className="my-4 flex items-center gap-2 text-[11px] uppercase tracking-wider text-muted-foreground">
                      <span className="h-px flex-1 bg-border" /> {t('proposalDetail.convert.orLinkExisting')} <span className="h-px flex-1 bg-border" />
                    </div>
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
              <ProposalShareTab proposalId={proposalId} proposalStatus={proposal?.status ?? ProposalStatus.Draft} />
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
      {proposal ? (
        <ProposalSendModal
          open={isSendModalOpen}
          onOpenChange={setIsSendModalOpen}
          proposalId={proposalId}
          proposalName={proposal.name}
          agencyName={proposal.brand?.name}
          defaultRecipientEmail={proposal.opportunity?.contactEmail ?? proposal.brand?.contactEmail ?? undefined}
          defaultRecipientPhone={proposal.opportunity?.contactPhone ?? proposal.brand?.contactPhone ?? undefined}
          publicLinkUrl={publicLinkUrl}
          onSuccess={() => {
            setIsSendModalOpen(false)
            void loadProposal()
          }}
        />
      ) : null}

      <OpportunityApprovalRequestFormModal
        open={isApprovalRequestOpen}
        onOpenChange={setIsApprovalRequestOpen}
        proposal={proposal}
        onSuccess={() => {
          setIsApprovalRequestOpen(false)
          void loadProposal()
        }}
      />
    </PageLayout>
  )
}
