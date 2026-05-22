import { useEffect, useMemo, useState } from 'react'
import { Button, Checkbox, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, useApi, useI18n } from 'archon-ui'
import { opportunityWinReasonService, opportunityLossReasonService } from '../../services/opportunityOutcomeReasonService'
import type { OpportunityWinReason, OpportunityLossReason } from '../../types/opportunityOutcomeReason'

type ReasonKind = 'win' | 'loss'

type ReasonItem = OpportunityWinReason | OpportunityLossReason | null

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  kind: ReasonKind
  reason: ReasonItem
  onSuccess: () => void
}

const defaults: Record<ReasonKind, { color: string; placeholder: string; titleNew: string; titleEdit: string }> = {
  win: { color: '#15803d', placeholder: 'Ex.: Melhor proposta, Indicação...', titleNew: 'Novo motivo de ganho', titleEdit: 'Editar motivo de ganho' },
  loss: { color: '#b91c1c', placeholder: 'Ex.: Preço, Concorrente, Timing...', titleNew: 'Novo motivo de perda', titleEdit: 'Editar motivo de perda' },
}

export default function OpportunityOutcomeReasonFormModal({ open, onOpenChange, kind, reason, onSuccess }: Props) {
  const { t } = useI18n()
  const isEditing = !!reason
  const cfg = defaults[kind]
  const [name, setName] = useState('')
  const [color, setColor] = useState(cfg.color)
  const [displayOrder, setDisplayOrder] = useState(0)
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    if (reason) {
      setName(reason.name)
      setColor(reason.color)
      setDisplayOrder(reason.displayOrder)
      setIsActive(reason.isActive)
    } else {
      setName('')
      setColor(cfg.color)
      setDisplayOrder(0)
      setIsActive(true)
    }
  }, [open, reason, cfg.color])

  const isValid = useMemo(() => name.trim().length >= 2, [name])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    const result = await execute(() => {
      if (kind === 'win') {
        if (isEditing && reason) {
          return opportunityWinReasonService.update(reason.id, { id: reason.id, name: name.trim(), color, displayOrder, isActive })
        }
        return opportunityWinReasonService.create({ name: name.trim(), color, displayOrder })
      }
      if (isEditing && reason) {
        return opportunityLossReasonService.update(reason.id, { id: reason.id, name: name.trim(), color, displayOrder, isActive })
      }
      return opportunityLossReasonService.create({ name: name.trim(), color, displayOrder })
    })
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle>{isEditing ? cfg.titleEdit : cfg.titleNew}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('common.field.name')}</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder={cfg.placeholder} required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.color')}</label>
              <Input type="color" value={color} onChange={(e) => setColor(e.target.value)} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.order')}</label>
              <Input type="number" value={displayOrder} onChange={(e) => setDisplayOrder(Number(e.target.value))} />
            </div>
          </div>
          {isEditing && (
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span>{t('common.status.active')}</span>
            </label>
          )}
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !isValid}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
