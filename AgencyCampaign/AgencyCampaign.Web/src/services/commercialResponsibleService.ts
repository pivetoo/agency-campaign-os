import { httpClient } from 'archon-ui'
import type { CommercialResponsible } from '../types/commercialResponsible'

const BASE_URL = '/CommercialResponsibles'

export interface CreateCommercialResponsibleRequest {
  name: string
  email?: string
  phone?: string
  notes?: string
}

export interface UpdateCommercialResponsibleRequest extends CreateCommercialResponsibleRequest {
  id: number
  isActive: boolean
}

export const commercialResponsibleService = {
  async getAll(): Promise<CommercialResponsible[]> {
    const response = await httpClient.get<CommercialResponsible[]>(`${BASE_URL}/Get`)
    return response.data ?? []
  },

  create(data: CreateCommercialResponsibleRequest) {
    return httpClient.post<CommercialResponsible>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateCommercialResponsibleRequest) {
    return httpClient.put<CommercialResponsible>(`${BASE_URL}/Update/${id}`, data)
  },
}
