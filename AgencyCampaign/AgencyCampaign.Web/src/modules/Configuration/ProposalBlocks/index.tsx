import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import {
  proposalBlockService,
  type ProposalBlock,
} from '../../../services/proposalBlockService'
import ProposalBlockFormModal from '../../../components/modals/ProposalBlockFormModal'

export default function ProposalBlocks() {
  const { t } = useI18n()
  const [blocks, setBlocks] = useState<ProposalBlock[]>([])
  const [selected, setSelected] = useState<ProposalBlock | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)

  const { execute: fetchBlocks, loading } = useApi<ProposalBlock[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchBlocks(() => proposalBlockService.getAll(undefined, true))
    if (result) setBlocks(result)
  }

  useEffect(() => {
    void load()
  }, [])

  const handleDelete = async () => {
    if (!selected) return
    if (!window.confirm(`Excluir o bloco "${selected.name}"?`)) return
    const result = await runDelete(() => proposalBlockService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      void load()
    }
  }

  const columns: DataTableColumn<ProposalBlock>[] = [
    { key: 'name', title: 'Bloco', dataIndex: 'name' },
    { key: 'category', title: 'Categoria', dataIndex: 'category' },
    {
      key: 'body',
      title: 'Prévia do conteúdo',
      dataIndex: 'body',
      render: (value: string) => (
        <span className="line-clamp-2 text-xs text-muted-foreground">{value}</span>
      ),
    },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>{value ? 'Ativo' : 'Inativo'}</Badge>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title={t('configuration.proposalBlocks.title')}
        subtitle={t('configuration.proposalBlocks.subtitle')}
        onAdd={() => { setSelected(null); setIsFormOpen(true) }}
        onEdit={() => selected && setIsFormOpen(true)}
        onRefresh={() => void load()}
        selectedRowsCount={selected ? 1 : 0}
        actions={[
          {
            key: 'delete',
            label: 'Excluir',
            icon: <Trash2 className="h-4 w-4" />,
            variant: 'outline-danger',
            disabled: !selected || deleting,
            onClick: () => void handleDelete(),
          },
        ]}
      >
        <DataTable
          columns={columns}
          data={blocks}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          emptyText="Nenhum bloco cadastrado"
          loading={loading}
          pageSize={10}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <ProposalBlockFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        block={selected}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelected(null)
          void load()
        }}
      />
    </>
  )
}
