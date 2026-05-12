import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import { opportunityTagService } from '../../../services/opportunitySourceService'
import type { OpportunityTag } from '../../../types/opportunitySource'
import { OpportunityTagFormModal } from '../../../components/modals/OpportunitySourceFormModal'

export default function OpportunityTags() {
  const { t } = useI18n()
  const [items, setItems] = useState<OpportunityTag[]>([])
  const [selected, setSelected] = useState<OpportunityTag | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchAll, loading } = useApi<OpportunityTag[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchAll(() => opportunityTagService.getAll(true))
    if (result) setItems(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleDelete = async () => {
    if (!selected) return
    if (!window.confirm(t('configuration.opportunityTags.confirm.delete').replace('{0}', selected.name))) return
    const result = await runDelete(() => opportunityTagService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      void load()
    }
  }

  const columns: DataTableColumn<OpportunityTag>[] = [
    {
      key: 'name',
      title: t('configuration.opportunityTags.field.tag'),
      dataIndex: 'name',
      render: (value: string, record: OpportunityTag) => (
        <span
          className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium"
          style={{ backgroundColor: `${record.color}25`, color: record.color }}
        >
          {value}
        </span>
      ),
    },
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
        title={t('configuration.opportunityTags.title')}
        subtitle={t('configuration.opportunityTags.subtitle')}
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
            onClick: () => void handleDelete(),
          },
        ]}
      >
        <DataTable
          columns={columns}
          data={items}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          emptyText={t('configuration.opportunityTags.empty')}
          loading={loading}
          pageSize={10}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <OpportunityTagFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        tag={selected}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelected(null)
          void load()
        }}
      />
    </>
  )
}
