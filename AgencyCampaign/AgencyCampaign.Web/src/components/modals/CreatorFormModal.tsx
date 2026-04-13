import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, useApi } from 'archon-ui'
import { creatorService, type CreateCreatorRequest, type UpdateCreatorRequest } from '../../services/creatorService'
import type { Creator } from '../../types/creator'

interface CreatorFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  creator: Creator | null
  onSuccess: () => void
}

const initialFormData: CreateCreatorRequest = {
  name: '',
  email: '',
  phone: '',
  document: '',
  pixKey: '',
}

export default function CreatorFormModal({ open, onOpenChange, creator, onSuccess }: CreatorFormModalProps) {
  const isEditing = !!creator
  const [formData, setFormData] = useState<CreateCreatorRequest>(initialFormData)
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (creator) {
      setFormData({
        name: creator.name,
        email: creator.email || '',
        phone: creator.phone || '',
        document: creator.document || '',
        pixKey: creator.pixKey || '',
      })
      setIsActive(creator.isActive)
      return
    }

    setFormData(initialFormData)
    setIsActive(true)
  }, [creator, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() =>
      isEditing
        ? creatorService.update(creator.id, {
            id: creator.id,
            ...formData,
            isActive,
          } satisfies UpdateCreatorRequest)
        : creatorService.create(formData),
    )

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar influenciador' : 'Novo influenciador'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">Nome</label>
            <Input value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} required />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">E-mail</label>
              <Input type="email" value={formData.email || ''} onChange={(e) => setFormData((prev) => ({ ...prev, email: e.target.value }))} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Telefone</label>
              <Input value={formData.phone || ''} onChange={(e) => setFormData((prev) => ({ ...prev, phone: e.target.value }))} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Documento</label>
              <Input value={formData.document || ''} onChange={(e) => setFormData((prev) => ({ ...prev, document: e.target.value }))} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Chave PIX</label>
              <Input value={formData.pixKey || ''} onChange={(e) => setFormData((prev) => ({ ...prev, pixKey: e.target.value }))} />
            </div>
          </div>

          {isEditing && (
            <div className="flex items-center gap-2">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span className="text-sm">Ativo</span>
            </div>
          )}

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
