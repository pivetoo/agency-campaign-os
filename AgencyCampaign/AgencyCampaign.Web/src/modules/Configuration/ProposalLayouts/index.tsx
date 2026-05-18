import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import { agencySettingsService, type ProposalTemplateVersion } from '../../../services/agencySettingsService'
import AuditUtilityBar from '../../../components/buttons/AuditUtilityBar'
import { formatDate } from '../../../lib/format'

export default function ProposalLayouts() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [items, setItems] = useState<ProposalTemplateVersion[]>([])
  const [selected, setSelected] = useState<ProposalTemplateVersion | null>(null)
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false)

  const { execute: fetchAll, loading } = useApi<ProposalTemplateVersion[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchAll(() => agencySettingsService.getProposalTemplateVersions())
    if (result) setItems(result)
  }

  useEffect(() => { void load() }, [])

  const handleDelete = async () => {
    if (!selected) return
    const result = await runDelete(() => agencySettingsService.deleteProposalTemplateVersion(selected.id))
    if (result !== null) {
      setSelected(null)
      setConfirmDeleteOpen(false)
      void load()
    }
  }

  const columns: DataTableColumn<ProposalTemplateVersion>[] = [
    { key: 'name', title: t('common.field.name'), dataIndex: 'name' },
    {
      key: 'isActive',
      title: 'Padrão',
      dataIndex: 'isActive',
      render: (value: boolean) => value ? <Badge variant="success">Padrão</Badge> : <span className="text-xs text-muted-foreground">—</span>,
    },
    {
      key: 'createdAt',
      title: 'Criado em',
      dataIndex: 'createdAt',
      render: (value: string) => <span className="text-sm text-muted-foreground">{formatDate(value)}</span>,
    },
  ]

  return (
    <>
      <PageLayout
        title="Layouts da proposta"
        subtitle="Modelos de PDF reutilizáveis. Um é marcado como padrão; o vendedor pode escolher outro por proposta."
        onAdd={() => navigate('/configuracao/layouts-proposta/novo')}
        onEdit={() => selected && navigate(`/configuracao/layouts-proposta/${selected.id}`)}
        onRefresh={() => void load()}
        actionsSlot={<AuditUtilityBar entityName="ProposalTemplateVersion" entityLabel="Layout da proposta" entityId={selected?.id ?? null} />}
        addLabel="Novo layout"
        selectedRowsCount={selected ? 1 : 0}
        actions={[
          {
            key: 'delete',
            label: t('common.action.delete'),
            testId: 'crud-delete-button',
            icon: <Trash2 className="h-4 w-4" />,
            variant: 'ghost',
            disabled: !selected || deleting,
            onClick: () => setConfirmDeleteOpen(true),
          },
        ]}
      >
        <DataTable
          columns={columns}
          data={items}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          onRowDoubleClick={(row) => navigate(`/configuracao/layouts-proposta/${row.id}`)}
          emptyText="Nenhum layout cadastrado."
          loading={loading}
        />
      </PageLayout>

      <ConfirmModal
        open={confirmDeleteOpen}
        onOpenChange={setConfirmDeleteOpen}
        description={`Excluir o layout "${selected?.name ?? ''}"? Propostas que usavam este layout voltam ao padrão da agência.`}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />
    </>
  )
}
