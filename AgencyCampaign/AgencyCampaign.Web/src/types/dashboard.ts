export interface HeadlineSummary {
  activeCampaigns: number
  activeBrands: number
  activeCreators: number
  pendingDeliverables: number
}

export interface CommercialActivityItem {
  name: string
  criadas: number
  ganhas: number
  perdidas: number
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
  commercialActivity: CommercialActivityItem[]
  pipeline: PipelineStageItem[]
  platformDistribution: PlatformDistributionItem[]
  creatorGrowth: CreatorGrowthItem[]
  operationHealth: OperationHealthItem[]
}
