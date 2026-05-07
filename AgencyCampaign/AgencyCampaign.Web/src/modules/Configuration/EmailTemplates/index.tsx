import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import { emailTemplateService } from '../../../services/emailTemplateService'
import {
  emailEventTypeLabels,
  type EmailEventTypeValue,
  type EmailTemplate,
} from '../../../types/emailTemplate'
import EmailTemplateFormModal from '../../../components/modals/EmailTemplateFormModal'

export default function EmailTemplates() {
  const [items, setItems] = useState<EmailTemplate[]>([])
  const [selected, setSelected] = useState<EmailTemplate | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchAll, loading } = useApi<EmailTemplate[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({
    showSuccessMessage: true,
    showErrorMessage: true,
  })

  const load = async () => {
    const result = await fetchAll(() => emailTemplateService.getAll(true))
    if (result) setItems(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleDelete = async () => {
    if (!selected) return
    if (!window.confirm(`Excluir o template "${selected.name}"?`)) return
    const result = await runDelete(() => emailTemplateService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      void load()
    }
  }

  const columns: DataTableColumn<EmailTemplate>[] = [
    { key: 'name', title: 'Nome', dataIndex: 'name' },
    {
      key: 'eventType',
      title: 'Evento',
      dataIndex: 'eventType',
      render: (value: EmailEventTypeValue) => (
        <Badge variant="outline">{emailEventTypeLabels[value]}</Badge>
      ),
    },
    {
      key: 'subject',
      title: 'Assunto',
      dataIndex: 'subject',
      render: (value: string) => <span className="text-sm text-muted-foreground">{value}</span>,
    },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>{value ? 'Ativo' : 'Inativo'}</Badge>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title="Templates de e-mail"
        subtitle="Modelos disparados automaticamente em eventos de proposta e oportunidade."
        onAdd={() => {
          setSelected(null)
          setIsFormOpen(true)
        }}
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
          emptyText="Nenhum template cadastrado"
          loading={loading}
          pageSize={10}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <EmailTemplateFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        template={selected}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelected(null)
          void load()
        }}
      />
    </>
  )
}
