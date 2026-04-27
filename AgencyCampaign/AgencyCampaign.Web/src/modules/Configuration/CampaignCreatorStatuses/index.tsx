import { useEffect, useState } from 'react'
import { Badge, DataTable, PageLayout, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import CampaignCreatorStatusFormModal from '../../../components/modals/CampaignCreatorStatusFormModal'
import { campaignCreatorStatusService } from '../../../services/campaignCreatorStatusService'
import type { CampaignCreatorStatus } from '../../../types/campaignCreatorStatus'

const categoryLabels: Record<number, string> = {
  0: 'Em andamento',
  1: 'Sucesso',
  2: 'Falha',
}

export default function CampaignCreatorStatuses() {
  const [statuses, setStatuses] = useState<CampaignCreatorStatus[]>([])
  const [selectedStatus, setSelectedStatus] = useState<CampaignCreatorStatus | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchStatuses, loading } = useApi<CampaignCreatorStatus[]>({ showErrorMessage: true })

  const loadStatuses = async () => {
    const result = await fetchStatuses(() => campaignCreatorStatusService.getAll())
    if (result) {
      setStatuses(result)
    }
  }

  useEffect(() => {
    void loadStatuses()
  }, [])

  const columns: DataTableColumn<CampaignCreatorStatus>[] = [
    { key: 'name', title: 'Status', dataIndex: 'name' },
    { key: 'displayOrder', title: 'Ordem', dataIndex: 'displayOrder' },
    {
      key: 'color',
      title: 'Cor',
      dataIndex: 'color',
      render: (value: string) => (
        <div className="flex items-center gap-2">
          <span className="h-4 w-4 rounded-full border" style={{ backgroundColor: value }} />
          <span>{value}</span>
        </div>
      ),
    },
    { key: 'category', title: 'Categoria', dataIndex: 'category', render: (value: number) => categoryLabels[value] || 'Em andamento' },
    { key: 'isInitial', title: 'Inicial', dataIndex: 'isInitial', render: (value: boolean) => <Badge variant={value ? 'success' : 'outline'}>{value ? 'Sim' : 'Não'}</Badge> },
    { key: 'isActive', title: 'Ativo', dataIndex: 'isActive', render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? 'Sim' : 'Não'}</Badge> },
  ]

  return (
    <>
      <PageLayout
        title="Status dos Creators"
        subtitle="Configure os status dos creators vinculados às campanhas"
        onAdd={() => { setSelectedStatus(null); setIsFormOpen(true) }}
        onEdit={() => selectedStatus && setIsFormOpen(true)}
        onRefresh={() => void loadStatuses()}
        selectedRowsCount={selectedStatus ? 1 : 0}
      >
        <DataTable
          columns={columns}
          data={statuses}
          rowKey="id"
          selectedRows={selectedStatus ? [selectedStatus] : []}
          onSelectionChange={(rows) => setSelectedStatus(rows[0] ?? null)}
          emptyText="Nenhum status configurado"
          loading={loading}
          pageSize={5}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <CampaignCreatorStatusFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        status={selectedStatus}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedStatus(null)
          void loadStatuses()
        }}
      />
    </>
  )
}
