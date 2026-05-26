import { httpClient } from 'archon-ui'
import type { CampaignReport, CampaignReportLink } from '../types/campaignReport'

const BASE = '/CampaignReports'
const PUBLIC_BASE = '/campaign-report-public'

export const campaignReportService = {
  createOrGetLink(campaignId: number) {
    return httpClient.post<CampaignReportLink>(`${BASE}/campaign/${campaignId}`, {})
  },

  async getByToken(token: string): Promise<CampaignReport | null> {
    try {
      const response = await httpClient.get<CampaignReport>(`${PUBLIC_BASE}/${encodeURIComponent(token)}`)
      return response.data ?? null
    } catch {
      return null
    }
  },
}
