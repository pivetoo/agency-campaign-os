import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, useApi } from 'archon-ui'
import { proposalService, type Proposal, type CreateProposalRequest, type UpdateProposalRequest } from '../../services/proposalService'
import { opportunityService, type Opportunity } from '../../services/opportunityService'

interface ProposalFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  proposal: Proposal | null
  presetOpportunityId?: number | null
  onSuccess: (proposal?: Proposal) => void
}

const initialFormData: CreateProposalRequest = {
  opportunityId: 0,
  description: '',
  validityUntil: undefined,
  notes: '',
}

export default function ProposalFormModal({ open, onOpenChange, proposal, presetOpportunityId, onSuccess }: ProposalFormModalProps) {
  const isEditing = !!proposal
  const [formData, setFormData] = useState<CreateProposalRequest>(initialFormData)
  const [opportunities, setOpportunities] = useState<Opportunity[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    void opportunityService.getAll().then(setOpportunities)
  }, [])

  useEffect(() => {
    if (proposal) {
      setFormData({
        opportunityId: proposal.opportunityId,
        description: proposal.description || '',
        validityUntil: proposal.validityUntil,
        notes: proposal.notes || '',
      })
      return
    }

    setFormData({ ...initialFormData, opportunityId: presetOpportunityId ?? 0 })
  }, [proposal, open, presetOpportunityId])

  const opportunityOptions = opportunities.map((opportunity) => ({
    value: String(opportunity.id),
    label: `${opportunity.name} · ${opportunity.brand?.name || 'Marca'}`,
  }))

  const handleOpportunityChange = (value: string) => {
    setFormData((prev) => ({
      ...prev,
      opportunityId: Number(value),
    }))
  }

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
      onSuccess(result as Proposal)
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '960px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar proposta' : 'Criar proposta comercial'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            {!presetOpportunityId && (
              <div className="space-y-2">
                <label className="text-sm font-medium">Oportunidade</label>
                <SearchableSelect
                  value={formData.opportunityId ? String(formData.opportunityId) : ''}
                  onValueChange={handleOpportunityChange}
                  options={opportunityOptions}
                  placeholder="Selecione a oportunidade"
                  searchPlaceholder="Buscar oportunidade"
                />
              </div>
            )}

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
            <Button type="submit" disabled={loading || !formData.opportunityId}>{loading ? 'Salvando...' : isEditing ? 'Salvar' : 'Criar e continuar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
