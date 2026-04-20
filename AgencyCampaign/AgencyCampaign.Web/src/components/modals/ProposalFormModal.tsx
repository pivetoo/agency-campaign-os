import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import { proposalService, type Proposal, type CreateProposalRequest, type UpdateProposalRequest } from '../../services/proposalService'
import { brandService, type Brand } from '../../services/brandService'

interface ProposalFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  proposal: Proposal | null
  onSuccess: () => void
}

const initialFormData: CreateProposalRequest = {
  brandId: 0,
  name: '',
  description: '',
  validityUntil: undefined,
  notes: '',
}

export default function ProposalFormModal({ open, onOpenChange, proposal, onSuccess }: ProposalFormModalProps) {
  const isEditing = !!proposal
  const [formData, setFormData] = useState<CreateProposalRequest>(initialFormData)
  const [brands, setBrands] = useState<Brand[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    void brandService.getAll().then(setBrands)
  }, [])

  useEffect(() => {
    if (proposal) {
      setFormData({
        brandId: proposal.brandId,
        name: proposal.name,
        description: proposal.description || '',
        validityUntil: proposal.validityUntil,
        notes: proposal.notes || '',
      })
      return
    }

    setFormData(initialFormData)
  }, [proposal, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() =>
      isEditing
        ? proposalService.update(proposal.id, {
            id: proposal.id,
            ...formData,
          } satisfies UpdateProposalRequest)
        : proposalService.create(formData),
    )

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '960px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar proposta' : 'Nova proposta'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">Nome da proposta</label>
              <Input value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Marca</label>
              <Select
                value={formData.brandId ? String(formData.brandId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, brandId: Number(value) }))}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Selecione..." />
                </SelectTrigger>
                <SelectContent>
                {brands.map((brand) => (
                  <SelectItem key={brand.id} value={String(brand.id)}>
                    {brand.name}
                  </SelectItem>
                ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Validade</label>
              <Input
                type="date"
                value={formData.validityUntil?.split('T')[0] || ''}
                onChange={(e) => setFormData((prev) => ({ ...prev, validityUntil: e.target.value ? new Date(e.target.value).toISOString() : undefined }))}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Descrição</label>
              <Input value={formData.description || ''} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} />
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: '1rem' }}>
            <div className="space-y-2">
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
