import { httpClient } from 'archon-ui'
import type { FinancialSubcategory } from '../types/financialSubcategory'

export interface CreateFinancialSubcategoryRequest {
  name: string
  macroCategory: number
  color: string
}

export interface UpdateFinancialSubcategoryRequest extends CreateFinancialSubcategoryRequest {
  id: number
  isActive: boolean
}

const BASE_URL = '/FinancialSubcategories'

export const financialSubcategoryService = {
  async getAll(includeInactive = false): Promise<FinancialSubcategory[]> {
    const url = includeInactive ? `${BASE_URL}/Get?includeInactive=true` : `${BASE_URL}/Get`
    const response = await httpClient.get<FinancialSubcategory[]>(url)
    return response.data ?? []
  },

  create(data: CreateFinancialSubcategoryRequest) {
    return httpClient.post<FinancialSubcategory>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateFinancialSubcategoryRequest) {
    return httpClient.put<FinancialSubcategory>(`${BASE_URL}/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE_URL}/${id}`)
  },
}
