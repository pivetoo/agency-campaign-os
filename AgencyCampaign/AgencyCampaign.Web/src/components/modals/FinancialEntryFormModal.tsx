import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, useApi } from 'archon-ui'
import {
  financialEntryService,
  type CreateFinancialEntryRequest,
  type UpdateFinancialEntryRequest,
} from '../../services/financialEntryService'
import { financialAccountService } from '../../services/financialAccountService'
import { campaignService } from '../../services/campaignService'
import { campaignDeliverableService } from '../../services/campaignDeliverableService'
import {
  financialEntryCategoryLabels,
  type FinancialEntry,
} from '../../types/financialEntry'
import type { FinancialAccount } from '../../types/financialAccount'
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
  const isEditing = !!entry
  const [formData, setFormData] = useState<CreateFinancialEntryRequest>(initialFormData)
  const [accounts, setAccounts] = useState<FinancialAccount[]>([])
  const [campaigns, setCampaigns] = useState<Campaign[]>([])
  const [deliverables, setDeliverables] = useState<CampaignDeliverable[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    void financialAccountService.getAll(false).then(setAccounts)
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
      })
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
      campaignDeliverableId: formData.campaignDeliverableId || undefined,
      campaignId: formData.campaignId || undefined,
    }

    const result = await execute(() =>
      isEditing
        ? financialEntryService.update(entry.id, { id: entry.id, ...payload } satisfies UpdateFinancialEntryRequest)
        : financialEntryService.create(payload),
    )

    if (result !== null) onSuccess()
  }

  const isValid = formData.accountId > 0 && formData.description.trim().length >= 2 && formData.amount > 0

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '880px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar lançamento' : 'Novo lançamento'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">Tipo</label>
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
                  { value: '1', label: 'A receber' },
                  { value: '2', label: 'A pagar' },
                ]}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Conta</label>
              <SearchableSelect
                value={formData.accountId ? String(formData.accountId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, accountId: Number(value) }))}
                options={accounts.map((account) => ({ value: String(account.id), label: account.name }))}
                placeholder="Selecione uma conta"
                searchPlaceholder="Buscar conta"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Categoria</label>
              <SearchableSelect
                value={String(formData.category)}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, category: Number(value) }))}
                options={allowedCategories.map((value) => ({ value: String(value), label: financialEntryCategoryLabels[value] }))}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Status</label>
              <SearchableSelect
                value={String(formData.status)}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, status: Number(value) }))}
                options={[
                  { value: '1', label: 'Pendente' },
                  { value: '2', label: 'Pago' },
                  { value: '4', label: 'Cancelado' },
                ]}
              />
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Descrição</label>
              <Input value={formData.description} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Contraparte</label>
              <Input value={formData.counterpartyName || ''} onChange={(e) => setFormData((prev) => ({ ...prev, counterpartyName: e.target.value }))} placeholder="Quem paga ou recebe" />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Valor (R$)</label>
              <Input type="number" step="0.01" value={formData.amount === 0 ? '' : formData.amount} onChange={(e) => setFormData((prev) => ({ ...prev, amount: e.target.value === '' ? 0 : Number(e.target.value) }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Campanha (opcional)</label>
              <SearchableSelect
                value={formData.campaignId ? String(formData.campaignId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignId: value ? Number(value) : undefined, campaignDeliverableId: undefined }))}
                options={[
                  { value: '', label: 'Sem vínculo de campanha' },
                  ...campaigns.map((campaign) => ({ value: String(campaign.id), label: campaign.name })),
                ]}
                placeholder="Sem vínculo"
                searchPlaceholder="Buscar campanha"
              />
            </div>

            {formData.campaignId && deliverables.length > 0 && (
              <div className="space-y-2">
                <label className="text-sm font-medium">Entrega vinculada</label>
                <SearchableSelect
                  value={formData.campaignDeliverableId ? String(formData.campaignDeliverableId) : ''}
                  onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignDeliverableId: value ? Number(value) : undefined }))}
                  options={[
                    { value: '', label: 'Sem entrega' },
                    ...deliverables.map((deliverable) => ({ value: String(deliverable.id), label: deliverable.title })),
                  ]}
                  placeholder="Opcional"
                  searchPlaceholder="Buscar entrega"
                />
              </div>
            )}

            <div className="space-y-2">
              <label className="text-sm font-medium">Ocorrência</label>
              <Input type="date" value={formData.occurredAt} onChange={(e) => setFormData((prev) => ({ ...prev, occurredAt: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Vencimento</label>
              <Input type="date" value={formData.dueAt} onChange={(e) => setFormData((prev) => ({ ...prev, dueAt: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Pago em</label>
              <Input type="date" value={formData.paidAt || ''} onChange={(e) => setFormData((prev) => ({ ...prev, paidAt: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Forma de pagamento</label>
              <Input value={formData.paymentMethod || ''} onChange={(e) => setFormData((prev) => ({ ...prev, paymentMethod: e.target.value }))} placeholder="PIX, boleto, transferência..." />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Referência (NF, boleto, etc.)</label>
              <Input value={formData.referenceCode || ''} onChange={(e) => setFormData((prev) => ({ ...prev, referenceCode: e.target.value }))} />
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Observações</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading || !isValid}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
