import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
import type { CommercialPipelineStage } from '../types/commercialPipelineStage'

const BASE_URL = '/CommercialPipelineStages'

export interface CreateCommercialPipelineStageRequest {
  name: string
  description?: string
  displayOrder: number
  color: string
  isInitial: boolean
  isFinal: boolean
  finalBehavior: number
  defaultProbability?: number
  slaInDays?: number
}

export interface UpdateCommercialPipelineStageRequest extends CreateCommercialPipelineStageRequest {
  id: number
  isActive: boolean
}

export const commercialPipelineStageService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string }): Promise<ApiResponse<CommercialPipelineStage[]>> {
    const query = buildPaginationQuery(params)
    const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : ''
    return httpClient.get<CommercialPipelineStage[]>(`${BASE_URL}/Get${query}${searchParam}`)
  },

  async getActive(): Promise<CommercialPipelineStage[]> {
    const response = await httpClient.get<CommercialPipelineStage[]>(`${BASE_URL}/active`)
    return response.data ?? []
  },

  create(data: CreateCommercialPipelineStageRequest) {
    return httpClient.post<CommercialPipelineStage>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateCommercialPipelineStageRequest) {
    return httpClient.put<CommercialPipelineStage>(`${BASE_URL}/${id}`, data)
  },
}
