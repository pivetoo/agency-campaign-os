import { httpClient } from 'archon-ui'
import type { DashboardData, DashboardChartsData } from '../types/dashboard'

export const dashboardService = {
  async getData(): Promise<DashboardData> {
    const campaignsResponse = await httpClient.get<Array<{ status: number; budget: number }> | { items?: Array<{ status: number; budget: number }> }>('/Campaigns/Get')
    const brandsResponse = await httpClient.get<{ items?: unknown[] } | unknown[]>('/Brands/Get')
    const creatorsResponse = await httpClient.get<{ items?: unknown[] } | unknown[]>('/Creators/Get')
    const deliverablesResponse = await httpClient.get<{ items?: Array<{ status: number; grossAmount: number; agencyFeeAmount: number }> } | Array<{ status: number; grossAmount: number; agencyFeeAmount: number }>>('/CampaignDeliverables/Get')
    const approvalsResponse = await httpClient.get<{ items?: Array<{ status: number }> } | Array<{ status: number }>>('/DeliverableApprovals/Get')

    const campaigns = Array.isArray(campaignsResponse.data) ? campaignsResponse.data : campaignsResponse.data?.items ?? []
    const brands = Array.isArray(brandsResponse.data) ? brandsResponse.data : brandsResponse.data?.items ?? []
    const creators = Array.isArray(creatorsResponse.data) ? creatorsResponse.data : creatorsResponse.data?.items ?? []
    const deliverables = Array.isArray(deliverablesResponse.data) ? deliverablesResponse.data : deliverablesResponse.data?.items ?? []
    const approvals = Array.isArray(approvalsResponse.data) ? approvalsResponse.data : approvalsResponse.data?.items ?? []

    return {
      activeCampaigns: campaigns.filter((item) => item.status === 2 || item.status === 3 || item.status === 4).length,
      activeBrands: brands.length,
      activeCreators: creators.length,
      deliverablesCount: deliverables.length,
      pendingDeliverablesCount: deliverables.filter((item) => item.status === 1 || item.status === 2).length,
      publishedDeliverablesCount: deliverables.filter((item) => item.status === 4).length,
      pendingApprovalsCount: approvals.filter((item) => item.status === 1).length,
      totalBudget: campaigns.reduce((sum, item) => sum + (item.budget ?? 0), 0),
      totalGrossAmount: deliverables.reduce((sum, item) => sum + (item.grossAmount ?? 0), 0),
      totalAgencyFeeAmount: deliverables.reduce((sum, item) => sum + (item.agencyFeeAmount ?? 0), 0),
    }
  },

  async getChartsData(): Promise<DashboardChartsData> {
    const response = await httpClient.get<DashboardChartsData>('/Dashboard/Charts')
    return response.data
  },
}
