import { useEffect, useState } from 'react'
import { Badge, DataTable, PageLayout, TableToolbar, useApi, useI18n } from 'archon-ui'
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
  const { t } = useI18n()
  const [statuses, setStatuses] = useState<CampaignCreatorStatus[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [selectedStatus, setSelectedStatus] = useState<CampaignCreatorStatus | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchStatuses, loading, pagination } = useApi<CampaignCreatorStatus[]>({ showErrorMessage: true })

  const loadStatuses = async () => {
    const result = await fetchStatuses(() => campaignCreatorStatusService.getAll({ page, pageSize, search: debouncedSearch || undefined }))
    if (result) {
      setStatuses(result)
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
    void loadStatuses()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch])

  const columns: DataTableColumn<CampaignCreatorStatus>[] = [
    { key: 'name', title: t('common.field.name'), dataIndex: 'name' },
    { key: 'displayOrder', title: t('common.field.order'), dataIndex: 'displayOrder' },
    {
      key: 'color',
      title: t('common.field.color'),
      dataIndex: 'color',
      render: (value: string) => (
        <div className="flex items-center gap-2">
          <span className="h-4 w-4 rounded-full border" style={{ backgroundColor: value }} />
          <span>{value}</span>
        </div>
      ),
    },
    { key: 'category', title: t('common.field.category'), dataIndex: 'category', render: (value: number) => categoryLabels[value] || 'Em andamento' },
    { key: 'isInitial', title: t('configuration.creatorStatuses.field.initial'), dataIndex: 'isInitial', render: (value: boolean) => <Badge variant={value ? 'success' : 'outline'}>{value ? t('common.status.yes') : t('common.status.no')}</Badge> },
    { key: 'isActive', title: t('common.field.status'), dataIndex: 'isActive', render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? t('common.status.yes') : t('common.status.no')}</Badge> },
  ]

  return (
    <>
      <PageLayout
        title={t('configuration.creatorStatuses.title')}
        subtitle={t('configuration.creatorStatuses.subtitle')}
        onAdd={() => { setSelectedStatus(null); setIsFormOpen(true) }}
        onEdit={() => selectedStatus && setIsFormOpen(true)}
        onRefresh={() => void loadStatuses()}
        selectedRowsCount={selectedStatus ? 1 : 0}
        filtersSlot={
          <TableToolbar
            searchValue={search}
            onSearchChange={setSearch}
            searchPlaceholder={t('common.action.search')}
          />
        }
      >
        <DataTable
          columns={columns}
          data={statuses}
          rowKey="id"
          selectedRows={selectedStatus ? [selectedStatus] : []}
          onSelectionChange={(rows) => setSelectedStatus(rows[0] ?? null)}
          emptyText={t('configuration.creatorStatuses.empty')}
          loading={loading}
          pageSize={pageSize}
          pageSizeOptions={[5, 10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
        />
      </PageLayout>

      <CampaignCreatorStatusFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        status={selectedStatus}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedStatus(null)
          if (page === 1) {
            void loadStatuses()
          } else {
            setPage(1)
          }
        }}
      />
    </>
  )
}
