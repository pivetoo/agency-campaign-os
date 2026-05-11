import { useEffect, useMemo, useState } from 'react'
import {
  Button,
  Input,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  SearchableSelect,
  useApi,
  useI18n,
} from 'archon-ui'
import { creatorPaymentService, type CreateCreatorPaymentRequest, type UpdateCreatorPaymentRequest } from '../../services/creatorPaymentService'
import { campaignCreatorService } from '../../services/campaignCreatorService'
import {
  PaymentMethod,
  paymentMethodLabels,
  type CreatorPayment,
  type PaymentMethodValue,
} from '../../types/creatorPayment'
import type { CampaignCreator } from '../../types/campaignCreator'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  payment: CreatorPayment | null
  campaignId?: number
  onSuccess: () => void
}

const initial: CreateCreatorPaymentRequest = {
  campaignCreatorId: 0,
  grossAmount: 0,
  discounts: 0,
  method: PaymentMethod.Pix,
  description: '',
}

const methodOptions = Object.values(PaymentMethod).map((value) => ({
  value: String(value),
  label: paymentMethodLabels[value as PaymentMethodValue],
}))

export default function CreatorPaymentFormModal({ open, onOpenChange, payment, campaignId, onSuccess }: Props) {
  const { t } = useI18n()
  const isEditing = !!payment
  const [formData, setFormData] = useState<CreateCreatorPaymentRequest>(initial)
  const [campaignCreators, setCampaignCreators] = useState<CampaignCreator[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: loadCreators, loading: loadingCreators } = useApi<CampaignCreator[]>({ showErrorMessage: true })

  useEffect(() => {
    if (!open) return

    if (payment) {
      setFormData({
        campaignCreatorId: payment.campaignCreatorId,
        campaignDocumentId: payment.campaignDocumentId,
        grossAmount: payment.grossAmount,
        discounts: payment.discounts,
        method: payment.method,
        description: payment.description ?? '',
      })
    } else {
      setFormData({ ...initial })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, payment?.id])

  useEffect(() => {
    if (!open || !campaignId) return
    void loadCreators(() => campaignCreatorService.getByCampaign(campaignId)).then((res) => {
      if (res) setCampaignCreators(res)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, campaignId])

  const creatorOptions = useMemo(
    () =>
      campaignCreators.map((cc) => ({
        value: String(cc.id),
        label: cc.creator?.stageName || cc.creator?.name || `Creator #${cc.creatorId}`,
      })),
    [campaignCreators],
  )

  const netAmount = (formData.grossAmount || 0) - (formData.discounts || 0)
  const isValid =
    (isEditing || formData.campaignCreatorId > 0) &&
    formData.grossAmount > 0 &&
    formData.discounts >= 0 &&
    formData.discounts <= formData.grossAmount

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!isValid) return

    const result = await execute(() => {
      if (isEditing && payment) {
        const updatePayload: UpdateCreatorPaymentRequest = {
          id: payment.id,
          grossAmount: formData.grossAmount,
          discounts: formData.discounts ?? 0,
          method: formData.method,
          description: formData.description?.trim() || undefined,
        }
        return creatorPaymentService.update(payment.id, updatePayload)
      }
      return creatorPaymentService.create({
        campaignCreatorId: formData.campaignCreatorId,
        campaignDocumentId: formData.campaignDocumentId,
        grossAmount: formData.grossAmount,
        discounts: formData.discounts ?? 0,
        method: formData.method,
        description: formData.description?.trim() || undefined,
      })
    })
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '720px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.creatorPayment.title.edit') : t('modal.creatorPayment.title.new')}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            {!isEditing && (
              <div className="space-y-2 md:col-span-2">
                <label className="text-sm font-medium">{t('modal.creatorPayment.field.campaignCreator')}</label>
                <SearchableSelect
                  value={formData.campaignCreatorId ? String(formData.campaignCreatorId) : ''}
                  onValueChange={(value) => setFormData((p) => ({ ...p, campaignCreatorId: value ? Number(value) : 0 }))}
                  options={creatorOptions}
                  placeholder={loadingCreators ? 'Carregando...' : campaignId ? 'Selecione um creator' : 'Selecione uma campanha primeiro'}
                  searchPlaceholder="Buscar"
                  disabled={loadingCreators || !campaignId || creatorOptions.length === 0}
                />
              </div>
            )}
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.creatorPayment.field.grossAmount')}</label>
              <Input
                type="number"
                step="0.01"
                min="0.01"
                value={formData.grossAmount}
                onChange={(e) => setFormData((p) => ({ ...p, grossAmount: Number(e.target.value) }))}
                required
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.creatorPayment.field.discounts')}</label>
              <Input
                type="number"
                step="0.01"
                min="0"
                value={formData.discounts}
                onChange={(e) => setFormData((p) => ({ ...p, discounts: Number(e.target.value) }))}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.creatorPayment.field.netAmount')}</label>
              <div className="rounded-md border bg-primary/5 px-3 py-2 text-sm font-semibold">
                R$ {netAmount.toFixed(2)}
              </div>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.creatorPayment.field.method')}</label>
              <SearchableSelect
                value={String(formData.method)}
                onValueChange={(value) => setFormData((p) => ({ ...p, method: Number(value) as PaymentMethodValue }))}
                options={methodOptions}
                placeholder="Selecione"
                searchPlaceholder="Buscar"
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('common.field.description')}</label>
              <Input
                value={formData.description ?? ''}
                onChange={(e) => setFormData((p) => ({ ...p, description: e.target.value }))}
                placeholder="Ex: Repasse referente à campanha X — entrega 2 de 3"
              />
            </div>
          </div>
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !isValid}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
