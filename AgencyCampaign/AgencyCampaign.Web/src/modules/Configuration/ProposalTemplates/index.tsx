import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import {
  proposalTemplateService,
  type ProposalTemplate,
} from '../../../services/proposalTemplateService'
import ProposalTemplateFormModal from '../../../components/modals/ProposalTemplateFormModal'

export default function ProposalTemplates() {
  const { t } = useI18n()
  const [templates, setTemplates] = useState<ProposalTemplate[]>([])
  const [selected, setSelected] = useState<ProposalTemplate | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)

  const { execute: fetchTemplates, loading } = useApi<ProposalTemplate[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchTemplates(() => proposalTemplateService.getAll(true))
    if (result) setTemplates(result)
  }

  useEffect(() => {
    void load()
  }, [])

  const handleDelete = async () => {
    if (!selected) return
    if (!window.confirm(`Excluir o template "${selected.name}"? Os itens já copiados em propostas continuarão lá.`)) return
    const result = await runDelete(() => proposalTemplateService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      void load()
    }
  }

  const columns: DataTableColumn<ProposalTemplate>[] = [
    { key: 'name', title: 'Template', dataIndex: 'name' },
    {
      key: 'description',
      title: 'Descrição',
      dataIndex: 'description',
      render: (value?: string) => value || '-',
    },
    {
      key: 'items',
      title: 'Itens',
      dataIndex: 'items',
      render: (value?: ProposalTemplate['items']) => value?.length ?? 0,
    },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>{value ? 'Ativo' : 'Inativo'}</Badge>
      ),
    },
    {
      key: 'createdByUserName',
      title: 'Criado por',
      dataIndex: 'createdByUserName',
      render: (value?: string) => value || '-',
    },
  ]

  return (
    <>
      <PageLayout
        title={t('configuration.proposalTemplates.title')}
        subtitle={t('configuration.proposalTemplates.subtitle')}
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
          data={templates}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          emptyText="Nenhum template cadastrado"
          loading={loading}
          pageSize={10}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <ProposalTemplateFormModal
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
