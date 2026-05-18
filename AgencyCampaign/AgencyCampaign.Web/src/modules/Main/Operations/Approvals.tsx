import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, Card, CardContent, DataTable, useApi, Badge, Button, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Link as LinkIcon, ExternalLink, ShieldCheck } from 'lucide-react'
import {
  deliverableApprovalsService,
  deliverableShareLinkService,
} from '../../services/deliverableShareLinkService'
import { DeliverableApprovalStatus } from '../../types/deliverableShareLink'
import type { PendingApproval } from '../../types/deliverableShareLink'

const deliverableStatusLabels: Record<number, string> = {
  1: 'Pendente',
  2: 'Em revisão',
  3: 'Aprovada',
  4: 'Publicada',
  5: 'Cancelada',
}

const approvalStatusLabels: Record<number, string> = {
  1: 'Pendente',
  2: 'Aprovada',
  3: 'Rejeitada',
}

const approvalTypeLabels: Record<number, string> = {
  1: 'Interna',
  2: 'Marca',
}

export default function OperationsApprovals() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [items, setItems] = useState<PendingApproval[]>([])
  const [reviewerName, setReviewerName] = useState('')
  const { execute: fetchItems, loading } = useApi<PendingApproval[]>({ showErrorMessage: true })
  const { execute: createShareLink, loading: creatingShareLink } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchItems(() => deliverableApprovalsService.getPending())
    if (result) setItems(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleGenerateLink = async (deliverableId: number) => {
    const name = reviewerName.trim() || 'Marca'
    const result = await createShareLink(() =>
      deliverableShareLinkService.create({ campaignDeliverableId: deliverableId, reviewerName: name }),
    )
    if (result?.data) {
      const url = `${window.location.origin}/d/${result.data.token}`
      try {
        await navigator.clipboard.writeText(url)
      } catch {
        // ignore clipboard failure
      }
      await load()
      window.alert(`Link copiado para área de transferência:\n\n${url}`)
    }
  }

  const columns: DataTableColumn<PendingApproval>[] = [
    {
      key: 'deliverableTitle',
      title: 'Entrega',
      dataIndex: 'deliverableTitle',
      render: (value: string, record) => (
        <div className="flex flex-col">
          <span className="font-medium">{value}</span>
          <span className="text-[10px] text-muted-foreground">
            {record.brandName} · {record.campaignName}
          </span>
        </div>
      ),
    },
    {
      key: 'creatorName',
      title: t('operations.approvals.field.creator'),
      dataIndex: 'creatorName',
      render: (value, record) => `${value || '-'}${record.platformName ? ` · ${record.platformName}` : ''}`,
    },
    {
      key: 'dueAt',
      title: 'Prazo',
      dataIndex: 'dueAt',
      render: (value: string) => new Date(value).toLocaleDateString('pt-BR'),
    },
    {
      key: 'deliverableStatus',
      title: 'Status',
      dataIndex: 'deliverableStatus',
      render: (value: number) => <Badge variant="warning">{deliverableStatusLabels[value] || '-'}</Badge>,
    },
    {
      key: 'approvals',
      title: 'Aprovações registradas',
      dataIndex: 'approvals',
      render: (_value, record) =>
        record.approvals.length === 0 ? (
          <span className="text-xs text-muted-foreground">{t('operations.approvals.field.none')}</span>
        ) : (
          <div className="flex flex-wrap gap-1">
            {record.approvals.map((approval) => (
              <Badge
                key={approval.id}
                variant={approval.status === DeliverableApprovalStatus.Approved ? 'success' : approval.status === DeliverableApprovalStatus.Rejected ? 'destructive' : 'warning'}
              >
                {approvalTypeLabels[approval.approvalType]}: {approvalStatusLabels[approval.status]}
              </Badge>
            ))}
          </div>
        ),
    },
    {
      key: 'actions',
      title: '',
      width: 200,
      render: (_value, record) => (
        <div className="flex gap-2">
          <Button size="sm" variant="outline" onClick={() => void handleGenerateLink(record.deliverableId)} disabled={creatingShareLink}>
            <LinkIcon size={14} className="mr-1.5" />
            {record.hasActiveShareLink ? 'Novo link' : 'Gerar link'}
          </Button>
          <Button size="sm" variant="outline" onClick={() => navigate(`/campanhas`)}>
            <ExternalLink size={14} />
          </Button>
        </div>
      ),
    },
  ]

  return (
    <PageLayout
      title={t('operations.approvals.title')}
      subtitle={t('operations.approvals.subtitle')}
      onRefresh={() => void load()}
      showDefaultActions={false}
    >
      <Card>
        <CardContent className="pt-4 space-y-3">
          <div className="flex items-center gap-3 rounded-md border bg-muted/40 px-3 py-2">
            <ShieldCheck size={16} className="text-primary" />
            <p className="text-xs text-muted-foreground">
              Toda entrega só pode ser publicada após aprovação da marca. Use o botão abaixo para gerar um link público em que a marca registra a decisão.
            </p>
          </div>
          <div className="flex items-center gap-2">
            <label className="text-xs text-muted-foreground">Nome do revisor padrão:</label>
            <input
              className="rounded-md border bg-background px-2 py-1 text-sm"
              value={reviewerName}
              onChange={(e) => setReviewerName(e.target.value)}
              placeholder={t('operations.approvals.placeholder.brand')}
            />
          </div>
          <DataTable
            columns={columns}
            data={items}
            rowKey="deliverableId"
            emptyText={t('operations.approvals.empty')}
            loading={loading}
            pageSize={10}
            pageSizeOptions={[10, 20, 50]}
          />
        </CardContent>
      </Card>
    </PageLayout>
  )
}
