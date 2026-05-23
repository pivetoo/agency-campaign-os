export interface UpcomingClosingItem {
  id: number
  name: string
  brandName?: string
  estimatedValue: number
  probability: number
  expectedCloseAt: string
  daysUntilClose: number
  stageName: string
  stageColor?: string
}

export interface AtRiskItem {
  id: number
  name: string
  brandName?: string
  estimatedValue: number
  stageName: string
  stageColor?: string
  daysInStage: number
}

export interface CommercialOpportunityInsights {
  upcomingClosings: UpcomingClosingItem[]
  atRisk: AtRiskItem[]
}
