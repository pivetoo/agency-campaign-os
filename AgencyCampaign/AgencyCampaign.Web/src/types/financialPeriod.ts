export interface FinancialPeriod {
  year: number
  month: number
  isClosed: boolean
  closedAt?: string | null
  closedByUserId?: number | null
}
