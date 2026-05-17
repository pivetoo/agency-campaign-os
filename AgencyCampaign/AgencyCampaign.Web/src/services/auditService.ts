import { httpClient, type ApiResponse, type PaginationParams } from 'archon-ui'
import type { AuditEntry } from '../types/audit'

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
  getByEntity(
    entityName: string,
    entityId: string | number,
    pagination: PaginationParams = { page: 1, pageSize: 50 },
  ): Promise<ApiResponse<AuditEntry[]>> {
    const query = buildQuery({
      page: pagination.page,
      pageSize: pagination.pageSize,
    })
    return httpClient.get<AuditEntry[]>(
      `${BASE_URL}/entity/${encodeURIComponent(entityName)}/${encodeURIComponent(String(entityId))}${query}`,
    )
  },

  getById(auditEntryId: number): Promise<ApiResponse<AuditEntry>> {
    return httpClient.get<AuditEntry>(`${BASE_URL}/GetById/${auditEntryId}`)
  },
}
