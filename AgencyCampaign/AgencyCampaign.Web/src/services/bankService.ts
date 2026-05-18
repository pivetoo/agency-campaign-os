import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
import type { Bank, CreateBankRequest, UpdateBankRequest } from '../types/bank'
import { resolveUploadUrl } from '../lib/uploadUrl'

const BASE_URL = '/Banks'

export const resolveBankLogoUrl = resolveUploadUrl

export const bankService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): Promise<ApiResponse<Bank[]>> {
    const query = buildPaginationQuery(params)
    const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : ''
    const inactiveParam = params?.includeInactive ? `${query || searchParam ? '&' : '?'}includeInactive=true` : ''
    return httpClient.get<Bank[]>(`${BASE_URL}/Get${query}${searchParam}${inactiveParam}`)
  },

  async getActive(): Promise<Bank[]> {
    const response = await httpClient.get<Bank[]>(`${BASE_URL}/active`)
    return response.data ?? []
  },

  async getById(id: number): Promise<Bank | null> {
    const response = await httpClient.get<Bank>(`${BASE_URL}/GetById/${id}`)
    return response.data ?? null
  },

  create(data: CreateBankRequest) {
    return httpClient.post<Bank>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateBankRequest) {
    return httpClient.put<Bank>(`${BASE_URL}/Update/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE_URL}/Delete/${id}`)
  },
}
