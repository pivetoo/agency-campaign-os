import { httpClient, type PaginationParams, type PaginatedResult } from 'archon-ui'
import type { AuditEntry, AuditSearchFilters, AuditStats } from '../types/audit'

const BASE_URL = '/Audit'

function buildQuery(params: Record<string, string | number | undefined | null>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue
    search.set(key, String(value))
  }
  const query = search.toString()
  return query ? `?${query}` : ''
}

export const auditService = {
  async recent(filters: AuditSearchFilters, pagination: PaginationParams = { page: 1, pageSize: 25 }): Promise<PaginatedResult<AuditEntry>> {
    const query = buildQuery({
      entityName: filters.entityName,
      action: filters.action,
      changedBy: filters.changedBy,
      from: filters.from,
      to: filters.to,
      page: pagination.page,
      pageSize: pagination.pageSize,
      orderBy: pagination.orderBy ?? 'id',
    })
    const response = await httpClient.get<PaginatedResult<AuditEntry>>(`${BASE_URL}/Recent${query}`)
    return response.data ?? { data: [], totalCount: 0, page: 1, pageSize: 25 } as PaginatedResult<AuditEntry>
  },

  async getById(id: number): Promise<AuditEntry | null> {
    const response = await httpClient.get<AuditEntry>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },

  async getByEntity(entityName: string, entityId: string, pagination: PaginationParams = { page: 1, pageSize: 25 }): Promise<PaginatedResult<AuditEntry>> {
    const query = buildQuery({
      page: pagination.page,
      pageSize: pagination.pageSize,
      orderBy: pagination.orderBy ?? 'id',
    })
    const response = await httpClient.get<PaginatedResult<AuditEntry>>(`${BASE_URL}/entity/${encodeURIComponent(entityName)}/${encodeURIComponent(entityId)}${query}`)
    return response.data ?? { data: [], totalCount: 0, page: 1, pageSize: 25 } as PaginatedResult<AuditEntry>
  },

  async stats(from?: string, to?: string): Promise<AuditStats | null> {
    const query = buildQuery({ from, to })
    const response = await httpClient.get<AuditStats>(`${BASE_URL}/Stats${query}`)
    return response.data ?? null
  },
}
