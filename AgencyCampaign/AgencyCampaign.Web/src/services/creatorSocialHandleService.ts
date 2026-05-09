import { httpClient } from 'archon-ui'
import type { CreatorSocialHandle } from '../types/creatorSocialHandle'

export interface CreateCreatorSocialHandleRequest {
  creatorId: number
  platformId: number
  handle: string
  profileUrl?: string
  followers?: number | null
  engagementRate?: number | null
  isPrimary?: boolean
}

export interface UpdateCreatorSocialHandleRequest extends CreateCreatorSocialHandleRequest {
  id: number
  isActive: boolean
}

const BASE = '/CreatorSocialHandles'

export const creatorSocialHandleService = {
  async getByCreator(creatorId: number): Promise<CreatorSocialHandle[]> {
    const response = await httpClient.get<CreatorSocialHandle[]>(`${BASE}/creator/${creatorId}`)
    return response.data ?? []
  },

  create(data: CreateCreatorSocialHandleRequest) {
    return httpClient.post<CreatorSocialHandle>(`${BASE}/Create`, data)
  },

  update(id: number, data: UpdateCreatorSocialHandleRequest) {
    return httpClient.put<CreatorSocialHandle>(`${BASE}/Update/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE}/Delete/${id}`)
  },
}
