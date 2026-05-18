import { useEffect, useMemo, useState } from 'react'
import {
  Badge,
  Button,
  Checkbox,
  Input,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  useApi,
  useI18n,
} from 'archon-ui'
import { bankService } from '../../services/bankService'
import type { Bank } from '../../types/bank'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  bank: Bank | null
  onSuccess: () => void
}

export default function BankFormModal({ open, onOpenChange, bank, onSuccess }: Props) {
  const { t } = useI18n()
  const isEditing = !!bank
  const isSystem = bank?.isSystem ?? false
  const [compe, setCompe] = useState('')
  const [ispb, setIspb] = useState('')
  const [name, setName] = useState('')
  const [shortName, setShortName] = useState('')
  const [logoUrl, setLogoUrl] = useState('')
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    if (bank) {
      setCompe(bank.compe)
      setIspb(bank.ispb ?? '')
      setName(bank.name)
      setShortName(bank.shortName)
      setLogoUrl(bank.logoUrl ?? '')
      setIsActive(bank.isActive)
    } else {
      setCompe('')
      setIspb('')
      setName('')
      setShortName('')
      setLogoUrl('')
      setIsActive(true)
    }
  }, [open, bank])

  const isValid = useMemo(() => {
    return /^[0-9]{3}$/.test(compe) && name.trim().length >= 2 && shortName.trim().length >= 2
  }, [compe, name, shortName])

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    const payload = {
      compe: compe.trim(),
      ispb: ispb.trim() || undefined,
      name: name.trim(),
      shortName: shortName.trim(),
      logoUrl: logoUrl.trim() || undefined,
    }
    const result = await execute(() => {
      if (isEditing && bank) {
        return bankService.update(bank.id, { id: bank.id, ...payload, isActive })
      }
      return bankService.create(payload)
    })
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle className="flex items-center gap-2">
            {isEditing ? t('modal.bank.title.edit') : t('modal.bank.title.new')}
            {isSystem && <Badge variant="outline">{t('configuration.banks.systemBadge')}</Badge>}
          </ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('configuration.banks.field.compe')}</label>
              <Input
                value={compe}
                onChange={(e) => setCompe(e.target.value.replace(/[^0-9]/g, '').slice(0, 3))}
                disabled={isSystem}
                placeholder="237"
                required
              />
              <p className="text-xs text-muted-foreground">
                {isSystem ? t('modal.bank.field.compeSystemHint') : t('modal.bank.field.compeHint')}
              </p>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('configuration.banks.field.ispb')}</label>
              <Input
                value={ispb}
                onChange={(e) => setIspb(e.target.value.replace(/[^0-9]/g, '').slice(0, 8))}
                placeholder="60746948"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('configuration.banks.field.shortName')}</label>
              <Input value={shortName} onChange={(e) => setShortName(e.target.value)} placeholder="Bradesco" required />
            </div>
            <div className="space-y-2 md:col-span-3">
              <label className="text-sm font-medium">{t('common.field.name')}</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Banco Bradesco S.A." required />
            </div>
            <div className="space-y-2 md:col-span-3">
              <label className="text-sm font-medium">{t('configuration.banks.field.logoUrl')}</label>
              <Input value={logoUrl} onChange={(e) => setLogoUrl(e.target.value)} placeholder="https://..." />
              <p className="text-xs text-muted-foreground">{t('modal.bank.field.logoHint')}</p>
            </div>
            {logoUrl.trim() && (
              <div className="md:col-span-3">
                <div className="flex h-16 w-16 items-center justify-center overflow-hidden rounded border bg-muted/30">
                  <img src={logoUrl.trim()} alt={shortName || compe} className="h-full w-full object-contain p-1" />
                </div>
              </div>
            )}
          </div>
          {isEditing && (
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span>{t('common.status.active')}</span>
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
