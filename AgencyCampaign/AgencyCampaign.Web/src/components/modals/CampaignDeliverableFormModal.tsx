import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui'
import { campaignDeliverableService, type CreateCampaignDeliverableRequest, type UpdateCampaignDeliverableRequest } from '../../services/campaignDeliverableService'
import { campaignCreatorService } from '../../services/campaignCreatorService'
import { platformService } from '../../services/platformService'
import { deliverableKindService } from '../../services/deliverableKindService'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'
import type { CampaignCreator } from '../../types/campaignCreator'
import type { Platform } from '../../types/platform'
import type { DeliverableKind } from '../../types/deliverableKind'

interface CampaignDeliverableFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  campaignId: number
  deliverable: CampaignDeliverable | null
  onSuccess: () => void
}

const initialFormData: CreateCampaignDeliverableRequest = {
  campaignId: 0,
  campaignCreatorId: 0,
  title: '',
  description: '',
  deliverableKindId: 0,
  platformId: 0,
  dueAt: '',
  status: 1,
  publishedUrl: '',
  evidenceUrl: '',
  notes: '',
  grossAmount: 0,
  creatorAmount: 0,
  agencyFeeAmount: 0,
}

const emptyMetrics = { likes: '', comments: '', views: '', reach: '', impressions: '', saves: '', shares: '' }
const metricFields = [['likes', 'modal.deliverable.metrics.likes'], ['comments', 'modal.deliverable.metrics.comments'], ['views', 'modal.deliverable.metrics.views'], ['reach', 'modal.deliverable.metrics.reach'], ['impressions', 'modal.deliverable.metrics.impressions'], ['saves', 'modal.deliverable.metrics.saves'], ['shares', 'modal.deliverable.metrics.shares']] as const

