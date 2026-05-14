import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
import type { Brand } from '../types/brand'
import { resolveUploadUrl } from '../lib/uploadUrl'

const BASE_URL = '/Brands'

export const resolveBrandLogoUrl = resolveUploadUrl

export interface CreateBrandRequest {
  name: string
  tradeName?: string
  document?: string
  contactName?: string
  contactEmail?: string
  notes?: string
}

export interface UpdateBrandRequest extends CreateBrandRequest {
  id: number
  isActive: boolean
}

export const brandService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): Promise<ApiResponse<Brand[]>> {
    const query = buildPaginationQuery(params)
    const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : ''
    const inactiveParam = params?.includeInactive ? `${query || searchParam ? '&' : '?'}includeInactive=true` : ''
    return httpClient.get<Brand[]>(`${BASE_URL}/Get${query}${searchParam}${inactiveParam}`)
  },

  create(data: CreateBrandRequest) {
    return httpClient.post<Brand>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateBrandRequest) {
    return httpClient.put<Brand>(`${BASE_URL}/Update/${id}`, data)
  },

  uploadLogo(id: number, file: File) {
    const formData = new FormData()
    formData.append('file', file)
    return httpClient.post<Brand>(`${BASE_URL}/UploadLogo/${id}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  removeLogo(id: number) {
    return httpClient.delete<Brand>(`${BASE_URL}/RemoveLogo/${id}`)
  },

  async exportCsv(): Promise<Blob> {
    const response = await httpClient.get<Blob>(`${BASE_URL}/Export`, { responseType: 'blob' })
    return response.data as Blob
  },
}
