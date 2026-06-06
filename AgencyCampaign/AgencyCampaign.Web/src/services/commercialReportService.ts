import { httpClient } from 'archon-ui'
import { downloadCsvReport } from '../lib/downloadReport'

export interface ProposalsFunnel {
  from: string
  to: string
  emittedCount: number
  emittedValue: number
  acceptedCount: number
  acceptedValue: number
  rejectedCount: number
  acceptanceRate: number
}

export interface BrandRankingLine {
  brandId: number
  brandName: string
  wonCount: number
  lostCount: number
  wonValue: number
  winRate: number
}

export interface BrandRanking {
  generatedAt: string
  from: string
  to: string
  lines: BrandRankingLine[]
}

const BASE_URL = '/CommercialReports'

export const commercialReportService = {
  async getProposalsFunnel(from: string, to: string): Promise<ProposalsFunnel | null> {
    const params = new URLSearchParams({ from, to })
    const response = await httpClient.get<ProposalsFunnel>(`${BASE_URL}/proposals-funnel?${params.toString()}`)
    return response.data ?? null
  },

  async getBrandRanking(from: string, to: string): Promise<BrandRanking | null> {
    const params = new URLSearchParams({ from, to })
    const response = await httpClient.get<BrandRanking>(`${BASE_URL}/brand-ranking?${params.toString()}`)
    return response.data ?? null
  },

  exportProposalsFunnel(from: string, to: string): Promise<void> {
    const params = new URLSearchParams({ from, to })
    return downloadCsvReport(`${BASE_URL}/proposals-funnel/export?${params.toString()}`, 'propostas-funil.csv')
  },

  exportBrandRanking(from: string, to: string): Promise<void> {
    const params = new URLSearchParams({ from, to })
    return downloadCsvReport(`${BASE_URL}/brand-ranking/export?${params.toString()}`, 'ranking-marcas.csv')
  },
}
