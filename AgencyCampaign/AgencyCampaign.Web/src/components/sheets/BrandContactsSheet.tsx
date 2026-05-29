import { useEffect, useState } from 'react'
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription, Badge, Button, Input, useApi, useI18n } from 'archon-ui'
import { Mail, Phone, Plus, Pencil, Trash2, Star, Contact } from 'lucide-react'
import { brandContactService } from '../../services/brandContactService'
import type { BrandContact, BrandContactType } from '../../types/brandContact'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  brandId: number | null
  brandName?: string
  onChanged?: () => void
}

export default function BrandContactsSheet({ open, onOpenChange, brandId, brandName, onChanged }: Props) {
  const { t } = useI18n()
  const [contacts, setContacts] = useState<BrandContact[]>([])
  const [formType, setFormType] = useState<BrandContactType | null>(null)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [value, setValue] = useState('')
  const [label, setLabel] = useState('')

  const { execute: fetchContacts, loading } = useApi<BrandContact[]>({ showErrorMessage: true })
  const { execute: runSave, loading: saving } = useApi({ showErrorMessage: true, showSuccessMessage: true })
  const { execute: runMutate } = useApi({ showErrorMessage: true, showSuccessMessage: true })

  useEffect(() => {
    if (open && brandId) {
      void load()
    }
    if (!open) {
      resetForm()
      setContacts([])
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, brandId])

  async function load() {
    if (!brandId) return
    const result = await fetchContacts(() => brandContactService.getByBrand(brandId))
    setContacts(result ?? [])
  }

  function resetForm() {
    setFormType(null)
    setEditingId(null)
    setValue('')
    setLabel('')
  }

  function openAdd(type: BrandContactType) {
    setEditingId(null)
    setValue('')
    setLabel('')
    setFormType(type)
  }

  function openEdit(contact: BrandContact) {
    setEditingId(contact.id)
    setValue(contact.value)
    setLabel(contact.label ?? '')
    setFormType(contact.type)
  }

  async function handleSave() {
    if (!brandId || !value.trim() || formType === null) return
    const result = await runSave(() => (editingId
      ? brandContactService.update(editingId, { value: value.trim(), label: label.trim() || null })
      : brandContactService.add(brandId, { type: formType, value: value.trim(), label: label.trim() || null })))
    if (result !== null) {
      resetForm()
      await load()
      onChanged?.()
    }
  }

  async function handleRemove(contact: BrandContact) {
    if (!window.confirm(t('brandContacts.confirm.delete'))) return
    const result = await runMutate(() => brandContactService.remove(contact.id))
    if (result !== null) {
      await load()
      onChanged?.()
    }
  }

  async function handleSetPrimary(contact: BrandContact) {
    const result = await runMutate(() => brandContactService.setPrimary(contact.id))
    if (result !== null) {
      await load()
      onChanged?.()
    }
  }

  const emails = contacts.filter((item) => item.type === 1)
  const phones = contacts.filter((item) => item.type === 2)

  function section(type: BrandContactType, title: string, headerIcon: React.ReactNode, placeholder: string, items: BrandContact[]) {
    return (
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <p className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">{headerIcon} {title}</p>
          <Button size="sm" variant="outline" onClick={() => openAdd(type)}>
            <Plus size={13} className="mr-1" /> {t('common.action.add')}
          </Button>
        </div>
        {items.length === 0 ? (
          <p className="text-xs text-muted-foreground italic">{t('brandContacts.empty')}</p>
        ) : (
          <ul className="space-y-1.5">
            {items.map((item) => (
              <li key={item.id} className="flex items-center justify-between gap-2 rounded border border-border/70 bg-background px-3 py-2">
                <div className="min-w-0">
                  <p className="flex items-center gap-2 truncate text-sm text-foreground">
                    {item.value}
                    {item.isPrimary && <Badge variant="success">{t('brandContacts.primary')}</Badge>}
                  </p>
                  {item.label && <p className="truncate text-xs text-muted-foreground">{item.label}</p>}
                </div>
                <div className="flex shrink-0 items-center gap-1">
                  {!item.isPrimary && (
                    <button type="button" title={t('brandContacts.setPrimary')} className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-amber-500 hover:bg-accent transition-colors" onClick={() => void handleSetPrimary(item)}>
                      <Star size={14} />
                    </button>
                  )}
                  <button type="button" title={t('common.action.edit')} className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors" onClick={() => openEdit(item)}>
                    <Pencil size={14} />
                  </button>
                  <button type="button" title={t('common.action.delete')} className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-destructive hover:bg-accent transition-colors" onClick={() => void handleRemove(item)}>
                    <Trash2 size={14} />
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
        {formType === type && (
          <div className="space-y-2 rounded border bg-muted/20 px-3 py-2.5">
            <Input className="h-8 text-sm" placeholder={placeholder} value={value} onChange={(event) => setValue(event.target.value)} />
            <Input className="h-8 text-sm" placeholder={t('brandContacts.field.label')} value={label} onChange={(event) => setLabel(event.target.value)} />
            <div className="flex justify-end gap-2">
              <Button size="sm" variant="outline" onClick={resetForm}>{t('common.action.cancel')}</Button>
              <Button size="sm" disabled={saving || !value.trim()} onClick={() => void handleSave()}>{saving ? t('common.action.saving') : t('common.action.save')}</Button>
            </div>
          </div>
        )}
      </div>
    )
  }

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full sm:max-w-lg flex flex-col gap-0 p-0">
        <SheetHeader className="px-6 pt-6 pb-4 border-b">
          <div className="flex items-center gap-2">
            <Contact size={18} className="text-muted-foreground" />
            <SheetTitle>{t('brandContacts.title')}</SheetTitle>
          </div>
          <SheetDescription className="sr-only">{brandName ?? t('brandContacts.title')}</SheetDescription>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto px-6 py-5 space-y-6">
          {brandName && <p className="text-sm font-medium text-foreground">{brandName}</p>}
          {loading && contacts.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('common.loading')}</p>
          ) : (
            <>
              {section(1, t('brandContacts.emails'), <Mail size={13} />, t('brandContacts.field.email'), emails)}
              {section(2, t('brandContacts.phones'), <Phone size={13} />, t('brandContacts.field.phone'), phones)}
            </>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}
