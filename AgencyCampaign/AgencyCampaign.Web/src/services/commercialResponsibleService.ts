import { httpClient } from 'archon-ui'
import type { CommercialResponsible, CommercialUser } from '../types/commercialResponsible'

const BASE_URL = '/CommercialResponsibles'

export interface CreateCommercialResponsibleRequest {
  userId: number
  notes?: string
}

export interface UpdateCommercialResponsibleRequest {
  id: number
  notes?: string
  isActive: boolean
}

export const commercialResponsibleService = {
  async getAll(): Promise<CommercialResponsible[]> {
    const response = await httpClient.get<CommercialResponsible[]>(`${BASE_URL}/Get`)
    return response.data ?? []
  },

  async getAvailableUsers(): Promise<CommercialUser[]> {
    const response = await httpClient.get<CommercialUser[]>(`${BASE_URL}/AvailableUsers`)
    return response.data ?? []
  },

  create(data: CreateCommercialResponsibleRequest) {
    return httpClient.post<CommercialResponsible>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateCommercialResponsibleRequest) {
    return httpClient.put<CommercialResponsible>(`${BASE_URL}/Update/${id}`, data)
  },

  sync(id: number) {
    return httpClient.post<CommercialResponsible>(`${BASE_URL}/${id}/Sync`, {})
  },
}
