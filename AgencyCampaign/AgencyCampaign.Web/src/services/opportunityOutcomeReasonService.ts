import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
import type { OpportunityLossReason, OpportunityWinReason } from '../types/opportunityOutcomeReason'

export interface CreateOpportunityWinReasonRequest {
  name: string
  color: string
  displayOrder: number
}

export interface UpdateOpportunityWinReasonRequest extends CreateOpportunityWinReasonRequest {
  id: number
  isActive: boolean
}

export interface CreateOpportunityLossReasonRequest {
  name: string
  color: string
  displayOrder: number
}

export interface UpdateOpportunityLossReasonRequest extends CreateOpportunityLossReasonRequest {
  id: number
  isActive: boolean
}

const WIN_BASE = '/OpportunityWinReasons'
const LOSS_BASE = '/OpportunityLossReasons'

function buildQuery(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): string {
  const query = buildPaginationQuery(params)
  const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : ''
  const inactiveParam = params?.includeInactive ? `${query || searchParam ? '&' : '?'}includeInactive=true` : ''
  return `${query}${searchParam}${inactiveParam}`
}

export const opportunityWinReasonService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): Promise<ApiResponse<OpportunityWinReason[]>> {
    return httpClient.get<OpportunityWinReason[]>(`${WIN_BASE}/Get${buildQuery(params)}`)
  },
  create(data: CreateOpportunityWinReasonRequest) {
    return httpClient.post<OpportunityWinReason>(`${WIN_BASE}/Create`, data)
  },
  update(id: number, data: UpdateOpportunityWinReasonRequest) {
    return httpClient.put<OpportunityWinReason>(`${WIN_BASE}/Update/${id}`, data)
  },
  delete(id: number) {
    return httpClient.delete(`${WIN_BASE}/Delete/${id}`)
  },
}

export const opportunityLossReasonService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): Promise<ApiResponse<OpportunityLossReason[]>> {
    return httpClient.get<OpportunityLossReason[]>(`${LOSS_BASE}/Get${buildQuery(params)}`)
  },
  create(data: CreateOpportunityLossReasonRequest) {
    return httpClient.post<OpportunityLossReason>(`${LOSS_BASE}/Create`, data)
  },
  update(id: number, data: UpdateOpportunityLossReasonRequest) {
    return httpClient.put<OpportunityLossReason>(`${LOSS_BASE}/Update/${id}`, data)
  },
  delete(id: number) {
    return httpClient.delete(`${LOSS_BASE}/Delete/${id}`)
  },
}
