import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  PageLayout,
  Card,
  CardContent,
  Badge,
  DataTable,
  Tabs,
  TabsList,
  TabsTrigger,
  TabsContent,
  Button,
  useApi,
  useI18n,
} from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { ExternalLink, Plus, Pencil, Trash2, Users, Activity, Megaphone } from 'lucide-react'
import { creatorService, resolveCreatorPhotoUrl } from '../../services/creatorService'
import { creatorSocialHandleService } from '../../services/creatorSocialHandleService'
import type { Creator } from '../../types/creator'
import type {
  CreatorCampaignEntry,
  CreatorSocialHandle,
  CreatorSummary,
} from '../../types/creatorSocialHandle'
import CreatorSocialHandleFormModal from '../../components/modals/CreatorSocialHandleFormModal'

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function formatNumber(value?: number | null): string {
  if (value == null) return '-'
  return value.toLocaleString('pt-BR')
}

function formatPercent(value?: number | null): string {
  if (value == null) return '-'
  return `${value.toFixed(2)}%`
}

export default function CreatorDetail() {
  const { t } = useI18n()
  const { id } = useParams<{ id: string }>()
  const creatorId = Number(id || 0)
  const navigate = useNavigate()

  const [creator, setCreator] = useState<Creator | null>(null)
  const [summary, setSummary] = useState<CreatorSummary | null>(null)
  const [handles, setHandles] = useState<CreatorSocialHandle[]>([])
  const [campaigns, setCampaigns] = useState<CreatorCampaignEntry[]>([])
  const [selectedHandle, setSelectedHandle] = useState<CreatorSocialHandle | null>(null)
  const [isHandleFormOpen, setIsHandleFormOpen] = useState(false)

  const { execute: fetchCreator } = useApi<Creator | null>({ showErrorMessage: true })
  const { execute: fetchSummary } = useApi<CreatorSummary | null>({ showErrorMessage: true })
  const { execute: fetchHandles, loading: handlesLoading } = useApi<CreatorSocialHandle[]>({ showErrorMessage: true })
  const { execute: fetchCampaigns, loading: campaignsLoading } = useApi<CreatorCampaignEntry[]>({ showErrorMessage: true })
  const { execute: runDelete } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const loadCreator = async () => {
    const result = await fetchCreator(() => creatorService.getById(creatorId))
    if (result) setCreator(result)
  }

  const loadSummary = async () => {
    const result = await fetchSummary(() => creatorService.getSummary(creatorId))
    if (result) setSummary(result)
  }

  const loadHandles = async () => {
    const result = await fetchHandles(() => creatorSocialHandleService.getByCreator(creatorId))
    if (result) setHandles(result)
  }

  const loadCampaigns = async () => {
    const result = await fetchCampaigns(() => creatorService.getCampaigns(creatorId))
    if (result) setCampaigns(result)
  }

  useEffect(() => {
    if (!creatorId) return
    void loadCreator()
    void loadSummary()
    void loadHandles()
    void loadCampaigns()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [creatorId])

  const handleDeleteHandle = async (handle: CreatorSocialHandle) => {
    if (!window.confirm(`Excluir o handle ${handle.handle}?`)) return
    const result = await runDelete(() => creatorSocialHandleService.delete(handle.id))
    if (result !== null) {
      void loadHandles()
      if (selectedHandle?.id === handle.id) setSelectedHandle(null)
    }
  }

  const handleColumns: DataTableColumn<CreatorSocialHandle>[] = [
    {
      key: 'platformName',
      title: 'Plataforma',
      dataIndex: 'platformName',
      render: (value: string, record) => (
        <span className="inline-flex items-center gap-2">
          <span className="font-medium">{value}</span>
          {record.isPrimary && <Badge variant="success">Principal</Badge>}
        </span>
      ),
    },
    { key: 'handle', title: 'Handle', dataIndex: 'handle' },
    { key: 'followers', title: 'Seguidores', dataIndex: 'followers', render: (value?: number | null) => formatNumber(value) },
    { key: 'engagementRate', title: 'Engajamento', dataIndex: 'engagementRate', render: (value?: number | null) => formatPercent(value) },
    {
      key: 'profileUrl',
      title: 'Link',
      dataIndex: 'profileUrl',
      render: (value?: string) =>
        value ? (
          <a href={value} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-1 text-primary hover:underline">
            Abrir <ExternalLink size={12} />
          </a>
        ) : '-',
    },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? 'Ativo' : 'Inativo'}</Badge>,
    },
    {
      key: 'actions',
      title: '',
      width: 80,
      render: (_: unknown, record) => (
        <div className="flex gap-1">
          <button className="p-1 text-muted-foreground hover:text-foreground" onClick={() => { setSelectedHandle(record); setIsHandleFormOpen(true) }}>
            <Pencil size={14} />
          </button>
          <button className="p-1 text-muted-foreground hover:text-destructive" onClick={() => void handleDeleteHandle(record)}>
            <Trash2 size={14} />
          </button>
        </div>
      ),
    },
  ]

  const campaignColumns: DataTableColumn<CreatorCampaignEntry>[] = [
    {
      key: 'campaignName',
      title: 'Campanha',
      dataIndex: 'campaignName',
      render: (value, record) => (
        <button
          className="text-left text-primary hover:underline"
          onClick={() => navigate(`/campanhas/${record.campaignId}`)}
        >
          {value || '-'}
        </button>
      ),
    },
    { key: 'brandName', title: 'Marca', dataIndex: 'brandName', render: (value?: string | null) => value || '-' },
    {
      key: 'statusName',
      title: 'Status',
      dataIndex: 'statusName',
      render: (value, record) =>
        value ? (
          <span
            className="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium"
            style={{ backgroundColor: `${record.statusColor ?? '#6b7280'}25`, color: record.statusColor ?? '#6b7280' }}
          >
            {value}
          </span>
        ) : '-',
    },
    { key: 'agreedAmount', title: 'Combinado', dataIndex: 'agreedAmount', render: (value: number) => formatCurrency(value) },
    { key: 'agencyFeeAmount', title: 'Fee', dataIndex: 'agencyFeeAmount', render: (value: number) => formatCurrency(value) },
    {
      key: 'confirmedAt',
      title: 'Confirmado',
      dataIndex: 'confirmedAt',
      render: (value?: string | null) => (value ? new Date(value).toLocaleDateString('pt-BR') : '-'),
    },
  ]

  const platformColumns: DataTableColumn<CreatorSummary['performanceByPlatform'][number]>[] = [
    { key: 'platformName', title: 'Plataforma', dataIndex: 'platformName' },
    { key: 'deliverables', title: 'Entregas', dataIndex: 'deliverables' },
    { key: 'published', title: 'Publicadas', dataIndex: 'published' },
    { key: 'grossAmount', title: 'Faturamento', dataIndex: 'grossAmount', render: (value: number) => formatCurrency(value) },
  ]

  return (
    <div className="space-y-4">
      <PageLayout
        title={creator?.stageName || creator?.name || t('creators.detail.title')}
        subtitle={creator?.primaryNiche ? `${creator.primaryNiche}${creator.city ? ` · ${creator.city}/${creator.state ?? ''}` : ''}` : t('creators.detail.subtitle')}
        showDefaultActions={false}
        onRefresh={() => {
          void loadCreator()
          void loadSummary()
          void loadHandles()
          void loadCampaigns()
        }}
      >
        {creator && (
          <Card className="mb-3">
            <CardContent className="flex items-center gap-4 pt-5 pb-5">
              {(() => {
                const photoSrc = resolveCreatorPhotoUrl(creator.photoUrl)
                const initial = (creator.stageName?.trim() || creator.name?.trim() || '?').charAt(0).toUpperCase()
                return (
                  <div className="flex items-center justify-center overflow-hidden rounded-full border bg-muted/30 shrink-0" style={{ width: 96, height: 96 }}>
                    {photoSrc ? (
                      <img src={photoSrc} alt={creator.name} className="h-full w-full object-cover" />
                    ) : (
                      <span className="text-3xl font-semibold text-muted-foreground">{initial}</span>
                    )}
                  </div>
                )
              })()}
              <div className="flex-1 min-w-0">
                <p className="text-xl font-semibold truncate">{creator.stageName || creator.name}</p>
                {creator.stageName && creator.name !== creator.stageName && (
                  <p className="text-sm text-muted-foreground truncate">{creator.name}</p>
                )}
                <div className="flex flex-wrap items-center gap-2 mt-1.5 text-xs text-muted-foreground">
                  {creator.primaryNiche && <span>{creator.primaryNiche}</span>}
                  {creator.city && <span>· {creator.city}{creator.state ? `/${creator.state}` : ''}</span>}
                  {creator.email && <span>· {creator.email}</span>}
                  <Badge variant={creator.isActive ? 'success' : 'destructive'} className="ml-1">
                    {creator.isActive ? 'Ativo' : 'Inativo'}
                  </Badge>
                </div>
              </div>
            </CardContent>
          </Card>
        )}

        <Card>
          <CardContent className="grid gap-3 md:grid-cols-2 lg:grid-cols-4 pt-5 pb-5">
            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Campanhas</p>
              <p className="text-lg font-semibold mt-1">{summary?.totalCampaigns ?? 0}</p>
              <p className="text-[10px] text-muted-foreground">{summary?.confirmedCampaigns ?? 0} confirmadas · {summary?.cancelledCampaigns ?? 0} canceladas</p>
            </div>
            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Entregas publicadas</p>
              <p className="text-lg font-semibold mt-1">{summary?.publishedDeliverables ?? 0}/{summary?.totalDeliverables ?? 0}</p>
              <p className="text-[10px] text-muted-foreground">{summary?.overdueDeliverables ?? 0} atrasadas</p>
            </div>
            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">Faturamento (bruto)</p>
              <p className="text-lg font-semibold mt-1">{formatCurrency(summary?.totalGrossAmount ?? 0)}</p>
              <p className="text-[10px] text-muted-foreground">{t('creators.detail.finance').replace('{0}', formatCurrency(summary?.totalCreatorAmount ?? 0))} · Fee: {formatCurrency(summary?.totalAgencyFeeAmount ?? 0)}</p>
            </div>
            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">On-time delivery</p>
              <p className="text-lg font-semibold mt-1">{(summary?.onTimeDeliveryRate ?? 0).toFixed(1)}%</p>
              <p className="text-[10px] text-muted-foreground">de entregas publicadas</p>
            </div>
          </CardContent>
        </Card>

        <Tabs defaultValue="handles" className="pt-6">
          <TabsList className="mb-6 h-auto w-full justify-start gap-6 rounded-none border-b border-border bg-transparent p-0">
            <TabsTrigger value="handles" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <Users size={14} />
              Redes sociais
              {handles.length > 0 && (
                <span className="ml-0.5 text-[10px] bg-muted text-muted-foreground rounded-full px-1.5 py-0.5 font-medium group-data-[state=active]:bg-primary/15 group-data-[state=active]:text-primary">{handles.length}</span>
              )}
            </TabsTrigger>
            <TabsTrigger value="campaigns" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <Megaphone size={14} />
              Campanhas
              {campaigns.length > 0 && (
                <span className="ml-0.5 text-[10px] bg-muted text-muted-foreground rounded-full px-1.5 py-0.5 font-medium group-data-[state=active]:bg-primary/15 group-data-[state=active]:text-primary">{campaigns.length}</span>
              )}
            </TabsTrigger>
            <TabsTrigger value="performance" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
              <Activity size={14} />
              Performance por plataforma
            </TabsTrigger>
          </TabsList>

          <TabsContent value="handles" className="mt-0">
            <Card>
              <CardContent className="pt-4">
                <div className="mb-3 flex justify-end">
                  <Button size="sm" onClick={() => { setSelectedHandle(null); setIsHandleFormOpen(true) }}>
                    <Plus size={14} className="mr-1.5" />
                    Novo handle
                  </Button>
                </div>
                <DataTable
                  columns={handleColumns}
                  data={handles}
                  rowKey="id"
                  emptyText="Nenhum handle social cadastrado"
                  loading={handlesLoading}
                  pageSize={10}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="campaigns" className="mt-0">
            <Card>
              <CardContent className="pt-4">
                <DataTable
                  columns={campaignColumns}
                  data={campaigns}
                  rowKey="campaignCreatorId"
                  emptyText={t('creators.detail.noCampaigns')}
                  loading={campaignsLoading}
                  pageSize={10}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="performance" className="mt-0">
            <Card>
              <CardContent className="pt-4">
                <DataTable
                  columns={platformColumns}
                  data={summary?.performanceByPlatform ?? []}
                  rowKey="platformId"
                  emptyText="Sem entregas registradas para o creator"
                  pageSize={10}
                />
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </PageLayout>

      <CreatorSocialHandleFormModal
        open={isHandleFormOpen}
        onOpenChange={setIsHandleFormOpen}
        creatorId={creatorId}
        handle={selectedHandle}
        onSuccess={() => {
          setIsHandleFormOpen(false)
          setSelectedHandle(null)
          void loadHandles()
        }}
      />
    </div>
  )
}
