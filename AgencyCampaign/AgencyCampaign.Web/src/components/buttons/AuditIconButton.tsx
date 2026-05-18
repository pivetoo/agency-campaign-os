import { useState } from 'react'
import { History } from 'lucide-react'
import { usePermissions } from 'archon-ui'
import AuditTimelineModal from '../modals/AuditTimelineModal'

interface AuditIconButtonProps {
  entityName: string
  entityLabel: string
  entityId: number | null
}

export default function AuditIconButton({ entityName, entityLabel, entityId }: AuditIconButtonProps) {
  const { isRoot } = usePermissions()
  const [open, setOpen] = useState(false)

  if (!isRoot) return null

  const enabled = entityId !== null
  const title = enabled ? `Ver auditoria deste registro` : `Selecione um registro para ver auditoria`

  return (
    <>
      <span title={title} className="inline-flex">
        <button
          type="button"
          onClick={() => enabled && setOpen(true)}
          disabled={!enabled}
          className="inline-flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-muted hover:text-primary disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:bg-transparent disabled:hover:text-muted-foreground"
        >
          <History size={16} />
        </button>
      </span>

      <AuditTimelineModal
        entityName={entityName}
        entityLabel={entityLabel}
        entityId={open ? entityId : null}
        open={open}
        onClose={() => setOpen(false)}
      />
    </>
  )
}
