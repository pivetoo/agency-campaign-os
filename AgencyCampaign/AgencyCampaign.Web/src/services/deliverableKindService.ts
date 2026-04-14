import { httpClient } from 'archon-ui'
import type { DeliverableKind } from '../types/deliverableKind'

const BASE_URL = '/DeliverableKinds'

export interface CreateDeliverableKindRequest {
  name: string
  displayOrder: number
}

export interface UpdateDeliverableKindRequest extends CreateDeliverableKindRequest {
  id: number
  isActive: boolean
}

function extractItems<T>(data: T[] | { items?: T[] } | undefined): T[] {
  return Array.isArray(data) ? data : data?.items ?? []
}

export const deliverableKindService = {
  async getAll(): Promise<DeliverableKind[]> {
    const response = await httpClient.get<DeliverableKind[] | { items?: DeliverableKind[] }>(`${BASE_URL}/Get`)
    return extractItems(response.data)
  },

  async getActive(): Promise<DeliverableKind[]> {
    const response = await httpClient.get<DeliverableKind[]>(`${BASE_URL}/active`)
    return response.data ?? []
  },

  create(data: CreateDeliverableKindRequest) {
    return httpClient.post<DeliverableKind>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateDeliverableKindRequest) {
    return httpClient.put<DeliverableKind>(`${BASE_URL}/Update/${id}`, data)
  },
}
