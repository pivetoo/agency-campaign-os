import { useEffect, useRef, useState } from 'react'
import {
  AreaChart,
  BarChart,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  ChartContainer,
  LineChart,
  Modal,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalTitle,
  ModalDescription,
  PieChart,
  useApi,
} from 'archon-ui'
import {
  LayoutDashboard,
  TrendingUp,
  PieChart as PieChartIcon,
  LineChart as LineChartIcon,
  BarChart4,
  Megaphone,
  Building2,
  Users,
  Clock,
  CheckCircle,
  ShieldCheck,
  DollarSign,
  Target,
} from 'lucide-react'
import { dashboardService } from '../../services/dashboardService'
import type { DashboardData, DashboardChartsData } from '../../types/dashboard'

function formatCurrency(value: number) {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  }).format(value)
}

export default function Dashboard() {
  const [data, setData] = useState<DashboardData | null>(null)
  const [chartsData, setChartsData] = useState<DashboardChartsData | null>(null)
  const [isStatsOpen, setIsStatsOpen] = useState(false)
  const { execute: fetchData } = useApi<DashboardData>({ showErrorMessage: true })
  const { execute: fetchCharts } = useApi<DashboardChartsData>({ showErrorMessage: true })

  const fetchDataRef = useRef(fetchData)
  const fetchChartsRef = useRef(fetchCharts)

  useEffect(() => {
    fetchDataRef.current = fetchData
    fetchChartsRef.current = fetchCharts
  })

  useEffect(() => {
    void fetchDataRef.current(() => dashboardService.getData()).then((result) => {
      if (result) {
        setData(result)
      }
    })
    void fetchChartsRef.current(() => dashboardService.getChartsData()).then((result) => {
      if (result) {
        setChartsData(result)
      }
    })
  }, [])

  const kpiCards = [
    {
      title: 'Campanhas ativas',
      value: data?.activeCampaigns ?? 0,
      icon: <Megaphone className="h-5 w-5 text-blue-500" />,
      description: 'Campanhas em andamento',
    },
    {
      title: 'Marcas',
      value: data?.activeBrands ?? 0,
      icon: <Building2 className="h-5 w-5 text-violet-500" />,
      description: 'Marcas cadastradas',
    },
    {
      title: 'Creators',
      value: data?.activeCreators ?? 0,
      icon: <Users className="h-5 w-5 text-cyan-500" />,
      description: 'Creators na base',
    },
    {
      title: 'Entregas pendentes',
      value: data?.pendingDeliverablesCount ?? 0,
      icon: <Clock className="h-5 w-5 text-amber-500" />,
      description: 'Aguardando execução',
    },
    {
      title: 'Entregas publicadas',
      value: data?.publishedDeliverablesCount ?? 0,
      icon: <CheckCircle className="h-5 w-5 text-green-500" />,
      description: 'Já publicadas',
    },
    {
      title: 'Aprovações pendentes',
      value: data?.pendingApprovalsCount ?? 0,
      icon: <ShieldCheck className="h-5 w-5 text-orange-500" />,
      description: 'Aguardando aprovação',
    },
    {
      title: 'Budget total',
      value: formatCurrency(data?.totalBudget ?? 0),
      icon: <DollarSign className="h-5 w-5 text-purple-500" />,
      description: 'Investimento total',
    },
    {
      title: 'Fee da agência',
      value: formatCurrency(data?.totalAgencyFeeAmount ?? 0),
      icon: <DollarSign className="h-5 w-5 text-emerald-500" />,
      description: 'Receita da agência',
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <LayoutDashboard className="h-8 w-8 text-primary" />
          <div>
            <h1 className="text-2xl font-bold">Dashboard</h1>
            <p className="text-sm text-muted-foreground">
              Visão geral do desempenho da agência
            </p>
          </div>
        </div>
        <Button
          variant="outline-primary"
          icon={<BarChart4 className="h-4 w-4" />}
          onClick={() => setIsStatsOpen(true)}
        >
          Estatísticas da Agência
        </Button>
      </div>

      <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
        <ChartContainer
          title="Evolução mensal"
          icon={<TrendingUp className="h-4 w-4 text-primary" />}
          height={300}
          isEmpty={(chartsData?.monthlyRevenue ?? []).length === 0}
          emptyMessage="Nenhuma receita registrada nos últimos 12 meses."
        >
          <AreaChart
            data={chartsData?.monthlyRevenue ?? []}
            xAxisKey="name"
            dataKeys={['receita', 'fee']}
            height={240}
            colors={['hsl(142 71% 45%)', 'hsl(221.2 83.2% 53.3%)']}
            fillOpacity={0.2}
          />
        </ChartContainer>

        <ChartContainer
          title="Pipeline comercial"
          icon={<Target className="h-4 w-4 text-primary" />}
          height={300}
          isEmpty={(chartsData?.pipeline ?? []).length === 0}
          emptyMessage="Nenhuma oportunidade no pipeline."
        >
          <BarChart
            data={chartsData?.pipeline ?? []}
            xAxisKey="name"
            dataKeys={['oportunidades']}
            height={240}
            barSize={28}
            colors={['hsl(221.2 83.2% 53.3%)']}
          />
        </ChartContainer>

        <ChartContainer
          title="Distribuição por plataforma"
          icon={<PieChartIcon className="h-4 w-4 text-primary" />}
          height={300}
          isEmpty={(chartsData?.platformDistribution ?? []).length === 0}
          emptyMessage="Nenhuma entrega cadastrada para exibir distribuição."
        >
          <PieChart
            data={chartsData?.platformDistribution ?? []}
            height={240}
            innerRadius={50}
            outerRadius={90}
            colors={[
              'hsl(221.2 83.2% 53.3%)',
              'hsl(326 100% 60%)',
              'hsl(0 84.2% 60.2%)',
              'hsl(221.2 83.2% 53.3%)',
              'hsl(220 14.3% 95.9%)',
            ]}
          />
        </ChartContainer>

        <ChartContainer
          title="Crescimento de creators"
          icon={<LineChartIcon className="h-4 w-4 text-primary" />}
          height={300}
          isEmpty={(chartsData?.creatorGrowth ?? []).length === 0}
          emptyMessage="Nenhum creator cadastrado nos últimos 12 meses."
        >
          <LineChart
            data={chartsData?.creatorGrowth ?? []}
            xAxisKey="name"
            dataKeys={['ativos', 'novos']}
            height={240}
            colors={['hsl(221.2 83.2% 53.3%)', 'hsl(142 71% 45%)']}
            strokeWidth={2.5}
            showDots
          />
        </ChartContainer>
      </div>

      <Modal open={isStatsOpen} onOpenChange={setIsStatsOpen}>
        <ModalContent size="5xl">
          <ModalHeader>
            <ModalTitle className="flex items-center gap-2">
              <BarChart4 className="h-5 w-5 text-primary" />
              Estatísticas da Agência
            </ModalTitle>
            <ModalDescription>
              Métricas consolidadas de todas as áreas de operação
            </ModalDescription>
          </ModalHeader>
          <ModalBody>
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 py-4">
              {kpiCards.map((card) => (
                <Card
                  key={card.title}
                  className="transition-all hover:shadow-md hover:-translate-y-0.5"
                >
                  <CardHeader className="flex flex-row items-center justify-between pb-3 pt-5 px-5">
                    <CardTitle className="text-base font-medium text-muted-foreground">
                      {card.title}
                    </CardTitle>
                    {card.icon}
                  </CardHeader>
                  <CardContent className="px-5 pb-5">
                    <div className="text-3xl font-bold truncate">{card.value}</div>
                    <p className="mt-2 text-sm text-muted-foreground">
                      {card.description}
                    </p>
                  </CardContent>
                </Card>
              ))}
            </div>
          </ModalBody>
        </ModalContent>
      </Modal>
    </div>
  )
}
