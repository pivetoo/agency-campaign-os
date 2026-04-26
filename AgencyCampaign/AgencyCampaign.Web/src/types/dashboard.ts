export interface DashboardData {
  activeCampaigns: number
  activeBrands: number
  activeCreators: number
  deliverablesCount: number
  pendingDeliverablesCount: number
  publishedDeliverablesCount: number
  pendingApprovalsCount: number
  totalBudget: number
  totalGrossAmount: number
  totalAgencyFeeAmount: number
}

export interface MonthlyRevenueItem {
  name: string
  receita: number
  fee: number
}

export interface PipelineStageItem {
  name: string
  oportunidades: number
  valor: number
}

export interface PlatformDistributionItem {
  name: string
  value: number
}

export interface CreatorGrowthItem {
  name: string
  ativos: number
  novos: number
}

export interface DashboardChartsData {
  monthlyRevenue: MonthlyRevenueItem[]
  pipeline: PipelineStageItem[]
  platformDistribution: PlatformDistributionItem[]
  creatorGrowth: CreatorGrowthItem[]
}
