import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
import type { OpportunitySource, OpportunityTag } from '../types/opportunitySource'

export interface CreateOpportunitySourceRequest {
  name: string
  color: string
  displayOrder: number
}

export interface UpdateOpportunitySourceRequest extends CreateOpportunitySourceRequest {
  id: number
  isActive: boolean
}

export interface CreateOpportunityTagRequest {
  name: string
  color: string
}

export interface UpdateOpportunityTagRequest extends CreateOpportunityTagRequest {
  id: number
  isActive: boolean
}

const SOURCE_BASE = '/OpportunitySources'
const TAG_BASE = '/OpportunityTags'

function buildQuery(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): string {
  const query = buildPaginationQuery(params)
  const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : ''
  const inactiveParam = params?.includeInactive ? `${query || searchParam ? '&' : '?'}includeInactive=true` : ''
  return `${query}${searchParam}${inactiveParam}`
}

export const opportunitySourceService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): Promise<ApiResponse<OpportunitySource[]>> {
    return httpClient.get<OpportunitySource[]>(`${SOURCE_BASE}/Get${buildQuery(params)}`)
  },

  create(data: CreateOpportunitySourceRequest) {
    return httpClient.post<OpportunitySource>(`${SOURCE_BASE}/Create`, data)
  },

  update(id: number, data: UpdateOpportunitySourceRequest) {
    return httpClient.put<OpportunitySource>(`${SOURCE_BASE}/Update/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${SOURCE_BASE}/Delete/${id}`)
  },
}

export const opportunityTagService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): Promise<ApiResponse<OpportunityTag[]>> {
    return httpClient.get<OpportunityTag[]>(`${TAG_BASE}/Get${buildQuery(params)}`)
  },

  create(data: CreateOpportunityTagRequest) {
    return httpClient.post<OpportunityTag>(`${TAG_BASE}/Create`, data)
  },

  update(id: number, data: UpdateOpportunityTagRequest) {
    return httpClient.put<OpportunityTag>(`${TAG_BASE}/Update/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${TAG_BASE}/Delete/${id}`)
  },
}
