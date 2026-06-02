import { useEffect, useState } from 'react'
import { Button, Card, CardContent, ConfirmModal, Input, useI18n } from 'archon-ui'
import { Pencil, Plus, Trash2 } from 'lucide-react'
import { rateCardItemService, type RateCardItem } from '../../../../services/rateCardItemService'
import { formatCurrency } from '../../../../lib/format'

export default function RateCardTab({ creatorId }: { creatorId: number }) {
  const { t } = useI18n()
  const [items, setItems] = useState<RateCardItem[]>([])
  const [label, setLabel] = useState('')
  const [price, setPrice] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [saving, setSaving] = useState(false)
  const [confirmDeleteId, setConfirmDeleteId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState(false)

  const load = async () => {
    setItems(await rateCardItemService.getByCreator(creatorId))
  }

  useEffect(() => {
    if (creatorId) {
      void load()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [creatorId])

  const reset = () => {
    setLabel('')
    setPrice('')
    setEditingId(null)
  }

  const save = async () => {
    if (!label.trim()) {
      return
    }
    setSaving(true)
    try {
      const unitPrice = Number(price) || 0
      if (editingId) {
        const existing = items.find((item) => item.id === editingId)
        await rateCardItemService.update(editingId, { id: editingId, label: label.trim(), unitPrice, displayOrder: existing?.displayOrder ?? 0, isActive: true })
      } else {
        await rateCardItemService.create({ creatorId, label: label.trim(), unitPrice })
      }
      reset()
      await load()
    } finally {
      setSaving(false)
    }
  }

  const startEdit = (item: RateCardItem) => {
    setEditingId(item.id)
    setLabel(item.label)
    setPrice(String(item.unitPrice))
  }

  const remove = (id: number) => {
    setConfirmDeleteId(id)
  }

  const confirmRemove = async () => {
    if (confirmDeleteId == null) {
      return
    }
    setDeleting(true)
    try {
      await rateCardItemService.delete(confirmDeleteId)
      await load()
      setConfirmDeleteId(null)
    } finally {
      setDeleting(false)
    }
  }

  return (
    <>
      <Card>
      <CardContent className="space-y-4 pt-4">
        <p className="text-xs text-muted-foreground">{t('rateCard.hint')}</p>
        <div className="flex flex-wrap items-end gap-2">
          <div className="min-w-[180px] flex-1">
            <label className="text-xs font-medium text-muted-foreground">{t('rateCard.label')}</label>
            <Input value={label} onChange={(e) => setLabel(e.target.value)} placeholder={t('rateCard.labelPlaceholder')} />
          </div>
          <div className="w-40">
            <label className="text-xs font-medium text-muted-foreground">{t('rateCard.price')}</label>
            <Input type="number" min={0} value={price} onChange={(e) => setPrice(e.target.value)} placeholder="0,00" />
          </div>
          <Button size="sm" onClick={() => void save()} disabled={saving || !label.trim()}>
            <Plus size={14} className="mr-1.5" /> {editingId ? t('common.action.save') : t('common.action.add')}
          </Button>
          {editingId && <Button size="sm" variant="outline" onClick={reset}>{t('common.action.cancel')}</Button>}
        </div>

        {items.length === 0 ? (
          <p className="text-sm text-muted-foreground">{t('rateCard.empty')}</p>
        ) : (
          <div className="divide-y divide-border/60 rounded-lg border border-border">
            {items.map((item) => (
              <div key={item.id} className="flex items-center justify-between gap-3 px-3 py-2">
                <span className="text-sm font-medium text-foreground">{item.label}</span>
                <div className="flex items-center gap-3">
                  <span className="font-mono text-sm text-foreground">{formatCurrency(item.unitPrice)}</span>
                  <button type="button" aria-label={t('common.action.edit')} title={t('common.action.edit')} onClick={() => startEdit(item)} className="text-muted-foreground transition-colors hover:text-foreground"><Pencil size={14} /></button>
                  <button type="button" aria-label={t('common.action.delete')} title={t('common.action.delete')} onClick={() => remove(item.id)} className="text-muted-foreground transition-colors hover:text-destructive"><Trash2 size={14} /></button>
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>
      </Card>

      <ConfirmModal
        open={confirmDeleteId != null}
        onOpenChange={(open) => { if (!open) { setConfirmDeleteId(null) } }}
        description={t('rateCard.deleteConfirm')}
        variant="danger"
        onConfirm={() => void confirmRemove()}
        loading={deleting}
      />
    </>
  )
}
