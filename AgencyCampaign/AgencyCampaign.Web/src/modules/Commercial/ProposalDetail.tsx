import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, DataTable, PageLayout, SearchableSelect, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { CheckCircle, Eye, FileCheck, Send, XCircle } from 'lucide-react'
import ProposalFormModal from '../../components/modals/ProposalFormModal'
import ProposalItemFormModal from '../../components/modals/ProposalItemFormModal'
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
  const [campaignId, setCampaignId] = useState<string>('')

  const { execute: fetchProposal, loading } = useApi<Proposal | undefined>({ showErrorMessage: true })
  const { execute: fetchItems } = useApi<ProposalItem[]>({ showErrorMessage: true })
  const { execute: executeAction, loading: actionLoading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadProposal = async () => {
    const result = await fetchProposal(() => proposalService.getById(proposalId))
    if (result) {
      setProposal(result)
    }

    const proposalItems = await fetchItems(() => proposalService.getItems(proposalId))
    if (proposalItems) {
      setItems(proposalItems)
    }
  }

  useEffect(() => {
    if (!proposalId) {
      return
    }

    void loadProposal()
    void campaignService.getAll().then(setCampaigns)
  }, [proposalId])

  const total = useMemo(() => items.reduce((sum, item) => sum + item.total, 0), [items])
  const campaignOptions = campaigns.map((campaign) => ({
    value: String(campaign.id),
    label: campaign.name,
  }))

  const runProposalAction = async (action: () => Promise<unknown>) => {
    const result = await executeAction(action)
    if (result !== null) {
      await loadProposal()
    }
  }

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

  return (
    <PageLayout
      title={proposal?.name ?? 'Proposta'}
      subtitle="Gerencie itens, envio, visualização, aprovação e conversão da proposta"
      onEdit={() => setIsProposalFormOpen(true)}
      selectedRowsCount={proposal ? 1 : 0}
      onRefresh={() => void loadProposal()}
      actions={[
        { key: 'send', label: 'Enviar', icon: <Send className="h-4 w-4" />, variant: 'outline-primary', disabled: !proposal || actionLoading, onClick: () => void runProposalAction(() => proposalService.send(proposalId)) },
        { key: 'viewed', label: 'Visualizada', icon: <Eye className="h-4 w-4" />, variant: 'outline', disabled: !proposal || actionLoading, onClick: () => void runProposalAction(() => proposalService.markAsViewed(proposalId)) },
        { key: 'approve', label: 'Aprovar', icon: <CheckCircle className="h-4 w-4" />, variant: 'outline-success', disabled: !proposal || actionLoading, onClick: () => void runProposalAction(() => proposalService.approve(proposalId)) },
        { key: 'reject', label: 'Rejeitar', icon: <XCircle className="h-4 w-4" />, variant: 'outline-danger', disabled: !proposal || actionLoading, onClick: () => void runProposalAction(() => proposalService.reject(proposalId)) },
        { key: 'cancel', label: 'Cancelar', variant: 'outline-danger', disabled: !proposal || actionLoading, onClick: () => void runProposalAction(() => proposalService.cancel(proposalId)) },
      ]}
    >
      {proposal && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
            <Card><CardHeader className="pb-2"><CardTitle className="text-sm text-muted-foreground">Status</CardTitle></CardHeader><CardContent><Badge variant={proposalStatusVariant[proposal.status] || 'default'}>{proposalStatusLabels[proposal.status] || '-'}</Badge></CardContent></Card>
            <Card><CardHeader className="pb-2"><CardTitle className="text-sm text-muted-foreground">Marca</CardTitle></CardHeader><CardContent><div className="text-xl font-semibold">{proposal.brand?.name || '-'}</div></CardContent></Card>
            <Card><CardHeader className="pb-2"><CardTitle className="text-sm text-muted-foreground">Total da proposta</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold">{formatCurrency(total || proposal.totalValue)}</div></CardContent></Card>
            <Card><CardHeader className="pb-2"><CardTitle className="text-sm text-muted-foreground">Validade</CardTitle></CardHeader><CardContent><div className="text-xl font-semibold">{formatDate(proposal.validityUntil)}</div></CardContent></Card>
          </div>

          <Card>
            <CardHeader>
              <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                <CardTitle>Itens da proposta</CardTitle>
                <div className="flex flex-wrap gap-2">
                  <Button size="sm" onClick={() => { setSelectedItem(null); setIsItemFormOpen(true) }}>Adicionar item</Button>
                  <Button size="sm" variant="outline" disabled={!selectedItem} onClick={() => selectedItem && setIsItemFormOpen(true)}>Editar item</Button>
                  <Button size="sm" variant="outline-danger" disabled={!selectedItem} onClick={() => selectedItem && void runProposalAction(() => proposalService.deleteItem(selectedItem.id))}>Excluir item</Button>
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <DataTable
                columns={columns}
                data={items}
                rowKey="id"
                selectedRows={selectedItem ? [selectedItem] : []}
                onSelectionChange={(rows) => setSelectedItem(rows[0] ?? null)}
                emptyText="Nenhum item cadastrado para esta proposta"
                pageSize={5}
                pageSizeOptions={[5, 10, 20, 50]}
              />
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle>Conversão em campanha</CardTitle></CardHeader>
            <CardContent>
              <div className="flex flex-col gap-3 md:flex-row md:items-end">
                <div className="w-full space-y-2 md:max-w-md">
                  <label className="text-sm font-medium">Campanha existente</label>
                  <SearchableSelect
                    value={campaignId}
                    onValueChange={setCampaignId}
                    options={campaignOptions}
                    placeholder="Selecione uma campanha"
                    searchPlaceholder="Buscar campanha"
                    emptyMessage="Nenhuma campanha encontrada"
                  />
                </div>
                <Button disabled={!campaignId || actionLoading} onClick={() => void runProposalAction(() => proposalService.convertToCampaign(proposalId, Number(campaignId)))}>
                  <FileCheck className="mr-2 h-4 w-4" /> Converter
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      <ProposalFormModal open={isProposalFormOpen} onOpenChange={setIsProposalFormOpen} proposal={proposal} onSuccess={() => { setIsProposalFormOpen(false); void loadProposal() }} />
      <ProposalItemFormModal open={isItemFormOpen} onOpenChange={setIsItemFormOpen} proposalId={proposalId} item={selectedItem} onSuccess={() => { setIsItemFormOpen(false); setSelectedItem(null); void loadProposal() }} />
    </PageLayout>
  )
}
