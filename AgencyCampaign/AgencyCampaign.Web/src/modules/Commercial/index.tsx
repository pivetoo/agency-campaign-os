import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { proposalService, type Proposal } from '../../services/proposalService'
import ProposalFormModal from '../../components/modals/ProposalFormModal'

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

export default function Commercial() {
  const [proposals, setProposals] = useState<Proposal[]>([])
  const [selectedProposal, setSelectedProposal] = useState<Proposal | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)

  const { execute: fetchProposals, loading } = useApi<Proposal[]>({ showErrorMessage: true })
  const loadProposals = async () => {
    const result = await fetchProposals(() => proposalService.getAll())
    if (result) {
      setProposals(result)
    }
  }

  useEffect(() => {
    void loadProposals()
  }, [])

  const columns: DataTableColumn<Proposal>[] = [
    { key: 'name', title: 'Proposta', dataIndex: 'name' },
    { key: 'brand', title: 'Marca', dataIndex: 'brand', render: (value?: Proposal['brand']) => value?.name || '-' },
    { key: 'totalValue', title: 'Valor Total', dataIndex: 'totalValue', render: (value?: number) => value != null ? `R$ ${value.toFixed(2)}` : '-' },
    { key: 'validityUntil', title: 'Validade', dataIndex: 'validityUntil', render: (value?: string) => value ? new Date(value).toLocaleDateString('pt-BR') : '-' },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      render: (value?: number) => (
        <Badge variant={proposalStatusVariant[value ?? 0] || 'default'}>
          {proposalStatusLabels[value ?? 0] || '-'}
        </Badge>
      ),
    },
    { key: 'items', title: 'Itens', dataIndex: 'items', render: (value?: Proposal['items']) => value?.length ?? 0 },
    { key: 'createdAt', title: 'Criada em', dataIndex: 'createdAt', render: (value?: string) => value ? new Date(value).toLocaleDateString('pt-BR') : '-' },
  ]

  return (
    <>
      <PageLayout
        title="Propostas"
        subtitle="Gerencie propostas e conversão para campanhas"
        onAdd={() => { setSelectedProposal(null); setIsFormOpen(true) }}
        onEdit={() => selectedProposal && setIsFormOpen(true)}
        onRefresh={() => void loadProposals()}
        selectedRowsCount={selectedProposal ? 1 : 0}
      >
        <DataTable
          columns={columns}
          data={proposals}
          rowKey="id"
          selectedRows={selectedProposal ? [selectedProposal] : []}
          onSelectionChange={(rows) => setSelectedProposal(rows[0] ?? null)}
          emptyText="Nenhuma proposta cadastrada"
          loading={loading}
        />
      </PageLayout>

      <ProposalFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        proposal={selectedProposal}
        onSuccess={() => {
          setIsFormOpen(false)
          void loadProposals()
        }}
      />
    </>
  )
}
