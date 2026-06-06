import { useEffect, useState } from 'react'
import { useApi, DataTable, Badge, Input, type DataTableColumn } from 'archon-ui'
import { commercialGoalService } from '../../../services/commercialGoalService'
import type { CommercialGoalProgress } from '../../../types/commercialGoal'
import { commercialGoalPeriodTypeLabels } from '../../../types/commercialGoal'
import { formatCurrency, todayDateInput } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'

function defaultReferenceDate(): string {
  return todayDateInput()
}

export default function Metas() {
  const [referenceDate, setReferenceDate] = useState(defaultReferenceDate())
  const [data, setData] = useState<CommercialGoalProgress[]>([])
  const { execute } = useApi<CommercialGoalProgress[]>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => commercialGoalService.progress({ referenceDate: new Date(referenceDate).toISOString() }))
    setData(result ?? [])
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [referenceDate])

  const columns: DataTableColumn<CommercialGoalProgress>[] = [
    { key: 'userName', title: 'Responsável', dataIndex: 'userName', primary: true, render: (value?: string | null) => value ?? 'Agência' },
    { key: 'periodType', title: 'Período', dataIndex: 'periodType', render: (value: CommercialGoalProgress['periodType']) => commercialGoalPeriodTypeLabels[value] },
    { key: 'targetAmount', title: 'Meta', dataIndex: 'targetAmount', render: (value: number) => formatCurrency(value) },
    { key: 'achievedAmount', title: 'Realizado', dataIndex: 'achievedAmount', render: (value: number) => formatCurrency(value) },
    { key: 'achievedDealsCount', title: 'Negócios', dataIndex: 'achievedDealsCount' },
    { key: 'percentAchieved', title: 'Atingido', dataIndex: 'percentAchieved', render: (value: number) => <Badge variant={value >= 100 ? 'success' : 'default'}>{`${value.toFixed(1)}%`}</Badge> },
  ]

  const filters = (
    <div className="space-y-1">
      <label className="text-xs text-muted-foreground">Data de referência</label>
      <Input type="date" value={referenceDate} onChange={(e) => setReferenceDate(e.target.value || todayDateInput())} />
    </div>
  )

  return (
    <ReportLayout title="Metas × Realizado" subtitle="Meta vs realizado por período" filters={filters} onRefresh={() => void load()}>
      <DataTable columns={columns} data={data} rowKey="id" emptyText="Nenhuma meta ativa na data." />
    </ReportLayout>
  )
}
