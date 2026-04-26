import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, useApi } from 'archon-ui'
import { commercialResponsibleService } from '../../services/commercialResponsibleService'
import type { CommercialResponsible } from '../../types/commercialResponsible'

interface CommercialResponsibleFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  responsible: CommercialResponsible | null
  onSuccess: () => void
}

const initialFormData = {
  name: '',
  email: '',
  phone: '',
  notes: '',
}

export default function CommercialResponsibleFormModal({ open, onOpenChange, responsible, onSuccess }: CommercialResponsibleFormModalProps) {
  const isEditing = !!responsible
  const [formData, setFormData] = useState(initialFormData)
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (responsible) {
      setFormData({
        name: responsible.name,
        email: responsible.email || '',
        phone: responsible.phone || '',
        notes: responsible.notes || '',
      })
      setIsActive(responsible.isActive)
      return
    }

    setFormData(initialFormData)
    setIsActive(true)
  }, [responsible, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() => (
      isEditing
        ? commercialResponsibleService.update(responsible.id, {
            id: responsible.id,
            ...formData,
            isActive,
          })
        : commercialResponsibleService.create(formData)
    ))

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar responsável comercial' : 'Novo responsável comercial'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">Nome</label>
            <Input value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} required />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">E-mail</label>
            <Input type="email" value={formData.email} onChange={(e) => setFormData((prev) => ({ ...prev, email: e.target.value }))} />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">Telefone</label>
            <Input value={formData.phone} onChange={(e) => setFormData((prev) => ({ ...prev, phone: e.target.value }))} />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">Observações</label>
            <Input value={formData.notes} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '1rem' }}>
            <div>
              {isEditing && (
                <div className="flex items-center gap-2">
                  <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
                  <span className="text-sm">Ativo</span>
                </div>
              )}
            </div>

            <ModalFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
              <Button type="submit" disabled={loading || !formData.name}>{loading ? 'Salvando...' : 'Salvar'}</Button>
            </ModalFooter>
          </div>
        </form>
      </ModalContent>
    </Modal>
  )
}
