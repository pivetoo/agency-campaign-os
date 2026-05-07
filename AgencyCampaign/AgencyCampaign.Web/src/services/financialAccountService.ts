import { httpClient } from 'archon-ui'
import type { FinancialAccount } from '../types/financialAccount'

const BASE_URL = '/FinancialAccounts'

export interface CreateFinancialAccountRequest {
  name: string
  type: number
  bank?: string
  agency?: string
  number?: string
  initialBalance: number
  color: string
}

export interface UpdateFinancialAccountRequest extends CreateFinancialAccountRequest {
  id: number
  isActive: boolean
}

export const financialAccountService = {
  async getAll(includeInactive = false): Promise<FinancialAccount[]> {
    const url = includeInactive ? `${BASE_URL}/Get?includeInactive=true` : `${BASE_URL}/Get`
    const response = await httpClient.get<FinancialAccount[]>(url)
    return response.data ?? []
  },

  async getById(id: number): Promise<FinancialAccount | null> {
    const response = await httpClient.get<FinancialAccount>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },

  create(data: CreateFinancialAccountRequest) {
    return httpClient.post<FinancialAccount>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateFinancialAccountRequest) {
    return httpClient.put<FinancialAccount>(`${BASE_URL}/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE_URL}/${id}`)
  },
}
