import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { deliverableKindService } from '../../../services/deliverableKindService'
import type { DeliverableKind } from '../../../types/deliverableKind'
import DeliverableKindFormModal from '../../../components/modals/DeliverableKindFormModal'

export default function DeliverableKinds() {
  const [deliverableKinds, setDeliverableKinds] = useState<DeliverableKind[]>([])
  const [selectedDeliverableKind, setSelectedDeliverableKind] = useState<DeliverableKind | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchDeliverableKinds, loading } = useApi<DeliverableKind[]>({ showErrorMessage: true })

  const loadDeliverableKinds = async () => {
    const result = await fetchDeliverableKinds(() => deliverableKindService.getAll())
    if (result) {
      setDeliverableKinds(result)
    }
  }

  useEffect(() => {
    void loadDeliverableKinds()
  }, [])

  const columns: DataTableColumn<DeliverableKind>[] = [
    { key: 'name', title: 'Tipo de entrega', dataIndex: 'name' },
    { key: 'displayOrder', title: 'Ordem', dataIndex: 'displayOrder' },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? 'Ativo' : 'Inativo'}</Badge>,
    },
  ]

  return (
    <>
      <PageLayout
        title="Tipos de entrega"
        subtitle="Cadastre e organize os tipos de entrega disponíveis para campanhas"
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
          emptyText="Nenhum tipo de entrega cadastrado"
          loading={loading}
        />
      </PageLayout>

      <DeliverableKindFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        deliverableKind={selectedDeliverableKind}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedDeliverableKind(null)
          void loadDeliverableKinds()
        }}
      />
    </>
  )
}
