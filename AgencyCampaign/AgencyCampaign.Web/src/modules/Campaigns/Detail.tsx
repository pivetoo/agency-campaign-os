import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageLayout, Button, Card, CardContent, CardHeader, CardTitle, DataTable, useApi, Badge, Tabs, TabsList, TabsTrigger, TabsContent } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Mail, Pencil, Plus, Signature, Users, FileText, Package } from 'lucide-react'
import { campaignService } from '../../services/campaignService'
import { campaignCreatorService } from '../../services/campaignCreatorService'
import { campaignDeliverableService } from '../../services/campaignDeliverableService'
import { campaignDocumentService } from '../../services/campaignDocumentService'
import type { Campaign } from '../../types/campaign'
import type { CampaignCreator } from '../../types/campaignCreator'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'
import type { CampaignDocument } from '../../types/campaignDocument'
import CampaignCreatorFormModal from '../../components/modals/CampaignCreatorFormModal'
import CampaignDeliverableFormModal from '../../components/modals/CampaignDeliverableFormModal'
import CampaignDocumentFormModal from '../../components/modals/CampaignDocumentFormModal'
import CampaignDocumentEmailModal from '../../components/modals/CampaignDocumentEmailModal'

const campaignStatusLabels: Record<number, string> = {
  1: 'Rascunho',
  2: 'Planejada',
  3: 'Em execução',
  4: 'Em revisão',
  5: 'Concluída',
  6: 'Cancelada',
}

const deliverableStatusLabels: Record<number, string> = {
  1: 'Pendente',
  2: 'Em revisão',
  3: 'Aprovada',
  4: 'Publicada',
  5: 'Cancelada',
}

const campaignCreatorStatusLabels: Record<number, string> = {
  1: 'Convidado',
  2: 'Pendente aprovação',
  3: 'Confirmado',
  4: 'Em execução',
  5: 'Entregue',
  6: 'Cancelado',
}

const documentTypeLabels: Record<number, string> = {
  1: 'Aceite do creator',
  2: 'Contrato da marca',
  3: 'Termo de autorização',
  4: 'Anexo de briefing',
  5: 'Outro',
}

const documentStatusLabels: Record<number, string> = {
  1: 'Rascunho',
  2: 'Pronto para envio',
  3: 'Enviado',
  4: 'Visualizado',
  5: 'Assinado',
  6: 'Rejeitado',
  7: 'Cancelado',
}

