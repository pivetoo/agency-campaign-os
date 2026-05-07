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
} from 'archon-ui'
import type { DataTableColumn, PageAction } from 'archon-ui'
import { CalendarClock, CheckCircle, Eye, FileCheck, Pencil, Send, Trash2, XCircle } from 'lucide-react'
import ProposalFormModal from '../../components/modals/ProposalFormModal'
import ProposalItemFormModal from '../../components/modals/ProposalItemFormModal'
import ApplyProposalTemplateModal from '../../components/modals/ApplyProposalTemplateModal'
import ProposalShareTab from './ProposalShareTab'
import { campaignService } from '../../services/campaignService'
import { proposalService, type Proposal, type ProposalItem } from '../../services/proposalService'
import type { Campaign } from '../../types/campaign'

const proposalStatusLabels: Record<number, string> = {
  1: 'Rascunho',
  2: 'Enviada',
  3: 'Visualizada',
  4: 'Aprovada',
  5: 'Rejeitada',
  6: 'Convertida',
  7: 'Expirada',
  8: 'Cancelada',
}

const proposalStatusVariant: Record<number, 'default' | 'warning' | 'success' | 'destructive'> = {
  1: 'default',
  2: 'warning',
  3: 'warning',
  4: 'success',
  5: 'destructive',
  6: 'success',
  7: 'destructive',
  8: 'destructive',
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
}

function formatDate(value?: string) {
  return value ? new Date(value).toLocaleDateString('pt-BR') : '-'
}

