import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
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

export const deliverableKindService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string }): Promise<ApiResponse<DeliverableKind[]>> {
    const query = buildPaginationQuery(params)
    const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : ''
    return httpClient.get<DeliverableKind[]>(`${BASE_URL}/Get${query}${searchParam}`)
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
