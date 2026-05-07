import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import { financialSubcategoryService } from '../../../services/financialSubcategoryService'
import { financialEntryCategoryLabels } from '../../../types/financialEntry'
import type { FinancialSubcategory } from '../../../types/financialSubcategory'
import FinancialSubcategoryFormModal from '../../../components/modals/FinancialSubcategoryFormModal'

export default function FinancialSubcategories() {
  const [items, setItems] = useState<FinancialSubcategory[]>([])
  const [selected, setSelected] = useState<FinancialSubcategory | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchAll, loading } = useApi<FinancialSubcategory[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchAll(() => financialSubcategoryService.getAll(true))
    if (result) setItems(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleDelete = async () => {
    if (!selected) return
    if (!window.confirm(`Excluir a subcategoria "${selected.name}"?`)) return
    const result = await runDelete(() => financialSubcategoryService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      void load()
    }
  }

  const columns: DataTableColumn<FinancialSubcategory>[] = [
    {
      key: 'name',
      title: 'Subcategoria',
      dataIndex: 'name',
      render: (value: string, record) => (
        <span
          className="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
          style={{ backgroundColor: `${record.color}25`, color: record.color }}
        >
          {value}
        </span>
      ),
    },
    {
      key: 'macroCategory',
      title: 'Categoria macro',
      dataIndex: 'macroCategory',
      render: (value: number) => financialEntryCategoryLabels[value] || '-',
    },
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
        title="Subcategorias financeiras"
        subtitle="Subcategorias customizadas para detalhar lançamentos (ex.: Patrocínio anual, Mensalidade SaaS)."
        onAdd={() => { setSelected(null); setIsFormOpen(true) }}
        onEdit={() => selected && setIsFormOpen(true)}
        onRefresh={() => void load()}
        selectedRowsCount={selected ? 1 : 0}
        actions={[
          {
            key: 'delete',
            label: 'Excluir',
            icon: <Trash2 className="h-4 w-4" />,
            variant: 'outline-danger',
            disabled: !selected || deleting,
            onClick: () => void handleDelete(),
          },
        ]}
      >
        <DataTable
          columns={columns}
          data={items}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          emptyText="Nenhuma subcategoria cadastrada"
          loading={loading}
          pageSize={10}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <FinancialSubcategoryFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        subcategory={selected}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelected(null)
          void load()
        }}
      />
    </>
  )
}
