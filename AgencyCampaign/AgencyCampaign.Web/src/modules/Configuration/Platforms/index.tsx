import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { platformService } from '../../../services/platformService'
import type { Platform } from '../../../types/platform'
import PlatformFormModal from '../../../components/modals/PlatformFormModal'

export default function Platforms() {
  const [platforms, setPlatforms] = useState<Platform[]>([])
  const [selectedPlatform, setSelectedPlatform] = useState<Platform | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchPlatforms, loading } = useApi<Platform[]>({ showErrorMessage: true })

  const loadPlatforms = async () => {
    const result = await fetchPlatforms(() => platformService.getAll())
    if (result) {
      setPlatforms(result)
    }
  }

  useEffect(() => {
    void loadPlatforms()
  }, [])

  const columns: DataTableColumn<Platform>[] = [
    { key: 'name', title: 'Plataforma', dataIndex: 'name' },
    { key: 'displayOrder', title: 'Ordem', dataIndex: 'displayOrder' },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? 'Ativa' : 'Inativa'}</Badge>,
    },
  ]

  return (
    <>
      <PageLayout
        title="Plataformas"
        subtitle="Cadastre e organize as plataformas disponíveis para entregas"
        onAdd={() => { setSelectedPlatform(null); setIsFormOpen(true) }}
        onEdit={() => selectedPlatform && setIsFormOpen(true)}
        onRefresh={() => void loadPlatforms()}
        selectedRowsCount={selectedPlatform ? 1 : 0}
      >
        <DataTable
          columns={columns}
          data={platforms}
          rowKey="id"
          selectedRows={selectedPlatform ? [selectedPlatform] : []}
          onSelectionChange={(rows) => setSelectedPlatform(rows[0] ?? null)}
          emptyText="Nenhuma plataforma cadastrada"
          loading={loading}
          pageSize={10}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <PlatformFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        platform={selectedPlatform}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedPlatform(null)
          void loadPlatforms()
        }}
      />
    </>
  )
}
