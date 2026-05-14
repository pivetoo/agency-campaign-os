import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, TableToolbar, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { platformService } from '../../../services/platformService'
import type { Platform } from '../../../types/platform'
import PlatformFormModal from '../../../components/modals/PlatformFormModal'

export default function Platforms() {
  const { t } = useI18n()
  const [platforms, setPlatforms] = useState<Platform[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [selectedPlatform, setSelectedPlatform] = useState<Platform | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchPlatforms, loading, pagination } = useApi<Platform[]>({ showErrorMessage: true })

  const loadPlatforms = async () => {
    const result = await fetchPlatforms(() => platformService.getAll({ page, pageSize, search: debouncedSearch || undefined }))
    if (result) {
      setPlatforms(result)
    }
  }

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(timeout)
  }, [search])

  useEffect(() => {
    setPage(1)
  }, [debouncedSearch])

  useEffect(() => {
    void loadPlatforms()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch])

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
        <TableToolbar
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder={t('common.action.search')}
          className="mb-3"
        />
        <DataTable
          columns={columns}
          data={platforms}
          rowKey="id"
          selectedRows={selectedPlatform ? [selectedPlatform] : []}
          onSelectionChange={(rows) => setSelectedPlatform(rows[0] ?? null)}
          emptyText={t('configuration.platforms.empty')}
          loading={loading}
          pageSize={pageSize}
          pageSizeOptions={[5, 10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
        />
      </PageLayout>

      <PlatformFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        platform={selectedPlatform}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedPlatform(null)
          if (page === 1) {
            void loadPlatforms()
          } else {
            setPage(1)
          }
        }}
      />
    </>
  )
}
