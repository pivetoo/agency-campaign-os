import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, useApi, useI18n } from 'archon-ui'
import { deliverableKindService, type CreateDeliverableKindRequest, type UpdateDeliverableKindRequest } from '../../services/deliverableKindService'
import type { DeliverableKind } from '../../types/deliverableKind'

interface DeliverableKindFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  deliverableKind: DeliverableKind | null
  onSuccess: () => void
}

const initialFormData: CreateDeliverableKindRequest = {
  name: '',
  displayOrder: 0,
}

export default function DeliverableKindFormModal({ open, onOpenChange, deliverableKind, onSuccess }: DeliverableKindFormModalProps) {
  const { t } = useI18n()
  const isEditing = !!deliverableKind
  const [formData, setFormData] = useState<CreateDeliverableKindRequest>(initialFormData)
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (deliverableKind) {
      setFormData({
        name: deliverableKind.name,
        displayOrder: deliverableKind.displayOrder,
      })
      setIsActive(deliverableKind.isActive)
      return
    }

    setFormData(initialFormData)
    setIsActive(true)
  }, [deliverableKind, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() =>
      isEditing
        ? deliverableKindService.update(deliverableKind.id, {
            id: deliverableKind.id,
            ...formData,
            isActive,
          } satisfies UpdateDeliverableKindRequest)
        : deliverableKindService.create(formData),
    )

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '760px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.deliverableKind.title.edit') : t('modal.deliverableKind.title.new')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.name')}</label>
              <Input value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.order')}</label>
              <Input type="number" value={formData.displayOrder} onChange={(e) => setFormData((prev) => ({ ...prev, displayOrder: Number(e.target.value) }))} />
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '1rem' }}>
            <div>
              {isEditing && (
                <div className="flex items-center gap-2">
                  <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
                  <span className="text-sm">{t('common.status.active')}</span>
                </div>
              )}
            </div>

            <ModalFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
              <Button type="submit" disabled={loading}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
            </ModalFooter>
          </div>
        </form>
      </ModalContent>
    </Modal>
  )
}
