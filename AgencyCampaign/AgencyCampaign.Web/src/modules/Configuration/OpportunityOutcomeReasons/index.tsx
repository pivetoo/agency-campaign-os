import { useEffect, useState } from 'react'
import { Button, ConfirmModal, DataTable, PageLayout, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Plus, Trash2 } from 'lucide-react'
import { opportunityWinReasonService, opportunityLossReasonService } from '../../../services/opportunityOutcomeReasonService'
import type { OpportunityWinReason, OpportunityLossReason } from '../../../types/opportunityOutcomeReason'
import OpportunityOutcomeReasonFormModal from '../../../components/modals/OpportunityOutcomeReasonFormModal'

type AnyReason = OpportunityWinReason | OpportunityLossReason
type Kind = 'win' | 'loss'

interface Props {
  kind: Kind
}

const config: Record<Kind, { title: string; subtitle: string; addLabel: string }> = {
  win: { title: 'Motivos de ganho', subtitle: 'Por que ganhamos esta oportunidade? Cadastre as categorias.', addLabel: 'Novo motivo de ganho' },
  loss: { title: 'Motivos de perda', subtitle: 'Por que perdemos esta oportunidade? Cadastre as categorias.', addLabel: 'Novo motivo de perda' },
}

export default function OpportunityOutcomeReasons({ kind }: Props) {
  const cfg = config[kind]
  const service = kind === 'win' ? opportunityWinReasonService : opportunityLossReasonService
  const [items, setItems] = useState<AnyReason[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [selected, setSelected] = useState<AnyReason | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)
  const { execute: fetchAll, loading, pagination } = useApi<AnyReason[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchAll(() => service.getAll({ page, pageSize, includeInactive: true }))
    if (result) setItems(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, kind])

  useEffect(() => {
    setSelected(null)
  }, [kind])

  const handleDelete = async () => {
    if (!selected) return
    const result = await runDelete(() => service.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      setIsConfirmOpen(false)
      void load()
    }
  }

  const columns: DataTableColumn<AnyReason>[] = [
    {
      key: 'name',
      title: 'Motivo',
      dataIndex: 'name',
      render: (value: string, record) => (
        <span className="flex items-center gap-2">
          <span className="inline-block h-3 w-3 rounded-full" style={{ backgroundColor: record.color }} />
          {value}
        </span>
      ),
    },
    { key: 'displayOrder', title: 'Ordem', dataIndex: 'displayOrder' },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => value
        ? <span className="inline-flex items-center rounded bg-emerald-100 px-1.5 py-0.5 text-[11px] font-semibold uppercase tracking-wider text-emerald-800">Ativo</span>
        : <span className="inline-flex items-center rounded bg-muted px-1.5 py-0.5 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">Inativo</span>,
    },
  ]

  return (
    <>
      <PageLayout
        title={cfg.title}
        subtitle={cfg.subtitle}
        showDefaultActions={false}
        actionsSlot={(
          <div className="flex flex-wrap items-center gap-2">
            <Button size="sm" variant="outline" onClick={() => { setSelected(null); setIsFormOpen(true) }}>
              <Plus className="mr-1.5 h-4 w-4" /> {cfg.addLabel}
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
          emptyText="Nenhum motivo cadastrado."
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
        description={`Excluir o motivo "${selected?.name}"?`}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />

      <OpportunityOutcomeReasonFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        kind={kind}
        reason={selected}
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
