import { useEffect, useMemo, useState } from 'react'
import {
  Button,
  Modal,
  ModalBody,
  ModalContent,
  ModalDescription,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  SearchableSelect,
  useApi,
  useI18n,
} from 'archon-ui'
import {
  proposalTemplateService,
  type ProposalTemplate,
} from '../../services/proposalTemplateService'

interface ApplyProposalTemplateModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  proposalId: number
  onApplied: () => void
}

export default function ApplyProposalTemplateModal(props: ApplyProposalTemplateModalProps) {
  const { t } = useI18n()
  const { open, onOpenChange, proposalId, onApplied } = props
  const [templates, setTemplates] = useState<ProposalTemplate[]>([])
  const [selectedId, setSelectedId] = useState<string>('')

  const { execute: load } = useApi<ProposalTemplate[]>({ showErrorMessage: true })
  const { execute: runApply, loading: applying } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    setSelectedId('')
    void load(() => proposalTemplateService.getAll({ pageSize: 200 })).then((result) => {
      if (result) setTemplates(result)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const options = useMemo(
    () => templates.map((template) => ({
      value: template.id.toString(),
      label: `${template.name} (${template.items.length} ${template.items.length === 1 ? 'item' : 'itens'})`,
    })),
    [templates]
  )

  const selectedTemplate = useMemo(
    () => templates.find((template) => template.id.toString() === selectedId) ?? null,
    [templates, selectedId]
  )

  const apply = async () => {
    if (!selectedTemplate) return
    const result = await runApply(() => proposalTemplateService.applyToProposal(selectedTemplate.id, proposalId))
    if (result !== null) {
      onApplied()
      onOpenChange(false)
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{t('modal.applyProposalTemplate.title')}</ModalTitle>
          <ModalDescription>
            {t('modal.applyProposalTemplate.description')}
          </ModalDescription>
        </ModalHeader>
        <ModalBody>
          <div className="space-y-3">
            <SearchableSelect
              value={selectedId}
              onValueChange={setSelectedId}
              options={options}
              placeholder={templates.length === 0 ? t('modal.applyProposalTemplate.placeholder.noTemplate') : t('modal.applyProposalTemplate.placeholder.selectTemplate')}
              searchPlaceholder="Buscar template..."
              disabled={templates.length === 0}
            />

            {selectedTemplate ? (
              <div className="rounded-md border border-border/70 bg-muted/20 p-3">
                <div className="text-sm font-medium text-foreground">{selectedTemplate.name}</div>
                {selectedTemplate.description ? (
                  <p className="mt-1 text-xs text-muted-foreground">{selectedTemplate.description}</p>
                ) : null}
                <div className="mt-2 max-h-[35vh] space-y-1 overflow-y-auto pr-1 text-xs text-muted-foreground">
                  {selectedTemplate.items.map((item, index) => (
                    <div key={item.id ?? index} className="flex items-center justify-between gap-2">
                      <span className="truncate">{item.description}</span>
                      <span className="shrink-0">
                        {item.defaultQuantity}× R$ {item.defaultUnitPrice.toFixed(2)}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            ) : null}
          </div>
        </ModalBody>
        <ModalFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={applying}>
            {t('common.action.cancel')}
          </Button>
          <Button onClick={() => void apply()} disabled={!selectedTemplate || applying}>
            {applying ? t('common.action.applying') : t('common.action.apply')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
