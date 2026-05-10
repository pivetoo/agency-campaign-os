import { httpClient } from 'archon-ui'
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
  async getAll(): Promise<Brand[]> {
    const response = await httpClient.get<Brand[]>(`${BASE_URL}/Get`)
    return response.data ?? []
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
}
