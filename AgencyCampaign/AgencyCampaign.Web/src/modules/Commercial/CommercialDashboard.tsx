import { useEffect, useMemo, useState } from 'react'
import {
  AreaChart,
  BarChart,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  ChartContainer,
  GlobalLoader,
} from 'archon-ui'
import {
  TrendingUp,
  Filter,
  Trophy,
  AlertTriangle,
  Target,
  Briefcase,
  Megaphone,
  Clock,
} from 'lucide-react'
import {
  opportunityService,
  type CommercialDashboardSummary,
  type CommercialForecast,
  type CommercialFunnelStage,
  type CommercialResponsibleRanking,
  type CommercialAlert,
} from '../../services/opportunityService'

const chartColors = ['#6366f1', '#22c55e', '#f59e0b', '#ec4899', '#06b6d4', '#8b5cf6']

function formatCurrencyShort(value: number) {
  if (value >= 1_000_000) return `R$ ${(value / 1_000_000).toFixed(1)}M`
  if (value >= 1_000) return `R$ ${(value / 1_000).toFixed(0)}k`
  return `R$ ${value.toFixed(0)}`
}

function truncateLabel(value: string) {
  return value.length > 14 ? `${value.slice(0, 12)}...` : value
}

export default function CommercialDashboard() {
  const [summary, setSummary] = useState<CommercialDashboardSummary | null>(null)
  const [forecast, setForecast] = useState<CommercialForecast | null>(null)
  const [funnel, setFunnel] = useState<CommercialFunnelStage[]>([])
  const [ranking, setRanking] = useState<CommercialResponsibleRanking[]>([])
  const [alerts, setAlerts] = useState<CommercialAlert[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true

    Promise.all([
      opportunityService.getDashboard(),
      opportunityService.getForecast(),
      opportunityService.getFunnelConversion(),
      opportunityService.getResponsibleRanking(),
      opportunityService.getAlerts(),
    ])
      .then(([summaryData, forecastData, funnelData, rankingData, alertsData]) => {
        if (!isMounted) return
        setSummary(summaryData)
        setForecast(forecastData)
        setFunnel(funnelData)
        setRanking(rankingData)
        setAlerts(alertsData)
      })
      .finally(() => {
        if (isMounted) {
          setIsLoading(false)
        }
      })

    return () => {
      isMounted = false
    }
  }, [])

  const headlineChips = useMemo(() => [
    {
      label: 'Pipeline aberto',
      value: summary ? formatCurrencyShort(summary.totalPipelineValue) : '-',
      icon: Briefcase,
      tone: 'text-indigo-600',
    },
    {
      label: 'Receita ganha',
      value: summary ? formatCurrencyShort(summary.wonValue) : '-',
      icon: Trophy,
      tone: 'text-emerald-600',
    },
    {
      label: 'Oportunidades abertas',
      value: summary?.openOpportunities ?? 0,
      icon: Target,
      tone: 'text-violet-600',
    },
    {
      label: 'Follow-ups atrasados',
      value: summary?.overdueFollowUpsCount ?? 0,
      icon: Clock,
      tone: 'text-amber-600',
    },
  ], [summary])

  const forecastData = useMemo(
    () => forecast?.months.map((item) => ({
      name: item.label,
      previsto: Number(item.estimated.toFixed(0)),
      ponderado: Number(item.weighted.toFixed(0)),
    })) ?? [],
    [forecast]
  )

  const funnelData = useMemo(
    () => funnel
      .filter((stage) => stage.isFinalBehavior === 0)
      .map((stage) => ({
        name: truncateLabel(stage.name),
        Aberto: stage.openCount,
      })),
    [funnel]
  )

  const rankingData = useMemo(
    () => ranking.slice(0, 6).map((item) => ({
      name: truncateLabel(item.name),
      Ganho: Number((item.wonValue / 1000).toFixed(0)),
    })),
    [ranking]
  )

  const winRate = useMemo(() => {
    if (!summary) return 0
    const closed = summary.wonOpportunities + summary.lostOpportunities
    return closed === 0 ? 0 : Math.round((summary.wonOpportunities / closed) * 100)
  }, [summary])

  if (isLoading) {
    return <GlobalLoader isVisible={true} className="bg-background" />
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div className="border-l-4 border-primary pl-5">
          <h1 className="text-3xl font-bold text-foreground tracking-tight">
            <strong className="text-primary">Dashboard comercial</strong>
          </h1>
          <p className="text-lg text-muted-foreground mt-3 leading-relaxed">
            Funil, previsão de fechamento e ranking de vendedores
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {headlineChips.map(({ label, value, icon: Icon, tone }) => (
            <div
              key={label}
              className="flex items-center gap-2 rounded-full border border-border/70 bg-muted/30 px-3 py-1.5 text-xs"
            >
              <Icon className={`h-3.5 w-3.5 ${tone}`} />
              <span className="font-medium text-muted-foreground">{label}</span>
              <span className="font-semibold text-foreground">{value}</span>
            </div>
          ))}
        </div>
      </div>

      <div className="grid gap-4 xl:grid-cols-[1.4fr_0.9fr]">
        <Card className="overflow-hidden border border-border/70 shadow-sm">
          <CardHeader className="border-b bg-muted/20 pb-4">
            <CardTitle className="flex items-center justify-between gap-2 text-base">
              <span className="flex items-center gap-2">
                <TrendingUp className="h-5 w-5 text-primary" />
                Previsão de receita (próximos meses)
              </span>
              {forecast ? (
                <span className="text-xs font-normal text-muted-foreground">
                  Ponderado: <strong className="text-foreground">{formatCurrencyShort(forecast.totalWeighted)}</strong>
                </span>
              ) : null}
            </CardTitle>
          </CardHeader>
          <CardContent className="p-5">
            <ChartContainer
              title="Estimado x ponderado pela probabilidade"
              height={290}
              isEmpty={forecastData.length === 0}
              emptyMessage="Sem oportunidades com previsão de fechamento."
            >
              <AreaChart
                data={forecastData}
                dataKeys={['previsto', 'ponderado']}
                colors={['#6366f1', '#22c55e']}
                height={230}
                fillOpacity={0.18}
              />
            </ChartContainer>
          </CardContent>
        </Card>

        <Card className="overflow-hidden border border-border/70 shadow-sm">
          <CardHeader className="border-b bg-muted/20 pb-4">
            <CardTitle className="flex items-center gap-2 text-base">
              <Filter className="h-5 w-5 text-violet-600" />
              Funil de conversão
            </CardTitle>
          </CardHeader>
          <CardContent className="p-5">
            <ChartContainer
              title="Oportunidades abertas por estágio"
              height={290}
              isEmpty={funnelData.length === 0}
              emptyMessage="Nenhum estágio configurado."
            >
              <BarChart
                data={funnelData}
                dataKeys={['Aberto']}
                colors={['#8b5cf6']}
                height={230}
                showLegend={false}
                showGrid={false}
                layout="horizontal"
              />
            </ChartContainer>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="border border-border/70 shadow-sm lg:col-span-2">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-sm font-semibold">
              <Trophy className="h-4 w-4 text-emerald-600" />
              Ranking de vendedores (receita ganha em milhares)
            </CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            {rankingData.length === 0 ? (
              <div className="flex h-[200px] items-center justify-center text-xs text-muted-foreground">
                Nenhum responsável com oportunidades.
              </div>
            ) : (
              <BarChart
                data={rankingData}
                dataKeys={['Ganho']}
                colors={['#22c55e']}
                height={200}
                showLegend={false}
                showGrid={false}
                layout="horizontal"
              />
            )}
          </CardContent>
        </Card>

        <Card className="border border-border/70 shadow-sm">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-sm font-semibold">
              <AlertTriangle className="h-4 w-4 text-amber-600" />
              Saúde do comercial
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 px-4 pb-4">
            {[
              { name: 'Taxa de ganho', value: winRate },
              {
                name: 'Cobertura follow-ups',
                value: summary
                  ? Math.round(
                      ((summary.pendingFollowUpsCount - summary.overdueFollowUpsCount) /
                        Math.max(summary.pendingFollowUpsCount, 1)) *
                        100
                    )
                  : 0,
              },
              {
                name: 'Pipeline vs ganho',
                value: summary
                  ? Math.min(
                      Math.round(
                        (summary.totalPipelineValue / Math.max(summary.wonValue || 1, 1)) * 50
                      ),
                      100
                    )
                  : 0,
              },
              {
                name: 'Fechamento',
                value: summary
                  ? Math.round(
                      (summary.wonOpportunities /
                        Math.max(summary.totalOpportunities || 1, 1)) *
                        100
                    )
                  : 0,
              },
            ].map((item, index) => (
              <div key={item.name} className="space-y-1.5">
                <div className="flex items-center justify-between text-xs">
                  <span className="font-medium text-foreground">{item.name}</span>
                  <span className="font-semibold text-muted-foreground">{item.value}%</span>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-muted">
                  <div
                    className="h-full rounded-full"
                    style={{
                      width: `${Math.min(Math.max(item.value, 0), 100)}%`,
                      backgroundColor: chartColors[index % chartColors.length],
                    }}
                  />
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>

      {alerts.length > 0 ? (
        <Card className="border border-amber-200 bg-amber-50/50 shadow-sm">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-sm font-semibold text-amber-900">
              <AlertTriangle className="h-4 w-4 text-amber-600" />
              {alerts.length} alerta{alerts.length === 1 ? '' : 's'} no comercial
            </CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
              {alerts.slice(0, 6).map((alert, index) => (
                <div
                  key={`${alert.type}-${alert.opportunityId}-${index}`}
                  className="rounded-md border border-amber-200/60 bg-white px-3 py-2 text-xs"
                >
                  <div className="flex items-center gap-1.5">
                    {alert.severity === 'high' ? (
                      <Clock className="h-3 w-3 text-red-500" />
                    ) : (
                      <Megaphone className="h-3 w-3 text-amber-600" />
                    )}
                    <span className="font-semibold text-foreground">{alert.title}</span>
                  </div>
                  <div className="mt-1 text-muted-foreground line-clamp-2">{alert.description}</div>
                  <div className="mt-1 text-[11px] font-medium text-foreground">
                    {alert.opportunityName}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      ) : null}
    </div>
  )
}
