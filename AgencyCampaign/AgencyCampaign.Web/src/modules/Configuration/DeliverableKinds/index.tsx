import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { deliverableKindService } from '../../../services/deliverableKindService'
import type { DeliverableKind } from '../../../types/deliverableKind'
import DeliverableKindFormModal from '../../../components/modals/DeliverableKindFormModal'

export default function DeliverableKinds() {
  const { t } = useI18n()
  const [deliverableKinds, setDeliverableKinds] = useState<DeliverableKind[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [selectedDeliverableKind, setSelectedDeliverableKind] = useState<DeliverableKind | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchDeliverableKinds, loading, pagination } = useApi<DeliverableKind[]>({ showErrorMessage: true })

  const loadDeliverableKinds = async () => {
    const result = await fetchDeliverableKinds(() => deliverableKindService.getAll({ page, pageSize }))
    if (result) {
      setDeliverableKinds(result)
    }
  }

  useEffect(() => {
    void loadDeliverableKinds()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize])

  const columns: DataTableColumn<DeliverableKind>[] = [
    { key: 'name', title: t('configuration.deliverableKinds.field.type'), dataIndex: 'name' },
    { key: 'displayOrder', title: t('common.field.order'), dataIndex: 'displayOrder' },
    {
      key: 'isActive',
      title: t('common.field.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? t('common.status.active') : t('common.status.inactive')}</Badge>,
    },
  ]

  return (
    <>
      <PageLayout
        title={t('configuration.deliverableKinds.title')}
        subtitle={t('configuration.deliverableKinds.subtitle')}
        onAdd={() => { setSelectedDeliverableKind(null); setIsFormOpen(true) }}
        onEdit={() => selectedDeliverableKind && setIsFormOpen(true)}
        onRefresh={() => void loadDeliverableKinds()}
        selectedRowsCount={selectedDeliverableKind ? 1 : 0}
      >
        <DataTable
          columns={columns}
          data={deliverableKinds}
          rowKey="id"
          selectedRows={selectedDeliverableKind ? [selectedDeliverableKind] : []}
          onSelectionChange={(rows) => setSelectedDeliverableKind(rows[0] ?? null)}
          emptyText={t('configuration.deliverableKinds.empty')}
          loading={loading}
          pageSize={pageSize}
          pageSizeOptions={[5, 10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
        />
      </PageLayout>

      <DeliverableKindFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        deliverableKind={selectedDeliverableKind}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedDeliverableKind(null)
          if (page === 1) {
            void loadDeliverableKinds()
          } else {
            setPage(1)
          }
        }}
      />
    </>
  )
}
