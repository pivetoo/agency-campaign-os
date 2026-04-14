import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle, useApi } from 'archon-ui'
import { LayoutDashboard, Building2, Users, Megaphone, CheckCircle, Clock, DollarSign, ShieldCheck } from 'lucide-react'
import { dashboardService } from '../../services/dashboardService'
import type { DashboardData } from '../../types/dashboard'

export default function Dashboard() {
  const [data, setData] = useState<DashboardData | null>(null)
  const { execute: fetchData } = useApi<DashboardData>({ showErrorMessage: true })

  useEffect(() => {
    void fetchData(() => dashboardService.getData()).then((result) => {
      if (result) {
        setData(result)
      }
    })
  }, [])

  const cards = [
    { title: 'Campanhas ativas', value: data?.activeCampaigns ?? 0, icon: <Megaphone className="h-5 w-5 text-blue-500" /> },
    { title: 'Marcas', value: data?.activeBrands ?? 0, icon: <Building2 className="h-5 w-5 text-violet-500" /> },
    { title: 'Creators', value: data?.activeCreators ?? 0, icon: <Users className="h-5 w-5 text-cyan-500" /> },
    { title: 'Entregas pendentes', value: data?.pendingDeliverablesCount ?? 0, icon: <Clock className="h-5 w-5 text-amber-500" /> },
    { title: 'Entregas publicadas', value: data?.publishedDeliverablesCount ?? 0, icon: <CheckCircle className="h-5 w-5 text-green-500" /> },
    { title: 'Aprovações pendentes', value: data?.pendingApprovalsCount ?? 0, icon: <ShieldCheck className="h-5 w-5 text-orange-500" /> },
    { title: 'Budget total', value: `R$ ${(data?.totalBudget ?? 0).toFixed(2)}`, icon: <DollarSign className="h-5 w-5 text-purple-500" /> },
    { title: 'Fee da agência', value: `R$ ${(data?.totalAgencyFeeAmount ?? 0).toFixed(2)}`, icon: <DollarSign className="h-5 w-5 text-emerald-500" /> },
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <LayoutDashboard className="h-8 w-8 text-primary" />
        <h1 className="text-3xl font-bold">Dashboard</h1>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        {cards.map((card) => (
          <Card key={card.title}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">{card.title}</CardTitle>
              {card.icon}
            </CardHeader>
            <CardContent>
              <div className="text-3xl font-bold">{card.value}</div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}
