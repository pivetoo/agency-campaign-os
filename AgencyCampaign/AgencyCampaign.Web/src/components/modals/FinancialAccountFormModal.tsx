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
import { financialAccountService } from '../../services/financialAccountService'
import { financialAccountTypeLabels, type FinancialAccount } from '../../types/financialAccount'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  account: FinancialAccount | null
  onSuccess: () => void
}

const accountTypeOptions = Object.entries(financialAccountTypeLabels).map(([value, label]) => ({ value, label }))

export default function FinancialAccountFormModal({ open, onOpenChange, account, onSuccess }: Props) {
  const isEditing = !!account
  const [name, setName] = useState('')
  const [type, setType] = useState<number>(2)
  const [bank, setBank] = useState('')
  const [agency, setAgency] = useState('')
  const [number, setNumber] = useState('')
  const [initialBalance, setInitialBalance] = useState<string>('')
  const [color, setColor] = useState('#6366f1')
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    if (account) {
      setName(account.name)
      setType(account.type)
      setBank(account.bank ?? '')
      setAgency(account.agency ?? '')
      setNumber(account.number ?? '')
      setInitialBalance(String(account.initialBalance))
      setColor(account.color)
      setIsActive(account.isActive)
    } else {
      setName('')
      setType(2)
      setBank('')
      setAgency('')
      setNumber('')
      setInitialBalance('0')
      setColor('#6366f1')
      setIsActive(true)
    }
  }, [open, account])

  const isValid = useMemo(() => name.trim().length >= 2 && color.length > 0, [name, color])

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    const payload = {
      name: name.trim(),
      type,
      bank: bank.trim() || undefined,
      agency: agency.trim() || undefined,
      number: number.trim() || undefined,
      initialBalance: initialBalance.trim() === '' ? 0 : Number(initialBalance),
      color,
    }
    const result = await execute(() => {
      if (isEditing && account) {
        return financialAccountService.update(account.id, { id: account.id, ...payload, isActive })
      }
      return financialAccountService.create(payload)
    })
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar conta' : 'Nova conta financeira'}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Nome</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: PJ Itaú, Caixinha PIX" required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Tipo</label>
              <SearchableSelect
                value={String(type)}
                onValueChange={(value) => setType(Number(value))}
                options={accountTypeOptions}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Cor</label>
              <Input type="color" value={color} onChange={(e) => setColor(e.target.value)} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Banco</label>
              <Input value={bank} onChange={(e) => setBank(e.target.value)} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Saldo inicial (R$)</label>
              <Input type="number" step="0.01" value={initialBalance} onChange={(e) => setInitialBalance(e.target.value)} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Agência</label>
              <Input value={agency} onChange={(e) => setAgency(e.target.value)} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Conta</label>
              <Input value={number} onChange={(e) => setNumber(e.target.value)} />
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
