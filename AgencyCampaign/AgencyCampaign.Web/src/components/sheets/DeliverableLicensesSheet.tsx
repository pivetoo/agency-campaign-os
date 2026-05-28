import { useEffect, useState } from 'react'
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription, Badge, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, SearchableSelect, useApi, useI18n } from 'archon-ui'
import { ScrollText, Plus, Pencil, Trash2, CopyPlus } from 'lucide-react'
import { contentLicenseService } from '../../services/contentLicenseService'
import { campaignDocumentService } from '../../services/campaignDocumentService'
import type { ContentLicense, ContentLicenseInput, ContentLicenseType, ContentLicenseStatus } from '../../types/contentLicense'
import type { CampaignDocument } from '../../types/campaignDocument'
import { formatCurrency, formatDate } from '../../lib/format'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  deliverableId: number | null
  campaignId: number
}

const NO_CONTRACT = '0'

function typeLabel(type: ContentLicenseType, t: (k: string) => string): string {
  if (type === 1) return t('contentLicense.type.ugcReuse')
  if (type === 2) return t('contentLicense.type.paidWhitelisting')
  if (type === 3) return t('contentLicense.type.exclusivity')
  return t('contentLicense.type.other')
}

function statusVariant(status: ContentLicenseStatus): 'success' | 'warning' | 'destructive' {
  if (status === 1) return 'success'
  if (status === 2) return 'warning'
  return 'destructive'
}

function statusLabel(status: ContentLicenseStatus, t: (k: string) => string): string {
  if (status === 1) return t('contentLicense.status.active')
  if (status === 2) return t('contentLicense.status.expiringSoon')
  return t('contentLicense.status.expired')
}

interface FormState {
  type: ContentLicenseType
  channels: string
  startsAt: string
  expiresAt: string
  value: string
  notes: string
  documentId: string
}

const emptyForm: FormState = { type: 1, channels: '', startsAt: '', expiresAt: '', value: '', notes: '', documentId: NO_CONTRACT }

