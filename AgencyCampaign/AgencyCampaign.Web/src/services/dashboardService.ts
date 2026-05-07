import { httpClient } from 'archon-ui'
import type { DashboardOverview } from '../types/dashboard'

export const dashboardService = {
  async getOverview(): Promise<DashboardOverview> {
    const response = await httpClient.get<DashboardOverview>('/Dashboard/Overview')
    return response.data
  },
}
