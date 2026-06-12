import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, Card, CardContent, CardHeader, CardTitle, Button, useI18n, usePermissions } from 'archon-ui'
import { financialMonitorService } from '../../../services/financialMonitorService'
import { MonitorAlertSeverity, type FinancialMonitor, type MonitorAlert } from '../../../types/financialMonitor'
import { formatCurrency, formatDate } from '../../../lib/format'

const REFRESH_INTERVAL_MS = 60000

const alertRoutes: Record<string, string> = {
  'cash-gap': '/relatorios/financeiro/projecao',
  'overdue-receivable': '/financeiro/receber',
  'payment-failed': '/financeiro/repasses-creators',
  'overdue-payable': '/financeiro/pagar',
  'due-next-48h': '/financeiro/receber',
  'approval-stuck': '/financeiro/repasses-creators',
  'reconciliation-backlog': '/financeiro/conciliacao',
  'period-open': '/financeiro/periodos',
}

const alertLabelKeys: Record<string, string> = {
  'cash-gap': 'financialMonitor.alert.cashGap',
  'overdue-receivable': 'financialMonitor.alert.overdueReceivable',
  'payment-failed': 'financialMonitor.alert.paymentFailed',
  'overdue-payable': 'financialMonitor.alert.overduePayable',
  'due-next-48h': 'financialMonitor.alert.dueNext48h',
  'approval-stuck': 'financialMonitor.alert.approvalStuck',
  'reconciliation-backlog': 'financialMonitor.alert.reconciliationBacklog',
  'period-open': 'financialMonitor.alert.periodOpen',
}

const alertCtaKeys: Record<string, string> = {
  'cash-gap': 'financialMonitor.cta.cashGap',
  'overdue-receivable': 'financialMonitor.cta.overdueReceivable',
  'payment-failed': 'financialMonitor.cta.paymentFailed',
  'overdue-payable': 'financialMonitor.cta.overduePayable',
  'due-next-48h': 'financialMonitor.cta.dueNext48h',
  'approval-stuck': 'financialMonitor.cta.approvalStuck',
  'reconciliation-backlog': 'financialMonitor.cta.reconciliationBacklog',
  'period-open': 'financialMonitor.cta.periodOpen',
}

const severityStyles: Record<number, string> = {
  [MonitorAlertSeverity.Critical]: 'bg-red-500/15 text-red-700',
  [MonitorAlertSeverity.Warning]: 'bg-amber-500/15 text-amber-700',
  [MonitorAlertSeverity.Info]: 'bg-sky-500/15 text-sky-700',
}

function formatMonth(month: number, year: number): string {
  return `${String(month).padStart(2, '0')}/${year}`
}

