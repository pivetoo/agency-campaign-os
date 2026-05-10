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
        title={creator?.stageName || creator?.name || 'Creator'}
        subtitle={creator?.primaryNiche ? `${creator.primaryNiche}${creator.city ? ` · ${creator.city}/${creator.state ?? ''}` : ''}` : 'Perfil 360 do creator'}
        showDefaultActions={false}
        onRefresh={() => {
          void loadCreator()
          void loadSummary()
          void loadHandles()
          void loadCampaigns()
        }}
      >
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
              <p className="text-[10px] text-muted-foreground">Creator: {formatCurrency(summary?.totalCreatorAmount ?? 0)} · Fee: {formatCurrency(summary?.totalAgencyFeeAmount ?? 0)}</p>
            </div>
            <div>
              <p className="text-xs text-muted-foreground uppercase tracking-wide">On-time delivery</p>
              <p className="text-lg font-semibold mt-1">{(summary?.onTimeDeliveryRate ?? 0).toFixed(1)}%</p>
              <p className="text-[10px] text-muted-foreground">de entregas publicadas</p>
            </div>
          </CardContent>
        </Card>

        <Tabs defaultValue="handles" className="mt-4">
          <TabsList>
            <TabsTrigger value="handles">
              <Users size={14} className="mr-1.5" />
              Redes sociais
              {handles.length > 0 && (
                <span className="ml-1.5 text-[10px] bg-muted text-muted-foreground rounded-full px-1.5 py-0.5 font-medium">{handles.length}</span>
              )}
            </TabsTrigger>
            <TabsTrigger value="campaigns">
              <Megaphone size={14} className="mr-1.5" />
              Campanhas
              {campaigns.length > 0 && (
                <span className="ml-1.5 text-[10px] bg-muted text-muted-foreground rounded-full px-1.5 py-0.5 font-medium">{campaigns.length}</span>
              )}
            </TabsTrigger>
            <TabsTrigger value="performance">
              <Activity size={14} className="mr-1.5" />
              Performance por plataforma
            </TabsTrigger>
          </TabsList>

          <TabsContent value="handles" className="mt-2">
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

          <TabsContent value="campaigns" className="mt-2">
            <Card>
              <CardContent className="pt-4">
                <DataTable
                  columns={campaignColumns}
                  data={campaigns}
                  rowKey="campaignCreatorId"
                  emptyText="Creator ainda não participou de campanhas"
                  loading={campaignsLoading}
                  pageSize={10}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="performance" className="mt-2">
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
