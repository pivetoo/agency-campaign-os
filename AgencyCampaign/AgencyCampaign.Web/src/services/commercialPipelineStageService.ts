import { httpClient } from 'archon-ui'
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

function extractItems<T>(data: T[] | { items?: T[] } | undefined): T[] {
  return Array.isArray(data) ? data : data?.items ?? []
}

export const commercialPipelineStageService = {
  async getAll(): Promise<CommercialPipelineStage[]> {
    const response = await httpClient.get<CommercialPipelineStage[] | { items?: CommercialPipelineStage[] }>(`${BASE_URL}/Get`)
    return extractItems(response.data)
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
