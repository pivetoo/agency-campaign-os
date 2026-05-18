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
  useI18n,
} from 'archon-ui'
import { financialAccountService } from '../../services/financialAccountService'
import { FinancialAccountType, financialAccountTypeLabels, type FinancialAccount, type FinancialAccountTypeValue } from '../../types/financialAccount'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  account: FinancialAccount | null
  onSuccess: () => void
}

const accountTypeOptions = Object.entries(financialAccountTypeLabels).map(([value, label]) => ({ value, label }))

export default function FinancialAccountFormModal({ open, onOpenChange, account, onSuccess }: Props) {
  const { t } = useI18n()
  const isEditing = !!account
  const [name, setName] = useState('')
  const [type, setType] = useState<FinancialAccountTypeValue>(FinancialAccountType.Cash)
  const [bank, setBank] = useState('')
  const [agency, setAgency] = useState('')
  const [number, setNumber] = useState('')
  const [initialBalance, setInitialBalance] = useState<string>('')
  const [color, setColor] = useState('#6366f1')
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const showBankFields = type === FinancialAccountType.Bank
  const initialBalanceLocked = isEditing && (account?.hasEntries ?? false)

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
      setType(FinancialAccountType.Cash)
      setBank('')
      setAgency('')
      setNumber('')
      setInitialBalance('0')
      setColor('#6366f1')
      setIsActive(true)
    }
  }, [open, account])

  const isValid = useMemo(() => name.trim().length >= 2 && /^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/.test(color), [name, color])

  const handleTypeChange = (value: string) => {
    const next = Number(value) as FinancialAccountTypeValue
    setType(next)
    if (next !== FinancialAccountType.Bank) {
      setBank('')
      setAgency('')
      setNumber('')
    }
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    const payload = {
      name: name.trim(),
      type,
      bank: showBankFields && bank.trim() ? bank.trim() : undefined,
      agency: showBankFields && agency.trim() ? agency.trim() : undefined,
      number: showBankFields && number.trim() ? number.trim() : undefined,
      initialBalance: initialBalanceLocked && account
        ? account.initialBalance
        : initialBalance.trim() === ''
          ? 0
          : Number(initialBalance),
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
          <ModalTitle>{isEditing ? t('modal.financialAccount.title.edit') : t('modal.financialAccount.title.new')}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('common.field.name')}</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder={t('modal.financialAccount.field.namePlaceholder')} required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.type')}</label>
              <SearchableSelect
                value={String(type)}
                onValueChange={handleTypeChange}
                options={accountTypeOptions}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.color')}</label>
              <Input type="color" value={color} onChange={(e) => setColor(e.target.value)} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.financialAccount.field.initialBalance')}</label>
              <Input
                type="number"
                step="0.01"
                value={initialBalance}
                onChange={(e) => setInitialBalance(e.target.value)}
                disabled={initialBalanceLocked}
              />
              {initialBalanceLocked && (
                <p className="text-xs text-muted-foreground">{t('modal.financialAccount.field.initialBalanceLockedHint')}</p>
              )}
            </div>
            {showBankFields && (
              <>
                <div className="space-y-2">
                  <label className="text-sm font-medium">{t('modal.financialAccount.field.bank')}</label>
                  <Input value={bank} onChange={(e) => setBank(e.target.value)} />
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-medium">{t('modal.financialAccount.field.agency')}</label>
                  <Input value={agency} onChange={(e) => setAgency(e.target.value)} />
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-medium">{t('modal.financialAccount.field.number')}</label>
                  <Input value={number} onChange={(e) => setNumber(e.target.value)} />
                </div>
              </>
            )}
          </div>
          {isEditing && (
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span>{t('common.status.activeFemale')}</span>
            </label>
          )}
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !isValid}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
