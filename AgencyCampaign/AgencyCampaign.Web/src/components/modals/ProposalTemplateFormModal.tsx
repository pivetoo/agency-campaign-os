import { useEffect, useMemo, useState } from 'react'
import {
  Button,
  Input,
  Modal,
  ModalBody,
  ModalContent,
  ModalDescription,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  Switch,
  useApi,
  useI18n,
} from 'archon-ui'
import { Plus, Trash2 } from 'lucide-react'
import {
  proposalTemplateService,
  type ProposalTemplate,
  type ProposalTemplateItem,
} from '../../services/proposalTemplateService'

interface ProposalTemplateFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  template: ProposalTemplate | null
  onSuccess: () => void
}

function emptyItem(displayOrder: number): ProposalTemplateItem {
  return {
    description: '',
    defaultQuantity: 1,
    defaultUnitPrice: 0,
    defaultDeliveryDays: undefined,
    observations: '',
    displayOrder,
  }
}

export default function ProposalTemplateFormModal(props: ProposalTemplateFormModalProps) {
  const { open, onOpenChange, template, onSuccess } = props
  const { t } = useI18n()
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [items, setItems] = useState<ProposalTemplateItem[]>([])

  const { execute, loading } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    if (template) {
      setName(template.name)
      setDescription(template.description ?? '')
      setIsActive(template.isActive)
      setItems(template.items.length > 0 ? [...template.items] : [emptyItem(0)])
    } else {
      setName('')
      setDescription('')
      setIsActive(true)
      setItems([emptyItem(0)])
    }
  }, [open, template])

  const isValid = useMemo(() => {
    if (name.trim().length < 2) return false
    return items.every((item) => item.description.trim().length > 0 && item.defaultQuantity > 0)
  }, [name, items])

  const updateItem = (index: number, patch: Partial<ProposalTemplateItem>) => {
    setItems((prev) => prev.map((item, i) => (i === index ? { ...item, ...patch } : item)))
  }

  const addItem = () => {
    setItems((prev) => [...prev, emptyItem(prev.length)])
  }

  const removeItem = (index: number) => {
    setItems((prev) => prev.filter((_, i) => i !== index).map((item, i) => ({ ...item, displayOrder: i })))
  }

  const submit = async () => {
    if (!isValid) return

    const payload = {
      name: name.trim(),
      description: description.trim() || undefined,
      items: items.map((item, index) => ({ ...item, displayOrder: index })),
    }

    const result = template
      ? await execute(() => proposalTemplateService.update(template.id, { ...payload, id: template.id, isActive }))
      : await execute(() => proposalTemplateService.create(payload))

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="4xl">
        <ModalHeader>
          <ModalTitle>{template ? t('modal.proposalTemplate.title.edit') : t('modal.proposalTemplate.title.new')}</ModalTitle>
          <ModalDescription>
            {t('modal.proposalTemplate.description')}
          </ModalDescription>
        </ModalHeader>
        <ModalBody>
          <div className="space-y-5">
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div>
                <label className="mb-1 block text-sm font-medium">{t('common.field.name')}</label>
                <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Pacote Influência Reels" />
              </div>
              {template ? (
                <div className="flex items-end gap-3">
                  <Switch checked={isActive} onCheckedChange={setIsActive} />
                  <span className="text-sm">Template ativo</span>
                </div>
              ) : null}
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium">{t('common.field.description')}</label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={2}
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                placeholder="Para que esse template é usado?"
              />
            </div>

            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold">Itens do template</h3>
                <Button size="sm" variant="outline" icon={<Plus className="h-4 w-4" />} onClick={addItem}>
                  Adicionar item
                </Button>
              </div>

              {items.map((item, index) => (
                <div key={index} className="rounded-md border border-border/70 p-3">
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-xs font-medium text-muted-foreground">Item #{index + 1}</span>
                    {items.length > 1 ? (
                      <button
                        type="button"
                        onClick={() => removeItem(index)}
                        className="inline-flex h-7 w-7 items-center justify-center rounded text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    ) : null}
                  </div>
                  <div className="mt-2 grid grid-cols-1 gap-2 md:grid-cols-12">
                    <div className="md:col-span-6">
                      <label className="mb-1 block text-xs text-muted-foreground">Descrição</label>
                      <Input
                        value={item.description}
                        onChange={(e) => updateItem(index, { description: e.target.value })}
                        placeholder="Reels patrocinado, 30s, 1 entrega"
                      />
                    </div>
                    <div className="md:col-span-2">
                      <label className="mb-1 block text-xs text-muted-foreground">Qtd</label>
                      <Input
                        type="number"
                        min={1}
                        value={item.defaultQuantity}
                        onChange={(e) => updateItem(index, { defaultQuantity: Math.max(1, Number(e.target.value)) })}
                      />
                    </div>
                    <div className="md:col-span-2">
                      <label className="mb-1 block text-xs text-muted-foreground">Unitário (R$)</label>
                      <Input
                        type="number"
                        min={0}
                        step={0.01}
                        value={item.defaultUnitPrice}
                        onChange={(e) => updateItem(index, { defaultUnitPrice: Math.max(0, Number(e.target.value)) })}
                      />
                    </div>
                    <div className="md:col-span-2">
                      <label className="mb-1 block text-xs text-muted-foreground">Prazo (dias)</label>
                      <Input
                        type="number"
                        min={0}
                        placeholder="-"
                        value={item.defaultDeliveryDays ?? ''}
                        onChange={(e) => updateItem(index, {
                          defaultDeliveryDays: e.target.value === '' ? undefined : Math.max(0, Number(e.target.value)),
                        })}
                      />
                    </div>
                    <div className="md:col-span-12">
                      <label className="mb-1 block text-xs text-muted-foreground">Observações</label>
                      <Input
                        value={item.observations ?? ''}
                        onChange={(e) => updateItem(index, { observations: e.target.value })}
                        placeholder="Opcional"
                      />
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </ModalBody>
        <ModalFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={loading}>
            {t('common.action.cancel')}
          </Button>
          <Button onClick={() => void submit()} disabled={!isValid || loading}>
            {loading ? t('common.action.saving') : t('common.action.save')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