export default function CampaignDetail() {
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
      title: 'Creator',
      dataIndex: 'creator',
      render: (value: CampaignCreator['creator']) => value?.stageName || value?.name || '-',
    },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      width: 140,
      render: (value: number) => {
        const variant = value === 3 ? 'success' : value === 5 ? 'success' : value === 6 ? 'destructive' : 'warning'
        return <Badge variant={variant}>{campaignCreatorStatusLabels[value] || '-'}</Badge>
      },
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
      title: 'Creator',
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
      render: (value: string) => new Date(value).toLocaleDateString('pt-BR'),
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
      title: 'Creator',
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
      key: 'documentType',
      title: 'Tipo',
      dataIndex: 'documentType',
      render: (value: number) => documentTypeLabels[value] || '-',
    },
    {
      key: 'campaignCreatorId',
      title: 'Creator',
      dataIndex: 'campaignCreatorId',
      render: (value?: number) => {
        const campaignCreator = campaignCreators.find((item) => item.id === value)
        return campaignCreator?.creator?.stageName || campaignCreator?.creator?.name || '-'
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
      width: 48,
      render: (_: any, record: CampaignDocument) => (
        <button
          className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
          onClick={() => { setSelectedDocument(record); setIsDocumentFormOpen(true) }}
        >
          <Pencil size={14} />
        </button>
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
          <CardContent className="grid gap-4 md:grid-cols-2 lg:grid-cols-4 pt-6">
            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Status</p>
              <div className="mt-1">
                <Badge className="px-2 py-0.5 text-xs" variant={campaign?.status === 5 ? 'success' : campaign?.status === 6 ? 'destructive' : 'warning'}>
                  {campaign ? campaignStatusLabels[campaign.status] : '-'}
                </Badge>
              </div>
            </div>

            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Budget</p>
              <p className="text-sm font-medium mt-1">R$ {(campaign?.budget ?? 0).toFixed(2)}</p>
            </div>

            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Creators</p>
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

            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Responsável</p>
              <p className="text-sm font-medium mt-1">{campaign?.commercialResponsible?.name || campaign?.internalOwnerName || '-'}</p>
            </div>

            <div className="lg:col-span-2">
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Objetivo</p>
              <p className="text-sm font-medium mt-1">{campaign?.objective || '-'}</p>
            </div>
          </CardContent>
        </Card>

        <Tabs defaultValue="creators" className="space-y-4">
          <TabsList className="mb-4">
            <TabsTrigger value="creators">
              <Users size={14} className="mr-1.5" />
              Creators
              {campaignCreators.length > 0 && (
                <span className="ml-1.5 text-[10px] bg-muted text-muted-foreground rounded-full px-1.5 py-0.5 font-medium">
                  {campaignCreators.length}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="documents">
              <FileText size={14} className="mr-1.5" />
              Documentos
              {documents.length > 0 && (
                <span className="ml-1.5 text-[10px] bg-muted text-muted-foreground rounded-full px-1.5 py-0.5 font-medium">
                  {documents.length}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="deliverables">
              <Package size={14} className="mr-1.5" />
              Entregas
              {deliverables.length > 0 && (
                <span className="ml-1.5 text-[10px] bg-muted text-muted-foreground rounded-full px-1.5 py-0.5 font-medium">
                  {deliverables.length}
                </span>
              )}
            </TabsTrigger>
          </TabsList>

          <TabsContent value="creators">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle>Creators da campanha</CardTitle>
                <Button size="sm" onClick={() => { setSelectedCampaignCreator(null); setIsCreatorFormOpen(true) }}>
                  <Plus size={16} className="mr-2" />
                  Adicionar creator
                </Button>
              </CardHeader>
              <CardContent>
                <DataTable
                  columns={campaignCreatorColumns}
                  data={campaignCreators}
                  rowKey="id"
                  selectedRows={selectedCampaignCreator ? [selectedCampaignCreator] : []}
                  onSelectionChange={(rows) => setSelectedCampaignCreator(rows[0] ?? null)}
                  emptyText="Nenhum creator vinculado à campanha"
                  loading={creatorsLoading}
                  pageSize={5}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="documents">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle>Documentos</CardTitle>
                <div className="flex gap-2">
                  <Button size="sm" variant="outline" onClick={() => void handleMarkDocumentAsSigned()} disabled={!selectedDocument || signingDocument}>
                    <Signature size={16} className="mr-2" />
                    Marcar assinado
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => setIsDocumentEmailOpen(true)} disabled={!selectedDocument}>
                    <Mail size={16} className="mr-2" />
                    Enviar e-mail
                  </Button>
                  <Button size="sm" onClick={() => { setSelectedDocument(null); setIsDocumentFormOpen(true) }}>
                    <Plus size={16} className="mr-2" />
                    Novo documento
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                <DataTable
                  columns={documentColumns}
                  data={documents}
                  rowKey="id"
                  selectedRows={selectedDocument ? [selectedDocument] : []}
                  onSelectionChange={(rows) => setSelectedDocument(rows[0] ?? null)}
                  emptyText="Nenhum documento cadastrado"
                  loading={documentsLoading}
                  pageSize={5}
                  pageSizeOptions={[5, 10, 20, 50]}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="deliverables">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle>Entregas</CardTitle>
                <Button size="sm" onClick={() => { setSelectedDeliverable(null); setIsDeliverableFormOpen(true) }}>
                  <Plus size={16} className="mr-2" />
                  Nova entrega
                </Button>
              </CardHeader>
              <CardContent>
                <DataTable
                  columns={deliverableColumns}
                  data={deliverables}
                  rowKey="id"
                  selectedRows={selectedDeliverable ? [selectedDeliverable] : []}
                  onSelectionChange={(rows) => setSelectedDeliverable(rows[0] ?? null)}
                  emptyText="Nenhuma entrega cadastrada"
                  loading={deliverablesLoading}
                  pageSize={5}
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
    </div>
  )
}
