import { httpClient } from 'archon-ui'
import type { Creator } from '../types/creator'

const BASE_URL = '/Creators'

export interface CreateCreatorRequest {
  name: string
  stageName?: string
  email?: string
  phone?: string
  document?: string
  pixKey?: string
  primaryNiche?: string
  city?: string
  state?: string
  notes?: string
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
    return httpClient.put<Creator>(`${BASE_URL}/${id}`, data)
  },
}