export default function CampaignDeliverableFormModal({ open, onOpenChange, campaignId, deliverable, onSuccess }: CampaignDeliverableFormModalProps) {
  const { t } = useI18n()
  const isEditing = !!deliverable
  const [formData, setFormData] = useState<CreateCampaignDeliverableRequest>(initialFormData)
  const [metrics, setMetrics] = useState<Record<string, string>>(emptyMetrics)
  const [campaignCreators, setCampaignCreators] = useState<CampaignCreator[]>([])
  const [platforms, setPlatforms] = useState<Platform[]>([])
  const [deliverableKinds, setDeliverableKinds] = useState<DeliverableKind[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: fetchCampaignCreators } = useApi<CampaignCreator[]>({ showErrorMessage: true })
  const { execute: fetchPlatforms } = useApi<Platform[]>({ showErrorMessage: true })
  const { execute: fetchDeliverableKinds } = useApi<DeliverableKind[]>({ showErrorMessage: true })

  useEffect(() => {
    if (open && campaignId > 0) {
      void fetchCampaignCreators(() => campaignCreatorService.getByCampaign(campaignId)).then((result) => {
        if (result) {
          setCampaignCreators(result)
        }
      })

      void fetchPlatforms(() => platformService.getActive()).then((result) => {
        if (result) {
          setPlatforms(result)
        }
      })

      void fetchDeliverableKinds(() => deliverableKindService.getActive()).then((result) => {
        if (result) {
          setDeliverableKinds(result)
        }
      })
    }
  }, [open, campaignId])

  useEffect(() => {
    if (deliverable) {
      setFormData({
        campaignId,
        campaignCreatorId: deliverable.campaignCreatorId,
        title: deliverable.title,
        description: deliverable.description || '',
        deliverableKindId: deliverable.deliverableKindId,
        platformId: deliverable.platformId,
        dueAt: deliverable.dueAt.slice(0, 10),
        status: deliverable.status,
        publishedUrl: deliverable.publishedUrl || '',
        evidenceUrl: deliverable.evidenceUrl || '',
        notes: deliverable.notes || '',
        grossAmount: deliverable.grossAmount,
        creatorAmount: deliverable.creatorAmount,
        agencyFeeAmount: deliverable.agencyFeeAmount,
      })
      setMetrics({
        likes: deliverable.likes?.toString() ?? '',
        comments: deliverable.comments?.toString() ?? '',
        views: deliverable.views?.toString() ?? '',
        reach: deliverable.reach?.toString() ?? '',
        impressions: deliverable.impressions?.toString() ?? '',
        saves: deliverable.saves?.toString() ?? '',
        shares: deliverable.shares?.toString() ?? '',
      })
      return
    }

    setFormData({ ...initialFormData, campaignId })
    setMetrics(emptyMetrics)
  }, [deliverable, campaignId, open])

  // M6: deriva fee da agencia e valor do creator a partir do fee % acordado no CampaignCreator selecionado.
  // Recalcula quando o operador muda o bruto ou o creator; ainda pode ajustar manualmente depois.
  const deriveAmounts = (gross: number, campaignCreatorId: number) => {
    const cc = campaignCreators.find((item) => item.id === campaignCreatorId)
    const pct = cc?.agencyFeePercent ?? 0
    const fee = Math.round((gross * pct) / 100 * 100) / 100
    return { grossAmount: gross, agencyFeeAmount: fee, creatorAmount: Math.max(0, Math.round((gross - fee) * 100) / 100) }
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    if (!formData.campaignCreatorId || !formData.deliverableKindId || !formData.platformId) {
      return
    }

    const toMetric = (value: string) => (value.trim() === '' ? undefined : Number(value))

    const payload = {
      ...formData,
      description: formData.description || undefined,
      publishedUrl: formData.publishedUrl || undefined,
      evidenceUrl: formData.evidenceUrl || undefined,
      notes: formData.notes || undefined,
    }

    const result = await execute(() =>
      isEditing
        ? campaignDeliverableService.update(deliverable.id, {
            id: deliverable.id,
            title: payload.title,
            description: payload.description,
            deliverableKindId: payload.deliverableKindId,
            platformId: payload.platformId,
            dueAt: payload.dueAt,
            status: payload.status,
            publishedUrl: payload.publishedUrl,
            evidenceUrl: payload.evidenceUrl,
            notes: payload.notes,
            grossAmount: payload.grossAmount,
            creatorAmount: payload.creatorAmount,
            agencyFeeAmount: payload.agencyFeeAmount,
            likes: toMetric(metrics.likes),
            comments: toMetric(metrics.comments),
            views: toMetric(metrics.views),
            reach: toMetric(metrics.reach),
            impressions: toMetric(metrics.impressions),
            saves: toMetric(metrics.saves),
            shares: toMetric(metrics.shares),
          } satisfies UpdateCampaignDeliverableRequest)
        : campaignDeliverableService.create(payload),
    )

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '980px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.deliverable.title.edit') : t('modal.deliverable.title.new')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.deliverable.field.campaignCreator')}</label>
              <SearchableSelect
                value={formData.campaignCreatorId ? String(formData.campaignCreatorId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignCreatorId: Number(value), ...deriveAmounts(prev.grossAmount, Number(value)) }))}
                options={campaignCreators.map((item) => ({ value: String(item.id), label: item.creator?.stageName || item.creator?.name || `Creator #${item.creatorId}` }))}
                placeholder={t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.status')}</label>
              <Select value={String(formData.status)} onValueChange={(value) => setFormData((prev) => ({ ...prev, status: Number(value) }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">{t('deliverable.status.pending')}</SelectItem>
                  <SelectItem value="2">{t('deliverable.status.reviewing')}</SelectItem>
                  <SelectItem value="3">{t('deliverable.status.approved')}</SelectItem>
                  <SelectItem value="4">{t('deliverable.status.published')}</SelectItem>
                  <SelectItem value="5">{t('deliverable.status.cancelled')}</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('common.field.title')}</label>
              <Input value={formData.title} onChange={(e) => setFormData((prev) => ({ ...prev, title: e.target.value }))} required />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('common.field.description')}</label>
              <Input value={formData.description || ''} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.deliverable.field.kind')}</label>
              <SearchableSelect
                value={formData.deliverableKindId ? String(formData.deliverableKindId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, deliverableKindId: Number(value) }))}
                options={deliverableKinds.map((item) => ({ value: String(item.id), label: item.name }))}
                placeholder={t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.platform')}</label>
              <SearchableSelect
                value={formData.platformId ? String(formData.platformId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, platformId: Number(value) }))}
                options={platforms.map((item) => ({ value: String(item.id), label: item.name }))}
                placeholder={t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.dueDate')}</label>
              <Input type="date" value={formData.dueAt} onChange={(e) => setFormData((prev) => ({ ...prev, dueAt: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.deliverable.field.publishedUrl')}</label>
              <Input value={formData.publishedUrl || ''} onChange={(e) => setFormData((prev) => ({ ...prev, publishedUrl: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.deliverable.field.evidence')}</label>
              <Input value={formData.evidenceUrl || ''} onChange={(e) => setFormData((prev) => ({ ...prev, evidenceUrl: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.notes')}</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.deliverable.field.grossAmount')}</label>
              <Input type="number" value={formData.grossAmount} onChange={(e) => setFormData((prev) => ({ ...prev, ...deriveAmounts(Number(e.target.value), prev.campaignCreatorId) }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.deliverable.field.creatorAmount')}</label>
              <Input type="number" value={formData.creatorAmount} onChange={(e) => setFormData((prev) => ({ ...prev, creatorAmount: Number(e.target.value) }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.deliverable.field.agencyFee')}</label>
              <Input type="number" value={formData.agencyFeeAmount} onChange={(e) => setFormData((prev) => ({ ...prev, agencyFeeAmount: Number(e.target.value) }))} required />
            </div>
          </div>

          {formData.creatorAmount + formData.agencyFeeAmount > formData.grossAmount && (
            <p className="text-xs text-destructive">{t('modal.deliverable.amountsExceedGross')}</p>
          )}

          {isEditing && (
            <div className="space-y-3 rounded-lg border border-border/60 bg-muted/20 p-4">
              <div>
                <p className="text-sm font-semibold">{t('modal.deliverable.metrics.section')}</p>
                <p className="text-xs text-muted-foreground">{t('modal.deliverable.metrics.hint')}</p>
              </div>
              <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
                {metricFields.map(([key, labelKey]) => (
                  <div key={key} className="space-y-2">
                    <label className="text-sm font-medium">{t(labelKey)}</label>
                    <Input type="number" min={0} value={metrics[key]} onChange={(e) => setMetrics((prev) => ({ ...prev, [key]: e.target.value }))} />
                  </div>
                ))}
              </div>
            </div>
          )}

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !formData.campaignCreatorId || !formData.deliverableKindId || !formData.platformId || formData.creatorAmount + formData.agencyFeeAmount > formData.grossAmount}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
