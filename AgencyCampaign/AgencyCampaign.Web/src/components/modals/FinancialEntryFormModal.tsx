import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Checkbox, Input, SearchableSelect, useApi, useI18n } from 'archon-ui'
import {
  financialEntryService,
  type CreateFinancialEntryRequest,
  type UpdateFinancialEntryRequest,
} from '../../services/financialEntryService'
import { financialAccountService } from '../../services/financialAccountService'
import { financialSubcategoryService } from '../../services/financialSubcategoryService'
import { campaignService } from '../../services/campaignService'
import { campaignDeliverableService } from '../../services/campaignDeliverableService'
import {
  financialEntryCategoryLabels,
  type FinancialEntry,
} from '../../types/financialEntry'
import type { FinancialAccount } from '../../types/financialAccount'
import type { FinancialSubcategory } from '../../types/financialSubcategory'
import type { Campaign } from '../../types/campaign'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'

interface FinancialEntryFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  entry: FinancialEntry | null
  defaultCampaignId?: number | null
  defaultType?: number
  onSuccess: () => void
}

const initialFormData: CreateFinancialEntryRequest = {
  accountId: 0,
  campaignId: undefined,
  campaignDeliverableId: undefined,
  type: 1,
  category: 1,
  description: '',
  amount: 0,
  dueAt: '',
  occurredAt: '',
  paymentMethod: '',
  referenceCode: '',
  paidAt: '',
  status: 1,
  counterpartyName: '',
  notes: '',
  subcategoryId: undefined,
  invoiceNumber: '',
  invoiceUrl: '',
  invoiceIssuedAt: '',
}

const RECEIVABLE_CATEGORIES = [1, 5, 6, 7]
const PAYABLE_CATEGORIES = [2, 3, 4, 5, 6, 7, 8]

