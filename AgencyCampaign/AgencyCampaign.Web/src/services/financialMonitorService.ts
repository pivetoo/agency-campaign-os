import { httpClient } from 'archon-ui'
import type { FinancialMonitor } from '../types/financialMonitor'

const BASE_URL = '/FinancialMonitor'

export const financialMonitorService = {
  async get(): Promise<FinancialMonitor | null> {
    const response = await httpClient.get<FinancialMonitor>(`${BASE_URL}/Get`)
    return response.data ?? null
  },
}
