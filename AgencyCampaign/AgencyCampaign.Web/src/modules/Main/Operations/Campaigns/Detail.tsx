import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageLayout, Button, Card, CardContent, CardHeader, CardTitle, DataTable, useApi, Badge, Tabs, TabsList, TabsTrigger, TabsContent, useI18n, useToast, usePermissions } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { ClipboardCheck, Eye, Pencil, Plus, Send, Signature, Sparkles, Users, FileText, Package, BarChart3, RefreshCw, ScrollText, TrendingUp, ClipboardList, CalendarDays, HandCoins } from 'lucide-react'
import { campaignService } from '../../../../services/campaignService'
import { campaignCreatorService } from '../../../../services/campaignCreatorService'
import { resolveCreatorPhotoUrl } from '../../../../services/creatorService'
import { campaignDeliverableService } from '../../../../services/campaignDeliverableService'
import { campaignDocumentService } from '../../../../services/campaignDocumentService'
import { campaignReportService } from '../../../../services/campaignReportService'
import { CampaignStatus } from '../../../../types/campaign'
import type { Campaign, CampaignStatusValue, CampaignSummary } from '../../../../types/campaign'
import type { CampaignCreator } from '../../../../types/campaignCreator'
import type { CampaignDeliverable } from '../../../../types/campaignDeliverable'
import type { CampaignDocument } from '../../../../types/campaignDocument'
import { CampaignDocumentStatus } from '../../../../types/campaignDocument'
import CampaignCreatorFormModal from '../../../../components/modals/CampaignCreatorFormModal'
import CreatorPaymentFormModal from '../../../../components/modals/CreatorPaymentFormModal'
import CampaignDeliverableFormModal from '../../../../components/modals/CampaignDeliverableFormModal'
import CampaignDocumentFormModal from '../../../../components/modals/CampaignDocumentFormModal'
import CampaignDocumentSendModal from '../../../../components/modals/CampaignDocumentSendModal'
import CampaignDocumentGenerateFromTemplateModal from '../../../../components/modals/CampaignDocumentGenerateFromTemplateModal'
import CampaignDocumentSendForSignatureModal from '../../../../components/modals/CampaignDocumentSendForSignatureModal'
import CampaignDocumentDetailsModal from '../../../../components/modals/CampaignDocumentDetailsModal'
import ContentReviewSheet from '../../../../components/sheets/ContentReviewSheet'
import DeliverableLicensesSheet from '../../../../components/sheets/DeliverableLicensesSheet'
import CampaignCreatorSalesSheet from '../../../../components/sheets/CampaignCreatorSalesSheet'
import CampaignBriefingTab from '../../../../components/CampaignBriefingTab'
import CampaignDeliverableCalendar from '../../../../components/CampaignDeliverableCalendar'
import { formatCurrency } from '../../../../lib/format'


function getContrastColor(hexColor: string): string {
  const hex = hexColor.replace('#', '')
  const r = parseInt(hex.substring(0, 2), 16)
  const g = parseInt(hex.substring(2, 4), 16)
  const b = parseInt(hex.substring(4, 6), 16)
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255
  return luminance > 0.5 ? '#111827' : '#ffffff'
}

