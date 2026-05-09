import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, SearchableSelect, useApi } from 'archon-ui'
import { creatorService, type CreateCreatorRequest, type UpdateCreatorRequest } from '../../services/creatorService'
import type { Creator } from '../../types/creator'
import { PixKeyType, pixKeyTypeLabels, type PixKeyTypeValue } from '../../types/creatorPayment'
import { cleanFormPayload } from '../../lib/cleanFormPayload'

interface CreatorFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  creator: Creator | null
  onSuccess: () => void
}

const initialFormData: CreateCreatorRequest = {
  name: '',
  stageName: '',
  email: '',
  phone: '',
  document: '',
  pixKey: '',
  pixKeyType: undefined,
  primaryNiche: '',
  city: '',
  state: '',
  notes: '',
  defaultAgencyFeePercent: 0,
}

const pixKeyTypeOptions = [
  { value: '', label: 'Não informado' },
  ...Object.values(PixKeyType).map((value) => ({
    value: String(value),
    label: pixKeyTypeLabels[value as PixKeyTypeValue],
  })),
]

export default function CreatorFormModal({ open, onOpenChange, creator, onSuccess }: CreatorFormModalProps) {
  const isEditing = !!creator
  const [formData, setFormData] = useState<CreateCreatorRequest>(initialFormData)
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (creator) {
      setFormData({
        name: creator.name,
        stageName: creator.stageName || '',
        email: creator.email || '',
        phone: creator.phone || '',
        document: creator.document || '',
        pixKey: creator.pixKey || '',
        pixKeyType: creator.pixKeyType,
        primaryNiche: creator.primaryNiche || '',
        city: creator.city || '',
        state: creator.state || '',
        notes: creator.notes || '',
        defaultAgencyFeePercent: creator.defaultAgencyFeePercent || 0,
      })
      setIsActive(creator.isActive)
      return
    }

    setFormData(initialFormData)
    setIsActive(true)
  }, [creator, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const cleaned = cleanFormPayload(formData)

    const result = await execute(() =>
      isEditing
        ? creatorService.update(creator.id, {
            id: creator.id,
            ...cleaned,
            isActive,
          } satisfies UpdateCreatorRequest)
        : creatorService.create(cleaned),
    )

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '1100px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar influenciador' : 'Novo influenciador'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">Nome</label>
              <Input value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Nome artístico</label>
              <Input value={formData.stageName || ''} onChange={(e) => setFormData((prev) => ({ ...prev, stageName: e.target.value }))} />
            </div>
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
            <div className="space-y-2">
              <label className="text-sm font-medium">Tipo da chave PIX</label>
              <SearchableSelect
                value={formData.pixKeyType ? String(formData.pixKeyType) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, pixKeyType: value ? (Number(value) as PixKeyTypeValue) : undefined }))}
                options={pixKeyTypeOptions}
                placeholder="Não informado"
                searchPlaceholder="Buscar"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Nicho principal</label>
              <Input value={formData.primaryNiche || ''} onChange={(e) => setFormData((prev) => ({ ...prev, primaryNiche: e.target.value }))} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Fee padrão da agência (%)</label>
              <Input type="number" value={formData.defaultAgencyFeePercent} onChange={(e) => setFormData((prev) => ({ ...prev, defaultAgencyFeePercent: Number(e.target.value) }))} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Cidade</label>
              <Input value={formData.city || ''} onChange={(e) => setFormData((prev) => ({ ...prev, city: e.target.value }))} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Estado</label>
              <Input value={formData.state || ''} onChange={(e) => setFormData((prev) => ({ ...prev, state: e.target.value }))} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Observações</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
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
              <Button type="submit" disabled={loading}>{loading ? 'Salvando...' : 'Salvar'}</Button>
            </ModalFooter>
          </div>
        </form>
      </ModalContent>
    </Modal>
  )
}
