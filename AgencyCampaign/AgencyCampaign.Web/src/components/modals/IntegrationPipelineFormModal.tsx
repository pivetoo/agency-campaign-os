import { useEffect, useState } from 'react'
import { Button, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, SearchableSelect, useApi } from 'archon-ui'
import { integrationPipelineService, type CreateIntegrationPipelineRequest, type UpdateIntegrationPipelineRequest } from '../../services/integrationPipelineService'
import type { Integration, IntegrationPipeline } from '../../types/integration'

interface IntegrationPipelineFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  pipeline: IntegrationPipeline | null
  integrations: Integration[]
  onSuccess: () => void
}

const initialFormData: CreateIntegrationPipelineRequest = {
  integrationId: 0,
  identifier: '',
  name: '',
  description: '',
}

export default function IntegrationPipelineFormModal({ open, onOpenChange, pipeline, integrations, onSuccess }: IntegrationPipelineFormModalProps) {
  const isEditing = !!pipeline
  const [formData, setFormData] = useState<CreateIntegrationPipelineRequest>(initialFormData)
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (pipeline) {
      setFormData({
        integrationId: pipeline.integrationId,
        identifier: pipeline.identifier,
        name: pipeline.name,
        description: pipeline.description || '',
      })
      setIsActive(pipeline.isActive)
      return
    }

    setFormData(initialFormData)
    setIsActive(true)
  }, [pipeline, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() => (
      isEditing
        ? integrationPipelineService.update(pipeline.id, {
            id: pipeline.id,
            ...formData,
            isActive,
          } satisfies UpdateIntegrationPipelineRequest)
        : integrationPipelineService.create(formData)
    ))

    if (result !== null) {
      onSuccess()
    }
  }

  const integrationOptions = integrations.map((item) => ({ value: String(item.id), label: item.name }))

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '600px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar pipeline' : 'Novo pipeline'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Integracao</label>
              <SearchableSelect
                options={integrationOptions}
                value={formData.integrationId ? String(formData.integrationId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, integrationId: Number(value) || 0 }))}
                placeholder="Selecione uma integracao"
              />
            </div>

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
            <Button type="submit" disabled={loading || !formData.integrationId || !formData.identifier || !formData.name}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