export default function CampaignDetail() {
  const { t } = useI18n()
  const { toast } = useToast()
  const { hasPermission } = usePermissions()
  // M7: valores financeiros (orçamento, acordado, fee) só para quem tem acesso ao financeiro.
  const canSeeFinancials = hasPermission('financialEntries.get.description')
  const { id } = useParams<{ id: string }>()
  const campaignId = Number(id || 0)

  const [campaign, setCampaign] = useState<Campaign | null>(null)
  const [summary, setSummary] = useState<CampaignSummary | null>(null)
  const [activeTab, setActiveTab] = useState('creators')
  const [campaignCreators, setCampaignCreators] = useState<CampaignCreator[]>([])
  const [selectedCampaignCreator, setSelectedCampaignCreator] = useState<CampaignCreator | null>(null)
  const [deliverables, setDeliverables] = useState<CampaignDeliverable[]>([])
  const [selectedDeliverable, setSelectedDeliverable] = useState<CampaignDeliverable | null>(null)
  // M5: filtro de status nas listas de produção (entregáveis), para escalar com volume.
  const [deliverableStatusFilter, setDeliverableStatusFilter] = useState<number | 'all'>('all')
  const [documents, setDocuments] = useState<CampaignDocument[]>([])
  const [selectedDocument, setSelectedDocument] = useState<CampaignDocument | null>(null)
  const [isCreatorFormOpen, setIsCreatorFormOpen] = useState(false)
  const [payoutCreator, setPayoutCreator] = useState<CampaignCreator | null>(null)
  const [isDeliverableFormOpen, setIsDeliverableFormOpen] = useState(false)
  const [isDocumentFormOpen, setIsDocumentFormOpen] = useState(false)
  const [isDocumentEmailOpen, setIsDocumentEmailOpen] = useState(false)
  const [isDocumentGenerateOpen, setIsDocumentGenerateOpen] = useState(false)
  const [isDocumentSignatureOpen, setIsDocumentSignatureOpen] = useState(false)
  const [isDocumentDetailsOpen, setIsDocumentDetailsOpen] = useState(false)
  const [isContentReviewOpen, setIsContentReviewOpen] = useState(false)
  const [reviewDeliverableId, setReviewDeliverableId] = useState<number | null>(null)
  const [isLicensesOpen, setIsLicensesOpen] = useState(false)
  const [licensesDeliverableId, setLicensesDeliverableId] = useState<number | null>(null)
  const [isSalesOpen, setIsSalesOpen] = useState(false)
  const [salesCampaignCreator, setSalesCampaignCreator] = useState<CampaignCreator | null>(null)

  const campaignStatusLabels: Record<CampaignStatusValue, string> = {
    [CampaignStatus.Draft]: t('campaign.status.draft'),
    [CampaignStatus.Planned]: t('campaign.status.planned'),
    [CampaignStatus.InProgress]: t('campaign.status.executing'),
    [CampaignStatus.InReview]: t('campaign.status.reviewing'),
    [CampaignStatus.Completed]: t('campaign.status.completed'),
    [CampaignStatus.Cancelled]: t('campaign.status.cancelled'),
  }

  const deliverableStatusLabels: Record<number, string> = {
    1: t('deliverable.status.pending'),
    2: t('deliverable.status.reviewing'),
    3: t('deliverable.status.approved'),
    4: t('deliverable.status.published'),
    5: t('deliverable.status.cancelled'),
  }

  const documentTypeLabels: Record<number, string> = {
    1: t('campaignDocument.type.creatorAcceptance'),
    2: t('campaignDocument.type.brandContract'),
    3: t('campaignDocument.type.authorizationTerm'),
    4: t('campaignDocument.type.briefingAttachment'),
    5: t('campaignDocument.type.other'),
  }

  const documentStatusLabels: Record<number, string> = {
    1: t('campaignDocument.status.draft'),
    2: t('campaignDocument.status.readyToSend'),
    3: t('campaignDocument.status.sent'),
    4: t('campaignDocument.status.viewed'),
    5: t('campaignDocument.status.signed'),
    6: t('campaignDocument.status.rejected'),
    7: t('campaignDocument.status.cancelled'),
  }

  const { execute: fetchCampaign } = useApi<Campaign | null>({ showErrorMessage: true })
  const { execute: fetchSummary } = useApi<CampaignSummary | null>({ showErrorMessage: true })
  const { execute: fetchCampaignCreators, loading: creatorsLoading } = useApi<CampaignCreator[]>({ showErrorMessage: true })
  const { execute: fetchDeliverables, loading: deliverablesLoading } = useApi<CampaignDeliverable[]>({ showErrorMessage: true })
  const { execute: fetchDocuments, loading: documentsLoading } = useApi<CampaignDocument[]>({ showErrorMessage: true })
  const { execute: markSigned, loading: signingDocument } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: createReportLink, loading: reportLinkLoading } = useApi({ showErrorMessage: true })
  const { execute: syncMetrics, loading: syncingMetrics } = useApi({ showErrorMessage: true })

  const loadCampaign = async () => {
    const result = await fetchCampaign(() => campaignService.getById(campaignId))
    if (result) {
      setCampaign(result)
    }
  }

  const loadCampaignCreators = async () => {
    const result = await fetchCampaignCreators(() => campaignCreatorService.getByCampaign(campaignId))
    if (result) {
      setCampaignCreators(result)
    }
  }

  const loadDeliverables = async () => {
    const result = await fetchDeliverables(() => campaignDeliverableService.getByCampaign(campaignId))
    if (result) {
      setDeliverables(result)
    }
    void loadSummary()
  }

  const loadSummary = async () => {
    const result = await fetchSummary(() => campaignService.getSummary(campaignId))
    if (result) {
      setSummary(result)
    }
  }

  const loadDocuments = async () => {
    const result = await fetchDocuments(() => campaignDocumentService.getByCampaign(campaignId))
    if (result) {
      setDocuments(result)
    }
    void loadSummary()
  }

  useEffect(() => {
    if (!campaignId) {
      return
    }

    void loadCampaign()
    void loadCampaignCreators()
    void loadDeliverables()
    void loadDocuments()
    void loadSummary()
  }, [campaignId])

  const campaignCreatorColumns: DataTableColumn<CampaignCreator>[] = [
    {
      key: 'creator',
      title: t('creators.singular'),
      dataIndex: 'creator',
      render: (value: CampaignCreator['creator']) => {
        const label = value?.stageName || value?.name || '-'
        const url = resolveCreatorPhotoUrl(value?.photoUrl)
        const initial = (value?.stageName?.trim() || value?.name?.trim() || '?').charAt(0).toUpperCase()
        return (
          <div className="flex items-center gap-2">
            <div className="flex h-8 w-8 shrink-0 items-center justify-center overflow-hidden rounded-full border bg-muted/30">
              {url ? (
                <img src={url} alt={label} className="h-full w-full object-cover" />
              ) : (
                <span className="text-xs font-semibold text-muted-foreground">{initial}</span>
              )}
            </div>
            <span>{label}</span>
          </div>
        )
      },
    },
    {
      key: 'campaignCreatorStatus',
      title: t('common.field.status'),
      dataIndex: 'campaignCreatorStatus',
      width: 140,
      render: (value: CampaignCreator['campaignCreatorStatus']) => (
        <span
          className="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
          style={{ backgroundColor: value?.color ?? '#6b7280', color: getContrastColor(value?.color ?? '#6b7280') }}
        >
          {value?.name || '-'}
        </span>
      ),
    },
    {
      key: 'agreedAmount',
      title: t('campaign.detail.field.agreedAmount'),
      dataIndex: 'agreedAmount',
      render: (value: number) => formatCurrency(value),
    },
    {
      key: 'agencyFeeAmount',
      title: t('campaign.detail.field.agencyFee'),
      dataIndex: 'agencyFeeAmount',
      render: (_value: number, record: CampaignCreator) => `${formatCurrency(record.agencyFeeAmount)} (${record.agencyFeePercent.toFixed(2)}%)`,
    },
    {
      key: 'confirmedAt',
      title: t('campaign.detail.field.confirmedAt'),
      dataIndex: 'confirmedAt',
      render: (value?: string) => value ? new Date(value).toLocaleDateString('pt-BR') : '-',
    },
    {
      key: 'actions',
      title: '',
      width: 80,
      render: (_: any, record: CampaignCreator) => (
        <div className="flex items-center gap-1">
          <button
            className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
            onClick={() => { setSalesCampaignCreator(record); setIsSalesOpen(true) }}
            title={t('campaignCreatorSales.open')}
          >
            <TrendingUp size={14} />
          </button>
          <button
            className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
            onClick={() => setPayoutCreator(record)}
            title={t('campaign.detail.action.releasePayout')}
          >
            <HandCoins size={14} />
          </button>
          <button
            className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
            onClick={() => { setSelectedCampaignCreator(record); setIsCreatorFormOpen(true) }}
            title={t('common.action.edit')}
          >
            <Pencil size={14} />
          </button>
        </div>
      ),
    },
  ]

  const deliverableColumns: DataTableColumn<CampaignDeliverable>[] = [
    { key: 'title', title: t('common.field.deliverable'), dataIndex: 'title' },
    {
      key: 'campaignCreator',
      title: t('creators.singular'),
      dataIndex: 'campaignCreator',
      render: (value: CampaignDeliverable['campaignCreator']) => value?.stageName || value?.creatorName || '-',
    },
    {
      key: 'deliverableKind',
      title: t('common.field.type'),
      dataIndex: 'deliverableKind',
      render: (value: CampaignDeliverable['deliverableKind']) => value?.name || '-',
    },
    {
      key: 'platform',
      title: t('common.field.platform'),
      dataIndex: 'platform',
      render: (value: CampaignDeliverable['platform']) => value?.name || '-',
    },
    {
      key: 'status',
      title: t('common.field.status'),
      dataIndex: 'status',
      width: 120,
      render: (value: number) => {
        const variant = value === 3 ? 'success' : value === 4 ? 'success' : value === 5 ? 'destructive' : 'warning'
        return <Badge variant={variant}>{deliverableStatusLabels[value] || '-'}</Badge>
      },
    },
    {
      key: 'dueAt',
      title: t('common.field.dueDate'),
      dataIndex: 'dueAt',
      render: (value: string, record: CampaignDeliverable) => {
        const formatted = new Date(value).toLocaleDateString('pt-BR')
        const sla = record.slaStatus ?? 0
        const days = record.daysUntilDue ?? 0

        if (sla === 2) {
          return (
            <span className="inline-flex flex-col gap-0.5">
              <span>{formatted}</span>
              <span className="text-[10px] font-semibold text-destructive">{t('campaign.detail.deliverable.overdueDays').replace('{0}', String(Math.abs(days)))}</span>
            </span>
          )
        }
        if (sla === 1) {
          return (
            <span className="inline-flex flex-col gap-0.5">
              <span>{formatted}</span>
              <span className="text-[10px] font-semibold text-amber-600">{t('campaign.detail.deliverable.dueInDays').replace('{0}', String(days))}</span>
            </span>
          )
        }
        return formatted
      },
    },
    {
      key: 'grossAmount',
      title: t('campaign.detail.field.grossAmount'),
      dataIndex: 'grossAmount',
      render: (value: number) => formatCurrency(value),
    },
    {
      key: 'publishedUrl',
      title: t('common.field.url'),
      dataIndex: 'publishedUrl',
      render: (value?: string) =>
        value ? (
          <a href={value} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline text-xs truncate max-w-[120px] block">
            {value}
          </a>
        ) : '-',
    },
    {
      key: 'actions',
      title: '',
      width: 100,
      render: (_: any, record: CampaignDeliverable) => (
        <div className="flex items-center gap-1">
          <button
            className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
            onClick={() => { setReviewDeliverableId(record.id); setIsContentReviewOpen(true) }}
            title={t('contentReview.open')}
          >
            <ClipboardCheck size={14} />
          </button>
          <button
            className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
            onClick={() => { setLicensesDeliverableId(record.id); setIsLicensesOpen(true) }}
            title={t('contentLicense.open')}
          >
            <ScrollText size={14} />
          </button>
          <button
            className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
            onClick={() => { setSelectedDeliverable(record); setIsDeliverableFormOpen(true) }}
            title={t('common.action.edit')}
          >
            <Pencil size={14} />
          </button>
        </div>
      ),
    },
  ]

  const documentColumns: DataTableColumn<CampaignDocument>[] = [
    { key: 'title', title: t('common.field.document'), dataIndex: 'title' },
    {
      key: 'documentType',
      title: t('common.field.type'),
      dataIndex: 'documentType',
      render: (value: number) => documentTypeLabels[value] || '-',
    },
    {
      key: 'campaignCreatorId',
      title: t('creators.singular'),
      dataIndex: 'campaignCreatorId',
      render: (value?: number) => {
        const campaignCreator = campaignCreators.find((item) => item.id === value)
        return campaignCreator?.creator?.stageName || campaignCreator?.creator?.name || '-'
      },
    },
    {
      key: 'status',
      title: t('common.field.status'),
      dataIndex: 'status',
      width: 140,
      render: (value: number) => {
        const variant = value === 5 ? 'success' : value === 6 ? 'destructive' : value === 3 ? 'warning' : 'default'
        return <Badge variant={variant}>{documentStatusLabels[value] || '-'}</Badge>
      },
    },
    {
      key: 'sentAt',
      title: t('campaign.detail.field.sentAt'),
      dataIndex: 'sentAt',
      render: (value?: string) => value ? new Date(value).toLocaleDateString('pt-BR') : '-',
    },
    {
      key: 'signedAt',
      title: t('campaign.detail.field.signedAt'),
      dataIndex: 'signedAt',
      render: (value?: string) => value ? new Date(value).toLocaleDateString('pt-BR') : '-',
    },
    {
      key: 'actions',
      title: '',
      width: 96,
      render: (_: any, record: CampaignDocument) => (
        <div className="flex items-center gap-1">
          <button
            className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
            onClick={() => { setSelectedDocument(record); setIsDocumentDetailsOpen(true) }}
            title={t('common.action.viewDetails')}
          >
            <Eye size={14} />
          </button>
          <button
            className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
            onClick={() => { setSelectedDocument(record); setIsDocumentFormOpen(true) }}
            title={t('common.action.edit')}
          >
            <Pencil size={14} />
          </button>
        </div>
      ),
    },
  ]

  // So permite marcar como assinado um documento que de fato saiu para assinatura (pronto/enviado/visualizado).
  // Bloqueia rascunho, ja assinado, recusado e cancelado - evita criar "contrato assinado" que nunca foi enviado.
  const canMarkSigned = (document: CampaignDocument | null): boolean =>
    document !== null &&
    [CampaignDocumentStatus.ReadyToSend, CampaignDocumentStatus.Sent, CampaignDocumentStatus.Viewed].includes(document.status as never)

  const handleMarkDocumentAsSigned = async () => {
    if (!canMarkSigned(selectedDocument)) {
      return
    }

    const result = await markSigned(() => campaignDocumentService.markSigned(selectedDocument.id, {
      signedAt: new Date().toISOString(),
    }))

    if (result !== null) {
      await loadDocuments()
      await loadCampaignCreators()
    }
  }

  const handleBrandReport = async () => {
    const result = await createReportLink(() => campaignReportService.createOrGetLink(campaignId))
    if (result?.data) {
      const url = `${window.location.origin}/r/${result.data.token}`
      let copied = true
      try {
        await navigator.clipboard.writeText(url)
      } catch {
        copied = false
      }
      toast(copied
        ? { title: t('campaignReport.linkCopied').replace('{0}', url), variant: 'success' }
        : { title: `Nao foi possivel copiar. Link: ${url}`, variant: 'warning' })
    }
  }

  const handleSyncMetrics = async () => {
    const result = await syncMetrics(() => campaignDeliverableService.syncCampaignMetrics(campaignId))
    if (result?.data) {
      await loadDeliverables()
      toast({ title: t('campaignReport.metricsSynced').replace('{0}', String(result.data.synced)), variant: 'success' })
    }
  }

  return (
    <div className="space-y-4">
      <PageLayout
        title={campaign?.name || t('campaign.field.campaign')}
        subtitle={campaign ? `${t('common.field.code')} ${campaign.id}${campaign.brand?.name ? ` · ${campaign.brand.name}` : ''} · ${t('campaign.detail.operations')}` : t('campaign.detail.operations')}
        onRefresh={() => {
          void loadCampaign()
          void loadCampaignCreators()
          void loadDeliverables()
          void loadDocuments()
        }}
        showDefaultActions={false}
      >
        <Card>
          <CardContent className="grid gap-3 md:grid-cols-2 lg:grid-cols-4 pt-5 pb-5">
            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">{t('common.field.status')}</p>
              <div className="mt-1">
                <Badge className="px-2 py-0.5 text-xs" variant={campaign?.status === CampaignStatus.Completed ? 'success' : campaign?.status === CampaignStatus.Cancelled ? 'destructive' : 'warning'}>
                  {campaign ? campaignStatusLabels[campaign.status] : '-'}
                </Badge>
              </div>
            </div>

            {canSeeFinancials && (
              <div>
                <p className="text-xs text-muted-foreground uppercase tracking-wide">{t('campaign.field.budget')}</p>
                <p className="text-sm font-medium mt-1">{formatCurrency(campaign?.budget ?? 0)}</p>
              </div>
            )}

            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">{t('campaign.detail.creatorsTab')}</p>
              <p className="text-sm font-medium mt-1">{campaignCreators.length}</p>
            </div>

            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">{t('campaign.detail.deliverablesTab')}</p>
              <p className="text-sm font-medium mt-1">{deliverables.length}</p>
            </div>

            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">{t('common.field.period')}</p>
              <p className="text-sm font-medium mt-1">
                {campaign?.startsAt ? new Date(campaign.startsAt).toLocaleDateString('pt-BR') : '-'}
                {` ${t('common.label.to')} `}
                {campaign?.endsAt ? new Date(campaign.endsAt).toLocaleDateString('pt-BR') : '-'}
              </p>
            </div>

            <div className="lg:col-span-3">
              <p className="text-xs text-muted-foreground uppercase tracking-wide">{t('campaign.field.objective')}</p>
              <p className="text-sm font-medium mt-1">{campaign?.objective || '-'}</p>
            </div>
          </CardContent>
        </Card>

        {summary && (summary.overdueDeliverablesCount > 0 || summary.awaitingApprovalCount > 0 || summary.unsignedDocumentsCount > 0 || (canSeeFinancials && summary.budget > 0 && summary.remainingBudget < 0)) && (
          <Card className="mt-4">
            <CardContent className="flex flex-wrap items-center gap-2 py-3">
              <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{t('campaign.detail.nextSteps.title')}</span>
              {canSeeFinancials && summary.budget > 0 && summary.remainingBudget < 0 && (
                <span className="inline-flex items-center gap-1.5 rounded-full bg-destructive/15 px-3 py-1 text-xs font-medium text-destructive">
                  <TrendingUp size={13} />
                  {t('campaign.detail.nextSteps.budgetExceeded').replace('{0}', formatCurrency(Math.abs(summary.remainingBudget)))}
                </span>
              )}
              {summary.awaitingApprovalCount > 0 && (
                <button type="button" onClick={() => setActiveTab('deliverables')} className="inline-flex items-center gap-1.5 rounded-full bg-amber-500/15 px-3 py-1 text-xs font-medium text-amber-700 hover:bg-amber-500/25">
                  <ClipboardList size={13} />
                  {t('campaign.detail.nextSteps.awaitingApproval').replace('{0}', String(summary.awaitingApprovalCount))}
                </button>
              )}
              {summary.unsignedDocumentsCount > 0 && (
                <button type="button" onClick={() => setActiveTab('documents')} className="inline-flex items-center gap-1.5 rounded-full bg-blue-500/15 px-3 py-1 text-xs font-medium text-blue-700 hover:bg-blue-500/25">
                  <ScrollText size={13} />
                  {t('campaign.detail.nextSteps.unsignedDocuments').replace('{0}', String(summary.unsignedDocumentsCount))}
                </button>
              )}
              {summary.overdueDeliverablesCount > 0 && (
                <button type="button" onClick={() => setActiveTab('deliverables')} className="inline-flex items-center gap-1.5 rounded-full bg-destructive/15 px-3 py-1 text-xs font-medium text-destructive hover:bg-destructive/25">
                  <CalendarDays size={13} />
                  {t('campaign.detail.nextSteps.overdue').replace('{0}', String(summary.overdueDeliverablesCount))}
                </button>
              )}
            </CardContent>
          </Card>
        )}

        <Tabs value={activeTab} onValueChange={setActiveTab} className="pt-8">
          <TabsList className="mb-6 h-auto w-full justify-start gap-6 rounded-none border-b border-border bg-transparent p-0">
            <TabsTrigger value="creators" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <Users size={14} />
              {t('campaign.detail.creatorsTab')}
              {campaignCreators.length > 0 && (
                <span className="ml-0.5 text-[10px] bg-muted text-muted-foreground rounded-full px-1.5 py-0.5 font-medium group-data-[state=active]:bg-primary/15 group-data-[state=active]:text-primary">
                  {campaignCreators.length}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="documents" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <FileText size={14} />
              {t('campaign.detail.documentsTab')}
              {documents.length > 0 && (
                <span className="ml-0.5 text-[10px] bg-muted text-muted-foreground rounded-full px-1.5 py-0.5 font-medium group-data-[state=active]:bg-primary/15 group-data-[state=active]:text-primary">
                  {documents.length}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="deliverables" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <Package size={14} />
              {t('campaign.detail.deliverablesTab')}
              {deliverables.length > 0 && (
                <span className="ml-0.5 text-[10px] bg-muted text-muted-foreground rounded-full px-1.5 py-0.5 font-medium group-data-[state=active]:bg-primary/15 group-data-[state=active]:text-primary">
                  {deliverables.length}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="briefing" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <ClipboardList size={14} />
              {t('campaign.detail.briefingTab')}
            </TabsTrigger>
            <TabsTrigger value="calendar" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <CalendarDays size={14} />
              {t('campaign.detail.calendarTab')}
            </TabsTrigger>
          </TabsList>

          <TabsContent value="creators" className="mt-0">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between py-3">
                <CardTitle className="text-base">{t('campaign.detail.creatorsSection')}</CardTitle>
                <Button size="sm" data-testid="campaign-add-creator-button" onClick={() => { setSelectedCampaignCreator(null); setIsCreatorFormOpen(true) }}>
                  <Plus size={16} className="mr-2" />
                  {t('campaign.detail.addCreator')}
                </Button>
              </CardHeader>
              <CardContent className="pt-0">
                <DataTable
                  columns={canSeeFinancials ? campaignCreatorColumns : campaignCreatorColumns.filter((column) => column.key !== 'agreedAmount' && column.key !== 'agencyFeeAmount')}
                  data={campaignCreators}
                  rowKey="id"
                  selectedRows={selectedCampaignCreator ? [selectedCampaignCreator] : []}
                  onSelectionChange={(rows) => setSelectedCampaignCreator(rows[0] ?? null)}
                  emptyText={t('campaign.detail.noCreators')}
                  loading={creatorsLoading}
                  pageSize={10}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="documents" className="mt-0">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between py-3">
                <CardTitle className="text-base">{t('campaign.detail.documentsTab')}</CardTitle>
                <div className="flex flex-wrap gap-2">
                  <Button size="sm" variant="outline" onClick={() => setIsDocumentSignatureOpen(true)} disabled={!selectedDocument}>
                    <Send size={16} className="mr-2" />
                    {t('campaign.detail.action.sendForSignature')}
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => void handleMarkDocumentAsSigned()} disabled={!canMarkSigned(selectedDocument) || signingDocument}>
                    <Signature size={16} className="mr-2" />
                    {t('campaign.detail.action.markSigned')}
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => setIsDocumentEmailOpen(true)} disabled={!selectedDocument}>
                    <Send size={16} className="mr-2" />
                    {t('common.action.send')}
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => { setSelectedDocument(null); setIsDocumentGenerateOpen(true) }}>
                    <Sparkles size={16} className="mr-2" />
                    {t('campaign.detail.action.generateFromTemplate')}
                  </Button>
                  <Button size="sm" onClick={() => { setSelectedDocument(null); setIsDocumentFormOpen(true) }}>
                    <Plus size={16} className="mr-2" />
                    {t('campaign.detail.action.newDocument')}
                  </Button>
                </div>
              </CardHeader>
              <CardContent className="pt-0">
                <DataTable
                  columns={documentColumns}
                  data={documents}
                  rowKey="id"
                  selectedRows={selectedDocument ? [selectedDocument] : []}
                  onSelectionChange={(rows) => setSelectedDocument(rows[0] ?? null)}
                  emptyText={t('campaign.detail.empty.documents')}
                  loading={documentsLoading}
                  pageSize={10}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="deliverables" className="mt-0">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between py-3">
                <CardTitle className="text-base">{t('campaign.detail.deliverablesTab')}</CardTitle>
                <div className="flex flex-wrap items-center gap-2">
                  <select
                    className="rounded-md border bg-background px-2 py-1.5 text-sm"
                    value={String(deliverableStatusFilter)}
                    onChange={(e) => setDeliverableStatusFilter(e.target.value === 'all' ? 'all' : Number(e.target.value))}
                  >
                    <option value="all">{t('common.filter.allStatuses')}</option>
                    {Object.entries(deliverableStatusLabels).map(([value, label]) => (
                      <option key={value} value={value}>{label}</option>
                    ))}
                  </select>
                  <Button size="sm" variant="outline" onClick={() => void handleSyncMetrics()} disabled={syncingMetrics}>
                    <RefreshCw size={16} className="mr-2" />
                    {t('campaignReport.action.syncMetrics')}
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => void handleBrandReport()} disabled={reportLinkLoading}>
                    <BarChart3 size={16} className="mr-2" />
                    {t('campaignReport.action.generate')}
                  </Button>
                  <Button size="sm" onClick={() => { setSelectedDeliverable(null); setIsDeliverableFormOpen(true) }}>
                    <Plus size={16} className="mr-2" />
                    {t('campaign.detail.action.newDeliverable')}
                  </Button>
                </div>
              </CardHeader>
              <CardContent className="pt-0">
                <DataTable
                  columns={deliverableColumns}
                  data={deliverableStatusFilter === 'all' ? deliverables : deliverables.filter((item) => item.status === deliverableStatusFilter)}
                  rowKey="id"
                  selectedRows={selectedDeliverable ? [selectedDeliverable] : []}
                  onSelectionChange={(rows) => setSelectedDeliverable(rows[0] ?? null)}
                  emptyText={t('campaign.detail.empty.deliverables')}
                  loading={deliverablesLoading}
                  pageSize={10}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="briefing" className="mt-0">
            <CampaignBriefingTab campaignId={campaignId} />
          </TabsContent>

          <TabsContent value="calendar" className="mt-0">
            <Card>
              <CardContent className="pt-5 pb-5">
                <CampaignDeliverableCalendar
                  deliverables={deliverables}
                  onSelectDeliverable={(deliverable) => { setSelectedDeliverable(deliverable); setIsDeliverableFormOpen(true) }}
                />
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </PageLayout>

      <CampaignCreatorFormModal
        open={isCreatorFormOpen}
        onOpenChange={setIsCreatorFormOpen}
        campaignId={campaignId}
        campaignCreator={selectedCampaignCreator}
        onSuccess={() => {
          setIsCreatorFormOpen(false)
          setSelectedCampaignCreator(null)
          void loadCampaignCreators()
        }}
      />

      <CampaignDeliverableFormModal
        open={isDeliverableFormOpen}
        onOpenChange={setIsDeliverableFormOpen}
        campaignId={campaignId}
        deliverable={selectedDeliverable}
        onSuccess={() => {
          setIsDeliverableFormOpen(false)
          setSelectedDeliverable(null)
          void loadDeliverables()
        }}
      />

      <CampaignDocumentFormModal
        open={isDocumentFormOpen}
        onOpenChange={setIsDocumentFormOpen}
        campaignId={campaignId}
        document={selectedDocument}
        campaignCreators={campaignCreators}
        onSuccess={() => {
          setIsDocumentFormOpen(false)
          setSelectedDocument(null)
          void loadDocuments()
        }}
      />

      <CampaignDocumentSendModal
        open={isDocumentEmailOpen}
        onOpenChange={setIsDocumentEmailOpen}
        document={selectedDocument}
        onSuccess={() => {
          setIsDocumentEmailOpen(false)
          void loadDocuments()
        }}
      />

      <CampaignDocumentGenerateFromTemplateModal
        open={isDocumentGenerateOpen}
        onOpenChange={setIsDocumentGenerateOpen}
        campaignId={campaignId}
        campaignCreators={campaignCreators}
        onSuccess={() => {
          setIsDocumentGenerateOpen(false)
          void loadDocuments()
        }}
      />

      <CampaignDocumentSendForSignatureModal
        open={isDocumentSignatureOpen}
        onOpenChange={setIsDocumentSignatureOpen}
        document={selectedDocument}
        onSuccess={() => {
          setIsDocumentSignatureOpen(false)
          void loadDocuments()
        }}
      />

      <CampaignDocumentDetailsModal
        open={isDocumentDetailsOpen}
        onOpenChange={setIsDocumentDetailsOpen}
        documentId={selectedDocument?.id ?? null}
      />

      <ContentReviewSheet
        open={isContentReviewOpen}
        onOpenChange={setIsContentReviewOpen}
        deliverableId={reviewDeliverableId}
        onChanged={() => void loadDeliverables()}
      />

      <CreatorPaymentFormModal
        open={payoutCreator !== null}
        onOpenChange={(open) => { if (!open) setPayoutCreator(null) }}
        payment={null}
        campaignId={campaignId}
        presetCampaignCreatorId={payoutCreator?.id}
        presetGrossAmount={payoutCreator?.agreedAmount}
        onSuccess={() => { setPayoutCreator(null); void loadSummary() }}
      />

      <DeliverableLicensesSheet
        open={isLicensesOpen}
        onOpenChange={setIsLicensesOpen}
        deliverableId={licensesDeliverableId}
        campaignId={campaignId}
      />

      <CampaignCreatorSalesSheet
        open={isSalesOpen}
        onOpenChange={setIsSalesOpen}
        campaignCreator={salesCampaignCreator}
        onSuccess={() => {
          setIsSalesOpen(false)
          setSalesCampaignCreator(null)
          void loadCampaignCreators()
        }}
      />
    </div>
  )
}
