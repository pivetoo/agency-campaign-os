import { useEffect, useState } from 'react'
import { ConfirmModal, DataTable, PageLayout, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { commercialGoalService } from '../../../services/commercialGoalService'
import { commercialGoalPeriodTypeLabels, type CommercialGoal } from '../../../types/commercialGoal'
import { formatCurrency, formatDate } from '../../../lib/format'
import CommercialGoalFormModal from '../../../components/modals/CommercialGoalFormModal'

export default function CommercialGoals() {
  const { t } = useI18n()
  const [items, setItems] = useState<CommercialGoal[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [selected, setSelected] = useState<CommercialGoal | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)
  const { execute: fetchAll, loading, pagination } = useApi<CommercialGoal[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchAll(() => commercialGoalService.getAll({ page, pageSize, includeInactive: true }))
    if (result) setItems(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize])

  const handleDelete = async () => {
    if (!selected) return
    const result = await runDelete(() => commercialGoalService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      setIsConfirmOpen(false)
      void load()
    }
  }

  const columns: DataTableColumn<CommercialGoal>[] = [
    {
      key: 'scope',
      title: t('commercialGoals.colScope'),
      dataIndex: 'userId',
      render: (_, record) => record.userId
        ? <span className="text-sm text-foreground">{record.userName ?? t('commercialGoals.userFallback').replace('{0}', String(record.userId))}</span>
        : <span className="text-sm font-semibold text-primary">{t('commercialGoals.agency')}</span>,
    },
    {
      key: 'periodType',
      title: t('commercialGoals.colPeriod'),
      dataIndex: 'periodType',
      render: (value: number) => commercialGoalPeriodTypeLabels[value as 1 | 2 | 3] ?? '-',
    },
    {
      key: 'periodStart',
      title: t('commercialGoals.colStart'),
      dataIndex: 'periodStart',
      render: (value: string) => formatDate(value),
    },
    {
      key: 'periodEnd',
      title: t('commercialGoals.colEnd'),
      dataIndex: 'periodEnd',
      render: (value: string) => formatDate(value),
    },
    {
      key: 'targetAmount',
      title: t('commercialGoals.colTarget'),
      dataIndex: 'targetAmount',
      render: (value: number) => <span className="font-mono">{formatCurrency(value)}</span>,
    },
    {
      key: 'isActive',
      title: t('commercialGoals.colStatus'),
      dataIndex: 'isActive',
      render: (value: boolean) => value
        ? <span className="inline-flex items-center rounded bg-emerald-100 px-1.5 py-0.5 text-[11px] font-semibold uppercase tracking-wider text-emerald-800">{t('commercialGoals.active')}</span>
        : <span className="inline-flex items-center rounded bg-muted px-1.5 py-0.5 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">{t('commercialGoals.inactive')}</span>,
    },
  ]

  return (
    <>
      <PageLayout
        title={t('commercialGoals.title')}
        subtitle={t('commercialGoals.subtitle')}
        onAdd={() => { setSelected(null); setIsFormOpen(true) }}
        onEdit={() => selected && setIsFormOpen(true)}
        onDelete={() => selected && setIsConfirmOpen(true)}
        addLabel={t('commercialGoals.addLabel')}
        onRefresh={() => void load()}
        selectedRowsCount={selected ? 1 : 0}
      >
        <DataTable
          columns={columns}
          data={items}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          emptyText={t('commercialGoals.empty')}
          loading={loading}
          pageSize={pageSize}
          pageSizeOptions={[5, 10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
        />
      </PageLayout>

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        description={t('commercialGoals.deleteConfirm')
          .replace('{0}', selected?.userId ? selected.userName ?? t('commercialGoals.sellerFallback').replace('{0}', String(selected.userId)) : t('commercialGoals.agency'))
          .replace('{1}', commercialGoalPeriodTypeLabels[selected?.periodType as 1 | 2 | 3] ?? '')
          .replace('{2}', formatDate(selected?.periodStart))}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />

      <CommercialGoalFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        goal={selected}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelected(null)
          if (page === 1) {
            void load()
          } else {
            setPage(1)
          }
        }}
      />
    </>
  )
}
