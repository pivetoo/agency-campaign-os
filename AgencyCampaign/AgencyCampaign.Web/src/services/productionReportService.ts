import { httpClient } from 'archon-ui'
import { downloadCsvReport } from '../lib/downloadReport'

export interface CampaignPerformanceLine { campaignId: number; campaignName: string; brandName?: string | null; deliverables: number; totalReach: number; totalImpressions: number; totalEngagement: number; avgEngagementRate?: number | null; emv?: number | null }
export interface CampaignPerformance { generatedAt: string; from: string; to: string; lines: CampaignPerformanceLine[] }

export interface CreatorPerformanceLine { creatorId: number; creatorName: string; campaigns: number; deliverables: number; totalReach: number; totalEngagement: number; avgEngagementRate?: number | null }
export interface CreatorPerformance { generatedAt: string; from: string; to: string; lines: CreatorPerformanceLine[] }

export interface PlatformProductionLine { platformId: number; platformName: string; deliverables: number; totalReach: number; totalImpressions: number; totalEngagement: number; avgEngagementRate?: number | null }
export interface PlatformProduction { generatedAt: string; from: string; to: string; lines: PlatformProductionLine[] }

export interface DeliverableSlaCampaignLine { campaignId: number; campaignName: string; total: number; publishedOnTime: number; publishedLate: number; overdue: number; upcoming: number }
export interface DeliverableSla { generatedAt: string; from: string; to: string; publishedOnTime: number; publishedLate: number; overdue: number; upcoming: number; onTimeRate: number; byCampaign: DeliverableSlaCampaignLine[] }

export interface ApprovalCycle { generatedAt: string; from: string; to: string; internalApprovedCount: number; brandApprovedCount: number; avgInternalApprovalDays?: number | null; avgBrandApprovalDays?: number | null; contentApprovedCount: number; avgRounds?: number | null; firstRoundApprovalRate?: number | null }

export interface ContentLicenseReportLine { licenseId: number; campaignDeliverableId: number; deliverableTitle: string; campaignName?: string | null; type: number; channels?: string | null; startsAt?: string | null; expiresAt?: string | null; daysUntilExpiry?: number | null; status: number }
export interface ContentLicenseReport { generatedAt: string; expiringSoonDays: number; activeCount: number; expiringSoonCount: number; expiredCount: number; lines: ContentLicenseReportLine[] }

const BASE_URL = '/ProductionReports'

export const productionReportService = {
  async getCampaignPerformance(from: string, to: string): Promise<CampaignPerformance | null> {
    const params = new URLSearchParams({ from, to })
    const response = await httpClient.get<CampaignPerformance>(`${BASE_URL}/campaign-performance?${params.toString()}`)
    return response.data ?? null
  },

  async getCreatorPerformance(from: string, to: string): Promise<CreatorPerformance | null> {
    const params = new URLSearchParams({ from, to })
    const response = await httpClient.get<CreatorPerformance>(`${BASE_URL}/creator-performance?${params.toString()}`)
    return response.data ?? null
  },

  async getPlatformProduction(from: string, to: string): Promise<PlatformProduction | null> {
    const params = new URLSearchParams({ from, to })
    const response = await httpClient.get<PlatformProduction>(`${BASE_URL}/platform-production?${params.toString()}`)
    return response.data ?? null
  },

  async getDeliverableSla(from: string, to: string): Promise<DeliverableSla | null> {
    const params = new URLSearchParams({ from, to })
    const response = await httpClient.get<DeliverableSla>(`${BASE_URL}/deliverable-sla?${params.toString()}`)
    return response.data ?? null
  },

  async getApprovalCycle(from: string, to: string): Promise<ApprovalCycle | null> {
    const params = new URLSearchParams({ from, to })
    const response = await httpClient.get<ApprovalCycle>(`${BASE_URL}/approval-cycle?${params.toString()}`)
    return response.data ?? null
  },

  async getContentLicenses(expiringSoonDays = 30): Promise<ContentLicenseReport | null> {
    const params = new URLSearchParams({ expiringSoonDays: String(expiringSoonDays) })
    const response = await httpClient.get<ContentLicenseReport>(`${BASE_URL}/content-licenses?${params.toString()}`)
    return response.data ?? null
  },

  exportCampaignPerformance(from: string, to: string): Promise<void> {
    const params = new URLSearchParams({ from, to })
    return downloadCsvReport(`${BASE_URL}/campaign-performance/export?${params.toString()}`, 'performance-campanhas.csv')
  },

  exportCreatorPerformance(from: string, to: string): Promise<void> {
    const params = new URLSearchParams({ from, to })
    return downloadCsvReport(`${BASE_URL}/creator-performance/export?${params.toString()}`, 'performance-creators.csv')
  },

  exportPlatformProduction(from: string, to: string): Promise<void> {
    const params = new URLSearchParams({ from, to })
    return downloadCsvReport(`${BASE_URL}/platform-production/export?${params.toString()}`, 'producao-plataforma.csv')
  },

  exportDeliverableSla(from: string, to: string): Promise<void> {
    const params = new URLSearchParams({ from, to })
    return downloadCsvReport(`${BASE_URL}/deliverable-sla/export?${params.toString()}`, 'sla-entregaveis.csv')
  },

  exportApprovalCycle(from: string, to: string): Promise<void> {
    const params = new URLSearchParams({ from, to })
    return downloadCsvReport(`${BASE_URL}/approval-cycle/export?${params.toString()}`, 'ciclo-aprovacao.csv')
  },

  exportContentLicenses(expiringSoonDays = 30): Promise<void> {
    const params = new URLSearchParams({ expiringSoonDays: String(expiringSoonDays) })
    return downloadCsvReport(`${BASE_URL}/content-licenses/export?${params.toString()}`, 'licencas-conteudo.csv')
  },
}
