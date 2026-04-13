import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
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
  description: '',
  amount: 0,
  dueAt: '',
  paidAt: '',
  status: 1,
  counterpartyName: '',
  notes: '',
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
        description: entry.description,
        amount: entry.amount,
        dueAt: entry.dueAt.slice(0, 10),
        paidAt: entry.paidAt ? entry.paidAt.slice(0, 10) : '',
        status: entry.status,
        counterpartyName: entry.counterpartyName || '',
        notes: entry.notes || '',
      })
      return
    }

    setFormData({ ...initialFormData, campaignId })
  }, [entry, campaignId, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const payload = {
      ...formData,
      paidAt: formData.paidAt || undefined,
      campaignDeliverableId: formData.campaignDeliverableId || undefined,
    }

    const result = await execute(() =>
      isEditing
        ? campaignFinancialEntryService.update(entry.id, {
            id: entry.id,
            campaignDeliverableId: payload.campaignDeliverableId,
            type: payload.type,
            description: payload.description,
            amount: payload.amount,
            dueAt: payload.dueAt,
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
              <Select value={formData.campaignDeliverableId ? String(formData.campaignDeliverableId) : '_none'} onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignDeliverableId: value === '_none' ? undefined : Number(value) }))}>
                <SelectTrigger><SelectValue placeholder="Opcional" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="_none">Sem vínculo</SelectItem>
                  {deliverables.map((deliverable) => (
                    <SelectItem key={deliverable.id} value={String(deliverable.id)}>{deliverable.title}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Valor</label>
              <Input type="number" value={formData.amount} onChange={(e) => setFormData((prev) => ({ ...prev, amount: Number(e.target.value) }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Vencimento</label>
              <Input type="date" value={formData.dueAt} onChange={(e) => setFormData((prev) => ({ ...prev, dueAt: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Pago em</label>
              <Input type="date" value={formData.paidAt || ''} onChange={(e) => setFormData((prev) => ({ ...prev, paidAt: e.target.value }))} />
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
