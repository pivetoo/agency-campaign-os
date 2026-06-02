import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Card, CardContent, BarChart, useI18n } from 'archon-ui'
import { Activity, BarChart3, Eye, ExternalLink, AlertTriangle } from 'lucide-react'
import { campaignReportService } from '../../services/campaignReportService'
import type { CampaignReport } from '../../types/campaignReport'
import { formatNumber, formatCurrency, formatDate } from '../../lib/format'

function Kpi({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <div className="rounded-xl border border-border/70 bg-card p-4">
      <p className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-1 text-2xl font-bold tracking-tight text-foreground">{value}</p>
      {hint && <p className="text-[11px] text-muted-foreground">{hint}</p>}
    </div>
  )
}

export default function PublicCampaignReport() {
  const { t } = useI18n()
  const { token } = useParams<{ token: string }>()
  const [report, setReport] = useState<CampaignReport | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)

  useEffect(() => {
    if (!token) {
      setNotFound(true)
      setLoading(false)
      return
    }

    let mounted = true
    campaignReportService
      .getByToken(token)
      .then((result) => {
        if (!mounted) return
        if (!result) {
          setNotFound(true)
          return
        }
        setReport(result)
      })
      .finally(() => {
        if (mounted) setLoading(false)
      })

    return () => {
      mounted = false
    }
  }, [token])

  if (loading) {
    return <div className="flex min-h-screen items-center justify-center bg-muted/30 text-sm text-muted-foreground">...</div>
  }

  if (notFound || !report) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-3 bg-muted/30 px-4 text-center">
        <AlertTriangle className="h-10 w-10 text-amber-500" />
        <p className="text-sm font-medium text-foreground">{t('campaignReport.notFound')}</p>
      </div>
    )
  }

  const totals = report.totals
  const rate = totals.avgEngagementRate
  const platformData = report.byPlatform.map((item) => ({ name: item.name, value: item.reach }))

  return (
    <div className="min-h-screen bg-muted/30 py-8">
      <div className="mx-auto w-full max-w-5xl space-y-6 px-4">
        <div className="border-l-4 border-primary pl-5">
          <h1 className="text-2xl font-bold tracking-tight text-foreground">{report.campaignName}</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {report.brandName ? `${report.brandName} · ` : ''}
            {report.startsAt ? formatDate(report.startsAt) : '-'}
            {` ${t('common.label.to')} `}
            {report.endsAt ? formatDate(report.endsAt) : '-'}
          </p>
        </div>

        <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-5">
          <Kpi label={t('campaignReport.kpi.reach')} value={formatNumber(totals.totalReach)} />
          <Kpi label={t('campaignReport.kpi.impressions')} value={formatNumber(totals.totalImpressions)} />
          <Kpi label={t('campaignReport.kpi.engagement')} value={formatNumber(totals.totalEngagement)} />
          <Kpi label={t('campaignReport.kpi.engagementRate')} value={rate != null ? `${rate.toFixed(2)}%` : '-'} />
          <Kpi label={t('campaignReport.kpi.emv')} value={totals.emv != null ? formatCurrency(totals.emv) : '-'} hint={t('campaignReport.kpi.emvHint')} />
        </div>

        {totals.attributedRevenue != null && (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div className="rounded-xl border border-primary/30 bg-primary/10 p-4">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-primary/80">{t('campaignReport.kpi.attributedRevenue')}</p>
              <p className="mt-1 text-2xl font-bold tracking-tight text-foreground">{formatCurrency(totals.attributedRevenue)}</p>
              {totals.attributedOrders != null && (
                <p className="text-[11px] text-muted-foreground">{t('campaignReport.kpi.attributedOrdersHint').replace('{0}', String(totals.attributedOrders))}</p>
              )}
            </div>
            <div className="rounded-xl border border-primary/30 bg-primary/10 p-4">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-primary/80">{t('campaignReport.kpi.roi')}</p>
              <p className="mt-1 text-2xl font-bold tracking-tight text-foreground">{totals.roi != null ? `${totals.roi.toFixed(2)}x` : '-'}</p>
              <p className="text-[11px] text-muted-foreground">{t('campaignReport.kpi.roiHint')}</p>
            </div>
          </div>
        )}

        <div className="flex flex-wrap items-center gap-4 rounded-xl border border-border/70 bg-card px-4 py-3 text-sm">
          <span className="flex items-center gap-1.5 text-muted-foreground"><BarChart3 className="h-4 w-4 text-primary" /> {t('campaignReport.kpi.deliverables')}: <strong className="text-foreground">{totals.publishedCount}/{totals.deliverablesCount}</strong></span>
        </div>

        {platformData.length > 0 && (
          <Card className="border border-border/70 shadow-sm">
            <CardContent className="pt-5 pb-4">
              <p className="mb-3 flex items-center gap-2 text-sm font-semibold text-foreground"><Activity className="h-4 w-4 text-amber-600" /> {t('campaignReport.section.byPlatform')}</p>
              <BarChart data={platformData} dataKeys={['value']} colors={['#1F3B61']} height={200} showLegend={false} showGrid={false} layout="horizontal" />
            </CardContent>
          </Card>
        )}

        <Card className="border border-border/70 shadow-sm">
          <CardContent className="pt-5 pb-4">
            <p className="mb-3 text-sm font-semibold text-foreground">{t('campaignReport.section.deliverables')}</p>
            {report.deliverables.length === 0 ? (
              <p className="py-8 text-center text-xs text-muted-foreground">{t('campaignReport.empty')}</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-border text-[11px] uppercase tracking-wide text-muted-foreground">
                      <th className="py-2 pr-3 font-semibold">{t('common.field.deliverable')}</th>
                      <th className="py-2 pr-3 font-semibold">{t('creators.singular')}</th>
                      <th className="py-2 pr-3 font-semibold">{t('common.field.platform')}</th>
                      <th className="py-2 pr-3 text-right font-semibold">{t('campaignReport.kpi.reach')}</th>
                      <th className="py-2 pr-3 text-right font-semibold">{t('campaignReport.kpi.engagement')}</th>
                      <th className="py-2 pr-3 text-right font-semibold">{t('campaignReport.kpi.engagementRate')}</th>
                      <th className="py-2 font-semibold" />
                    </tr>
                  </thead>
                  <tbody>
                    {report.deliverables.map((item, index) => (
                      <tr key={index} className="border-b border-border/50">
                        <td className="py-2 pr-3 font-medium text-foreground">{item.title}</td>
                        <td className="py-2 pr-3 text-muted-foreground">{item.creatorName}</td>
                        <td className="py-2 pr-3 text-muted-foreground">{item.platformName}</td>
                        <td className="py-2 pr-3 text-right tabular-nums">{item.reach != null ? formatNumber(item.reach) : '-'}</td>
                        <td className="py-2 pr-3 text-right tabular-nums">{item.engagement != null ? formatNumber(item.engagement) : '-'}</td>
                        <td className="py-2 pr-3 text-right tabular-nums">{item.engagementRate != null ? `${item.engagementRate.toFixed(2)}%` : '-'}</td>
                        <td className="py-2">
                          {item.publishedUrl && (
                            <a href={item.publishedUrl} target="_blank" rel="noopener noreferrer" className="inline-flex items-center text-primary hover:underline">
                              <ExternalLink className="h-3.5 w-3.5" />
                            </a>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>

        <p className="flex items-center justify-center gap-1.5 pb-4 text-center text-[11px] text-muted-foreground">
          <Eye className="h-3 w-3" /> {t('campaignReport.footer')}
        </p>
      </div>
    </div>
  )
}
