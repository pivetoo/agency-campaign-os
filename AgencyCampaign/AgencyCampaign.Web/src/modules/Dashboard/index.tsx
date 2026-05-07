import { useMemo } from 'react'
import {
  AreaChart,
  BarChart,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  ChartContainer,
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

const chartColors = ['#6366f1', '#22c55e', '#f59e0b', '#ec4899', '#06b6d4', '#8b5cf6']

function truncateLabel(value: string) {
  const parts = value.trim().split(/\s+/)
  return parts.length > 1 && value.length > 12 ? `${parts[0]}...` : value
}

function formatCurrencyShort(value: number) {
  if (value >= 1_000_000) return `R$ ${(value / 1_000_000).toFixed(1)}M`
  if (value >= 1_000) return `R$ ${(value / 1_000).toFixed(0)}k`
  return `R$ ${value}`
}

const mockMonthlyRevenue = [
  { name: 'Jun', receita: 180000, fee: 27000 },
  { name: 'Jul', receita: 215000, fee: 32000 },
  { name: 'Ago', receita: 198000, fee: 29500 },
  { name: 'Set', receita: 240000, fee: 36000 },
  { name: 'Out', receita: 285000, fee: 42000 },
  { name: 'Nov', receita: 312000, fee: 47000 },
  { name: 'Dez', receita: 298000, fee: 44500 },
  { name: 'Jan', receita: 340000, fee: 51000 },
  { name: 'Fev', receita: 365000, fee: 54500 },
  { name: 'Mar', receita: 398000, fee: 59500 },
  { name: 'Abr', receita: 421000, fee: 63000 },
  { name: 'Mai', receita: 445000, fee: 66800 },
]

const mockPipeline = [
  { name: 'Prospecção', value: 12 },
  { name: 'Qualificação', value: 8 },
  { name: 'Proposta enviada', value: 5 },
  { name: 'Negociação', value: 3 },
  { name: 'Fechado', value: 7 },
]

const mockPlatformDistribution = [
  { name: 'Instagram Reels', acessos: 42 },
  { name: 'TikTok', acessos: 31 },
  { name: 'YouTube Shorts', acessos: 18 },
  { name: 'Instagram Stories', acessos: 12 },
  { name: 'YouTube Long', acessos: 7 },
]

const mockCreatorGrowth = [
  { name: 'Jun', creators: 84 },
  { name: 'Jul', creators: 92 },
  { name: 'Ago', creators: 98 },
  { name: 'Set', creators: 105 },
  { name: 'Out', creators: 118 },
  { name: 'Nov', creators: 127 },
  { name: 'Dez', creators: 134 },
  { name: 'Jan', creators: 145 },
  { name: 'Fev', creators: 156 },
  { name: 'Mar', creators: 168 },
  { name: 'Abr', creators: 182 },
  { name: 'Mai', creators: 196 },
]

const mockOperationHealth = [
  { name: 'Entregas no prazo', value: 87 },
  { name: 'Taxa de aprovação', value: 92 },
  { name: 'Fee / Budget', value: 15 },
  { name: 'Pipeline ativo', value: 68 },
]

const mockHeadline = {
  activeCampaigns: 18,
  activeBrands: 24,
  activeCreators: 196,
  pendingDeliverables: 31,
  monthRevenue: 445000,
}

export default function Dashboard() {
  const platformData = useMemo(
    () => mockPlatformDistribution.map((item) => ({ ...item, name: truncateLabel(item.name) })),
    []
  )

  const headlineChips = [
    { label: 'Campanhas ativas', value: mockHeadline.activeCampaigns, icon: Megaphone, tone: 'text-indigo-600' },
    { label: 'Marcas', value: mockHeadline.activeBrands, icon: Building2, tone: 'text-violet-600' },
    { label: 'Creators', value: mockHeadline.activeCreators, icon: Users, tone: 'text-cyan-600' },
    { label: 'Entregas pendentes', value: mockHeadline.pendingDeliverables, icon: Clock, tone: 'text-amber-600' },
  ]

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
                Mês atual: <strong className="text-foreground">{formatCurrencyShort(mockHeadline.monthRevenue)}</strong>
              </span>
            </CardTitle>
          </CardHeader>
          <CardContent className="p-5">
            <ChartContainer title="Receita bruta x fee da agência" height={290}>
              <AreaChart
                data={mockMonthlyRevenue}
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
            <ChartContainer title="Oportunidades por estágio" height={290}>
              <PieChart
                data={mockPipeline}
                colors={['#6366f1', '#06b6d4', '#f59e0b', '#ec4899', '#22c55e']}
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
            <BarChart
              data={platformData}
              dataKeys={['acessos']}
              colors={['#f59e0b']}
              height={170}
              showLegend={false}
              showGrid={false}
              layout="horizontal"
            />
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
            <LineChart
              data={mockCreatorGrowth}
              dataKeys={['creators']}
              colors={['#06b6d4']}
              height={170}
              showLegend={false}
              showDots={false}
              enableArea
              areaOpacity={0.14}
            />
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
            {mockOperationHealth.map((item, index) => (
              <div key={item.name} className="space-y-1.5">
                <div className="flex items-center justify-between text-xs">
                  <span className="font-medium text-foreground">{item.name}</span>
                  <span className="font-semibold text-muted-foreground">{item.value}%</span>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-muted">
                  <div
                    className="h-full rounded-full"
                    style={{
                      width: `${item.value}%`,
                      backgroundColor: chartColors[index % chartColors.length],
                    }}
                  />
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
