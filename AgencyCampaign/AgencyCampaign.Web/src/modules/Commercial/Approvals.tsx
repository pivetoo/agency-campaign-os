import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, Button, useApi, useAuth, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { opportunityService, OpportunityApprovalStatus, type OpportunityApprovalStatusValue, type OpportunityApprovalRequest, type ApprovalSummary } from '../../services/opportunityService'

const approvalTypeKeys: Record<number, string> = {
  1: 'approvals.type.discount',
  2: 'approvals.type.margin',
  3: 'approvals.type.deadline',
  4: 'approvals.type.exception',
}

const approvalStatusKeys: Record<OpportunityApprovalStatusValue, string> = {
  [OpportunityApprovalStatus.Pending]: 'approvals.status.pending',
  [OpportunityApprovalStatus.Approved]: 'approvals.status.approved',
  [OpportunityApprovalStatus.Rejected]: 'approvals.status.rejected',
  [OpportunityApprovalStatus.Cancelled]: 'approvals.status.cancelled',
}

export default function CommercialApprovals() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [approvals, setApprovals] = useState<OpportunityApprovalRequest[]>([])
  const [summary, setSummary] = useState<ApprovalSummary | null>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [selectedApproval, setSelectedApproval] = useState<OpportunityApprovalRequest | null>(null)
  const { execute: fetchApprovals, loading, pagination } = useApi<OpportunityApprovalRequest[]>({ showErrorMessage: true })
  const { execute: executeAction, loading: actionLoading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadData = async () => {
    const result = await fetchApprovals(() => opportunityService.getAllApprovals({ page, pageSize }))
    if (result) setApprovals(result)
    const summaryResult = await opportunityService.getApprovalsSummary()
    setSummary(summaryResult)
  }

  useEffect(() => {
    void loadData()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize])

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

  const columns: DataTableColumn<OpportunityApprovalRequest>[] = [
    { key: 'approvalType', title: t('common.field.type'), dataIndex: 'approvalType', render: (value: number) => approvalTypeKeys[value] ? t(approvalTypeKeys[value]) : '-' },
    { key: 'opportunityName', title: t('approvals.field.opportunity'), dataIndex: 'opportunityName', render: (value?: string) => value || '-' },
    { key: 'negotiationTitle', title: t('approvals.field.negotiation'), dataIndex: 'negotiationTitle', render: (value?: string) => value || '-' },
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
        <div className="rounded-xl border border-border bg-card p-4"><div className="text-sm text-muted-foreground">{t('approvals.kpi.pending')}</div><div className="text-2xl font-bold">{summary?.pending ?? 0}</div></div>
        <div className="rounded-xl border border-border bg-card p-4"><div className="text-sm text-muted-foreground">{t('approvals.kpi.approved')}</div><div className="text-2xl font-bold text-emerald-600">{summary?.approved ?? 0}</div></div>
        <div className="rounded-xl border border-border bg-card p-4"><div className="text-sm text-muted-foreground">{t('approvals.kpi.rejected')}</div><div className="text-2xl font-bold text-destructive">{summary?.rejected ?? 0}</div></div>
      </div>

      <div className="mb-3 flex flex-wrap gap-2">
        <Button variant="outline" onClick={() => navigate('/comercial/pipeline')}>{t('approvals.action.goToPipeline')}</Button>
        <Button variant="outline-success" disabled={!selectedApproval || selectedApproval.status !== OpportunityApprovalStatus.Pending || actionLoading} onClick={() => void decideApproval('approve')}>{t('approvals.action.approveSelected')}</Button>
        <Button variant="outline-danger" disabled={!selectedApproval || selectedApproval.status !== OpportunityApprovalStatus.Pending || actionLoading} onClick={() => void decideApproval('reject')}>{t('approvals.action.rejectSelected')}</Button>
      </div>
      <DataTable
        columns={columns}
        data={approvals}
        rowKey="id"
        selectedRows={selectedApproval ? [selectedApproval] : []}
        onSelectionChange={(rows) => setSelectedApproval(rows[0] ?? null)}
        onRowDoubleClick={(row) => row.opportunityId && navigate(`/comercial/oportunidades/${row.opportunityId}`)}
        emptyText={t('approvals.empty')}
        loading={loading}
        pageSize={pageSize}
        pageSizeOptions={[10, 20, 50]}
        totalCount={pagination?.totalCount}
        page={page}
        onPageChange={setPage}
        onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
      />
    </PageLayout>
  )
}
