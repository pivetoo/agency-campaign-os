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
import {
  proposalBlockService,
  type ProposalBlock,
} from '../../services/proposalBlockService'

interface ProposalBlockFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  block: ProposalBlock | null
  onSuccess: () => void
}

const SUGGESTED_CATEGORIES = ['Cláusula', 'Condição comercial', 'Descrição padrão', 'Pagamento', 'Cancelamento']

export default function ProposalBlockFormModal(props: ProposalBlockFormModalProps) {
  const { open, onOpenChange, block, onSuccess } = props
  const { t } = useI18n()
  const [name, setName] = useState('')
  const [body, setBody] = useState('')
  const [category, setCategory] = useState('')
  const [isActive, setIsActive] = useState(true)

  const { execute, loading } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    if (block) {
      setName(block.name)
      setBody(block.body)
      setCategory(block.category)
      setIsActive(block.isActive)
    } else {
      setName('')
      setBody('')
      setCategory('')
      setIsActive(true)
    }
  }, [open, block])

  const isValid = useMemo(
    () => name.trim().length >= 2 && body.trim().length >= 1 && category.trim().length >= 1,
    [name, body, category]
  )

  const submit = async () => {
    if (!isValid) return

    const payload = {
      name: name.trim(),
      body: body.trim(),
      category: category.trim(),
    }

    const result = block
      ? await execute(() => proposalBlockService.update(block.id, { ...payload, id: block.id, isActive }))
      : await execute(() => proposalBlockService.create(payload))

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="2xl">
        <ModalHeader>
          <ModalTitle>{block ? t('modal.proposalBlock.title.edit') : t('modal.proposalBlock.title.new')}</ModalTitle>
          <ModalDescription>
            {t('modal.proposalBlock.description')}
          </ModalDescription>
        </ModalHeader>
        <ModalBody>
          <div className="space-y-4">
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              <div>
                <label className="mb-1 block text-sm font-medium">{t('common.field.name')}</label>
                <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Cláusula de exclusividade" />
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium">{t('modal.proposalBlock.field.category')}</label>
                <Input
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                  list="block-categories"
                  placeholder="Cláusula, Condição comercial..."
                />
                <datalist id="block-categories">
                  {SUGGESTED_CATEGORIES.map((cat) => (
                    <option key={cat} value={cat} />
                  ))}
                </datalist>
              </div>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium">{t('modal.proposalBlock.field.content')}</label>
              <textarea
                value={body}
                onChange={(e) => setBody(e.target.value)}
                rows={8}
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                placeholder="Texto do bloco. Pode usar quebras de linha."
              />
            </div>

            {block ? (
              <div className="flex items-center gap-3">
                <Switch checked={isActive} onCheckedChange={setIsActive} />
                <span className="text-sm">Bloco ativo</span>
              </div>
            ) : null}
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
