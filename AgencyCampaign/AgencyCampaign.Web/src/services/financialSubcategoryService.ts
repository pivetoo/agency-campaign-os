import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
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
  getAll(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): Promise<ApiResponse<FinancialSubcategory[]>> {
    const query = buildPaginationQuery(params)
    const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : ''
    const inactiveParam = params?.includeInactive ? `${query || searchParam ? '&' : '?'}includeInactive=true` : ''
    return httpClient.get<FinancialSubcategory[]>(`${BASE_URL}/Get${query}${searchParam}${inactiveParam}`)
  },

  create(data: CreateFinancialSubcategoryRequest) {
    return httpClient.post<FinancialSubcategory>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateFinancialSubcategoryRequest) {
    return httpClient.put<FinancialSubcategory>(`${BASE_URL}/Update/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE_URL}/Delete/${id}`)
  },
}
