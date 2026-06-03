import { httpClient } from 'archon-ui'
import type { FinancialPeriod } from '../types/financialPeriod'

const BASE_URL = '/FinancialPeriods'

export const financialPeriodService = {
  async getRecent(months = 12): Promise<FinancialPeriod[]> {
    const response = await httpClient.get<FinancialPeriod[]>(`${BASE_URL}/Get?months=${months}`)
    return response.data ?? []
  },

  close(year: number, month: number) {
    return httpClient.post<FinancialPeriod>(`${BASE_URL}/close`, { year, month })
  },

  reopen(year: number, month: number) {
    return httpClient.post<FinancialPeriod>(`${BASE_URL}/reopen`, { year, month })
  },
}
