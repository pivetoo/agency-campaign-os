export interface CommercialForecastStageBreakdown {
  stageId: number
  stageName: string
  stageColor?: string
  count: number
  totalValue: number
  weightedValue: number
  averageProbability: number
}

export interface CommercialForecast {
  periodStart: string
  periodEnd: string
  userId?: number
  weightedTotal: number
  unweightedTotal: number
  wonTotal: number
  lostTotal: number
  openCount: number
  wonCount: number
  lostCount: number
  noDateCount: number
  noDateTotal: number
  byStage: CommercialForecastStageBreakdown[]
}
