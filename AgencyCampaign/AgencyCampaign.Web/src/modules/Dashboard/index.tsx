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
  LineChart,
  PieChart,
} from 'archon-ui'
import {
  Activity,
  BarChart3,
  LineChart as LineChartIcon,
  PieChart as PieChartIcon,
  Sparkles,
  Megaphone,
  Building2,
  Users,
  Clock,
} from 'lucide-react'
import { dashboardService } from '../../services/dashboardService'
import type { DashboardOverview } from '../../types/dashboard'
import TourButton from '../../components/tour/TourButton'

const chartColors = ['#6366f1', '#22c55e', '#f59e0b', '#ec4899', '#06b6d4', '#8b5cf6']
const pipelineColors = ['#6366f1', '#06b6d4', '#f59e0b', '#ec4899', '#22c55e']

function truncateLabel(value: string) {
  const parts = value.trim().split(/\s+/)
  return parts.length > 1 && value.length > 12 ? `${parts[0]}...` : value
}

function formatCurrencyShort(value: number) {
  if (value >= 1_000_000) return `R$ ${(value / 1_000_000).toFixed(1)}M`
  if (value >= 1_000) return `R$ ${(value / 1_000).toFixed(0)}k`
  return `R$ ${value.toFixed(0)}`
}

export default function Dashboard() {
  const [overview, setOverview] = useState<DashboardOverview | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true

    dashboardService
      .getOverview()
      .then((response) => {
        if (isMounted) {
          setOverview(response)
        }
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

  const platformData = useMemo(
    () => overview?.platformDistribution.map((item) => ({ name: truncateLabel(item.name), value: item.value })) ?? [],
    [overview]
  )

  const headlineChips = useMemo(() => {
    const headline = overview?.headline
    return [
      { label: 'Campanhas ativas', value: headline?.activeCampaigns ?? 0, icon: Megaphone, tone: 'text-indigo-600' },
      { label: 'Marcas', value: headline?.activeBrands ?? 0, icon: Building2, tone: 'text-violet-600' },
      { label: 'Creators', value: headline?.activeCreators ?? 0, icon: Users, tone: 'text-cyan-600' },
      { label: 'Entregas pendentes', value: headline?.pendingDeliverables ?? 0, icon: Clock, tone: 'text-amber-600' },
    ]
  }, [overview])

  if (isLoading) {
    return <GlobalLoader isVisible={true} className="bg-background" />
  }

  const monthlyRevenue = overview?.monthlyRevenue ?? []
  const pipeline = overview?.pipeline ?? []
  const creatorGrowth = overview?.creatorGrowth ?? []
  const operationHealth = overview?.operationHealth ?? []
  const monthRevenue = overview?.headline.monthRevenue ?? 0

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div className="border-l-4 border-primary pl-5">
          <h1 className="text-3xl font-bold text-foreground tracking-tight">
            <strong className="text-primary">Dashboard</strong>
          </h1>
          <p className="text-lg text-muted-foreground mt-3 leading-relaxed">
            Visão geral do desempenho da agência
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <TourButton />
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
                <LineChartIcon className="h-5 w-5 text-primary" />
                Receita dos últimos 12 meses
              </span>
              <span className="text-xs font-normal text-muted-foreground">
                Mês atual: <strong className="text-foreground">{formatCurrencyShort(monthRevenue)}</strong>
              </span>
            </CardTitle>
          </CardHeader>
          <CardContent className="p-5">
            <ChartContainer
              title="Receita bruta x fee da agência"
              height={290}
              isEmpty={monthlyRevenue.length === 0}
              emptyMessage="Nenhuma receita registrada nos últimos 12 meses."
            >
              <AreaChart
                data={monthlyRevenue}
                dataKeys={['receita', 'fee']}
                colors={['#6366f1', '#22c55e']}
                height={230}
                showLegend={false}
                fillOpacity={0.18}
              />
            </ChartContainer>
          </CardContent>
        </Card>

        <Card className="overflow-hidden border border-border/70 shadow-sm">
          <CardHeader className="border-b bg-muted/20 pb-4">
            <CardTitle className="flex items-center gap-2 text-base">
              <PieChartIcon className="h-5 w-5 text-violet-600" />
              Pipeline comercial
            </CardTitle>
          </CardHeader>
          <CardContent className="p-5">
            <ChartContainer
              title="Oportunidades por estágio"
              height={290}
              isEmpty={pipeline.length === 0}
              emptyMessage="Nenhuma oportunidade no pipeline."
            >
              <PieChart
                data={pipeline.map((item) => ({ name: item.name, value: item.oportunidades }))}
                colors={pipelineColors}
                height={230}
                innerRadius={62}
                showLabels={false}
              />
            </ChartContainer>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="border border-border/70 shadow-sm">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-sm font-semibold">
              <BarChart3 className="h-4 w-4 text-amber-600" />
              Plataformas mais entregues
            </CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            {platformData.length === 0 ? (
              <div className="flex h-[170px] items-center justify-center text-xs text-muted-foreground">
                Nenhuma entrega cadastrada.
              </div>
            ) : (
              <BarChart
                data={platformData}
                dataKeys={['value']}
                colors={['#f59e0b']}
                height={170}
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
              <Activity className="h-4 w-4 text-sky-600" />
              Crescimento de creators
            </CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            {creatorGrowth.length === 0 ? (
              <div className="flex h-[170px] items-center justify-center text-xs text-muted-foreground">
                Nenhum creator cadastrado.
              </div>
            ) : (
              <LineChart
                data={creatorGrowth}
                dataKeys={['ativos']}
                colors={['#06b6d4']}
                height={170}
                showLegend={false}
                showDots={false}
                enableArea
                areaOpacity={0.14}
              />
            )}
          </CardContent>
        </Card>

        <Card className="border border-border/70 shadow-sm">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-sm font-semibold">
              <Sparkles className="h-4 w-4 text-emerald-600" />
              Saúde da operação
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 px-4 pb-4">
            {operationHealth.length === 0 ? (
              <div className="flex h-[140px] items-center justify-center text-xs text-muted-foreground">
                Sem dados suficientes.
              </div>
            ) : (
              operationHealth.map((item, index) => (
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
              ))
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
