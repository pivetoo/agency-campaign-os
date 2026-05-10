export const AuditAction = {
  Insert: 1,
  Update: 2,
  Delete: 3,
} as const

export type AuditActionValue = (typeof AuditAction)[keyof typeof AuditAction]

export const auditActionLabels: Record<AuditActionValue, string> = {
  1: 'Criação',
  2: 'Edição',
  3: 'Exclusão',
}

export interface AuditPropertyChange {
  propertyName: string
  oldValue?: string | null
  newValue?: string | null
}

export interface AuditEntry {
  id: number
  entityName: string
  entityId: string
  tenantId?: string | null
  action: AuditActionValue
  changedAt: string
  changedBy?: string | null
  correlationId?: string | null
  parentEntityName?: string | null
  parentEntityId?: string | null
  source?: string | null
  propertyChanges: AuditPropertyChange[]
}

export interface AuditVolumePoint {
  date: string
  count: number
}

export interface AuditCountByName {
  name: string
  count: number
}

export interface AuditActionCount {
  action: AuditActionValue
  count: number
}

export interface AuditStats {
  totalEntries: number
  volumeByDay: AuditVolumePoint[]
  topUsers: AuditCountByName[]
  topEntities: AuditCountByName[]
  actionDistribution: AuditActionCount[]
}

export interface AuditSearchFilters {
  entityName?: string
  action?: AuditActionValue
  changedBy?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
}
