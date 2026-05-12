import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { platformService } from '../../../services/platformService'
import type { Platform } from '../../../types/platform'
import PlatformFormModal from '../../../components/modals/PlatformFormModal'

export default function Platforms() {
  const { t } = useI18n()
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
    { key: 'name', title: t('common.field.platform'), dataIndex: 'name' },
    { key: 'displayOrder', title: t('common.field.order'), dataIndex: 'displayOrder' },
    {
      key: 'isActive',
      title: t('common.field.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? t('common.status.activeFemale') : t('common.status.inactiveFemale')}</Badge>,
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
          emptyText={t('configuration.platforms.empty')}
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
