import { usePermissions } from 'archon-ui'
import AuditIconButton from './AuditIconButton'

interface AuditUtilityBarProps {
  entityName: string
  entityLabel: string
  entityId: number | null
}

export default function AuditUtilityBar({ entityName, entityLabel, entityId }: AuditUtilityBarProps) {
  const { isRoot } = usePermissions()
  if (!isRoot) return null

  return (
    <div className="flex items-center gap-1">
      <AuditIconButton entityName={entityName} entityLabel={entityLabel} entityId={entityId} />
      <span className="mx-1 h-5 w-px bg-border" aria-hidden />
    </div>
  )
}
