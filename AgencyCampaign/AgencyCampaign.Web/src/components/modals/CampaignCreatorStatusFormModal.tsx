import { useEffect, useState } from 'react'
import { Button, Checkbox, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import { campaignCreatorStatusService, type CreateCampaignCreatorStatusRequest, type UpdateCampaignCreatorStatusRequest } from '../../services/campaignCreatorStatusService'
import type { CampaignCreatorStatus } from '../../types/campaignCreatorStatus'

interface CampaignCreatorStatusFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  status: CampaignCreatorStatus | null
  onSuccess: () => void
}

const initialFormData: CreateCampaignCreatorStatusRequest = {
  name: '',
  description: '',
  displayOrder: 0,
  color: '#6366f1',
  isInitial: false,
  isFinal: false,
  category: 0,
}

export default function CampaignCreatorStatusFormModal({ open, onOpenChange, status, onSuccess }: CampaignCreatorStatusFormModalProps) {
  const isEditing = !!status
  const [formData, setFormData] = useState<CreateCampaignCreatorStatusRequest>(initialFormData)
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (status) {
      setFormData({
        name: status.name,
        description: status.description || '',
        displayOrder: status.displayOrder,
        color: status.color,
        isInitial: status.isInitial,
        isFinal: status.isFinal,
        category: status.category,
      })
      setIsActive(status.isActive)
      return
    }

    setFormData(initialFormData)
    setIsActive(true)
  }, [status, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() => (
      isEditing
        ? campaignCreatorStatusService.update(status.id, {
            id: status.id,
            ...formData,
            isActive,
          } satisfies UpdateCampaignCreatorStatusRequest)
        : campaignCreatorStatusService.create(formData)
    ))

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '860px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar status de creator' : 'Novo status de creator'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">Nome</label>
              <Input value={formData.name} onChange={(event) => setFormData((prev) => ({ ...prev, name: event.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Ordem</label>
              <Input type="number" value={formData.displayOrder} onChange={(event) => setFormData((prev) => ({ ...prev, displayOrder: Number(event.target.value) }))} />
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Descrição</label>
              <Input value={formData.description || ''} onChange={(event) => setFormData((prev) => ({ ...prev, description: event.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Cor</label>
              <Input type="color" value={formData.color} onChange={(event) => setFormData((prev) => ({ ...prev, color: event.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Categoria</label>
              <Select value={String(formData.category)} onValueChange={(value) => setFormData((prev) => ({ ...prev, category: Number(value) }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="0">Em andamento</SelectItem>
                  <SelectItem value="1">Sucesso</SelectItem>
                  <SelectItem value="2">Falha</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="flex flex-wrap gap-6">
            <label className="flex items-center gap-2 text-sm"><Checkbox checked={formData.isInitial} onCheckedChange={(checked) => setFormData((prev) => ({ ...prev, isInitial: !!checked }))} /><span>Status inicial</span></label>
            <label className="flex items-center gap-2 text-sm"><Checkbox checked={formData.isFinal} onCheckedChange={(checked) => setFormData((prev) => ({ ...prev, isFinal: !!checked }))} /><span>Status final</span></label>
            {isEditing && <label className="flex items-center gap-2 text-sm"><Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} /><span>Ativo</span></label>}
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading || !formData.name}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
