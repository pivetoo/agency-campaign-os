import { useEffect, useState } from 'react'
import { Button, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, SearchableSelect, useApi, useI18n } from 'archon-ui'
import { creatorService } from '../../services/creatorService'
import { proposalService, ProposalItemKind, type CreateProposalItemRequest, type ProposalItem, type UpdateProposalItemRequest } from '../../services/proposalService'
import type { Creator } from '../../types/creator'
import { rateCardItemService, type RateCardItem } from '../../services/rateCardItemService'
import { dateInputToIso, isoToDateInput, formatCurrency } from '../../lib/format'

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
  kind: ProposalItemKind.Deliverable,
}

export default function ProposalItemFormModal({ open, onOpenChange, proposalId, item, onSuccess }: ProposalItemFormModalProps) {
  const { t } = useI18n()
  const isEditing = !!item
  const [formData, setFormData] = useState<CreateProposalItemRequest>({ ...initialFormData, proposalId })
  const [creators, setCreators] = useState<Creator[]>([])
  const [rateCardItems, setRateCardItems] = useState<RateCardItem[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    void creatorService.getAll({ pageSize: 10 }).then((r) => setCreators(r.data ?? []))
  }, [open])

  useEffect(() => {
    if (!open || !formData.creatorId) {
      setRateCardItems([])
      return
    }
    void rateCardItemService.getByCreator(formData.creatorId).then(setRateCardItems).catch(() => setRateCardItems([]))
  }, [open, formData.creatorId])

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
        kind: item.kind,
        usageDurationMonths: item.usageDurationMonths,
        usageScope: item.usageScope,
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
            kind: formData.kind,
            usageDurationMonths: formData.usageDurationMonths,
            usageScope: formData.usageScope,
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
              <label className="text-sm font-medium">{t('modal.proposalItem.field.kind')}</label>
              <div className="inline-flex rounded-lg bg-muted p-0.5">
                <button type="button" onClick={() => setFormData((prev) => ({ ...prev, kind: ProposalItemKind.Deliverable }))} className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${(formData.kind ?? 0) === ProposalItemKind.Deliverable ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'}`}>
                  {t('modal.proposalItem.kind.deliverable')}
                </button>
                <button type="button" onClick={() => setFormData((prev) => ({ ...prev, kind: ProposalItemKind.UsageRights }))} className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${formData.kind === ProposalItemKind.UsageRights ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'}`}>
                  {t('modal.proposalItem.kind.usageRights')}
                </button>
              </div>
            </div>

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

            {formData.kind === ProposalItemKind.UsageRights && (
              <>
                <div className="space-y-2">
                  <label className="text-sm font-medium">{t('modal.proposalItem.field.usageDuration')}</label>
                  <Input type="number" min="1" value={formData.usageDurationMonths ?? ''} onChange={(event) => setFormData((prev) => ({ ...prev, usageDurationMonths: event.target.value === '' ? undefined : Number(event.target.value) }))} placeholder={t('modal.proposalItem.usage.perpetual')} />
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-medium">{t('modal.proposalItem.field.usageScope')}</label>
                  <Input value={formData.usageScope ?? ''} onChange={(event) => setFormData((prev) => ({ ...prev, usageScope: event.target.value }))} placeholder={t('modal.proposalItem.usage.scopePlaceholder')} />
                </div>
              </>
            )}

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.proposalItem.field.deliveryDeadline')}</label>
              <Input type="date" value={isoToDateInput(formData.deliveryDeadline)} onChange={(event) => setFormData((prev) => ({ ...prev, deliveryDeadline: event.target.value ? dateInputToIso(event.target.value) : undefined }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('creators.singular')}</label>
              <SearchableSelect
                value={formData.creatorId ? String(formData.creatorId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, creatorId: value ? Number(value) : undefined }))}
                options={[
                  { value: '', label: t('modal.proposalItem.option.noCreator') },
                  ...creators.map((creator) => ({ value: String(creator.id), label: creator.name })),
                ]}
                placeholder={t('modal.proposalItem.placeholder.optional')}
                searchPlaceholder={t('common.placeholder.search')}
                onSearch={async (term) => {
                  const r = await creatorService.getAll({ search: term, pageSize: 10 })
                  return (r.data ?? []).map((creator) => ({ value: String(creator.id), label: creator.name }))
                }}
              />
            </div>

            {rateCardItems.length > 0 && (
              <div className="space-y-1.5 md:col-span-2">
                <label className="text-xs font-medium text-muted-foreground">{t('rateCard.pickFromCard')}</label>
                <div className="flex flex-wrap gap-1.5">
                  {rateCardItems.map((rateItem) => (
                    <button
                      key={rateItem.id}
                      type="button"
                      onClick={() => setFormData((prev) => ({ ...prev, description: rateItem.label, unitPrice: rateItem.unitPrice }))}
                      className="inline-flex items-center gap-1.5 rounded-full border border-border bg-card px-2.5 py-1 text-xs font-medium text-foreground transition-colors hover:border-primary/40 hover:text-primary"
                    >
                      {rateItem.label} · {formatCurrency(rateItem.unitPrice)}
                    </button>
                  ))}
                </div>
              </div>
            )}

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
