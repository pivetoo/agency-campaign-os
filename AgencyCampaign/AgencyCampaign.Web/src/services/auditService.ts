import { httpClient } from 'archon-ui'
import type { AuditEntry, AuditSearchFilters, AuditStats } from '../types/audit'

const BASE_URL = '/Audit'

export interface AuditPagedResult {
  items: AuditEntry[]
  pagination: {
    page: number
    pageSize: number
    totalCount: number
    totalPages: number
  }
}

const buildQuery = (params: Record<string, string | number | undefined | null>): string => {
  const search = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') return
    search.append(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export const auditService = {
  async getRecent(filters: AuditSearchFilters): Promise<AuditPagedResult> {
    const query = buildQuery({
      entityName: filters.entityName,
      action: filters.action,
      changedBy: filters.changedBy,
      from: filters.from,
      to: filters.to,
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20,
    })
    const response = await httpClient.get<AuditPagedResult>(`${BASE_URL}/Recent${query}`)
    return (
      response.data ?? {
        items: [],
        pagination: { page: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
      }
    )
  },

  async getStats(from?: string, to?: string): Promise<AuditStats> {
    const query = buildQuery({ from, to })
    const response = await httpClient.get<AuditStats>(`${BASE_URL}/Stats${query}`)
    return (
      response.data ?? {
        totalEntries: 0,
        volumeByDay: [],
        topUsers: [],
        topEntities: [],
        actionDistribution: [],
      }
    )
  },

  async getById(id: number): Promise<AuditEntry | null> {
    const response = await httpClient.get<AuditEntry>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },
}
