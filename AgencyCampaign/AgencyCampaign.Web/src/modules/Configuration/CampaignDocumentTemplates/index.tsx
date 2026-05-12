import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import { campaignDocumentTemplateService } from '../../../services/campaignDocumentTemplateService'
import {
  campaignDocumentTypeLabels,
  type CampaignDocumentTypeValue,
} from '../../../types/campaignDocument'
import type { CampaignDocumentTemplate } from '../../../types/campaignDocumentTemplate'
import CampaignDocumentTemplateFormModal from '../../../components/modals/CampaignDocumentTemplateFormModal'

export default function CampaignDocumentTemplates() {
  const { t } = useI18n()
  const [items, setItems] = useState<CampaignDocumentTemplate[]>([])
  const [selected, setSelected] = useState<CampaignDocumentTemplate | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const { execute: fetchAll, loading, pagination } = useApi<CampaignDocumentTemplate[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({
    showSuccessMessage: true,
    showErrorMessage: true,
  })

  const load = async () => {
    const result = await fetchAll(() => campaignDocumentTemplateService.getAll({ page, pageSize }))
    if (result) setItems(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize])

  const handleDelete = async () => {
    if (!selected) return
    if (!window.confirm(t('configuration.contractTemplates.confirm.delete').replace('{0}', selected.name))) return
    const result = await runDelete(() => campaignDocumentTemplateService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      void load()
    }
  }

  const columns: DataTableColumn<CampaignDocumentTemplate>[] = [
    { key: 'name', title: t('common.field.name'), dataIndex: 'name' },
    {
      key: 'documentType',
      title: t('common.field.type'),
      dataIndex: 'documentType',
      render: (value: CampaignDocumentTypeValue) => (
        <Badge variant="outline">{campaignDocumentTypeLabels[value]}</Badge>
      ),
    },
    {
      key: 'description',
      title: t('common.field.description'),
      dataIndex: 'description',
      render: (value: string | undefined) => (
        <span className="text-sm text-muted-foreground">{value ?? '—'}</span>
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
        title={t('configuration.contractTemplates.title')}
        subtitle={t('configuration.contractTemplates.subtitle')}
        onAdd={() => {
          setSelected(null)
          setIsFormOpen(true)
        }}
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
          emptyText={t('configuration.contractTemplates.empty')}
          loading={loading}
          pageSize={pageSize}
          pageSizeOptions={[5, 10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
        />
      </PageLayout>

      <CampaignDocumentTemplateFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        template={selected}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelected(null)
          void load()
        }}
      />
    </>
  )
}