export default function FinancialEntryFormModal({
  open,
  onOpenChange,
  entry,
  defaultCampaignId,
  defaultType,
  onSuccess,
}: FinancialEntryFormModalProps) {
  const { t } = useI18n()
  const isEditing = !!entry
  const [formData, setFormData] = useState<CreateFinancialEntryRequest>(initialFormData)
  const [accounts, setAccounts] = useState<FinancialAccount[]>([])
  const [subcategories, setSubcategories] = useState<FinancialSubcategory[]>([])
  const [campaigns, setCampaigns] = useState<Campaign[]>([])
  const [deliverables, setDeliverables] = useState<CampaignDeliverable[]>([])
  const [installmentEnabled, setInstallmentEnabled] = useState(false)
  const [installmentTotal, setInstallmentTotal] = useState<number>(2)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    void financialAccountService.getAll(false).then(setAccounts)
    void financialSubcategoryService.getAll(false).then(setSubcategories)
    void campaignService.getAll().then(setCampaigns)
  }, [open])

  useEffect(() => {
    if (!open) return
    if (entry) {
      setFormData({
        accountId: entry.accountId,
        campaignId: entry.campaignId ?? undefined,
        campaignDeliverableId: entry.campaignDeliverableId,
        type: entry.type,
        category: entry.category,
        description: entry.description,
        amount: entry.amount,
        dueAt: entry.dueAt.slice(0, 10),
        occurredAt: entry.occurredAt.slice(0, 10),
        paymentMethod: entry.paymentMethod || '',
        referenceCode: entry.referenceCode || '',
        paidAt: entry.paidAt ? entry.paidAt.slice(0, 10) : '',
        status: entry.status,
        counterpartyName: entry.counterpartyName || '',
        notes: entry.notes || '',
        subcategoryId: entry.subcategoryId ?? undefined,
        invoiceNumber: entry.invoiceNumber || '',
        invoiceUrl: entry.invoiceUrl || '',
        invoiceIssuedAt: entry.invoiceIssuedAt ? entry.invoiceIssuedAt.slice(0, 10) : '',
      })
      setInstallmentEnabled(false)
      return
    }

    const today = new Date().toISOString().slice(0, 10)
    setFormData({
      ...initialFormData,
      campaignId: defaultCampaignId ?? undefined,
      type: defaultType ?? 1,
      category: defaultType === 2 ? 2 : 1,
      occurredAt: today,
      dueAt: today,
    })
  }, [entry, defaultCampaignId, defaultType, open])

  useEffect(() => {
    if (!formData.campaignId) {
      setDeliverables([])
      return
    }
    void campaignDeliverableService.getByCampaign(formData.campaignId).then(setDeliverables)
  }, [formData.campaignId])

  const allowedCategories = formData.type === 1 ? RECEIVABLE_CATEGORIES : PAYABLE_CATEGORIES

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const payload = {
      ...formData,
      paidAt: formData.paidAt || undefined,
      paymentMethod: formData.paymentMethod || undefined,
      referenceCode: formData.referenceCode || undefined,
      counterpartyName: formData.counterpartyName || undefined,
      notes: formData.notes || undefined,
      subcategoryId: formData.subcategoryId || undefined,
      invoiceNumber: formData.invoiceNumber || undefined,
      invoiceUrl: formData.invoiceUrl || undefined,
      invoiceIssuedAt: formData.invoiceIssuedAt || undefined,
      campaignDeliverableId: formData.campaignDeliverableId || undefined,
      campaignId: formData.campaignId || undefined,
    }

    const result = await execute(() => {
      if (isEditing) {
        return financialEntryService.update(entry.id, { id: entry.id, ...payload } satisfies UpdateFinancialEntryRequest)
      }
      if (installmentEnabled && installmentTotal >= 2) {
        return financialEntryService.createInstallments({ ...payload, installmentTotal })
      }
      return financialEntryService.create(payload)
    })

    if (result !== null) onSuccess()
  }

  const isValid = formData.accountId > 0 && formData.description.trim().length >= 2 && formData.amount > 0

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '880px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.financialEntry.title.edit') : t('modal.financialEntry.title.new')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.type')}</label>
              <SearchableSelect
                value={String(formData.type)}
                onValueChange={(value) => {
                  const newType = Number(value)
                  setFormData((prev) => ({
                    ...prev,
                    type: newType,
                    category: newType === 1 ? 1 : 2,
                  }))
                }}
                options={[
                  { value: '1', label: t('modal.financialEntry.type.receivable') },
                  { value: '2', label: t('modal.financialEntry.type.payable') },
                ]}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.financialEntry.field.account')}</label>
              <SearchableSelect
                value={formData.accountId ? String(formData.accountId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, accountId: Number(value) }))}
                options={accounts.map((account) => ({ value: String(account.id), label: account.name }))}
                placeholder="Selecione uma conta"
                searchPlaceholder="Buscar conta"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.category')}</label>
              <SearchableSelect
                value={String(formData.category)}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, category: Number(value) }))}
                options={allowedCategories.map((value) => ({ value: String(value), label: financialEntryCategoryLabels[value] }))}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.status')}</label>
              <SearchableSelect
                value={String(formData.status)}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, status: Number(value) }))}
                options={[
                  { value: '1', label: t('modal.financialEntry.status.pending') },
                  { value: '2', label: t('modal.financialEntry.status.paid') },
                  { value: '4', label: t('modal.financialEntry.status.cancelled') },
                ]}
              />
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('common.field.description')}</label>
              <Input value={formData.description} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.financialEntry.field.counterparty')}</label>
              <Input value={formData.counterpartyName || ''} onChange={(e) => setFormData((prev) => ({ ...prev, counterpartyName: e.target.value }))} placeholder={t('modal.financialEntry.placeholder.counterparty')} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.amountBrl')}</label>
              <Input type="number" step="0.01" value={formData.amount === 0 ? '' : formData.amount} onChange={(e) => setFormData((prev) => ({ ...prev, amount: e.target.value === '' ? 0 : Number(e.target.value) }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.financialEntry.field.campaign')}</label>
              <SearchableSelect
                value={formData.campaignId ? String(formData.campaignId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignId: value ? Number(value) : undefined, campaignDeliverableId: undefined }))}
                options={[
                  { value: '', label: t('modal.financialEntry.placeholder.noCampaign') },
                  ...campaigns.map((campaign) => ({ value: String(campaign.id), label: campaign.name })),
                ]}
                placeholder="Sem vínculo"
                searchPlaceholder="Buscar campanha"
              />
            </div>

            {formData.campaignId && deliverables.length > 0 && (
              <div className="space-y-2">
                <label className="text-sm font-medium">{t('modal.financialEntry.field.deliverable')}</label>
                <SearchableSelect
                  value={formData.campaignDeliverableId ? String(formData.campaignDeliverableId) : ''}
                  onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignDeliverableId: value ? Number(value) : undefined }))}
                  options={[
                    { value: '', label: t('modal.financialEntry.placeholder.noDeliverable') },
                    ...deliverables.map((deliverable) => ({ value: String(deliverable.id), label: deliverable.title })),
                  ]}
                  placeholder="Opcional"
                  searchPlaceholder="Buscar entrega"
                />
              </div>
            )}

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.financialEntry.field.occurrence')}</label>
              <Input type="date" value={formData.occurredAt} onChange={(e) => setFormData((prev) => ({ ...prev, occurredAt: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.financialEntry.field.dueDate')}</label>
              <Input type="date" value={formData.dueAt} onChange={(e) => setFormData((prev) => ({ ...prev, dueAt: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.paidAt')}</label>
              <Input type="date" value={formData.paidAt || ''} onChange={(e) => setFormData((prev) => ({ ...prev, paidAt: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.financialEntry.field.paymentMethod')}</label>
              <Input value={formData.paymentMethod || ''} onChange={(e) => setFormData((prev) => ({ ...prev, paymentMethod: e.target.value }))} placeholder={t('modal.financialEntry.placeholder.paymentMethod')} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.financialEntry.field.reference')}</label>
              <Input value={formData.referenceCode || ''} onChange={(e) => setFormData((prev) => ({ ...prev, referenceCode: e.target.value }))} />
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('common.field.notes')}</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.financialEntry.field.subcategory')}</label>
              <SearchableSelect
                value={formData.subcategoryId ? String(formData.subcategoryId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, subcategoryId: value ? Number(value) : undefined }))}
                options={[
                  { value: '', label: t('modal.financialEntry.placeholder.noSubcategory') },
                  ...subcategories
                    .filter((sub) => sub.macroCategory === formData.category && sub.isActive)
                    .map((sub) => ({ value: String(sub.id), label: sub.name })),
                ]}
                placeholder={t('modal.financialEntry.placeholder.noSubcategory')}
                searchPlaceholder="Buscar subcategoria"
              />
            </div>

            <div className="space-y-2 md:col-span-2 rounded-md border bg-muted/30 p-3">
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{t('modal.financialEntry.section.invoice')}</p>
              <div className="grid grid-cols-1 gap-2 md:grid-cols-3">
                <Input
                  placeholder="Número da NF"
                  value={formData.invoiceNumber || ''}
                  onChange={(e) => setFormData((prev) => ({ ...prev, invoiceNumber: e.target.value }))}
                />
                <Input
                  placeholder="URL do XML/PDF"
                  value={formData.invoiceUrl || ''}
                  onChange={(e) => setFormData((prev) => ({ ...prev, invoiceUrl: e.target.value }))}
                />
                <Input
                  type="date"
                  value={formData.invoiceIssuedAt || ''}
                  onChange={(e) => setFormData((prev) => ({ ...prev, invoiceIssuedAt: e.target.value }))}
                />
              </div>
            </div>

            {!isEditing && (
              <div className="space-y-2 md:col-span-2 rounded-md border bg-muted/30 p-3">
                <label className="flex items-center gap-2 text-sm font-medium">
                  <Checkbox
                    checked={installmentEnabled}
                    onCheckedChange={(checked) => setInstallmentEnabled(!!checked)}
                  />
                  <span>{t('modal.financialEntry.field.installments')}</span>
                </label>
                {installmentEnabled && (
                  <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
                    <div className="space-y-1">
                      <label className="text-xs text-muted-foreground">{t('modal.financialEntry.field.installmentTotal')}</label>
                      <Input
                        type="number"
                        min={2}
                        max={60}
                        value={installmentTotal}
                        onChange={(e) => setInstallmentTotal(Math.max(2, Number(e.target.value) || 2))}
                      />
                    </div>
                    <p className="text-xs text-muted-foreground self-end">
                      Cada parcela = {(formData.amount / installmentTotal).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })} · 1 ao mês
                    </p>
                  </div>
                )}
              </div>
            )}
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
