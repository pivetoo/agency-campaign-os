import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
import type { CommercialGoal, CommercialGoalPeriodTypeValue, CommercialGoalProgress } from '../types/commercialGoal'

export interface CreateCommercialGoalRequest {
  userId?: number | null
  periodType: CommercialGoalPeriodTypeValue
  periodStart: string
  targetAmount: number
  notes?: string
}

export interface UpdateCommercialGoalRequest extends CreateCommercialGoalRequest {
  id: number
  isActive: boolean
}

const BASE = '/CommercialGoals'

export const commercialGoalService = {
  getAll(params?: { page?: number; pageSize?: number; includeInactive?: boolean; userId?: number; periodType?: CommercialGoalPeriodTypeValue }): Promise<ApiResponse<CommercialGoal[]>> {
    const pagination = buildPaginationQuery({ page: params?.page, pageSize: params?.pageSize })
    const extras: string[] = []
    if (params?.includeInactive) extras.push('includeInactive=true')
    if (params?.userId) extras.push(`userId=${params.userId}`)
    if (params?.periodType) extras.push(`periodType=${params.periodType}`)
    const extra = extras.join('&')
    let url = `${BASE}/Get`
    if (pagination && extra) url += `${pagination}&${extra}`
    else if (pagination) url += pagination
    else if (extra) url += `?${extra}`
    return httpClient.get<CommercialGoal[]>(url)
  },

  async progress(params?: { referenceDate?: string; userId?: number; periodType?: CommercialGoalPeriodTypeValue }): Promise<CommercialGoalProgress[]> {
    const search = new URLSearchParams()
    if (params?.referenceDate) search.set('referenceDate', params.referenceDate)
    if (params?.userId) search.set('userId', String(params.userId))
    if (params?.periodType) search.set('periodType', String(params.periodType))
    const query = search.toString()
    const url = query ? `${BASE}/Progress?${query}` : `${BASE}/Progress`
    const response = await httpClient.get<CommercialGoalProgress[]>(url)
    return response.data ?? []
  },

  create(data: CreateCommercialGoalRequest) {
    return httpClient.post<CommercialGoal>(`${BASE}/Create`, data)
  },

  update(id: number, data: UpdateCommercialGoalRequest) {
    return httpClient.put<CommercialGoal>(`${BASE}/Update/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE}/Delete/${id}`)
  },
}
