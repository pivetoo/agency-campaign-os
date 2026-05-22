export interface StageConversion {
  stageId: number
  stageName: string
  stageColor?: string
  displayOrder: number
  entered: number
  advanced: number
  stuck: number
  lost: number
  conversionRate: number
}

export interface StageTime {
  stageId: number
  stageName: string
  stageColor?: string
  displayOrder: number
  averageDays: number
  samples: number
}

export interface ReasonAggregate {
  reasonId?: number
  reasonName: string
  reasonColor?: string
  count: number
  totalValue: number
}

export interface Performer {
  userId?: number
  userName: string
  wonCount: number
  wonTotal: number
}

export interface CommercialAnalytics {
  periodStart: string
  periodEnd: string
  userId?: number
  closedCount: number
  wonCount: number
  lostCount: number
  winRate: number
  averageCycleDays: number
  conversionByStage: StageConversion[]
  averageTimeInStage: StageTime[]
  winReasons: ReasonAggregate[]
  lossReasons: ReasonAggregate[]
  topPerformers: Performer[]
}
