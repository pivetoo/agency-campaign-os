import { useEffect, useMemo, useState } from 'react'
import {
  Button,
  Checkbox,
  Input,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  useApi,
} from 'archon-ui'
import {
  opportunitySourceService,
  opportunityTagService,
} from '../../services/opportunitySourceService'
import type { OpportunitySource, OpportunityTag } from '../../types/opportunitySource'

interface BaseProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export function OpportunitySourceFormModal({
  open,
  onOpenChange,
  source,
  onSuccess,
}: BaseProps & { source: OpportunitySource | null }) {
  const isEditing = !!source
  const [name, setName] = useState('')
  const [color, setColor] = useState('#6366f1')
  const [displayOrder, setDisplayOrder] = useState(0)
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    if (source) {
      setName(source.name)
      setColor(source.color)
      setDisplayOrder(source.displayOrder)
      setIsActive(source.isActive)
    } else {
      setName('')
      setColor('#6366f1')
      setDisplayOrder(0)
      setIsActive(true)
    }
  }, [open, source])

  const isValid = useMemo(() => name.trim().length >= 2, [name])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    const result = await execute(() => {
      if (isEditing && source) {
        return opportunitySourceService.update(source.id, {
          id: source.id,
          name: name.trim(),
          color,
          displayOrder,
          isActive,
        })
      }
      return opportunitySourceService.create({ name: name.trim(), color, displayOrder })
    })
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar origem' : 'Nova origem de oportunidade'}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Nome</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Inbound, Indicação..." required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Cor</label>
              <Input type="color" value={color} onChange={(e) => setColor(e.target.value)} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Ordem</label>
              <Input
                type="number"
                value={displayOrder}
                onChange={(e) => setDisplayOrder(Number(e.target.value))}
              />
            </div>
          </div>
          {isEditing && (
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span>Ativo</span>
            </label>
          )}
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading || !isValid}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}

export function OpportunityTagFormModal({
  open,
  onOpenChange,
  tag,
  onSuccess,
}: BaseProps & { tag: OpportunityTag | null }) {
  const isEditing = !!tag
  const [name, setName] = useState('')
  const [color, setColor] = useState('#6366f1')
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    if (tag) {
      setName(tag.name)
      setColor(tag.color)
      setIsActive(tag.isActive)
    } else {
      setName('')
      setColor('#6366f1')
      setIsActive(true)
    }
  }, [open, tag])

  const isValid = useMemo(() => name.trim().length >= 2, [name])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    const result = await execute(() => {
      if (isEditing && tag) {
        return opportunityTagService.update(tag.id, {
          id: tag.id,
          name: name.trim(),
          color,
          isActive,
        })
      }
      return opportunityTagService.create({ name: name.trim(), color })
    })
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar tag' : 'Nova tag de oportunidade'}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Nome</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Estratégico, Quente, Q4..." required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Cor</label>
              <Input type="color" value={color} onChange={(e) => setColor(e.target.value)} />
            </div>
          </div>
          {isEditing && (
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span>Ativo</span>
            </label>
          )}
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading || !isValid}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
