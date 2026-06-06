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

export interface CashFlowProjectionWeek {
  weekStart: string
  inflow: number
  outflow: number
  net: number
  projectedBalance: number
}

export interface CashFlowProjection {
  generatedAt: string
  openingBalance: number
  weeks: number
  series: CashFlowProjectionWeek[]
}

export interface AccrualResult {
  from: string
  to: string
  revenue: number
  expense: number
  result: number
}

export interface CampaignProfitabilityLine {
  campaignId: number
  campaignName?: string | null
  revenue: number
  creatorCost: number
  otherCost: number
  margin: number
  marginPercent: number
}

export interface CampaignProfitabilityReport {
  generatedAt: string
  lines: CampaignProfitabilityLine[]
  totalRevenue: number
  totalCreatorCost: number
  totalOtherCost: number
  totalMargin: number
}

export interface TaxWithholdingLine {
  creatorId: number
  creatorName?: string | null
  document?: string | null
  taxRegime?: number | null
  grossAmount: number
  taxWithheld: number
  netAmount: number
  paymentCount: number
}

export interface TaxWithholdingReport {
  generatedAt: string
  from: string
  to: string
  lines: TaxWithholdingLine[]
  totalGross: number
  totalWithheld: number
  totalNet: number
}

const BASE_URL = '/FinancialReports'

// Baixa um CSV de relatorio com guarda contra erro-como-blob: um 403/500 com responseType blob vem como
// JSON; nesse caso lemos o corpo como texto e lancamos, em vez de baixar um .csv contendo o erro.
async function downloadCsvReport(url: string, fileName: string): Promise<void> {
  const response = await httpClient.get<Blob>(url, { responseType: 'blob' })
  const blob = response.data as Blob | undefined
  if (!blob) return
  if (!blob.type || !blob.type.includes('csv')) {
    const text = await blob.text().catch(() => '')
    throw new Error(text || 'Falha ao exportar o relatorio.')
  }
  const objectUrl = window.URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = objectUrl
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  window.URL.revokeObjectURL(objectUrl)
}

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

  async getCashFlowProjection(weeks = 12): Promise<CashFlowProjection | null> {
    const response = await httpClient.get<CashFlowProjection>(`${BASE_URL}/cashflow-projection?weeks=${weeks}`)
    return response.data ?? null
  },

  async getAccrualResult(from: string, to: string): Promise<AccrualResult | null> {
    const params = new URLSearchParams({ from, to })
    const response = await httpClient.get<AccrualResult>(`${BASE_URL}/accrual-result?${params.toString()}`)
    return response.data ?? null
  },

  async getCampaignProfitability(): Promise<CampaignProfitabilityReport | null> {
    const response = await httpClient.get<CampaignProfitabilityReport>(`${BASE_URL}/campaign-profitability`)
    return response.data ?? null
  },

  async getTaxWithholding(from: string, to: string): Promise<TaxWithholdingReport | null> {
    const params = new URLSearchParams({ from, to })
    const response = await httpClient.get<TaxWithholdingReport>(`${BASE_URL}/tax-withholding?${params.toString()}`)
    return response.data ?? null
  },

  exportCashFlowProjection(weeks = 12): Promise<void> {
    return downloadCsvReport(`${BASE_URL}/cashflow-projection/export?weeks=${weeks}`, 'projecao-fluxo-caixa.csv')
  },

  exportCashFlow(from: string, to: string, granularity: CashFlowGranularityValue): Promise<void> {
    const params = new URLSearchParams({ from, to, granularity: String(granularity) })
    return downloadCsvReport(`${BASE_URL}/cashflow/export?${params.toString()}`, 'fluxo-de-caixa.csv')
  },

  exportAging(): Promise<void> {
    return downloadCsvReport(`${BASE_URL}/aging/export`, 'aging.csv')
  },

  exportTaxWithholding(from: string, to: string): Promise<void> {
    const params = new URLSearchParams({ from, to })
    return downloadCsvReport(`${BASE_URL}/tax-withholding/export?${params.toString()}`, 'retencoes.csv')
  },

  exportCampaignProfitability(): Promise<void> {
    return downloadCsvReport(`${BASE_URL}/campaign-profitability/export`, 'rentabilidade-campanhas.csv')
  },

  exportAccrualResult(from: string, to: string): Promise<void> {
    const params = new URLSearchParams({ from, to })
    return downloadCsvReport(`${BASE_URL}/accrual-result/export?${params.toString()}`, 'resultado-competencia.csv')
  },
}
