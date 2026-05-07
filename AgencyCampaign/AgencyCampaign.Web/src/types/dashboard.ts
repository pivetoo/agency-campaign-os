export interface HeadlineSummary {
  activeCampaigns: number
  activeBrands: number
  activeCreators: number
  pendingDeliverables: number
  monthRevenue: number
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

export interface OperationHealthItem {
  name: string
  value: number
}

export interface DashboardOverview {
  headline: HeadlineSummary
  monthlyRevenue: MonthlyRevenueItem[]
  pipeline: PipelineStageItem[]
  platformDistribution: PlatformDistributionItem[]
  creatorGrowth: CreatorGrowthItem[]
  operationHealth: OperationHealthItem[]
}
