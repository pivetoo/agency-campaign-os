import { httpClient } from 'archon-ui'

export interface RateCardItem {
  id: number
  creatorId: number
  label: string
  unitPrice: number
  displayOrder: number
  isActive: boolean
}

export interface CreateRateCardItemRequest {
  creatorId: number
  label: string
  unitPrice: number
  displayOrder?: number
}

export interface UpdateRateCardItemRequest {
  id: number
  label: string
  unitPrice: number
  displayOrder?: number
  isActive: boolean
}

const BASE = '/RateCardItems'

export const rateCardItemService = {
  async getByCreator(creatorId: number, includeInactive = false): Promise<RateCardItem[]> {
    const response = await httpClient.get<RateCardItem[]>(`${BASE}/creator/${creatorId}?includeInactive=${includeInactive}`)
    return response.data ?? []
  },

  create(data: CreateRateCardItemRequest) {
    return httpClient.post<RateCardItem>(`${BASE}/Create`, data)
  },

  update(id: number, data: UpdateRateCardItemRequest) {
    return httpClient.put<RateCardItem>(`${BASE}/Update/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE}/Delete/${id}`)
  },
}
