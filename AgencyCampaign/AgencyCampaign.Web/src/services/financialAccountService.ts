import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
import type { FinancialAccount, FinancialAccountSummary, FinancialAccountTypeValue } from '../types/financialAccount'

const BASE_URL = '/FinancialAccounts'

export interface CreateFinancialAccountRequest {
  name: string
  type: FinancialAccountTypeValue
  bankId?: number | null
  bank?: string
  agency?: string
  number?: string
  initialBalance: number
  color?: string
}

export interface UpdateFinancialAccountRequest extends CreateFinancialAccountRequest {
  id: number
  isActive: boolean
}

export const financialAccountService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): Promise<ApiResponse<FinancialAccount[]>> {
    const query = buildPaginationQuery(params)
    const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : ''
    const inactiveParam = params?.includeInactive ? `${query || searchParam ? '&' : '?'}includeInactive=true` : ''
    return httpClient.get<FinancialAccount[]>(`${BASE_URL}/Get${query}${searchParam}${inactiveParam}`)
  },

  getSummary(): Promise<ApiResponse<FinancialAccountSummary>> {
    return httpClient.get<FinancialAccountSummary>(`${BASE_URL}/GetSummary`)
  },

  async getById(id: number): Promise<FinancialAccount | null> {
    const response = await httpClient.get<FinancialAccount>(`${BASE_URL}/GetById/${id}`)
    return response.data ?? null
  },

  create(data: CreateFinancialAccountRequest) {
    return httpClient.post<FinancialAccount>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateFinancialAccountRequest) {
    return httpClient.put<FinancialAccount>(`${BASE_URL}/Update/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE_URL}/Delete/${id}`)
  },

  attachConnector(id: number, connectorId: number) {
    return httpClient.put<FinancialAccount>(`${BASE_URL}/attach-connector/${id}`, { connectorId })
  },

  detachConnector(id: number) {
    return httpClient.put<FinancialAccount>(`${BASE_URL}/detach-connector/${id}`, {})
  },

  sync(id: number) {
    return httpClient.post<{ executionId: number }>(`${BASE_URL}/sync/${id}`, {})
  },
}
