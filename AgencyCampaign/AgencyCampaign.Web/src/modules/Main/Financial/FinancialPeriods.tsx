import { useEffect, useState } from 'react'
import { PageLayout, Card, CardContent, Badge, Button, ConfirmModal, useApi, useI18n } from 'archon-ui'
import { Lock, Unlock } from 'lucide-react'
import { financialPeriodService } from '../../../services/financialPeriodService'
import type { FinancialPeriod } from '../../../types/financialPeriod'

export default function FinancialPeriods() {
  const { t } = useI18n()
  const [periods, setPeriods] = useState<FinancialPeriod[]>([])
  const [target, setTarget] = useState<FinancialPeriod | null>(null)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)
  const { execute: fetchPeriods, loading } = useApi<FinancialPeriod[]>({ showErrorMessage: true })
  const { execute: runToggle, loading: toggling } = useApi<FinancialPeriod>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchPeriods(() => financialPeriodService.getRecent(12))
    if (result) setPeriods(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleToggle = async () => {
    if (!target) return
    const action = target.isClosed
      ? financialPeriodService.reopen(target.year, target.month)
      : financialPeriodService.close(target.year, target.month)
    const result = await runToggle(() => action)
    if (result !== null) {
      setIsConfirmOpen(false)
      setTarget(null)
      void load()
    }
  }

  const monthLabel = (period: FinancialPeriod) =>
    new Date(period.year, period.month - 1, 1).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })

  return (
    <>
      <PageLayout title={t('financial.periods.title')} subtitle={t('financial.periods.subtitle')} onRefresh={() => void load()} showDefaultActions={false}>
        <Card>
          <CardContent className="pt-4 pb-4 divide-y">
            {periods.map((period) => (
              <div key={`${period.year}-${period.month}`} className="flex items-center justify-between py-3">
                <div className="flex items-center gap-3">
                  <span className="text-sm font-medium capitalize">{monthLabel(period)}</span>
                  {period.isClosed ? (
                    <Badge variant="destructive">{t('financial.periods.badge.closed')}</Badge>
                  ) : (
                    <Badge variant="success">{t('financial.periods.badge.open')}</Badge>
                  )}
                </div>
                <Button size="sm" variant="outline" disabled={toggling} onClick={() => { setTarget(period); setIsConfirmOpen(true) }}>
                  {period.isClosed ? (
                    <><Unlock size={14} className="mr-1" />{t('financial.periods.action.reopen')}</>
                  ) : (
                    <><Lock size={14} className="mr-1" />{t('financial.periods.action.close')}</>
                  )}
                </Button>
              </div>
            ))}
            {periods.length === 0 && (
              <p className="py-6 text-center text-sm text-muted-foreground">{loading ? t('common.loading') : t('financial.periods.empty')}</p>
            )}
          </CardContent>
        </Card>
      </PageLayout>

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        description={target?.isClosed ? t('financial.periods.confirm.reopen') : t('financial.periods.confirm.close')}
        variant="warning"
        onConfirm={() => void handleToggle()}
        loading={toggling}
      />
    </>
  )
}
