import { useEffect, useState } from 'react'
import { PageLayout, DataTable, useApi, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, Badge } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { campaignService } from '../../services/campaignService'
import type { Campaign, CampaignSummary } from '../../types/campaign'
import CampaignFormModal from '../../components/modals/CampaignFormModal'

export default function Campaigns() {
  const [campaigns, setCampaigns] = useState<Campaign[]>([])
  const [selectedCampaign, setSelectedCampaign] = useState<Campaign | null>(null)
  const [previewCampaign, setPreviewCampaign] = useState<Campaign | null>(null)
  const [summary, setSummary] = useState<CampaignSummary | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)

  const { execute: fetchCampaigns, loading } = useApi<Campaign[]>({ showErrorMessage: true })
  const { execute: fetchSummary } = useApi<CampaignSummary | null>({ showErrorMessage: true })

  const loadCampaigns = async () => {
    const result = await fetchCampaigns(() => campaignService.getAll())
    if (result) {
      setCampaigns(result)
    }
  }

  useEffect(() => {
    void loadCampaigns()
  }, [])

  useEffect(() => {
    const currentCampaign = previewCampaign ?? selectedCampaign
    if (!currentCampaign) {
      setSummary(null)
      return
    }

    void fetchSummary(() => campaignService.getSummary(currentCampaign.id)).then((result) => {
      if (result) {
        setSummary(result)
      }
    })
  }, [selectedCampaign, previewCampaign])

  const columns: DataTableColumn<Campaign>[] = [
    { key: 'name', title: 'Campanha', dataIndex: 'name' },
    { key: 'brand', title: 'Marca', dataIndex: 'brand', render: (value: Campaign['brand']) => value?.name || '-' },
    { key: 'budget', title: 'Budget', dataIndex: 'budget', render: (value: number) => `R$ ${value.toFixed(2)}` },
    { key: 'startsAt', title: 'Início', dataIndex: 'startsAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
  ]

  return (
    <>
      <PageLayout
        title="Campanhas"
        onAdd={() => { setSelectedCampaign(null); setIsFormOpen(true) }}
        onEdit={() => selectedCampaign && setIsFormOpen(true)}
        onRefresh={() => void loadCampaigns()}
        selectedRowsCount={selectedCampaign ? 1 : 0}
      >
        <DataTable
          columns={columns}
          data={campaigns}
          rowKey="id"
          selectedRows={selectedCampaign ? [selectedCampaign] : []}
          onSelectionChange={(rows) => setSelectedCampaign(rows[0] ?? null)}
          onRowDoubleClick={setPreviewCampaign}
          emptyText="Nenhuma campanha cadastrada"
          loading={loading}
        />
      </PageLayout>

      <CampaignFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        campaign={selectedCampaign}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedCampaign(null)
          void loadCampaigns()
        }}
      />

      <Sheet open={!!previewCampaign} onOpenChange={(open) => !open && setPreviewCampaign(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewCampaign ? (
            <div className="flex h-full flex-col">
              <SheetPreviewHeader
                title={previewCampaign.name}
                meta={
                  <>
                    <Badge variant={previewCampaign.isActive ? 'success' : 'destructive'}>
                      {previewCampaign.isActive ? 'Ativa' : 'Inativa'}
                    </Badge>
                    <span className="text-xs font-medium text-muted-foreground">
                      {previewCampaign.brand?.name || '-'}
                    </span>
                  </>
                }
                description="Resumo rápido da campanha selecionada"
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title="Dados gerais" description="Informações principais da campanha">
                  <div className="space-y-4">
                    <SheetPreviewGrid>
                      <SheetPreviewField label="Marca" value={previewCampaign.brand?.name || '-'} />
                      <SheetPreviewField label="Budget" value={`R$ ${previewCampaign.budget.toFixed(2)}`} />
                      <SheetPreviewField label="Início" value={new Date(previewCampaign.startsAt).toLocaleDateString('pt-BR')} />
                      <SheetPreviewField label="Fim" value={previewCampaign.endsAt ? new Date(previewCampaign.endsAt).toLocaleDateString('pt-BR') : '-'} />
                    </SheetPreviewGrid>
                    <SheetPreviewField label="Descrição" value={previewCampaign.description || '-'} />
                  </div>
                </SheetPreviewSection>

                <SheetPreviewSection title="Resumo operacional" description="Indicadores atuais da campanha">
                  <SheetPreviewGrid>
                    <SheetPreviewField label="Entregas" value={String(summary?.deliverablesCount ?? 0)} />
                    <SheetPreviewField label="Pendentes" value={String(summary?.pendingDeliverablesCount ?? 0)} />
                    <SheetPreviewField label="Publicadas" value={String(summary?.publishedDeliverablesCount ?? 0)} />
                    <SheetPreviewField label="Fee da agência" value={`R$ ${(summary?.agencyFeeAmountTotal ?? 0).toFixed(2)}`} />
                    <SheetPreviewField label="Valor bruto" value={`R$ ${(summary?.grossAmountTotal ?? 0).toFixed(2)}`} />
                    <SheetPreviewField label="Budget restante" value={`R$ ${(summary?.remainingBudget ?? 0).toFixed(2)}`} />
                  </SheetPreviewGrid>
                </SheetPreviewSection>
              </div>
            </div>
          ) : null}
        </SheetContent>
      </Sheet>
    </>
  )
}
