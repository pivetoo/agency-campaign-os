import { useEffect, useState } from 'react'
import { Button, Checkbox, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import { commercialPipelineStageService, type CreateCommercialPipelineStageRequest, type UpdateCommercialPipelineStageRequest } from '../../services/commercialPipelineStageService'
import type { CommercialPipelineStage } from '../../types/commercialPipelineStage'

interface CommercialPipelineStageFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  stage: CommercialPipelineStage | null
  onSuccess: () => void
}

const initialFormData: CreateCommercialPipelineStageRequest = {
  name: '',
  description: '',
  displayOrder: 0,
  color: '#6366f1',
  isInitial: false,
  isFinal: false,
  finalBehavior: 0,
  defaultProbability: undefined,
  slaInDays: undefined,
}

export default function CommercialPipelineStageFormModal({ open, onOpenChange, stage, onSuccess }: CommercialPipelineStageFormModalProps) {
  const isEditing = !!stage
  const [formData, setFormData] = useState<CreateCommercialPipelineStageRequest>(initialFormData)
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (stage) {
      setFormData({
        name: stage.name,
        description: stage.description || '',
        displayOrder: stage.displayOrder,
        color: stage.color,
        isInitial: stage.isInitial,
        isFinal: stage.isFinal,
        finalBehavior: stage.finalBehavior,
        defaultProbability: stage.defaultProbability,
        slaInDays: stage.slaInDays,
      })
      setIsActive(stage.isActive)
      return
    }

    setFormData(initialFormData)
    setIsActive(true)
  }, [stage, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() => (
      isEditing
        ? commercialPipelineStageService.update(stage.id, {
            id: stage.id,
            ...formData,
            isActive,
          } satisfies UpdateCommercialPipelineStageRequest)
        : commercialPipelineStageService.create(formData)
    ))

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '860px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar estágio do pipeline' : 'Novo estágio do pipeline'}</ModalTitle>
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
              <label className="text-sm font-medium">Comportamento final</label>
              <Select value={String(formData.finalBehavior)} onValueChange={(value) => setFormData((prev) => ({ ...prev, finalBehavior: Number(value), isFinal: value !== '0' ? true : prev.isFinal }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="0">Sem fechamento</SelectItem>
                  <SelectItem value="1">Ganha</SelectItem>
                  <SelectItem value="2">Perdida</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Probabilidade padrão (%)</label>
              <Input
                type="number"
                min={0}
                max={100}
                step={5}
                placeholder="-"
                value={formData.defaultProbability ?? ''}
                onChange={(event) => setFormData((prev) => ({
                  ...prev,
                  defaultProbability: event.target.value === '' ? undefined : Number(event.target.value),
                }))}
              />
              <p className="text-xs text-muted-foreground">Aplicada automaticamente quando uma oportunidade entra neste estágio.</p>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">SLA do estágio (dias)</label>
              <Input
                type="number"
                min={1}
                placeholder="-"
                value={formData.slaInDays ?? ''}
                onChange={(event) => setFormData((prev) => ({
                  ...prev,
                  slaInDays: event.target.value === '' ? undefined : Number(event.target.value),
                }))}
              />
              <p className="text-xs text-muted-foreground">Dias máximos no estágio antes do card ficar amarelo / vermelho no Kanban.</p>
            </div>
          </div>

          <div className="flex flex-wrap gap-6">
            <label className="flex items-center gap-2 text-sm"><Checkbox checked={formData.isInitial} onCheckedChange={(checked) => setFormData((prev) => ({ ...prev, isInitial: !!checked }))} /><span>Estágio inicial</span></label>
            <label className="flex items-center gap-2 text-sm"><Checkbox checked={formData.isFinal} onCheckedChange={(checked) => setFormData((prev) => ({ ...prev, isFinal: !!checked, finalBehavior: !checked ? 0 : prev.finalBehavior }))} /><span>Estágio final</span></label>
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
