import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageLayout, Button, Card, CardContent, CardHeader, CardTitle, DataTable, useApi, Badge } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Plus } from 'lucide-react'
import { campaignService } from '../../services/campaignService'
import { campaignCreatorService } from '../../services/campaignCreatorService'
import { campaignDeliverableService } from '../../services/campaignDeliverableService'
import { campaignFinancialEntryService } from '../../services/campaignFinancialEntryService'
import type { Campaign, CampaignSummary } from '../../types/campaign'
import type { CampaignCreator } from '../../types/campaignCreator'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'
import type { CampaignFinancialEntry } from '../../types/campaignFinancialEntry'
import CampaignCreatorFormModal from '../../components/modals/CampaignCreatorFormModal'
import CampaignDeliverableFormModal from '../../components/modals/CampaignDeliverableFormModal'
import CampaignFinancialEntryFormModal from '../../components/modals/CampaignFinancialEntryFormModal'

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

const financialCategoryLabels: Record<number, string> = {
  1: 'Recebível da marca',
  2: 'Repasse creator',
  3: 'Fee da agência',
  4: 'Custo operacional',
  5: 'Bônus',
  6: 'Ajuste',
  7: 'Reembolso',
  8: 'Imposto',
}

export default function CampaignDetail() {
  const { id } = useParams<{ id: string }>()
  const campaignId = Number(id || 0)

  const [campaign, setCampaign] = useState<Campaign | null>(null)
  const [summary, setSummary] = useState<CampaignSummary | null>(null)
  const [campaignCreators, setCampaignCreators] = useState<CampaignCreator[]>([])
  const [selectedCampaignCreator, setSelectedCampaignCreator] = useState<CampaignCreator | null>(null)
  const [deliverables, setDeliverables] = useState<CampaignDeliverable[]>([])
  const [selectedDeliverable, setSelectedDeliverable] = useState<CampaignDeliverable | null>(null)
  const [financialEntries, setFinancialEntries] = useState<CampaignFinancialEntry[]>([])
  const [selectedFinancialEntry, setSelectedFinancialEntry] = useState<CampaignFinancialEntry | null>(null)
  const [isCreatorFormOpen, setIsCreatorFormOpen] = useState(false)
  const [isDeliverableFormOpen, setIsDeliverableFormOpen] = useState(false)
  const [isFinancialFormOpen, setIsFinancialFormOpen] = useState(false)

  const { execute: fetchCampaign } = useApi<Campaign | null>({ showErrorMessage: true })
  const { execute: fetchSummary } = useApi<CampaignSummary | null>({ showErrorMessage: true })
  const { execute: fetchCampaignCreators, loading: creatorsLoading } = useApi<CampaignCreator[]>({ showErrorMessage: true })
  const { execute: fetchDeliverables, loading: deliverablesLoading } = useApi<CampaignDeliverable[]>({ showErrorMessage: true })
  const { execute: fetchFinancialEntries, loading: financialLoading } = useApi<CampaignFinancialEntry[]>({ showErrorMessage: true })

  const loadCampaign = async () => {
    const result = await fetchCampaign(() => campaignService.getById(campaignId))
    if (result) {
      setCampaign(result)
    }
  }

  const loadSummary = async () => {
    const result = await fetchSummary(() => campaignService.getSummary(campaignId))
    if (result) {
      setSummary(result)
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

  const loadFinancialEntries = async () => {
    const result = await fetchFinancialEntries(() => campaignFinancialEntryService.getByCampaign(campaignId))
    if (result) {
      setFinancialEntries(result)
    }
  }

  useEffect(() => {
    if (!campaignId) {
      return
    }

    void loadCampaign()
    void loadSummary()
    void loadCampaignCreators()
    void loadDeliverables()
    void loadFinancialEntries()
  }, [campaignId])

  const campaignCreatorColumns: DataTableColumn<CampaignCreator>[] = [
    { key: 'creator', title: 'Creator', dataIndex: 'creator', render: (value: CampaignCreator['creator']) => value?.stageName || value?.name || '-' },
    { key: 'status', title: 'Status', dataIndex: 'status', render: (value: number) => campaignCreatorStatusLabels[value] || '-' },
    { key: 'agreedAmount', title: 'Valor combinado', dataIndex: 'agreedAmount', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'agencyFeeAmount', title: 'Fee agência', dataIndex: 'agencyFeeAmount', render: (value: number) => `R$ ${value.toFixed(2)}` },
  ]

  const deliverableColumns: DataTableColumn<CampaignDeliverable>[] = [
    { key: 'title', title: 'Entrega', dataIndex: 'title' },
    { key: 'campaignCreator', title: 'Creator', dataIndex: 'campaignCreator', render: (value: CampaignDeliverable['campaignCreator']) => value?.stageName || value?.creatorName || '-' },
    { key: 'type', title: 'Tipo', dataIndex: 'type', render: (value: number) => ({ 1: 'Reel', 2: 'Story', 3: 'Post feed', 4: 'Vídeo', 5: 'Live', 6: 'Combo', 7: 'Outro' }[value] || '-') },
    { key: 'status', title: 'Status', dataIndex: 'status', render: (value: number) => deliverableStatusLabels[value] || '-' },
    { key: 'dueAt', title: 'Prazo', dataIndex: 'dueAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    { key: 'grossAmount', title: 'Valor bruto', dataIndex: 'grossAmount', render: (value: number) => `R$ ${value.toFixed(2)}` },
  ]

  const financialColumns: DataTableColumn<CampaignFinancialEntry>[] = [
    { key: 'type', title: 'Tipo', dataIndex: 'type', render: (value: number) => value === 1 ? 'A receber' : 'A pagar' },
    { key: 'category', title: 'Categoria', dataIndex: 'category', render: (value: number) => financialCategoryLabels[value] || '-' },
    { key: 'description', title: 'Descrição', dataIndex: 'description' },
    { key: 'amount', title: 'Valor', dataIndex: 'amount', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'dueAt', title: 'Vencimento', dataIndex: 'dueAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
  ]

  const receivablesTotal = financialEntries.filter((item) => item.type === 1).reduce((sum, item) => sum + item.amount, 0)
  const payablesTotal = financialEntries.filter((item) => item.type === 2).reduce((sum, item) => sum + item.amount, 0)

  return (
    <div className="space-y-4">
      <PageLayout
        title={campaign?.name || 'Campanha'}
        subtitle={campaign?.brand?.name ? `${campaign.brand.name} · operação da campanha` : 'Operação da campanha'}
        onRefresh={() => {
          void loadCampaign()
          void loadSummary()
          void loadCampaignCreators()
          void loadDeliverables()
          void loadFinancialEntries()
        }}
        showDefaultActions={false}
      >
        <div className="grid grid-cols-1 gap-4 md:grid-cols-3 xl:grid-cols-6">
          <Card>
            <CardHeader><CardTitle className="text-sm">Budget</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold">R$ {(summary?.budget ?? 0).toFixed(2)}</CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle className="text-sm">Creators</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold">{summary?.campaignCreatorsCount ?? 0}</CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle className="text-sm">Entregas</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold">{summary?.deliverablesCount ?? 0}</CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle className="text-sm">Aprovações pendentes</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold">{summary?.pendingApprovalsCount ?? 0}</CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle className="text-sm">A receber</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold">R$ {receivablesTotal.toFixed(2)}</CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle className="text-sm">A pagar</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold">R$ {payablesTotal.toFixed(2)}</CardContent>
          </Card>
        </div>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle>Resumo da campanha</CardTitle>
              <p className="text-sm text-muted-foreground mt-1">Objetivo, briefing e responsável interno</p>
            </div>
            <Badge variant={campaign?.isActive ? 'success' : 'destructive'}>{campaign?.isActive ? 'Ativa' : 'Inativa'}</Badge>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div>
              <p className="text-sm font-medium">Objetivo</p>
              <p className="text-sm text-muted-foreground">{campaign?.objective || '-'}</p>
            </div>
            <div>
              <p className="text-sm font-medium">Responsável interno</p>
              <p className="text-sm text-muted-foreground">{campaign?.internalOwnerName || '-'}</p>
            </div>
            <div className="md:col-span-2">
              <p className="text-sm font-medium">Briefing</p>
              <p className="text-sm text-muted-foreground">{campaign?.briefing || '-'}</p>
            </div>
          </CardContent>
        </Card>

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
              onRowDoubleClick={(row) => {
                setSelectedCampaignCreator(row)
                setIsCreatorFormOpen(true)
              }}
              emptyText="Nenhum creator vinculado à campanha"
              loading={creatorsLoading}
            />
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle>Entregas da campanha</CardTitle>
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
              onRowDoubleClick={(row) => {
                setSelectedDeliverable(row)
                setIsDeliverableFormOpen(true)
              }}
              emptyText="Nenhuma entrega cadastrada"
              loading={deliverablesLoading}
            />
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle>Financeiro da campanha</CardTitle>
            <Button size="sm" onClick={() => { setSelectedFinancialEntry(null); setIsFinancialFormOpen(true) }}>
              <Plus size={16} className="mr-2" />
              Novo lançamento
            </Button>
          </CardHeader>
          <CardContent>
            <DataTable
              columns={financialColumns}
              data={financialEntries}
              rowKey="id"
              selectedRows={selectedFinancialEntry ? [selectedFinancialEntry] : []}
              onSelectionChange={(rows) => setSelectedFinancialEntry(rows[0] ?? null)}
              onRowDoubleClick={(row) => {
                setSelectedFinancialEntry(row)
                setIsFinancialFormOpen(true)
              }}
              emptyText="Nenhum lançamento financeiro cadastrado"
              loading={financialLoading}
            />
          </CardContent>
        </Card>
      </PageLayout>

      <CampaignCreatorFormModal
        open={isCreatorFormOpen}
        onOpenChange={setIsCreatorFormOpen}
        campaignId={campaignId}
        campaignCreator={selectedCampaignCreator}
        onSuccess={() => {
          setIsCreatorFormOpen(false)
          setSelectedCampaignCreator(null)
          void loadSummary()
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
          void loadSummary()
          void loadDeliverables()
          void loadFinancialEntries()
        }}
      />

      <CampaignFinancialEntryFormModal
        open={isFinancialFormOpen}
        onOpenChange={setIsFinancialFormOpen}
        campaignId={campaignId}
        entry={selectedFinancialEntry}
        deliverables={deliverables}
        onSuccess={() => {
          setIsFinancialFormOpen(false)
          setSelectedFinancialEntry(null)
          void loadFinancialEntries()
        }}
      />
    </div>
  )
}
