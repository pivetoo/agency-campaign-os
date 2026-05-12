import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageLayout, Button, Card, CardContent, CardHeader, CardTitle, DataTable, useApi, Badge, Tabs, TabsList, TabsTrigger, TabsContent, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Eye, Mail, Pencil, Plus, Send, Signature, Sparkles, Users, FileText, Package } from 'lucide-react'
import { campaignService } from '../../services/campaignService'
import { campaignCreatorService } from '../../services/campaignCreatorService'
import { campaignDeliverableService } from '../../services/campaignDeliverableService'
import { campaignDocumentService } from '../../services/campaignDocumentService'
import { CampaignStatus } from '../../types/campaign'
import type { Campaign, CampaignStatusValue } from '../../types/campaign'
import type { CampaignCreator } from '../../types/campaignCreator'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'
import type { CampaignDocument } from '../../types/campaignDocument'
import CampaignCreatorFormModal from '../../components/modals/CampaignCreatorFormModal'
import CampaignDeliverableFormModal from '../../components/modals/CampaignDeliverableFormModal'
import CampaignDocumentFormModal from '../../components/modals/CampaignDocumentFormModal'
import CampaignDocumentEmailModal from '../../components/modals/CampaignDocumentEmailModal'
import CampaignDocumentGenerateFromTemplateModal from '../../components/modals/CampaignDocumentGenerateFromTemplateModal'
import CampaignDocumentSendForSignatureModal from '../../components/modals/CampaignDocumentSendForSignatureModal'
import CampaignDocumentDetailsModal from '../../components/modals/CampaignDocumentDetailsModal'


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
  const { id } = useParams<{ id: string }>()
  const campaignId = Number(id || 0)

  const [campaign, setCampaign] = useState<Campaign | null>(null)
  const [campaignCreators, setCampaignCreators] = useState<CampaignCreator[]>([])
  const [selectedCampaignCreator, setSelectedCampaignCreator] = useState<CampaignCreator | null>(null)
  const [deliverables, setDeliverables] = useState<CampaignDeliverable[]>([])
  const [selectedDeliverable, setSelectedDeliverable] = useState<CampaignDeliverable | null>(null)
  const [documents, setDocuments] = useState<CampaignDocument[]>([])
  const [selectedDocument, setSelectedDocument] = useState<CampaignDocument | null>(null)
  const [isCreatorFormOpen, setIsCreatorFormOpen] = useState(false)
  const [isDeliverableFormOpen, setIsDeliverableFormOpen] = useState(false)
  const [isDocumentFormOpen, setIsDocumentFormOpen] = useState(false)
  const [isDocumentEmailOpen, setIsDocumentEmailOpen] = useState(false)
  const [isDocumentGenerateOpen, setIsDocumentGenerateOpen] = useState(false)
  const [isDocumentSignatureOpen, setIsDocumentSignatureOpen] = useState(false)
  const [isDocumentDetailsOpen, setIsDocumentDetailsOpen] = useState(false)

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
  const { execute: fetchCampaignCreators, loading: creatorsLoading } = useApi<CampaignCreator[]>({ showErrorMessage: true })
  const { execute: fetchDeliverables, loading: deliverablesLoading } = useApi<CampaignDeliverable[]>({ showErrorMessage: true })
  const { execute: fetchDocuments, loading: documentsLoading } = useApi<CampaignDocument[]>({ showErrorMessage: true })
  const { execute: markSigned, loading: signingDocument } = useApi({ showSuccessMessage: true, showErrorMessage: true })

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
  }

  const loadDocuments = async () => {
    const result = await fetchDocuments(() => campaignDocumentService.getByCampaign(campaignId))
    if (result) {
      setDocuments(result)
    }
  }

  useEffect(() => {
    if (!campaignId) {
      return
    }

    void loadCampaign()
    void loadCampaignCreators()
    void loadDeliverables()
    void loadDocuments()
  }, [campaignId])

  const campaignCreatorColumns: DataTableColumn<CampaignCreator>[] = [
    {
      key: 'creator',
      title: t('creators.singular'),
      dataIndex: 'creator',
      render: (value: CampaignCreator['creator']) => value?.stageName || value?.name || '-',
    },
    {
      key: 'campaignCreatorStatus',
      title: 'Status',
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
      title: 'Valor combinado',
      dataIndex: 'agreedAmount',
      render: (value: number) => `R$ ${value.toFixed(2)}`,
    },
    {
      key: 'agencyFeeAmount',
      title: 'Fee agência',
      dataIndex: 'agencyFeeAmount',
      render: (_value: number, record: CampaignCreator) => `R$ ${record.agencyFeeAmount.toFixed(2)} (${record.agencyFeePercent.toFixed(2)}%)`,
    },
    {
      key: 'confirmedAt',
      title: 'Confirmado em',
      dataIndex: 'confirmedAt',
      render: (value?: string) => value ? new Date(value).toLocaleDateString('pt-BR') : '-',
    },
    {
      key: 'actions',
      title: '',
      width: 48,
      render: (_: any, record: CampaignCreator) => (
        <button
          className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
          onClick={() => { setSelectedCampaignCreator(record); setIsCreatorFormOpen(true) }}
        >
          <Pencil size={14} />
        </button>
      ),
    },
  ]

  const deliverableColumns: DataTableColumn<CampaignDeliverable>[] = [
    { key: 'title', title: 'Entrega', dataIndex: 'title' },
    {
      key: 'campaignCreator',
      title: t('creators.singular'),
      dataIndex: 'campaignCreator',
      render: (value: CampaignDeliverable['campaignCreator']) => value?.stageName || value?.creatorName || '-',
    },
    {
      key: 'deliverableKind',
      title: 'Tipo',
      dataIndex: 'deliverableKind',
      render: (value: CampaignDeliverable['deliverableKind']) => value?.name || '-',
    },
    {
      key: 'platform',
      title: 'Plataforma',
      dataIndex: 'platform',
      render: (value: CampaignDeliverable['platform']) => value?.name || '-',
    },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      width: 120,
      render: (value: number) => {
        const variant = value === 3 ? 'success' : value === 4 ? 'success' : value === 5 ? 'destructive' : 'warning'
        return <Badge variant={variant}>{deliverableStatusLabels[value] || '-'}</Badge>
      },
    },
    {
      key: 'dueAt',
      title: 'Prazo',
      dataIndex: 'dueAt',
      render: (value: string, record: CampaignDeliverable) => {
        const formatted = new Date(value).toLocaleDateString('pt-BR')
        const sla = record.slaStatus ?? 0
        const days = record.daysUntilDue ?? 0

        if (sla === 2) {
          return (
            <span className="inline-flex flex-col gap-0.5">
              <span>{formatted}</span>
              <span className="text-[10px] font-semibold text-destructive">Atrasado {Math.abs(days)}d</span>
            </span>
          )
        }
        if (sla === 1) {
          return (
            <span className="inline-flex flex-col gap-0.5">
              <span>{formatted}</span>
              <span className="text-[10px] font-semibold text-amber-600">Em {days}d</span>
            </span>
          )
        }
        return formatted
      },
    },
    {
      key: 'grossAmount',
      title: 'Valor bruto',
      dataIndex: 'grossAmount',
      render: (value: number) => `R$ ${value.toFixed(2)}`,
    },
    {
      key: 'publishedUrl',
      title: 'URL',
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
      width: 48,
      render: (_: any, record: CampaignDeliverable) => (
        <button
          className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
          onClick={() => { setSelectedDeliverable(record); setIsDeliverableFormOpen(true) }}
        >
          <Pencil size={14} />
        </button>
      ),
    },
  ]

  const documentColumns: DataTableColumn<CampaignDocument>[] = [
    { key: 'title', title: 'Documento', dataIndex: 'title' },
    {
      key: 'documentType',
      title: 'Tipo',
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
      title: 'Status',
      dataIndex: 'status',
      width: 140,
      render: (value: number) => {
        const variant = value === 5 ? 'success' : value === 6 ? 'destructive' : value === 3 ? 'warning' : 'default'
        return <Badge variant={variant}>{documentStatusLabels[value] || '-'}</Badge>
      },
    },
    {
      key: 'sentAt',
      title: 'Enviado em',
      dataIndex: 'sentAt',
      render: (value?: string) => value ? new Date(value).toLocaleDateString('pt-BR') : '-',
    },
    {
      key: 'signedAt',
      title: 'Assinado em',
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
            title="Ver detalhes"
          >
            <Eye size={14} />
          </button>
          <button
            className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
            onClick={() => { setSelectedDocument(record); setIsDocumentFormOpen(true) }}
            title="Editar"
          >
            <Pencil size={14} />
          </button>
        </div>
      ),
    },
  ]

  const handleMarkDocumentAsSigned = async () => {
    if (!selectedDocument) {
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

  return (
    <div className="space-y-4">
      <PageLayout
        title={campaign?.name || 'Campanha'}
        subtitle={campaign?.brand?.name ? `${campaign.brand.name} · operação da campanha` : 'Operação da campanha'}
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
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Status</p>
              <div className="mt-1">
                <Badge className="px-2 py-0.5 text-xs" variant={campaign?.status === CampaignStatus.Completed ? 'success' : campaign?.status === CampaignStatus.Cancelled ? 'destructive' : 'warning'}>
                  {campaign ? campaignStatusLabels[campaign.status] : '-'}
                </Badge>
              </div>
            </div>

            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Budget</p>
              <p className="text-sm font-medium mt-1">R$ {(campaign?.budget ?? 0).toFixed(2)}</p>
            </div>

            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">{t('campaign.detail.creatorsTab')}</p>
              <p className="text-sm font-medium mt-1">{campaignCreators.length}</p>
            </div>

            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Entregas</p>
              <p className="text-sm font-medium mt-1">{deliverables.length}</p>
            </div>

            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Período</p>
              <p className="text-sm font-medium mt-1">
                {campaign?.startsAt ? new Date(campaign.startsAt).toLocaleDateString('pt-BR') : '-'}
                {' até '}
                {campaign?.endsAt ? new Date(campaign.endsAt).toLocaleDateString('pt-BR') : '-'}
              </p>
            </div>

            <div className="lg:col-span-3">
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Objetivo</p>
              <p className="text-sm font-medium mt-1">{campaign?.objective || '-'}</p>
            </div>
          </CardContent>
        </Card>

        <Tabs defaultValue="creators" className="pt-2">
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
          </TabsList>

          <TabsContent value="creators" className="mt-0">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between py-3">
                <CardTitle className="text-base">{t('campaign.detail.creatorsSection')}</CardTitle>
                <Button size="sm" onClick={() => { setSelectedCampaignCreator(null); setIsCreatorFormOpen(true) }}>
                  <Plus size={16} className="mr-2" />
                  {t('campaign.detail.addCreator')}
                </Button>
              </CardHeader>
              <CardContent className="pt-0">
                <DataTable
                  columns={campaignCreatorColumns}
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
                <CardTitle className="text-base">Documentos</CardTitle>
                <div className="flex flex-wrap gap-2">
                  <Button size="sm" variant="outline" onClick={() => setIsDocumentSignatureOpen(true)} disabled={!selectedDocument}>
                    <Send size={16} className="mr-2" />
                    Enviar para assinatura
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => void handleMarkDocumentAsSigned()} disabled={!selectedDocument || signingDocument}>
                    <Signature size={16} className="mr-2" />
                    Marcar assinado
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => setIsDocumentEmailOpen(true)} disabled={!selectedDocument}>
                    <Mail size={16} className="mr-2" />
                    Enviar e-mail
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => { setSelectedDocument(null); setIsDocumentGenerateOpen(true) }}>
                    <Sparkles size={16} className="mr-2" />
                    Gerar de template
                  </Button>
                  <Button size="sm" onClick={() => { setSelectedDocument(null); setIsDocumentFormOpen(true) }}>
                    <Plus size={16} className="mr-2" />
                    Novo documento
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
                  emptyText="Nenhum documento cadastrado"
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
                <CardTitle className="text-base">Entregas</CardTitle>
                <Button size="sm" onClick={() => { setSelectedDeliverable(null); setIsDeliverableFormOpen(true) }}>
                  <Plus size={16} className="mr-2" />
                  Nova entrega
                </Button>
              </CardHeader>
              <CardContent className="pt-0">
                <DataTable
                  columns={deliverableColumns}
                  data={deliverables}
                  rowKey="id"
                  selectedRows={selectedDeliverable ? [selectedDeliverable] : []}
                  onSelectionChange={(rows) => setSelectedDeliverable(rows[0] ?? null)}
                  emptyText="Nenhuma entrega cadastrada"
                  loading={deliverablesLoading}
                  pageSize={10}
                  pageSizeOptions={[5, 10, 20, 50]}
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

      <CampaignDocumentEmailModal
        open={isDocumentEmailOpen}
        onOpenChange={setIsDocumentEmailOpen}
        document={selectedDocument}
        campaignCreators={campaignCreators}
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
    </div>
  )
}
