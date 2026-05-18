import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Trash2 } from 'lucide-react'
import { campaignDocumentTemplateService } from '../../../services/campaignDocumentTemplateService'
import { CampaignDocumentType, campaignDocumentTypeLabels, type CampaignDocumentTypeValue } from '../../../types/campaignDocument'

const documentTypeBadgeClass: Record<CampaignDocumentTypeValue, string> = {
  [CampaignDocumentType.CreatorAgreement]: 'border-transparent bg-blue-500/15 text-blue-700',
  [CampaignDocumentType.BrandContract]: 'border-transparent bg-primary/15 text-primary',
  [CampaignDocumentType.AuthorizationTerm]: 'border-transparent bg-amber-500/15 text-amber-700',
  [CampaignDocumentType.BriefingAttachment]: 'border-transparent bg-emerald-500/15 text-emerald-700',
  [CampaignDocumentType.Other]: 'border-transparent bg-muted text-muted-foreground',
}
import type { CampaignDocumentTemplate } from '../../../types/campaignDocumentTemplate'
import AuditUtilityBar from '../../../components/buttons/AuditUtilityBar'

export default function CampaignDocumentTemplates() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [items, setItems] = useState<CampaignDocumentTemplate[]>([])
  const [selected, setSelected] = useState<CampaignDocumentTemplate | null>(null)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const { execute: fetchAll, loading, pagination } = useApi<CampaignDocumentTemplate[]>({ showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi<unknown>({
    showSuccessMessage: true,
    showErrorMessage: true,
  })

  const load = async () => {
    const result = await fetchAll(() => campaignDocumentTemplateService.getAll({ page, pageSize }))
    if (result) setItems(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize])

  const handleDelete = async () => {
    if (!selected) return
    const result = await runDelete(() => campaignDocumentTemplateService.delete(selected.id))
    if (result !== null) {
      setSelected(null)
      setIsConfirmOpen(false)
      void load()
    }
  }

  const columns: DataTableColumn<CampaignDocumentTemplate>[] = [
    { key: 'name', title: t('common.field.name'), dataIndex: 'name' },
    {
      key: 'documentType',
      title: t('common.field.type'),
      dataIndex: 'documentType',
      render: (value: CampaignDocumentTypeValue) => (
        <Badge className={documentTypeBadgeClass[value]}>{campaignDocumentTypeLabels[value]}</Badge>
      ),
    },
    {
      key: 'description',
      title: t('common.field.description'),
      dataIndex: 'description',
      render: (value: string | undefined) => (
        <span className="text-sm text-muted-foreground">{value ?? '—'}</span>
      ),
    },
    {
      key: 'isActive',
      title: t('common.field.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>{value ? t('common.status.active') : t('common.status.inactive')}</Badge>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title={t('configuration.contractTemplates.title')}
        subtitle={t('configuration.contractTemplates.subtitle')}
        onAdd={() => navigate('/configuracao/modelos-contrato/novo')}
        onEdit={() => selected && navigate(`/configuracao/modelos-contrato/${selected.id}`)}
        onRefresh={() => void load()}
        actionsSlot={<AuditUtilityBar entityName="CampaignDocumentTemplate" entityLabel="Modelo de contrato" entityId={selected?.id ?? null} />}
        addLabel="Novo modelo"
        selectedRowsCount={selected ? 1 : 0}
        actions={[
          {
            key: 'delete',
            label: t('common.action.delete'),
            testId: 'crud-delete-button',
            icon: <Trash2 className="h-4 w-4" />,
            variant: 'ghost',
            disabled: !selected || deleting,
            onClick: () => setIsConfirmOpen(true),
          },
        ]}
      >
        <DataTable
          columns={columns}
          data={items}
          rowKey="id"
          selectedRows={selected ? [selected] : []}
          onSelectionChange={(rows) => setSelected(rows[0] ?? null)}
          onRowDoubleClick={(row) => navigate(`/configuracao/modelos-contrato/${row.id}`)}
          emptyText={t('configuration.contractTemplates.empty')}
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
        description={t('configuration.contractTemplates.confirm.delete').replace('{0}', selected?.name ?? '')}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />
    </>
  )
}
