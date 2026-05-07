import { httpClient } from 'archon-ui'

export const CashFlowGranularity = {
  Day: 0,
  Week: 1,
  Month: 2,
} as const

export type CashFlowGranularityValue = (typeof CashFlowGranularity)[keyof typeof CashFlowGranularity]

export interface CashFlowPoint {
  bucket: string
  inflow: number
  outflow: number
  net: number
}

export interface CashFlowSeries {
  from: string
  to: string
  granularity: number
  pending: CashFlowPoint[]
  settled: CashFlowPoint[]
}

export interface AgingBucket {
  label: string
  minDays: number
  maxDays?: number | null
  totalReceivable: number
  receivableCount: number
  totalPayable: number
  payableCount: number
}

export interface AgingReport {
  generatedAt: string
  buckets: AgingBucket[]
}

const BASE_URL = '/FinancialReports'

export const financialReportService = {
  async getCashFlow(from: string, to: string, granularity: CashFlowGranularityValue): Promise<CashFlowSeries | null> {
    const params = new URLSearchParams({ from, to, granularity: String(granularity) })
    const response = await httpClient.get<CashFlowSeries>(`${BASE_URL}/cashflow?${params.toString()}`)
    return response.data ?? null
  },

  async getAging(): Promise<AgingReport | null> {
    const response = await httpClient.get<AgingReport>(`${BASE_URL}/aging`)
    return response.data ?? null
  },
}
