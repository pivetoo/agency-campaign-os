import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, Button, useApi, useAuth, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { opportunityService, type Opportunity, type OpportunityApprovalRequest } from '../../services/opportunityService'

interface ApprovalRow extends OpportunityApprovalRequest {
  opportunityId: number
  opportunityName: string
  negotiationTitle: string
}

const approvalTypeKeys: Record<number, string> = {
  1: 'approvals.type.discount',
  2: 'approvals.type.margin',
  3: 'approvals.type.deadline',
  4: 'approvals.type.exception',
}

const approvalStatusKeys: Record<number, string> = {
  1: 'approvals.status.pending',
  2: 'approvals.status.approved',
  3: 'approvals.status.rejected',
  4: 'approvals.status.cancelled',
}

export default function CommercialApprovals() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [opportunities, setOpportunities] = useState<Opportunity[]>([])
  const [selectedApproval, setSelectedApproval] = useState<ApprovalRow | null>(null)
  const { execute: fetchOpportunities, loading } = useApi<Opportunity[]>({ showErrorMessage: true })
  const { execute: executeAction, loading: actionLoading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadData = async () => {
    const result = await fetchOpportunities(() => opportunityService.getAll())
    if (result) {
      setOpportunities(result)
    }
  }

  useEffect(() => {
    void loadData()
  }, [])

  const approvals = useMemo<ApprovalRow[]>(() => (
    opportunities.flatMap((opportunity) =>
      opportunity.negotiations.flatMap((negotiation) =>
        (negotiation.approvalRequests ?? []).map((approval) => ({
          ...approval,
          opportunityId: opportunity.id,
          opportunityName: opportunity.name,
          negotiationTitle: negotiation.title,
        })),
      ),
    )
      .sort((a, b) => new Date(b.requestedAt).getTime() - new Date(a.requestedAt).getTime())
  ), [opportunities])

  const pendingApprovals = approvals.filter((approval) => approval.status === 1)
  const approvedApprovals = approvals.filter((approval) => approval.status === 2)
  const rejectedApprovals = approvals.filter((approval) => approval.status === 3)

  const decideApproval = async (status: 'approve' | 'reject') => {
    if (!selectedApproval) {
      return
    }

    const payload = {
      approvedByUserName: user?.name || t('approvals.user.fallback'),
      decisionNotes: status === 'approve' ? t('approvals.decision.approved') : t('approvals.decision.rejected'),
    }

    const result = await executeAction(() => (
      status === 'approve'
        ? opportunityService.approveRequest(selectedApproval.id, payload)
        : opportunityService.rejectRequest(selectedApproval.id, payload)
    ))

    if (result !== null) {
      setSelectedApproval(null)
      await loadData()
    }
  }

  const columns: DataTableColumn<ApprovalRow>[] = [
    { key: 'approvalType', title: t('common.field.type'), dataIndex: 'approvalType', render: (value: number) => approvalTypeKeys[value] ? t(approvalTypeKeys[value]) : '-' },
    { key: 'opportunityName', title: t('approvals.field.opportunity'), dataIndex: 'opportunityName' },
    { key: 'negotiationTitle', title: t('approvals.field.negotiation'), dataIndex: 'negotiationTitle' },
    { key: 'requestedByUserName', title: t('approvals.field.requestedBy'), dataIndex: 'requestedByUserName' },
    {
      key: 'status',
      title: t('common.field.status'),
      dataIndex: 'status',
      render: (value: number) => <Badge variant={value === 2 ? 'success' : value === 3 ? 'destructive' : 'warning'}>{approvalStatusKeys[value] ? t(approvalStatusKeys[value]) : '-'}</Badge>,
    },
    { key: 'requestedAt', title: t('approvals.field.requestedAt'), dataIndex: 'requestedAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
  ]

  return (
    <PageLayout
      title={t('approvals.title')}
      subtitle={t('approvals.subtitle')}
      onRefresh={() => void loadData()}
      showDefaultActions={false}
    >
      <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-3">
        <div className="rounded-xl border border-border bg-card p-4"><div className="text-sm text-muted-foreground">{t('approvals.kpi.pending')}</div><div className="text-2xl font-bold">{pendingApprovals.length}</div></div>
        <div className="rounded-xl border border-border bg-card p-4"><div className="text-sm text-muted-foreground">{t('approvals.kpi.approved')}</div><div className="text-2xl font-bold text-emerald-600">{approvedApprovals.length}</div></div>
        <div className="rounded-xl border border-border bg-card p-4"><div className="text-sm text-muted-foreground">{t('approvals.kpi.rejected')}</div><div className="text-2xl font-bold text-destructive">{rejectedApprovals.length}</div></div>
      </div>

      <div className="mb-3 flex flex-wrap gap-2">
        <Button variant="outline" onClick={() => navigate('/comercial/pipeline')}>{t('approvals.action.goToPipeline')}</Button>
        <Button variant="outline-success" disabled={!selectedApproval || selectedApproval.status !== 1 || actionLoading} onClick={() => void decideApproval('approve')}>{t('approvals.action.approveSelected')}</Button>
        <Button variant="outline-danger" disabled={!selectedApproval || selectedApproval.status !== 1 || actionLoading} onClick={() => void decideApproval('reject')}>{t('approvals.action.rejectSelected')}</Button>
      </div>
      <DataTable
        columns={columns}
        data={approvals}
        rowKey="id"
        selectedRows={selectedApproval ? [selectedApproval] : []}
        onSelectionChange={(rows) => setSelectedApproval(rows[0] ?? null)}
        onRowDoubleClick={(row) => navigate(`/comercial/oportunidades/${row.opportunityId}`)}
        emptyText={t('approvals.empty')}
        loading={loading}
        pageSize={5}
        pageSizeOptions={[5, 10, 20, 50]}
      />
    </PageLayout>
  )
}
