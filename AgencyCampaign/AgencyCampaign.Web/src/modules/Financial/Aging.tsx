import { useEffect, useState } from 'react'
import { PageLayout, Card, CardContent, useApi, Badge } from 'archon-ui'
import { financialReportService, type AgingReport } from '../../services/financialReportService'

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function Aging() {
  const [report, setReport] = useState<AgingReport | null>(null)
  const { execute, loading } = useApi<AgingReport | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => financialReportService.getAging())
    setReport(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const totals = report?.buckets.reduce(
    (acc, bucket) => ({
      receivable: acc.receivable + bucket.totalReceivable,
      payable: acc.payable + bucket.totalPayable,
    }),
    { receivable: 0, payable: 0 },
  ) ?? { receivable: 0, payable: 0 }

  return (
    <PageLayout
      title="Aging financeiro"
      subtitle="Distribuição de lançamentos pendentes por faixa de atraso"
      onRefresh={() => void load()}
      showDefaultActions={false}
    >
      <div className="grid grid-cols-1 gap-3 md:grid-cols-2 mb-4">
        <Card>
          <CardContent className="pt-5 pb-5">
            <p className="text-xs text-muted-foreground uppercase tracking-wide">Total a receber pendente</p>
            <p className="text-2xl font-semibold mt-1 text-emerald-600">{formatCurrency(totals.receivable)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-5 pb-5">
            <p className="text-xs text-muted-foreground uppercase tracking-wide">Total a pagar pendente</p>
            <p className="text-2xl font-semibold mt-1 text-destructive">{formatCurrency(totals.payable)}</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardContent className="pt-4">
          {loading ? (
            <p className="text-sm text-muted-foreground">Carregando aging...</p>
          ) : !report ? (
            <p className="text-sm text-muted-foreground">Sem dados.</p>
          ) : (
            <div className="grid grid-cols-1 gap-3 md:grid-cols-5">
              {report.buckets.map((bucket) => {
                const isOverdue = bucket.minDays > 0
                return (
                  <div key={bucket.label} className={`rounded-md border p-4 ${isOverdue ? 'border-destructive/40' : ''}`}>
                    <div className="flex items-center justify-between mb-2">
                      <p className="text-sm font-semibold">{bucket.label}</p>
                      {isOverdue && <Badge variant="destructive">vencido</Badge>}
                    </div>
                    <div className="space-y-2">
                      <div>
                        <p className="text-[10px] uppercase text-muted-foreground tracking-wide">A receber</p>
                        <p className="text-sm font-semibold text-emerald-600">{formatCurrency(bucket.totalReceivable)}</p>
                        <p className="text-[10px] text-muted-foreground">{bucket.receivableCount} lançamento(s)</p>
                      </div>
                      <div>
                        <p className="text-[10px] uppercase text-muted-foreground tracking-wide">A pagar</p>
                        <p className="text-sm font-semibold text-destructive">{formatCurrency(bucket.totalPayable)}</p>
                        <p className="text-[10px] text-muted-foreground">{bucket.payableCount} lançamento(s)</p>
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </CardContent>
      </Card>
    </PageLayout>
  )
}
