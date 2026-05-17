export const AuditActionValue = {
  Insert: 1,
  Update: 2,
  Delete: 3,
} as const

export type AuditAction = typeof AuditActionValue[keyof typeof AuditActionValue]

export interface AuditPropertyChange {
  propertyName: string
  oldValue: string | null
  newValue: string | null
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
