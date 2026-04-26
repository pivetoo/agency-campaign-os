import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, Badge, Button, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { opportunityService, type Opportunity, type OpportunityFollowUp } from '../../services/opportunityService'

interface FollowUpRow extends OpportunityFollowUp {
  opportunityName: string
  brandName: string
}

export default function CommercialFollowUps() {
  const navigate = useNavigate()
  const [opportunities, setOpportunities] = useState<Opportunity[]>([])
  const [selectedStatus, setSelectedStatus] = useState<'overdue' | 'today' | 'upcoming' | 'completed'>('overdue')
  const [selectedActivity, setSelectedActivity] = useState<FollowUpRow | null>(null)
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

  const followUps = useMemo<FollowUpRow[]>(() => (
    opportunities.flatMap((opportunity) =>
      opportunity.followUps.map((followUp) => ({
        ...followUp,
        opportunityName: opportunity.name,
        brandName: opportunity.brand?.name || '-',
      })),
    )
      .sort((a, b) => new Date(a.dueAt).getTime() - new Date(b.dueAt).getTime())
  ), [opportunities])

  const today = new Date()
  today.setHours(0, 0, 0, 0)

  const tomorrow = new Date(today)
  tomorrow.setDate(today.getDate() + 1)

  const buckets = useMemo(() => {
    return {
      overdue: followUps.filter((item) => !item.isCompleted && new Date(item.dueAt) < today),
      today: followUps.filter((item) => !item.isCompleted && new Date(item.dueAt) >= today && new Date(item.dueAt) < tomorrow),
      upcoming: followUps.filter((item) => !item.isCompleted && new Date(item.dueAt) >= tomorrow),
      completed: followUps.filter((item) => item.isCompleted),
    }
  }, [followUps])

  const visibleActivities = buckets[selectedStatus]

  const completeSelectedActivity = async () => {
    if (!selectedActivity) {
      return
    }

    const result = await executeAction(() => opportunityService.completeFollowUp(selectedActivity.id))
    if (result !== null) {
      setSelectedActivity(null)
      await loadData()
    }
  }

  const columns: DataTableColumn<FollowUpRow>[] = [
    { key: 'subject', title: 'Atividade', dataIndex: 'subject' },
    { key: 'opportunityName', title: 'Oportunidade', dataIndex: 'opportunityName' },
    { key: 'brandName', title: 'Marca', dataIndex: 'brandName' },
    { key: 'dueAt', title: 'Prazo', dataIndex: 'dueAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    {
      key: 'isCompleted',
      title: 'Status',
      dataIndex: 'isCompleted',
      render: (value: boolean) => <Badge variant={value ? 'success' : 'warning'}>{value ? 'Concluído' : 'Pendente'}</Badge>,
    },
  ]

  return (
    <PageLayout
      title="Atividades"
      subtitle="Agenda comercial com atividades atrasadas, de hoje, próximas e concluídas"
      onRefresh={() => void loadData()}
      showDefaultActions={false}
    >
      <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-4">
        <button type="button" onClick={() => setSelectedStatus('overdue')} className={`rounded-xl border p-4 text-left ${selectedStatus === 'overdue' ? 'border-destructive bg-destructive/5' : 'border-border bg-card'}`}><div className="text-sm text-muted-foreground">Atrasadas</div><div className="text-2xl font-bold text-destructive">{buckets.overdue.length}</div></button>
        <button type="button" onClick={() => setSelectedStatus('today')} className={`rounded-xl border p-4 text-left ${selectedStatus === 'today' ? 'border-primary bg-primary/5' : 'border-border bg-card'}`}><div className="text-sm text-muted-foreground">Hoje</div><div className="text-2xl font-bold">{buckets.today.length}</div></button>
        <button type="button" onClick={() => setSelectedStatus('upcoming')} className={`rounded-xl border p-4 text-left ${selectedStatus === 'upcoming' ? 'border-primary bg-primary/5' : 'border-border bg-card'}`}><div className="text-sm text-muted-foreground">Próximas</div><div className="text-2xl font-bold">{buckets.upcoming.length}</div></button>
        <button type="button" onClick={() => setSelectedStatus('completed')} className={`rounded-xl border p-4 text-left ${selectedStatus === 'completed' ? 'border-emerald-500 bg-emerald-500/5' : 'border-border bg-card'}`}><div className="text-sm text-muted-foreground">Concluídas</div><div className="text-2xl font-bold text-emerald-600">{buckets.completed.length}</div></button>
      </div>

      <div className="mb-3 flex flex-wrap gap-2">
        <Button variant="outline" onClick={() => navigate('/comercial/pipeline')}>Ir para pipeline</Button>
        <Button variant="outline-success" disabled={!selectedActivity || selectedActivity.isCompleted || actionLoading} onClick={() => void completeSelectedActivity()}>Concluir atividade</Button>
      </div>
      <DataTable
        columns={columns}
        data={visibleActivities}
        rowKey="id"
        selectedRows={selectedActivity ? [selectedActivity] : []}
        onSelectionChange={(rows) => setSelectedActivity(rows[0] ?? null)}
        onRowDoubleClick={(row) => navigate(`/comercial/oportunidades/${row.opportunityId}`)}
        emptyText="Nenhuma atividade comercial cadastrada"
        loading={loading}
        pageSize={5}
        pageSizeOptions={[5, 10, 20, 50]}
      />
    </PageLayout>
  )
}
