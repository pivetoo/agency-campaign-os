import { useEffect, useState } from 'react'
import { Button, ConfirmModal, DataTable, PageLayout, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Plus, Trash2 } from 'lucide-react'
import { commercialGoalService } from '../../../services/commercialGoalService'
import { commercialGoalPeriodTypeLabels, type CommercialGoal } from '../../../types/commercialGoal'
import { formatCurrency, formatDate } from '../../../lib/format'
import CommercialGoalFormModal from '../../../components/modals/CommercialGoalFormModal'

export default function CommercialGoals() {
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
      title: 'Escopo',
      dataIndex: 'userId',
      render: (_, record) => record.userId
        ? <span className="text-sm text-foreground">{record.userName ?? `Usuário #${record.userId}`}</span>
        : <span className="text-sm font-semibold text-primary">Agência</span>,
    },
    {
      key: 'periodType',
      title: 'Período',
      dataIndex: 'periodType',
      render: (value: number) => commercialGoalPeriodTypeLabels[value as 1 | 2 | 3] ?? '-',
    },
    {
      key: 'periodStart',
      title: 'Início',
      dataIndex: 'periodStart',
      render: (value: string) => formatDate(value),
    },
    {
      key: 'periodEnd',
      title: 'Fim',
      dataIndex: 'periodEnd',
      render: (value: string) => formatDate(value),
    },
    {
      key: 'targetAmount',
      title: 'Meta',
      dataIndex: 'targetAmount',
      render: (value: number) => <span className="font-mono">{formatCurrency(value)}</span>,
    },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => value
        ? <span className="inline-flex items-center rounded bg-emerald-100 px-1.5 py-0.5 text-[11px] font-semibold uppercase tracking-wider text-emerald-800">Ativa</span>
        : <span className="inline-flex items-center rounded bg-muted px-1.5 py-0.5 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">Inativa</span>,
    },
  ]

  return (
    <>
      <PageLayout
        title="Metas comerciais"
        subtitle="Defina a meta financeira por vendedor ou pela agência inteira."
        showDefaultActions={false}
        actionsSlot={(
          <div className="flex flex-wrap items-center gap-2">
            <Button size="sm" variant="outline" onClick={() => { setSelected(null); setIsFormOpen(true) }}>
              <Plus className="mr-1.5 h-4 w-4" /> Nova meta
            </Button>
            {selected && (
              <>
                <Button size="sm" variant="outline" onClick={() => setIsFormOpen(true)}>Editar</Button>
                <Button size="sm" variant="ghost" disabled={deleting} onClick={() => setIsConfirmOpen(true)} className="text-muted-foreground hover:text-destructive">
                  <Trash2 className="h-4 w-4" />
                </Button>
              </>
            )}
          </div>
        )}
        onRefresh={() => void load()}
      >
        <DataTable
          columns={columns}
          data={items}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          emptyText="Nenhuma meta cadastrada. Crie uma meta de agência ou por vendedor para acompanhar o progresso no Pipeline."
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
        description={`Excluir a meta de ${selected?.userId ? selected.userName ?? `vendedor #${selected.userId}` : 'agência'} (${commercialGoalPeriodTypeLabels[selected?.periodType as 1 | 2 | 3] ?? ''} · ${formatDate(selected?.periodStart)})?`}
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
