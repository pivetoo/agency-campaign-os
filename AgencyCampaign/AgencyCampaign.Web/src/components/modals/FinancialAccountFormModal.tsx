import { useEffect, useMemo, useState } from 'react'
import { Button, Checkbox, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, SearchableSelect, useApi, useI18n } from 'archon-ui'
import { financialAccountService } from '../../services/financialAccountService'
import { bankService } from '../../services/bankService'
import { FinancialAccountType, financialAccountTypeLabels, type FinancialAccount, type FinancialAccountTypeValue } from '../../types/financialAccount'
import type { Bank } from '../../types/bank'

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
  const [type, setType] = useState<FinancialAccountTypeValue>(FinancialAccountType.Bank)
  const [bankId, setBankId] = useState<string>('')
  const [agency, setAgency] = useState('')
  const [number, setNumber] = useState('')
  const [initialBalance, setInitialBalance] = useState<string>('')
  const [isActive, setIsActive] = useState(true)
  const [banks, setBanks] = useState<Bank[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const showBankFields = type === FinancialAccountType.Bank
  const initialBalanceLocked = isEditing && (account?.hasEntries ?? false)

  useEffect(() => {
    if (!open) return
    let cancelled = false
    bankService.getActive()
      .then((list) => { if (!cancelled) setBanks(list) })
      .catch(() => { if (!cancelled) setBanks([]) })
    return () => { cancelled = true }
  }, [open])

  useEffect(() => {
    if (!open) return
    if (account) {
      setName(account.name)
      setType(account.type)
      setBankId(account.bankId ? String(account.bankId) : '')
      setAgency(account.agency ?? '')
      setNumber(account.number ?? '')
      setInitialBalance(String(account.initialBalance))
      setIsActive(account.isActive)
    } else {
      setName('')
      setType(FinancialAccountType.Bank)
      setBankId('')
      setAgency('')
      setNumber('')
      setInitialBalance('0')
      setIsActive(true)
    }
  }, [open, account])

  const isValid = useMemo(() => name.trim().length >= 2, [name])

  const bankOptions = useMemo(
    () => banks.map((bank) => ({ value: String(bank.id), label: `${bank.compe} — ${bank.shortName}` })),
    [banks],
  )

  const handleTypeChange = (value: string) => {
    const next = Number(value) as FinancialAccountTypeValue
    setType(next)
    if (next !== FinancialAccountType.Bank) {
      setBankId('')
      setAgency('')
      setNumber('')
    }
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    const payload = {
      name: name.trim(),
      type,
      bankId: showBankFields && bankId ? Number(bankId) : null,
      agency: showBankFields && agency.trim() ? agency.trim() : undefined,
      number: showBankFields && number.trim() ? number.trim() : undefined,
      initialBalance: initialBalanceLocked && account
        ? account.initialBalance
        : initialBalance.trim() === ''
          ? 0
          : Number(initialBalance),
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
                <div className="space-y-2 md:col-span-2">
                  <label className="text-sm font-medium">{t('modal.financialAccount.field.bank')}</label>
                  <SearchableSelect
                    value={bankId}
                    onValueChange={setBankId}
                    options={bankOptions}
                    placeholder={t('modal.financialAccount.field.bank')}
                  />
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