export default function CommercialProposalDetail() {
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
    void campaignService.getAll().then(setCampaigns)
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

    if (status === 1) {
      actions.push({
        key: 'send',
        label: 'Enviar',
        icon: <Send className="h-4 w-4" />,
        variant: 'outline-primary',
        disabled: actionLoading,
        onClick: () => void runProposalAction(() => proposalService.send(proposalId)),
      })
    }
    if (status === 2) {
      actions.push({
        key: 'viewed',
        label: 'Marcar como visualizada',
        icon: <Eye className="h-4 w-4" />,
        variant: 'outline',
        disabled: actionLoading,
        onClick: () => void runProposalAction(() => proposalService.markAsViewed(proposalId)),
      })
    }
    if (status === 2 || status === 3) {
      actions.push({
        key: 'approve',
        label: 'Aprovar',
        icon: <CheckCircle className="h-4 w-4" />,
        variant: 'outline-success',
        disabled: actionLoading,
        onClick: () => void runProposalAction(() => proposalService.approve(proposalId)),
      })
      actions.push({
        key: 'reject',
        label: 'Rejeitar',
        icon: <XCircle className="h-4 w-4" />,
        variant: 'outline-danger',
        disabled: actionLoading,
        onClick: () => void runProposalAction(() => proposalService.reject(proposalId)),
      })
    }
    if (status !== 6 && status !== 8) {
      actions.push({
        key: 'cancel',
        label: 'Cancelar',
        variant: 'outline-danger',
        disabled: actionLoading,
        onClick: () => void runProposalAction(() => proposalService.cancel(proposalId)),
      })
    }

    return actions
  }, [proposal, actionLoading, proposalId])

  const columns: DataTableColumn<ProposalItem>[] = [
    { key: 'description', title: 'Item', dataIndex: 'description' },
    { key: 'creator', title: 'Creator', dataIndex: 'creator', render: (value?: ProposalItem['creator']) => value?.name || '-' },
    { key: 'quantity', title: 'Qtd.', dataIndex: 'quantity' },
    { key: 'unitPrice', title: 'Valor unitário', dataIndex: 'unitPrice', render: (value: number) => formatCurrency(value) },
    { key: 'deliveryDeadline', title: 'Prazo', dataIndex: 'deliveryDeadline', render: (value?: string) => formatDate(value) },
    { key: 'total', title: 'Total', dataIndex: 'total', render: (value: number) => formatCurrency(value) },
  ]

  if (!proposal && !loading) {
    return (
      <PageLayout title="Proposta não encontrada" showDefaultActions={false}>
        <Button variant="outline" onClick={() => navigate('/comercial/propostas')}>Voltar para propostas</Button>
      </PageLayout>
    )
  }

  const isApproved = proposal?.status === 4
  const subtitleParts: string[] = []
  if (proposal?.brand?.name) subtitleParts.push(proposal.brand.name)
  if (proposal?.opportunity?.name) subtitleParts.push(`vinculada a ${proposal.opportunity.name}`)
  const subtitle = subtitleParts.length > 0 ? subtitleParts.join(' · ') : 'Detalhe da proposta'

  return (
    <PageLayout
      title={proposal?.name ?? 'Proposta'}
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
              <span className="text-xs uppercase tracking-wide text-muted-foreground">Status</span>
              <Badge variant={proposalStatusVariant[proposal.status] || 'default'}>
                {proposalStatusLabels[proposal.status] || '-'}
              </Badge>
            </div>
            <span className="hidden text-border md:inline">·</span>
            <div className="flex items-center gap-2">
              <span className="text-xs uppercase tracking-wide text-muted-foreground">Total</span>
              <strong className="text-base text-foreground">{formatCurrency(total || proposal.totalValue)}</strong>
            </div>
            <span className="hidden text-border md:inline">·</span>
            <div className="flex items-center gap-2">
              <CalendarClock className="h-3.5 w-3.5 text-muted-foreground" />
              <span className="text-xs uppercase tracking-wide text-muted-foreground">Validade</span>
              <span className="text-sm text-foreground">{formatDate(proposal.validityUntil)}</span>
            </div>
            {proposal.campaign?.name ? (
              <>
                <span className="hidden text-border md:inline">·</span>
                <div className="flex items-center gap-2">
                  <FileCheck className="h-3.5 w-3.5 text-emerald-600" />
                  <span className="text-xs uppercase tracking-wide text-muted-foreground">Campanha</span>
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
                    <CardTitle className="text-sm">Itens da proposta</CardTitle>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        size="sm"
                        onClick={() => { setSelectedItem(null); setIsItemFormOpen(true) }}
                      >
                        Adicionar
                      </Button>
                      <Button size="sm" variant="outline" onClick={() => setIsApplyTemplateOpen(true)}>
                        Aplicar template
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        icon={<Pencil className="h-3.5 w-3.5" />}
                        disabled={!selectedItem}
                        onClick={() => selectedItem && setIsItemFormOpen(true)}
                      >
                        Editar
                      </Button>
                      <Button
                        size="sm"
                        variant="outline-danger"
                        icon={<Trash2 className="h-3.5 w-3.5" />}
                        disabled={!selectedItem}
                        onClick={() => selectedItem && void runProposalAction(() => proposalService.deleteItem(selectedItem.id))}
                      >
                        Excluir
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
                    emptyText="Nenhum item cadastrado para esta proposta"
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
                      Conversão em campanha
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="p-4">
                    <p className="mb-3 text-xs text-muted-foreground">
                      Vincule esta proposta aprovada a uma campanha existente. Os itens já cadastrados serão referenciados pela campanha.
                    </p>
                    <div className="flex flex-col gap-3 md:flex-row md:items-end">
                      <div className="w-full md:max-w-md">
                        <label className="mb-1 block text-xs font-medium text-muted-foreground">Campanha</label>
                        <SearchableSelect
                          value={campaignId}
                          onValueChange={setCampaignId}
                          options={campaignOptions}
                          placeholder="Selecione uma campanha"
                          searchPlaceholder="Buscar campanha"
                        />
                      </div>
                      <Button
                        disabled={!campaignId || actionLoading}
                        onClick={() => void runProposalAction(() => proposalService.convertToCampaign(proposalId, Number(campaignId)))}
                      >
                        <FileCheck className="mr-2 h-4 w-4" /> Converter
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ) : null}
            </div>

            <aside className="space-y-4">
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
