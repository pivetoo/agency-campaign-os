import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n } from 'archon-ui'
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
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [selected, setSelected] = useState<ProposalBlock | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)

  const { execute: fetchBlocks, loading, pagination } = useApi<ProposalBlock[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchBlocks(() => proposalBlockService.getAll({ page, pageSize, includeInactive: true }))
    if (result) setBlocks(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize])

  const handleDelete = async () => {
    if (!selected) return
    const result = await runDelete(() => proposalBlockService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      setIsConfirmOpen(false)
      void load()
    }
  }

  const columns: DataTableColumn<ProposalBlock>[] = [
    { key: 'name', title: t('configuration.proposalBlocks.field.block'), dataIndex: 'name' },
    { key: 'category', title: t('common.field.category'), dataIndex: 'category' },
    {
      key: 'body',
      title: t('configuration.proposalBlocks.field.contentPreview'),
      dataIndex: 'body',
      render: (value: string) => (
        <span className="line-clamp-2 text-xs text-muted-foreground">{value}</span>
      ),
    },
    {
      key: 'isActive',
      title: t('common.field.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>{value ? t('common.status.active') : t('common.status.inactive')}</Badge>
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
            label: t('common.action.delete'),
            testId: 'crud-delete-button',
            icon: <Trash2 className="h-4 w-4" />,
            variant: 'outline-danger',
            disabled: !selected || deleting,
            onClick: () => setIsConfirmOpen(true),
          },
        ]}
      >
        <DataTable
          columns={columns}
          data={blocks}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          emptyText={t('configuration.proposalBlocks.empty')}
          loading={loading}
          pageSize={pageSize}
          pageSizeOptions={[5, 10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
        />
      </PageLayout>

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        description={t('configuration.proposalBlocks.confirm.delete').replace('{0}', selected?.name ?? '')}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />

      <ProposalBlockFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        block={selected}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelected(null)
          if (page === 1) {
            void load()
          } else {
            setPage(1)
          }
        }}
      />
    </>
  )
}
