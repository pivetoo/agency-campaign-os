import { httpClient } from 'archon-ui'
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

export const opportunitySourceService = {
  async getAll(includeInactive = false): Promise<OpportunitySource[]> {
    const url = includeInactive ? `${SOURCE_BASE}/Get?includeInactive=true` : `${SOURCE_BASE}/Get`
    const response = await httpClient.get<OpportunitySource[]>(url)
    return response.data ?? []
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
  async getAll(includeInactive = false): Promise<OpportunityTag[]> {
    const url = includeInactive ? `${TAG_BASE}/Get?includeInactive=true` : `${TAG_BASE}/Get`
    const response = await httpClient.get<OpportunityTag[]>(url)
    return response.data ?? []
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
