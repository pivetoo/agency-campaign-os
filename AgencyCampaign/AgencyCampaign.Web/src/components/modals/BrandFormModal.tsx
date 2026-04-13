import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, useApi } from 'archon-ui'
import { brandService, type CreateBrandRequest, type UpdateBrandRequest } from '../../services/brandService'
import type { Brand } from '../../types/brand'

interface BrandFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  brand: Brand | null
  onSuccess: () => void
}

const initialFormData: CreateBrandRequest = {
  name: '',
  contactName: '',
  contactEmail: '',
}

export default function BrandFormModal({ open, onOpenChange, brand, onSuccess }: BrandFormModalProps) {
  const isEditing = !!brand
  const [formData, setFormData] = useState<CreateBrandRequest>(initialFormData)
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (brand) {
      setFormData({
        name: brand.name,
        contactName: brand.contactName || '',
        contactEmail: brand.contactEmail || '',
      })
      setIsActive(brand.isActive)
      return
    }

    setFormData(initialFormData)
    setIsActive(true)
  }, [brand, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() =>
      isEditing
        ? brandService.update(brand.id, {
            id: brand.id,
            ...formData,
            isActive,
          } satisfies UpdateBrandRequest)
        : brandService.create(formData),
    )

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar marca' : 'Nova marca'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">Nome</label>
            <Input value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} required />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">Contato</label>
            <Input value={formData.contactName || ''} onChange={(e) => setFormData((prev) => ({ ...prev, contactName: e.target.value }))} />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">E-mail</label>
            <Input type="email" value={formData.contactEmail || ''} onChange={(e) => setFormData((prev) => ({ ...prev, contactEmail: e.target.value }))} />
          </div>

          {isEditing && (
            <div className="flex items-center gap-2">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span className="text-sm">Ativa</span>
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
