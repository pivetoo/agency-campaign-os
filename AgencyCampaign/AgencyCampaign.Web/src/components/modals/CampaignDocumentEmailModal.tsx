import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, useApi } from 'archon-ui'
import { campaignDocumentService, type SendCampaignDocumentEmailRequest } from '../../services/campaignDocumentService'
import type { CampaignDocument } from '../../types/campaignDocument'
import type { CampaignCreator } from '../../types/campaignCreator'

interface CampaignDocumentEmailModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  document: CampaignDocument | null
  campaignCreators: CampaignCreator[]
  onSuccess: () => void
}

const initialFormData: SendCampaignDocumentEmailRequest = {
  recipientEmail: '',
  subject: '',
  body: '',
}

export default function CampaignDocumentEmailModal({ open, onOpenChange, document, campaignCreators, onSuccess }: CampaignDocumentEmailModalProps) {
  const [formData, setFormData] = useState<SendCampaignDocumentEmailRequest>(initialFormData)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!document) {
      setFormData(initialFormData)
      return
    }

    const recipientEmail = document.recipientEmail || ''

    setFormData({
      recipientEmail,
      subject: document.emailSubject || `Documento: ${document.title}`,
      body: document.emailBody || `Olá, segue o documento ${document.title} para sua validação.`,
    })
  }, [document, campaignCreators, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    if (!document) {
      return
    }

    const result = await execute(() => campaignDocumentService.sendEmail(document.id, formData))
    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '920px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>Enviar documento por e-mail</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: '1rem' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">Destinatário</label>
              <Input type="email" value={formData.recipientEmail} onChange={(e) => setFormData((prev) => ({ ...prev, recipientEmail: e.target.value }))} required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Assunto</label>
              <Input value={formData.subject} onChange={(e) => setFormData((prev) => ({ ...prev, subject: e.target.value }))} required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Mensagem</label>
              <Input value={formData.body || ''} onChange={(e) => setFormData((prev) => ({ ...prev, body: e.target.value }))} />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading}>{loading ? 'Enviando...' : 'Enviar e-mail'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
