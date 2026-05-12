import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import { opportunitySourceService } from '../../../services/opportunitySourceService'
import type { OpportunitySource } from '../../../types/opportunitySource'
import { OpportunitySourceFormModal } from '../../../components/modals/OpportunitySourceFormModal'

export default function OpportunitySources() {
  const { t } = useI18n()
  const [items, setItems] = useState<OpportunitySource[]>([])
  const [selected, setSelected] = useState<OpportunitySource | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)
  const { execute: fetchAll, loading } = useApi<OpportunitySource[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchAll(() => opportunitySourceService.getAll(true))
    if (result) setItems(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleDelete = async () => {
    if (!selected) return
    const result = await runDelete(() => opportunitySourceService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      setIsConfirmOpen(false)
      void load()
    }
  }

  const columns: DataTableColumn<OpportunitySource>[] = [
    {
      key: 'name',
      title: t('configuration.opportunitySources.field.source'),
      dataIndex: 'name',
      render: (value: string, record: OpportunitySource) => (
        <span className="flex items-center gap-2">
          <span className="inline-block h-3 w-3 rounded-full" style={{ backgroundColor: record.color }} />
          {value}
        </span>
      ),
    },
    { key: 'displayOrder', title: t('common.field.order'), dataIndex: 'displayOrder' },
    {
      key: 'isActive',
      title: t('common.field.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>{value ? t('common.status.activeFemale') : t('common.status.inactiveFemale')}</Badge>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title={t('configuration.opportunitySources.title')}
        subtitle={t('configuration.opportunitySources.subtitle')}
        onAdd={() => { setSelected(null); setIsFormOpen(true) }}
        onEdit={() => selected && setIsFormOpen(true)}
        onRefresh={() => void load()}
        selectedRowsCount={selected ? 1 : 0}
        actions={[
          {
            key: 'delete',
            label: t('common.action.delete'),
            icon: <Trash2 className="h-4 w-4" />,
            variant: 'outline-danger',
            disabled: !selected || deleting,
            onClick: () => setIsConfirmOpen(true),
          },
        ]}
      >
        <DataTable
          columns={columns}
          data={items}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          emptyText={t('configuration.opportunitySources.empty')}
          loading={loading}
          pageSize={10}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        description={t('configuration.opportunitySources.confirm.delete').replace('{0}', selected?.name ?? '')}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />

      <OpportunitySourceFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        source={selected}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelected(null)
          void load()
        }}
      />
    </>
  )
}
