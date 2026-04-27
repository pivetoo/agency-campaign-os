import { useEffect, useState } from 'react'
import { Button, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import { integrationService, type CreateIntegrationRequest, type UpdateIntegrationRequest } from '../../services/integrationService'
import { integrationCategoryService, type IntegrationCategory } from '../../services/integrationCategoryService'
import type { Integration } from '../../types/integration'

interface IntegrationFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  integration: Integration | null
  onSuccess: () => void
}

const initialFormData: CreateIntegrationRequest = {
  identifier: '',
  name: '',
  description: '',
  categoryId: 0,
}

export default function IntegrationFormModal({ open, onOpenChange, integration, onSuccess }: IntegrationFormModalProps) {
  const isEditing = !!integration
  const [formData, setFormData] = useState<CreateIntegrationRequest>(initialFormData)
  const [isActive, setIsActive] = useState(true)
  const [categories, setCategories] = useState<IntegrationCategory[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: fetchCategories, loading: loadingCategories } = useApi<IntegrationCategory[]>({ showErrorMessage: true })

  useEffect(() => {
    if (open) {
      void loadCategories()
    }
  }, [open])

  useEffect(() => {
    if (integration) {
      setFormData({
        identifier: integration.identifier,
        name: integration.name,
        description: integration.description || '',
        categoryId: integration.categoryId,
      })
      setIsActive(integration.isActive)
      return
    }

    setFormData(initialFormData)
    setIsActive(true)
  }, [integration, open])

  const loadCategories = async () => {
    const result = await fetchCategories(() => integrationCategoryService.getActive())
    if (result) setCategories(result)
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() => (
      isEditing
        ? integrationService.update(integration.id, {
            id: integration.id,
            ...formData,
            isActive,
          } satisfies UpdateIntegrationRequest)
        : integrationService.create(formData)
    ))

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '600px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar integracao' : 'Nova integracao'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">Identificador</label>
              <Input value={formData.identifier} onChange={(event) => setFormData((prev) => ({ ...prev, identifier: event.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Nome</label>
              <Input value={formData.name} onChange={(event) => setFormData((prev) => ({ ...prev, name: event.target.value }))} required />
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Descricao</label>
              <Input value={formData.description || ''} onChange={(event) => setFormData((prev) => ({ ...prev, description: event.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Categoria</label>
              <Select
                value={formData.categoryId ? String(formData.categoryId) : undefined}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, categoryId: Number(value) }))}
              >
                <SelectTrigger>
                  {loadingCategories ? <span>Carregando...</span> : <SelectValue />}
                </SelectTrigger>
                <SelectContent>
                  {categories.map((cat) => (
                    <SelectItem key={cat.id} value={String(cat.id)}>{cat.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {isEditing && (
              <div className="flex items-center gap-2">
                <label className="flex items-center gap-2 text-sm">
                  <input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} className="h-4 w-4" />
                  <span>Ativo</span>
                </label>
              </div>
            )}
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading || !formData.identifier || !formData.name || !formData.categoryId}>
              {loading ? 'Salvando...' : 'Salvar'}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
