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
  SearchableSelect,
  useApi,
} from 'archon-ui'
import { financialSubcategoryService } from '../../services/financialSubcategoryService'
import { financialEntryCategoryLabels } from '../../types/financialEntry'
import type { FinancialSubcategory } from '../../types/financialSubcategory'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  subcategory: FinancialSubcategory | null
  onSuccess: () => void
}

const macroOptions = Object.entries(financialEntryCategoryLabels).map(([value, label]) => ({ value, label }))

export default function FinancialSubcategoryFormModal({ open, onOpenChange, subcategory, onSuccess }: Props) {
  const isEditing = !!subcategory
  const [name, setName] = useState('')
  const [macroCategory, setMacroCategory] = useState<number>(1)
  const [color, setColor] = useState('#6366f1')
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    if (subcategory) {
      setName(subcategory.name)
      setMacroCategory(subcategory.macroCategory)
      setColor(subcategory.color)
      setIsActive(subcategory.isActive)
    } else {
      setName('')
      setMacroCategory(1)
      setColor('#6366f1')
      setIsActive(true)
    }
  }, [open, subcategory])

  const isValid = useMemo(() => name.trim().length >= 2, [name])

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    const payload = { name: name.trim(), macroCategory, color }
    const result = await execute(() => {
      if (isEditing && subcategory) {
        return financialSubcategoryService.update(subcategory.id, { id: subcategory.id, ...payload, isActive })
      }
      return financialSubcategoryService.create(payload)
    })
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar subcategoria' : 'Nova subcategoria financeira'}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Nome</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Patrocínio anual, Software SaaS..." required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Categoria macro</label>
              <SearchableSelect
                value={String(macroCategory)}
                onValueChange={(value) => setMacroCategory(Number(value))}
                options={macroOptions}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Cor</label>
              <Input type="color" value={color} onChange={(e) => setColor(e.target.value)} />
            </div>
          </div>
          {isEditing && (
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span>Ativa</span>
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