export default function DeliverableLicensesSheet({ open, onOpenChange, deliverableId, campaignId }: Props) {
  const { t } = useI18n()
  const [licenses, setLicenses] = useState<ContentLicense[]>([])
  const [documents, setDocuments] = useState<CampaignDocument[]>([])
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)

  const { execute: fetchLicenses, loading: loadingLicenses } = useApi<ContentLicense[]>({ showErrorMessage: true })
  const { execute: runSave, loading: saving } = useApi({ showErrorMessage: true, showSuccessMessage: true })
  const { execute: runDelete } = useApi({ showErrorMessage: true, showSuccessMessage: true })
  const { execute: runApply, loading: applying } = useApi({ showErrorMessage: true, showSuccessMessage: true })

  useEffect(() => {
    if (open && deliverableId) {
      void loadLicenses()
      if (campaignId > 0) {
        void campaignDocumentService.getByCampaign(campaignId).then((docs) => setDocuments(docs)).catch(() => setDocuments([]))
      }
    }
    if (!open) {
      setLicenses([])
      resetForm()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, deliverableId])

  async function loadLicenses() {
    if (!deliverableId) return
    const result = await fetchLicenses(() => contentLicenseService.getByDeliverable(deliverableId))
    setLicenses(result ?? [])
  }

  function resetForm() {
    setFormOpen(false)
    setEditingId(null)
    setForm(emptyForm)
  }

  function openAdd() {
    setEditingId(null)
    setForm(emptyForm)
    setFormOpen(true)
  }

  function openEdit(license: ContentLicense) {
    setEditingId(license.id)
    setForm({
      type: license.type,
      channels: license.channels ?? '',
      startsAt: license.startsAt?.slice(0, 10) ?? '',
      expiresAt: license.expiresAt?.slice(0, 10) ?? '',
      value: license.value != null ? String(license.value) : '',
      notes: license.notes ?? '',
      documentId: license.campaignDocumentId ? String(license.campaignDocumentId) : NO_CONTRACT,
    })
    setFormOpen(true)
  }

  function buildInput(): ContentLicenseInput {
    return {
      type: form.type,
      channels: form.channels.trim() || null,
      startsAt: form.startsAt || null,
      expiresAt: form.expiresAt || null,
      value: form.value.trim() === '' ? null : Number(form.value),
      notes: form.notes.trim() || null,
      campaignDocumentId: form.documentId !== NO_CONTRACT ? Number(form.documentId) : null,
    }
  }

  async function handleSave() {
    if (!deliverableId) return
    const input = buildInput()
    const result = await runSave(() => (editingId ? contentLicenseService.update(editingId, input) : contentLicenseService.add(deliverableId, input)))
    if (result !== null) {
      resetForm()
      void loadLicenses()
    }
  }

  async function handleDelete(license: ContentLicense) {
    if (!window.confirm(t('contentLicense.confirm.delete'))) return
    const result = await runDelete(() => contentLicenseService.remove(license.id))
    if (result !== null) {
      void loadLicenses()
    }
  }

  async function handleApply(license: ContentLicense) {
    if (!window.confirm(t('contentLicense.confirm.apply'))) return
    await runApply(() => contentLicenseService.applyToCampaign(license.id))
  }

  const documentOptions = [{ value: NO_CONTRACT, label: t('contentLicense.field.noContract') }, ...documents.map((d) => ({ value: String(d.id), label: d.title }))]

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full sm:max-w-xl flex flex-col gap-0 p-0">
        <SheetHeader className="px-6 pt-6 pb-4 border-b">
          <div className="flex items-center gap-2">
            <ScrollText size={18} className="text-muted-foreground" />
            <SheetTitle>{t('contentLicense.title')}</SheetTitle>
          </div>
          <SheetDescription className="sr-only">{t('contentLicense.title')}</SheetDescription>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto">
          {loadingLicenses && licenses.length === 0 ? (
            <div className="flex items-center justify-center py-16 text-muted-foreground text-sm">{t('common.loading')}</div>
          ) : licenses.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 gap-2 text-muted-foreground">
              <ScrollText size={36} />
              <p className="text-sm">{t('contentLicense.empty')}</p>
            </div>
          ) : (
            <ul className="divide-y">
              {licenses.map((license) => (
                <li key={license.id} className="px-6 py-4 space-y-2">
                  <div className="flex items-center justify-between gap-2 flex-wrap">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-sm font-semibold">{typeLabel(license.type, t)}</span>
                      <Badge variant={statusVariant(license.status)}>{statusLabel(license.status, t)}</Badge>
                    </div>
                    <div className="flex items-center gap-1">
                      <button type="button" className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors" title={t('contentLicense.action.applyToCampaign')} disabled={applying} onClick={() => void handleApply(license)}>
                        <CopyPlus size={14} />
                      </button>
                      <button type="button" className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-foreground hover:bg-accent transition-colors" title={t('common.action.edit')} onClick={() => openEdit(license)}>
                        <Pencil size={14} />
                      </button>
                      <button type="button" className="inline-flex items-center justify-center p-1 rounded text-muted-foreground hover:text-destructive hover:bg-accent transition-colors" title={t('common.action.delete')} onClick={() => void handleDelete(license)}>
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </div>
                  <div className="text-xs text-muted-foreground space-y-0.5">
                    {license.channels && <p>{t('contentLicense.field.channels')}: {license.channels}</p>}
                    <p>
                      {license.expiresAt ? t('contentLicense.expiresOn').replace('{0}', formatDate(license.expiresAt)) : t('contentLicense.noExpiry')}
                      {license.daysUntilExpiry != null && license.status !== 3 ? ` (${t('contentLicense.daysLeft').replace('{0}', String(license.daysUntilExpiry))})` : ''}
                    </p>
                    {license.value != null && <p>{t('contentLicense.field.value')}: {formatCurrency(license.value)}</p>}
                    {license.notes && <p className="text-foreground/80">{license.notes}</p>}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="border-t px-6 py-4 bg-background space-y-3">
          {!formOpen ? (
            <Button size="sm" className="w-full" onClick={openAdd}>
              <Plus size={14} className="mr-1.5" />
              {t('contentLicense.action.add')}
            </Button>
          ) : (
            <div className="space-y-2 rounded border bg-muted/20 px-4 py-3">
              <p className="text-xs font-medium">{editingId ? t('contentLicense.action.edit') : t('contentLicense.action.add')}</p>

              <div className="space-y-1">
                <label className="text-xs text-muted-foreground">{t('contentLicense.field.type')}</label>
                <Select value={String(form.type)} onValueChange={(v) => setForm((p) => ({ ...p, type: Number(v) as ContentLicenseType }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="1">{t('contentLicense.type.ugcReuse')}</SelectItem>
                    <SelectItem value="2">{t('contentLicense.type.paidWhitelisting')}</SelectItem>
                    <SelectItem value="3">{t('contentLicense.type.exclusivity')}</SelectItem>
                    <SelectItem value="4">{t('contentLicense.type.other')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1">
                <label className="text-xs text-muted-foreground">{t('contentLicense.field.channels')}</label>
                <Input className="h-8 text-xs" value={form.channels} placeholder={t('contentLicense.field.channelsPlaceholder')} onChange={(e) => setForm((p) => ({ ...p, channels: e.target.value }))} />
              </div>

              <div className="grid grid-cols-2 gap-2">
                <div className="space-y-1">
                  <label className="text-xs text-muted-foreground">{t('contentLicense.field.startsAt')}</label>
                  <Input type="date" className="h-8 text-xs" value={form.startsAt} onChange={(e) => setForm((p) => ({ ...p, startsAt: e.target.value }))} />
                </div>
                <div className="space-y-1">
                  <label className="text-xs text-muted-foreground">{t('contentLicense.field.expiresAt')}</label>
                  <Input type="date" className="h-8 text-xs" value={form.expiresAt} onChange={(e) => setForm((p) => ({ ...p, expiresAt: e.target.value }))} />
                </div>
              </div>

              <div className="space-y-1">
                <label className="text-xs text-muted-foreground">{t('contentLicense.field.value')}</label>
                <Input type="number" min={0} className="h-8 text-xs" value={form.value} onChange={(e) => setForm((p) => ({ ...p, value: e.target.value }))} />
              </div>

              <div className="space-y-1">
                <label className="text-xs text-muted-foreground">{t('contentLicense.field.contract')}</label>
                <SearchableSelect value={form.documentId} onValueChange={(v) => setForm((p) => ({ ...p, documentId: v }))} options={documentOptions} placeholder={t('common.placeholder.select')} searchPlaceholder={t('common.placeholder.search')} />
              </div>

              <div className="space-y-1">
                <label className="text-xs text-muted-foreground">{t('common.field.notes')}</label>
                <textarea className="w-full rounded border bg-background text-xs px-3 py-2 resize-none focus:outline-none focus:ring-2 focus:ring-primary/30" rows={2} value={form.notes} onChange={(e) => setForm((p) => ({ ...p, notes: e.target.value }))} />
              </div>

              <div className="flex gap-2 justify-end">
                <Button size="sm" variant="outline" onClick={resetForm}>{t('common.action.cancel')}</Button>
                <Button size="sm" disabled={saving} onClick={() => void handleSave()}>{saving ? t('common.action.saving') : t('common.action.save')}</Button>
              </div>
            </div>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}
