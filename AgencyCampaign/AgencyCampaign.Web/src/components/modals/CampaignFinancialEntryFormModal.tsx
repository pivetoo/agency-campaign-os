import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import { campaignFinancialEntryService, type CreateCampaignFinancialEntryRequest, type UpdateCampaignFinancialEntryRequest } from '../../services/campaignFinancialEntryService'
import type { CampaignFinancialEntry } from '../../types/campaignFinancialEntry'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'

interface CampaignFinancialEntryFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  campaignId: number
  entry: CampaignFinancialEntry | null
  deliverables: CampaignDeliverable[]
  onSuccess: () => void
}

const initialFormData: CreateCampaignFinancialEntryRequest = {
  campaignId: 0,
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

const categoryLabels: Record<number, string> = {
  1: 'Recebível da marca',
  2: 'Repasse creator',
  3: 'Fee da agência',
  4: 'Custo operacional',
  5: 'Bônus',
  6: 'Ajuste',
  7: 'Reembolso',
  8: 'Imposto',
}

export default function CampaignFinancialEntryFormModal({ open, onOpenChange, campaignId, entry, deliverables, onSuccess }: CampaignFinancialEntryFormModalProps) {
  const isEditing = !!entry
  const [formData, setFormData] = useState<CreateCampaignFinancialEntryRequest>(initialFormData)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (entry) {
      setFormData({
        campaignId,
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

    setFormData({
      ...initialFormData,
      campaignId,
      occurredAt: new Date().toISOString().slice(0, 10),
      dueAt: new Date().toISOString().slice(0, 10),
    })
  }, [entry, campaignId, open])

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
    }

    const result = await execute(() =>
      isEditing
        ? campaignFinancialEntryService.update(entry.id, {
            id: entry.id,
            campaignDeliverableId: payload.campaignDeliverableId,
            type: payload.type,
            category: payload.category,
            description: payload.description,
            amount: payload.amount,
            dueAt: payload.dueAt,
            occurredAt: payload.occurredAt,
            paymentMethod: payload.paymentMethod,
            referenceCode: payload.referenceCode,
            paidAt: payload.paidAt,
            status: payload.status,
            counterpartyName: payload.counterpartyName,
            notes: payload.notes,
          } satisfies UpdateCampaignFinancialEntryRequest)
        : campaignFinancialEntryService.create(payload),
    )

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="xl">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar lançamento' : 'Novo lançamento'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">Tipo</label>
              <Select value={String(formData.type)} onValueChange={(value) => setFormData((prev) => ({ ...prev, type: Number(value) }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">A receber</SelectItem>
                  <SelectItem value="2">A pagar</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Categoria</label>
              <Select value={String(formData.category)} onValueChange={(value) => setFormData((prev) => ({ ...prev, category: Number(value) }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {Object.entries(categoryLabels).map(([value, label]) => (
                    <SelectItem key={value} value={value}>{label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Status</label>
              <Select value={String(formData.status)} onValueChange={(value) => setFormData((prev) => ({ ...prev, status: Number(value) }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">Pendente</SelectItem>
                  <SelectItem value="2">Pago</SelectItem>
                  <SelectItem value="3">Vencido</SelectItem>
                  <SelectItem value="4">Cancelado</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Descrição</label>
              <Input value={formData.description} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Contraparte</label>
              <Input value={formData.counterpartyName || ''} onChange={(e) => setFormData((prev) => ({ ...prev, counterpartyName: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Entrega vinculada</label>
              <SearchableSelect
                value={formData.campaignDeliverableId ? String(formData.campaignDeliverableId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignDeliverableId: value ? Number(value) : undefined }))}
                options={[
                  { value: '', label: 'Sem vínculo' },
                  ...deliverables.map((deliverable) => ({ value: String(deliverable.id), label: deliverable.title })),
                ]}
                placeholder="Opcional"
                searchPlaceholder="Buscar entrega"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Valor</label>
              <Input type="number" value={formData.amount} onChange={(e) => setFormData((prev) => ({ ...prev, amount: Number(e.target.value) }))} required />
            </div>

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
              <Input value={formData.paymentMethod || ''} onChange={(e) => setFormData((prev) => ({ ...prev, paymentMethod: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Referência</label>
              <Input value={formData.referenceCode || ''} onChange={(e) => setFormData((prev) => ({ ...prev, referenceCode: e.target.value }))} />
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Observações</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