export default function FinancialMonitorPage() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const [monitor, setMonitor] = useState<FinancialMonitor | null>(null)
  const [lastUpdatedAt, setLastUpdatedAt] = useState<Date | null>(null)

  const canSeePayouts = hasPermission('creatorPayments.get')
  const canSeeReconciliation = hasPermission('bankTransactions.getByAccount')
  const canManagePeriods = hasPermission('financialPeriods.get')

  const load = useCallback(async () => {
    const data = await financialMonitorService.get()
    if (data) {
      setMonitor(data)
      setLastUpdatedAt(new Date())
    }
  }, [])

  useEffect(() => {
    void load()
    const interval = window.setInterval(() => {
      if (document.visibilityState === 'visible') {
        void load()
      }
    }, REFRESH_INTERVAL_MS)
    const onVisible = () => {
      if (document.visibilityState === 'visible') {
        void load()
      }
    }
    document.addEventListener('visibilitychange', onVisible)
    return () => {
      window.clearInterval(interval)
      document.removeEventListener('visibilitychange', onVisible)
    }
  }, [load])

  const severityLabel = (severity: number): string => {
    if (severity === MonitorAlertSeverity.Critical) return t('financialMonitor.severity.critical')
    if (severity === MonitorAlertSeverity.Warning) return t('financialMonitor.severity.warning')
    return t('financialMonitor.severity.info')
  }

  const alertDetails = (alert: MonitorAlert): string => {
    const parts: string[] = []
    switch (alert.type) {
      case 'cash-gap':
        if (alert.referenceDate) parts.push(formatDate(alert.referenceDate))
        parts.push(formatCurrency(alert.amount ?? 0))
        break
      case 'overdue-receivable':
        parts.push(String(alert.count))
        parts.push(formatCurrency(alert.amount ?? 0))
        if (alert.worstDays) parts.push(`${t('financialMonitor.alert.worstDays')} ${alert.worstDays}d`)
        break
      case 'due-next-48h':
        parts.push(String(alert.count))
        parts.push(`+${formatCurrency(alert.amountIn ?? 0)} / -${formatCurrency(alert.amountOut ?? 0)}`)
        break
      case 'reconciliation-backlog':
        parts.push(String(alert.count))
        if (alert.accountName) parts.push(`${t('financialMonitor.alert.worstAccount')} ${alert.accountName}`)
        break
      case 'period-open':
        if (monitor) parts.push(formatMonth(monitor.periods.previous.month, monitor.periods.previous.year))
        break
      default:
        parts.push(String(alert.count))
        parts.push(formatCurrency(alert.amount ?? 0))
        break
    }
    return parts.join(' · ')
  }

  if (!monitor) {
    return (
      <PageLayout title={t('financialMonitor.title')} onRefresh={() => { void load() }}>
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
          {[1, 2, 3, 4, 5].map((index) => (
            <div key={index} className="h-28 animate-pulse rounded-lg border bg-muted/40" />
          ))}
        </div>
      </PageLayout>
    )
  }

  const { pulse, alerts, upcoming, payoutFunnel, reconciliation, periods } = monitor
  const projectionNegative = !!pulse.projectionNegativeAt

  return (
    <PageLayout title={t('financialMonitor.title')} onRefresh={() => { void load() }}>
      <div className="space-y-4">
        {lastUpdatedAt && (
          <p className="text-right text-xs text-muted-foreground">
            {t('financialMonitor.updatedAt')} {lastUpdatedAt.toLocaleTimeString()}
          </p>
        )}

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
          <Card>
            <CardHeader><CardTitle className="text-sm">{t('financialMonitor.kpi.realBalance')}</CardTitle></CardHeader>
            <CardContent>
              <p className="text-2xl font-bold">{formatCurrency(pulse.realBalance)}</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle className="text-sm">{t('financialMonitor.kpi.projected')}</CardTitle></CardHeader>
            <CardContent>
              <p className={`text-2xl font-bold ${projectionNegative ? 'text-red-600' : ''}`}>{formatCurrency(pulse.projectedBalance30d)}</p>
              {projectionNegative && (
                <p className="mt-1 text-xs text-red-600">{t('financialMonitor.kpi.negativeOn')} {formatDate(pulse.projectionNegativeAt!)}</p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle className="text-sm">{t('financialMonitor.kpi.receivable')}</CardTitle></CardHeader>
            <CardContent>
              <p className="text-2xl font-bold">{formatCurrency(pulse.receivableOpen)}</p>
              {pulse.receivableOverdue > 0 && (
                <p className="mt-1 text-xs text-red-600">{formatCurrency(pulse.receivableOverdue)} {t('financialMonitor.kpi.overdueSuffix')} ({pulse.receivableOverdueCount})</p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle className="text-sm">{t('financialMonitor.kpi.payable')}</CardTitle></CardHeader>
            <CardContent>
              <p className="text-2xl font-bold">{formatCurrency(pulse.payableOpen)}</p>
              {pulse.payableOverdue > 0 && (
                <p className="mt-1 text-xs text-amber-600">{formatCurrency(pulse.payableOverdue)} {t('financialMonitor.kpi.overdueSuffix')} ({pulse.payableOverdueCount})</p>
              )}
            </CardContent>
          </Card>

          {canSeePayouts && (
            <Card>
              <CardHeader><CardTitle className="text-sm">{t('financialMonitor.kpi.payouts')}</CardTitle></CardHeader>
              <CardContent>
                <p className="text-2xl font-bold">{pulse.payoutQueueCount}</p>
                <p className="mt-1 text-xs text-muted-foreground">{formatCurrency(pulse.payoutQueueAmount)}</p>
              </CardContent>
            </Card>
          )}
        </div>

        <Card>
          <CardHeader><CardTitle>{t('financialMonitor.alerts.title')}</CardTitle></CardHeader>
          <CardContent>
            {alerts.length === 0 ? (
              <p className="rounded-md bg-emerald-500/10 px-3 py-2 text-sm text-emerald-700">{t('financialMonitor.alerts.empty')}</p>
            ) : (
              <div className="space-y-2">
                {alerts.map((alert) => (
                  <div key={alert.type} className="flex flex-col gap-2 rounded-md border px-3 py-2 sm:flex-row sm:items-center sm:justify-between">
                    <div className="flex items-center gap-3">
                      <span className={`shrink-0 rounded px-2 py-0.5 text-xs font-medium ${severityStyles[alert.severity]}`}>{severityLabel(alert.severity)}</span>
                      <span className="text-sm">
                        {t(alertLabelKeys[alert.type] ?? alert.type)}
                        {alertDetails(alert) && <span className="text-muted-foreground">: {alertDetails(alert)}</span>}
                      </span>
                    </div>
                    <Button variant="outline" size="sm" className="shrink-0 self-start sm:self-auto" onClick={() => navigate(alertRoutes[alert.type] ?? '/financeiro/receber')}>
                      {t(alertCtaKeys[alert.type] ?? 'financialMonitor.viewAll')}
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          <Card>
            <CardHeader><CardTitle className="text-base">{t('financialMonitor.upcoming.title')}</CardTitle></CardHeader>
            <CardContent>
              {upcoming.length === 0 ? (
                <p className="text-sm text-muted-foreground">{t('financialMonitor.upcoming.empty')}</p>
              ) : (
                <div className="space-y-1.5">
                  {upcoming.map((day) => (
                    <div key={day.date} className="flex items-center justify-between text-sm">
                      <span className="text-muted-foreground">{formatDate(day.date)}</span>
                      <span className="flex gap-3">
                        {day.inAmount > 0 && <span className="text-emerald-600">+{formatCurrency(day.inAmount)}</span>}
                        {day.outAmount > 0 && <span className="text-red-600">-{formatCurrency(day.outAmount)}</span>}
                      </span>
                    </div>
                  ))}
                </div>
              )}
              <Button variant="ghost" size="sm" className="mt-3 px-0 text-primary" onClick={() => navigate('/financeiro/receber')}>{t('financialMonitor.viewAll')}</Button>
            </CardContent>
          </Card>

          {canSeePayouts && (
            <Card>
              <CardHeader><CardTitle className="text-base">{t('financialMonitor.funnel.title')}</CardTitle></CardHeader>
              <CardContent>
                <div className="space-y-1.5 text-sm">
                  <div className="flex items-center justify-between">
                    <span className="text-muted-foreground">{t('financialMonitor.funnel.pendingApproval')}</span>
                    <span className={`rounded px-2 py-0.5 text-xs font-medium ${payoutFunnel.pendingApproval > 0 ? 'bg-amber-500/15 text-amber-700' : 'bg-muted text-muted-foreground'}`}>{payoutFunnel.pendingApproval}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-muted-foreground">{t('financialMonitor.funnel.readyToPay')}</span>
                    <span className={`rounded px-2 py-0.5 text-xs font-medium ${payoutFunnel.readyToPay > 0 ? 'bg-primary/15 text-primary' : 'bg-muted text-muted-foreground'}`}>{payoutFunnel.readyToPay}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-muted-foreground">{t('financialMonitor.funnel.scheduled')}</span>
                    <span className="rounded bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">{payoutFunnel.scheduled}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-muted-foreground">{t('financialMonitor.funnel.failed')}</span>
                    <span className={`rounded px-2 py-0.5 text-xs font-medium ${payoutFunnel.failed > 0 ? 'bg-red-500/15 text-red-700' : 'bg-muted text-muted-foreground'}`}>{payoutFunnel.failed}</span>
                  </div>
                </div>
                <Button variant="ghost" size="sm" className="mt-3 px-0 text-primary" onClick={() => navigate('/financeiro/repasses-creators')}>{t('financialMonitor.viewAll')}</Button>
              </CardContent>
            </Card>
          )}

          {canSeeReconciliation && (
            <Card>
              <CardHeader><CardTitle className="text-base">{t('financialMonitor.reconciliation.title')}</CardTitle></CardHeader>
              <CardContent>
                {reconciliation.length === 0 ? (
                  <p className="text-sm text-muted-foreground">{t('financialMonitor.reconciliation.empty')}</p>
                ) : (
                  <div className="space-y-1.5">
                    {reconciliation.map((account) => (
                      <div key={account.accountId} className="flex items-center justify-between text-sm">
                        <span className="truncate text-muted-foreground">{account.accountName}</span>
                        <span className="flex items-center gap-2">
                          {account.lastImportAt && <span className="hidden text-xs text-muted-foreground lg:inline">{t('financialMonitor.reconciliation.lastImport')} {formatDate(account.lastImportAt)}</span>}
                          <span className={`rounded px-2 py-0.5 text-xs font-medium ${account.pending > 0 ? 'bg-amber-500/15 text-amber-700' : 'bg-emerald-500/15 text-emerald-700'}`}>
                            {account.pending > 0 ? `${account.pending} ${t('financialMonitor.reconciliation.pending')}` : 'OK'}
                          </span>
                        </span>
                      </div>
                    ))}
                  </div>
                )}
                <Button variant="ghost" size="sm" className="mt-3 px-0 text-primary" onClick={() => navigate('/financeiro/conciliacao')}>{t('financialMonitor.viewAll')}</Button>
              </CardContent>
            </Card>
          )}
        </div>

        <Card>
          <CardContent className="flex flex-col gap-2 py-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex flex-wrap items-center gap-4 text-sm">
              <span className="font-medium">{t('financialMonitor.periods.title')}</span>
              <span className="flex items-center gap-1.5">
                <span className="text-muted-foreground">{t('financialMonitor.periods.current')} {formatMonth(periods.current.month, periods.current.year)}</span>
                <span className={`rounded px-2 py-0.5 text-xs font-medium ${periods.current.isClosed ? 'bg-muted text-muted-foreground' : 'bg-emerald-500/15 text-emerald-700'}`}>{periods.current.isClosed ? t('financialMonitor.periods.closed') : t('financialMonitor.periods.open')}</span>
              </span>
              <span className="flex items-center gap-1.5">
                <span className="text-muted-foreground">{t('financialMonitor.periods.previous')} {formatMonth(periods.previous.month, periods.previous.year)}</span>
                <span className={`rounded px-2 py-0.5 text-xs font-medium ${periods.previous.isClosed ? 'bg-muted text-muted-foreground' : 'bg-amber-500/15 text-amber-700'}`}>{periods.previous.isClosed ? t('financialMonitor.periods.closed') : t('financialMonitor.periods.open')}</span>
              </span>
            </div>
            {canManagePeriods && (
              <Button variant="outline" size="sm" onClick={() => navigate('/financeiro/periodos')}>{t('financialMonitor.periods.manage')}</Button>
            )}
          </CardContent>
        </Card>
      </div>
    </PageLayout>
  )
}
