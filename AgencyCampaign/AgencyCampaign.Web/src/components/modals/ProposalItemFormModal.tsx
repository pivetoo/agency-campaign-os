import { useEffect, useState } from 'react'
import { Button, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, SearchableSelect, useApi, useI18n } from 'archon-ui'
import { creatorService } from '../../services/creatorService'
import { proposalService, type CreateProposalItemRequest, type ProposalItem, type UpdateProposalItemRequest } from '../../services/proposalService'
import type { Creator } from '../../types/creator'

interface ProposalItemFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  proposalId: number
  item: ProposalItem | null
  onSuccess: () => void
}

const initialFormData: CreateProposalItemRequest = {
  proposalId: 0,
  description: '',
  quantity: 1,
  unitPrice: 0,
  deliveryDeadline: undefined,
  creatorId: undefined,
  observations: '',
}

export default function ProposalItemFormModal({ open, onOpenChange, proposalId, item, onSuccess }: ProposalItemFormModalProps) {
  const { t } = useI18n()
  const isEditing = !!item
  const [formData, setFormData] = useState<CreateProposalItemRequest>({ ...initialFormData, proposalId })
  const [creators, setCreators] = useState<Creator[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    void creatorService.getAll({ pageSize: 200 }).then((r) => setCreators(r.data ?? []))
  }, [])

  useEffect(() => {
    if (item) {
      setFormData({
        proposalId,
        description: item.description,
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        deliveryDeadline: item.deliveryDeadline,
        creatorId: item.creatorId,
        observations: item.observations || '',
      })
      return
    }

    setFormData({ ...initialFormData, proposalId })
  }, [item, open, proposalId])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() => (
      isEditing
        ? proposalService.updateItem(item.id, {
            description: formData.description,
            quantity: formData.quantity,
            unitPrice: formData.unitPrice,
            deliveryDeadline: formData.deliveryDeadline,
            observations: formData.observations,
          } satisfies UpdateProposalItemRequest)
        : proposalService.createItem(proposalId, formData)
    ))

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '860px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.proposalItem.title.edit') : t('modal.proposalItem.title.new')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('common.field.description')}</label>
              <Input value={formData.description} onChange={(event) => setFormData((prev) => ({ ...prev, description: event.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.proposalItem.field.quantity')}</label>
              <Input type="number" min="1" value={formData.quantity} onChange={(event) => setFormData((prev) => ({ ...prev, quantity: Number(event.target.value) }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.proposalItem.field.unitPrice')}</label>
              <Input type="number" min="0" step="0.01" value={formData.unitPrice === 0 ? '' : formData.unitPrice} onChange={(event) => setFormData((prev) => ({ ...prev, unitPrice: event.target.value === '' ? 0 : Number(event.target.value) }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.proposalItem.field.deliveryDeadline')}</label>
              <Input type="date" value={formData.deliveryDeadline?.split('T')[0] || ''} onChange={(event) => setFormData((prev) => ({ ...prev, deliveryDeadline: event.target.value ? new Date(event.target.value).toISOString() : undefined }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('creators.singular')}</label>
              <SearchableSelect
                value={formData.creatorId ? String(formData.creatorId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, creatorId: value ? Number(value) : undefined }))}
                options={[
                  { value: '', label: 'Sem creator' },
                  ...creators.map((creator) => ({ value: String(creator.id), label: creator.name })),
                ]}
                placeholder="Opcional"
                searchPlaceholder={t('common.placeholder.search')}
              />
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('modal.proposalItem.field.observations')}</label>
              <Input value={formData.observations || ''} onChange={(event) => setFormData((prev) => ({ ...prev, observations: event.target.value }))} />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !formData.description || formData.quantity <= 0}>{loading ? t('common.action.saving') : t('modal.proposalItem.action.save')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
