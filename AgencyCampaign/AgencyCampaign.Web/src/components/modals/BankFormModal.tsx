import { useEffect, useMemo, useRef, useState } from 'react'
import { Button, Checkbox, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, useApi, useI18n } from 'archon-ui'
import { ImagePlus, Trash2 } from 'lucide-react'
import { bankService, resolveBankLogoUrl } from '../../services/bankService'
import type { Bank } from '../../types/bank'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  bank: Bank | null
  onSuccess: () => void
}

const ACCEPTED_TYPES = ['image/png', 'image/jpeg', 'image/jpg', 'image/webp']
const MAX_BYTES = 2 * 1024 * 1024

export default function BankFormModal({ open, onOpenChange, bank, onSuccess }: Props) {
  const { t } = useI18n()
  const isEditing = !!bank
  const isSystem = bank?.isSystem ?? false
  const [compe, setCompe] = useState('')
  const [ispb, setIspb] = useState('')
  const [name, setName] = useState('')
  const [shortName, setShortName] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [pendingLogo, setPendingLogo] = useState<File | null>(null)
  const [logoPreview, setLogoPreview] = useState<string | null>(null)
  const [logoError, setLogoError] = useState<string | null>(null)
  const [shouldRemoveLogo, setShouldRemoveLogo] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    if (bank) {
      setCompe(bank.compe)
      setIspb(bank.ispb ?? '')
      setName(bank.name)
      setShortName(bank.shortName)
      setIsActive(bank.isActive)
      setLogoPreview(bank.logoUrl ? resolveBankLogoUrl(bank.logoUrl) ?? null : null)
    } else {
      setCompe('')
      setIspb('')
      setName('')
      setShortName('')
      setIsActive(true)
      setLogoPreview(null)
    }
    setPendingLogo(null)
    setLogoError(null)
    setShouldRemoveLogo(false)
  }, [open, bank])

  const isValid = useMemo(() => {
    return /^[0-9]{3}$/.test(compe) && name.trim().length >= 2 && shortName.trim().length >= 2
  }, [compe, name, shortName])

  const handleSelectLogo = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    setLogoError(null)

    if (!file) return

    if (!ACCEPTED_TYPES.includes(file.type)) {
      setLogoError('Formato invalido. Use PNG, JPG ou WEBP.')
      return
    }

    if (file.size > MAX_BYTES) {
      setLogoError('Arquivo excede o limite de 2MB.')
      return
    }

    setPendingLogo(file)
    setShouldRemoveLogo(false)
    setLogoPreview(URL.createObjectURL(file))
  }

  const handleRemoveLogo = () => {
    setPendingLogo(null)
    setLogoPreview(null)
    setLogoError(null)
    setShouldRemoveLogo(!!bank?.logoUrl)
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    const payload = {
      compe: compe.trim(),
      ispb: ispb.trim() || undefined,
      name: name.trim(),
      shortName: shortName.trim(),
    }
    const result = await execute(async () => {
      const saved = isEditing && bank
        ? await bankService.update(bank.id, { id: bank.id, ...payload, isActive })
        : await bankService.create(payload)

      const savedBank = saved.data
      if (!savedBank) return saved

      if (pendingLogo) {
        return await bankService.uploadLogo(savedBank.id, pendingLogo)
      }

      if (shouldRemoveLogo && isEditing) {
        return await bankService.removeLogo(savedBank.id)
      }

      return saved
    })
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.bank.title.edit') : t('modal.bank.title.new')}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-6 items-start sm:grid-cols-[140px_1fr]">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('configuration.banks.field.logo')}</label>
              <div
                className="relative flex items-center justify-center overflow-hidden rounded-lg border border-dashed bg-muted/30"
                style={{ width: 140, height: 140 }}
              >
                {logoPreview ? (
                  <img src={logoPreview} alt={shortName || compe} className="h-full w-full object-contain p-2" />
                ) : (
                  <div className="flex flex-col items-center gap-1 text-xs text-muted-foreground">
                    <ImagePlus className="h-6 w-6" />
                    <span>{t('modal.bank.logo.empty')}</span>
                  </div>
                )}
              </div>
              <input
                ref={fileInputRef}
                type="file"
                accept={ACCEPTED_TYPES.join(',')}
                onChange={handleSelectLogo}
                className="hidden"
              />
              <div className="flex flex-col gap-1">
                <Button type="button" variant="outline" size="sm" onClick={() => fileInputRef.current?.click()}>
                  {logoPreview ? t('modal.bank.action.changeLogo') : t('modal.bank.action.uploadLogo')}
                </Button>
                {logoPreview && (
                  <Button type="button" variant="ghost" size="sm" onClick={handleRemoveLogo}>
                    <Trash2 className="mr-1 h-3 w-3" /> {t('common.action.remove')}
                  </Button>
                )}
              </div>
              <p className="text-xs text-muted-foreground">{t('modal.bank.field.logoHint')}</p>
              {logoError && <p className="text-xs text-destructive">{logoError}</p>}
            </div>

            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
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
              <div className="space-y-2 sm:col-span-2">
                <label className="text-sm font-medium">{t('configuration.banks.field.shortName')}</label>
                <Input value={shortName} onChange={(e) => setShortName(e.target.value)} placeholder="Bradesco" required />
              </div>
              <div className="space-y-2 sm:col-span-2">
                <label className="text-sm font-medium">{t('common.field.name')}</label>
                <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Banco Bradesco S.A." required />
              </div>
            </div>
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
