import { useEffect, useState } from 'react'
import { Receipt, X } from 'lucide-react'
import { Button, Input, useApi, useI18n } from 'archon-ui'
import { creatorPortalService } from '../../services/creatorPortalService'
import { paymentMethodLabels, paymentStatusLabels, type CreatorPayment, type PaymentStatusValue } from '../../types/creatorPayment'
import { usePortalContext } from './hooks'

const STATUS_COLOR: Record<PaymentStatusValue, string> = {
  1: 'bg-muted text-muted-foreground',
  2: 'bg-amber-500/15 text-amber-600',
  3: 'bg-emerald-500/15 text-emerald-600',
  4: 'bg-destructive/15 text-destructive',
  5: 'bg-destructive/15 text-destructive',
}

export default function CreatorPortalPayments() {
  const { t } = useI18n()
  const { token } = usePortalContext()
  const [payments, setPayments] = useState<CreatorPayment[]>([])
  const [loading, setLoading] = useState(true)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [invoiceUrl, setInvoiceUrl] = useState('')
  const [issuedAt, setIssuedAt] = useState('')
  const { execute, loading: saving } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token])

  const load = async () => {
    setLoading(true)
    const res = await creatorPortalService.getPayments(token)
    setPayments(res)
    setLoading(false)
  }

  const startEdit = (p: CreatorPayment) => {
    setEditingId(p.id)
    setInvoiceNumber(p.invoiceNumber ?? '')
    setInvoiceUrl(p.invoiceUrl ?? '')
    setIssuedAt(p.invoiceIssuedAt ? p.invoiceIssuedAt.slice(0, 10) : new Date().toISOString().slice(0, 10))
  }

  const cancelEdit = () => {
    setEditingId(null)
    setInvoiceNumber('')
    setInvoiceUrl('')
    setIssuedAt('')
  }

  const submit = async () => {
    if (!editingId || !invoiceUrl.trim()) return
    const result = await execute(() =>
      creatorPortalService.uploadInvoice(token, {
        creatorPaymentId: editingId,
        invoiceNumber: invoiceNumber.trim() || undefined,
        invoiceUrl: invoiceUrl.trim(),
        issuedAt: issuedAt ? new Date(issuedAt).toISOString() : undefined,
      }),
    )
    if (result !== null) {
      cancelEdit()
      void load()
    }
  }

  if (loading) return <p className="text-sm text-muted-foreground">Carregando...</p>
  if (payments.length === 0) return <p className="text-sm text-muted-foreground">Nenhum pagamento ainda.</p>

  return (
    <div className="space-y-3">
      <h2 className="text-base font-semibold">{t('creatorPortal.payments.title')}</h2>
      {payments.map((p) => {
        const isEditing = editingId === p.id
        return (
          <div key={p.id} className="rounded-lg border bg-background p-3">
            <div className="flex items-start justify-between gap-2">
              <div>
                <p className="font-medium">R$ {p.netAmount.toFixed(2)}</p>
                <p className="text-xs text-muted-foreground">
                  {p.campaignName ?? 'Sem campanha'} · {paymentMethodLabels[p.method]}
                </p>
              </div>
              <span className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-medium ${STATUS_COLOR[p.status]}`}>
                {paymentStatusLabels[p.status]}
              </span>
            </div>
            {p.description && <p className="mt-1 text-xs text-muted-foreground">{p.description}</p>}
            <div className="mt-2 grid grid-cols-2 gap-2 text-xs">
              {p.scheduledFor && (
                <div>
                  <p className="text-[10px] uppercase tracking-wide text-muted-foreground">{t('creatorPortal.payments.field.scheduled')}</p>
                  <p>{new Date(p.scheduledFor).toLocaleDateString('pt-BR')}</p>
                </div>
              )}
              {p.paidAt && (
                <div>
                  <p className="text-[10px] uppercase tracking-wide text-muted-foreground">{t('creatorPortal.payments.field.paidAt')}</p>
                  <p>{new Date(p.paidAt).toLocaleDateString('pt-BR')}</p>
                </div>
              )}
            </div>

            <div className="mt-3 border-t pt-2">
              {p.invoiceUrl ? (
                <div className="text-xs">
                  <span className="rounded bg-emerald-500/15 px-2 py-0.5 text-emerald-600">
                    NF anexada{p.invoiceNumber ? ` #${p.invoiceNumber}` : ''}
                  </span>
                  <a
                    href={p.invoiceUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="ml-2 text-primary hover:underline"
                  >
                    Abrir
                  </a>
                </div>
              ) : isEditing ? (
                <div className="space-y-2">
                  <div className="grid grid-cols-2 gap-2">
                    <Input value={invoiceNumber} onChange={(e) => setInvoiceNumber(e.target.value)} placeholder={t('creatorPortal.payments.field.nfNumber')} />
                    <Input type="date" value={issuedAt} onChange={(e) => setIssuedAt(e.target.value)} />
                  </div>
                  <Input value={invoiceUrl} onChange={(e) => setInvoiceUrl(e.target.value)} placeholder={t('creatorPortal.payments.field.nfUrl')} />
                  <div className="flex gap-2">
                    <Button size="sm" onClick={() => void submit()} disabled={saving || !invoiceUrl.trim()}>
                      {saving ? 'Salvando...' : 'Anexar'}
                    </Button>
                    <Button size="sm" variant="outline" onClick={cancelEdit}>
                      <X size={14} />
                    </Button>
                  </div>
                </div>
              ) : (
                <Button size="sm" variant="outline" onClick={() => startEdit(p)}>
                  <Receipt size={14} className="mr-1" /> Anexar NF
                </Button>
              )}
            </div>
          </div>
        )
      })}
    </div>
  )
}
