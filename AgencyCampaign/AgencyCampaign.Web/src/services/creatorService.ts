import { httpClient } from 'archon-ui'
import type { Creator } from '../types/creator'
import type { PixKeyTypeValue } from '../types/creatorPayment'
import type { CreatorCampaignEntry, CreatorSummary } from '../types/creatorSocialHandle'
import { resolveUploadUrl } from '../lib/uploadUrl'

const BASE_URL = '/Creators'

export const resolveCreatorPhotoUrl = resolveUploadUrl

export interface CreateCreatorRequest {
  name: string
  stageName?: string
  email?: string
  phone?: string
  document?: string
  pixKey?: string
  pixKeyType?: PixKeyTypeValue
  primaryNiche?: string
  city?: string
  state?: string
  notes?: string
  defaultAgencyFeePercent: number
}

export interface UpdateCreatorRequest extends CreateCreatorRequest {
  id: number
  isActive: boolean
}

export const creatorService = {
  async getAll(): Promise<Creator[]> {
    const response = await httpClient.get<Creator[]>(`${BASE_URL}/Get`)
    return response.data ?? []
  },

  create(data: CreateCreatorRequest) {
    return httpClient.post<Creator>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateCreatorRequest) {
    return httpClient.put<Creator>(`${BASE_URL}/Update/${id}`, data)
  },

  async getById(id: number): Promise<Creator | null> {
    const response = await httpClient.get<Creator>(`${BASE_URL}/GetById/${id}`)
    return response.data ?? null
  },

  async getSummary(id: number): Promise<CreatorSummary | null> {
    const response = await httpClient.get<CreatorSummary>(`${BASE_URL}/summary/${id}`)
    return response.data ?? null
  },

  async getCampaigns(id: number): Promise<CreatorCampaignEntry[]> {
    const response = await httpClient.get<CreatorCampaignEntry[]>(`${BASE_URL}/campaigns/${id}`)
    return response.data ?? []
  },

  uploadPhoto(id: number, file: File) {
    const formData = new FormData()
    formData.append('file', file)
    return httpClient.post<Creator>(`${BASE_URL}/UploadPhoto/${id}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  removePhoto(id: number) {
    return httpClient.delete<Creator>(`${BASE_URL}/RemovePhoto/${id}`)
  },

  async exportCsv(): Promise<Blob> {
    const response = await httpClient.get<Blob>(`${BASE_URL}/Export`, { responseType: 'blob' })
    return response.data as Blob
  },
}
