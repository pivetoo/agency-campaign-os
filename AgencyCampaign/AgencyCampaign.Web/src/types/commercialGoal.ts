export const CommercialGoalPeriodType = {
  Month: 1,
  Quarter: 2,
  Year: 3,
} as const
export type CommercialGoalPeriodTypeValue = (typeof CommercialGoalPeriodType)[keyof typeof CommercialGoalPeriodType]

export const commercialGoalPeriodTypeLabels: Record<CommercialGoalPeriodTypeValue, string> = {
  1: 'Mensal',
  2: 'Trimestral',
  3: 'Anual',
}

export interface CommercialGoal {
  id: number
  userId?: number
  userName?: string
  periodType: CommercialGoalPeriodTypeValue
  periodStart: string
  periodEnd: string
  targetAmount: number
  notes?: string
  isActive: boolean
}

export interface CommercialGoalProgress {
  id: number
  userId?: number
  userName?: string
  periodType: CommercialGoalPeriodTypeValue
  periodStart: string
  periodEnd: string
  targetAmount: number
  achievedAmount: number
  achievedDealsCount: number
  percentAchieved: number
}
