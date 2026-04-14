import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageLayout, Button, Card, CardContent, CardHeader, CardTitle, DataTable, useApi, Badge } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Plus } from 'lucide-react'
import { campaignService } from '../../services/campaignService'
import { campaignCreatorService } from '../../services/campaignCreatorService'
import { campaignDeliverableService } from '../../services/campaignDeliverableService'
import type { Campaign } from '../../types/campaign'
import type { CampaignCreator } from '../../types/campaignCreator'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'
import CampaignCreatorFormModal from '../../components/modals/CampaignCreatorFormModal'
import CampaignDeliverableFormModal from '../../components/modals/CampaignDeliverableFormModal'

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

export default function CampaignDetail() {
  const { id } = useParams<{ id: string }>()
  const campaignId = Number(id || 0)

  const [campaign, setCampaign] = useState<Campaign | null>(null)
  const [campaignCreators, setCampaignCreators] = useState<CampaignCreator[]>([])
  const [selectedCampaignCreator, setSelectedCampaignCreator] = useState<CampaignCreator | null>(null)
  const [deliverables, setDeliverables] = useState<CampaignDeliverable[]>([])
  const [selectedDeliverable, setSelectedDeliverable] = useState<CampaignDeliverable | null>(null)
  const [isCreatorFormOpen, setIsCreatorFormOpen] = useState(false)
  const [isDeliverableFormOpen, setIsDeliverableFormOpen] = useState(false)

  const { execute: fetchCampaign } = useApi<Campaign | null>({ showErrorMessage: true })
  const { execute: fetchCampaignCreators, loading: creatorsLoading } = useApi<CampaignCreator[]>({ showErrorMessage: true })
  const { execute: fetchDeliverables, loading: deliverablesLoading } = useApi<CampaignDeliverable[]>({ showErrorMessage: true })

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

  useEffect(() => {
    if (!campaignId) {
      return
    }

    void loadCampaign()
    void loadCampaignCreators()
    void loadDeliverables()
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
      render: (value: number) => campaignCreatorStatusLabels[value] || '-',
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
      key: 'type',
      title: 'Tipo',
      dataIndex: 'type',
      render: (value: number) => ({ 1: 'Reel', 2: 'Story', 3: 'Post feed', 4: 'Vídeo', 5: 'Live', 6: 'Combo', 7: 'Outro' }[value] || '-'),
    },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      render: (value: number) => deliverableStatusLabels[value] || '-',
    },
    {
      key: 'dueAt',
      title: 'Prazo',
      dataIndex: 'dueAt',
      render: (value: string) => new Date(value).toLocaleDateString('pt-BR'),
    },
  ]

  return (
    <div className="space-y-4">
      <PageLayout
        title={campaign?.name || 'Campanha'}
        subtitle={campaign?.brand?.name ? `${campaign.brand.name} · operação da campanha` : 'Operação da campanha'}
        onRefresh={() => {
          void loadCampaign()
          void loadCampaignCreators()
          void loadDeliverables()
        }}
        showDefaultActions={false}
      >
        <Card className="border-0 bg-transparent shadow-none">
          <CardContent className="grid gap-4 px-0 pt-0 pb-0 md:grid-cols-2 lg:grid-cols-3">
            <div>
              <p className="text-sm font-medium">Status</p>
              <div className="mt-1">
                <Badge className="px-2 py-0 text-[11px]" variant={campaign?.status === 5 ? 'success' : campaign?.status === 6 ? 'destructive' : 'warning'}>
                  {campaign ? campaignStatusLabels[campaign.status] : '-'}
                </Badge>
              </div>
            </div>

            <div>
              <p className="text-sm font-medium">Budget</p>
              <p className="text-sm text-muted-foreground">R$ {(campaign?.budget ?? 0).toFixed(2)}</p>
            </div>

            <div>
              <p className="text-sm font-medium">Período</p>
              <p className="text-sm text-muted-foreground">
                {campaign?.startsAt ? new Date(campaign.startsAt).toLocaleDateString('pt-BR') : '-'}
                {' até '}
                {campaign?.endsAt ? new Date(campaign.endsAt).toLocaleDateString('pt-BR') : '-'}
              </p>
            </div>

            <div>
              <p className="text-sm font-medium">Objetivo</p>
              <p className="text-sm text-muted-foreground">{campaign?.objective || '-'}</p>
            </div>

            <div>
              <p className="text-sm font-medium">Responsável interno</p>
              <p className="text-sm text-muted-foreground">{campaign?.internalOwnerName || '-'}</p>
            </div>

            <div>
              <p className="text-sm font-medium">Descrição</p>
              <p className="text-sm text-muted-foreground">{campaign?.description || '-'}</p>
            </div>

            <div className="lg:col-span-3">
              <p className="text-sm font-medium">Briefing</p>
              <p className="text-sm text-muted-foreground">{campaign?.briefing || '-'}</p>
            </div>

            <div className="lg:col-span-3">
              <p className="text-sm font-medium">Observações</p>
              <p className="text-sm text-muted-foreground">{campaign?.notes || '-'}</p>
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
              pageSize={3}
              pageSizeOptions={[3, 5, 10, 20]}
            />
          </CardContent>
        </Card>

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
              onRowDoubleClick={(row) => {
                setSelectedDeliverable(row)
                setIsDeliverableFormOpen(true)
              }}
              emptyText="Nenhuma entrega cadastrada"
              loading={deliverablesLoading}
              pageSize={3}
              pageSizeOptions={[3, 5, 10, 20]}
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
    </div>
  )
}
