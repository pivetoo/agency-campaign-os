export const AuditActionValue = {
  Insert: 1,
  Update: 2,
  Delete: 3,
} as const

export type AuditAction = typeof AuditActionValue[keyof typeof AuditActionValue]

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
  action: AuditAction
  changedAt: string
  changedBy?: string | null
  correlationId?: string | null
  parentEntityName?: string | null
  parentEntityId?: string | null
  source?: string | null
  propertyChanges: AuditPropertyChange[]
}

export interface AuditCountByName {
  name: string
  count: number
}

export interface AuditActionCount {
  action: AuditAction
  count: number
}

export interface AuditVolumePoint {
  date: string
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
  action?: AuditAction
  changedBy?: string
  from?: string
  to?: string
}
