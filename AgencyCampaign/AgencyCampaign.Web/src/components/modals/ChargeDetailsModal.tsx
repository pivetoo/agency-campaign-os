import { useState } from 'react'
import { Button, Modal, ModalContent, ModalHeader, ModalTitle, Badge, useI18n } from 'archon-ui'
import { Copy, Check, ExternalLink } from 'lucide-react'
import type { FinancialEntry } from '../../types/financialEntry'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  entry: FinancialEntry | null
}

function CopyField({ label, value }: { label: string; value: string }) {
  const { t } = useI18n()
  const [copied, setCopied] = useState(false)
  const copy = async () => {
    try {
      await navigator.clipboard.writeText(value)
      setCopied(true)
      setTimeout(() => setCopied(false), 1800)
    } catch {
      // clipboard indisponível — ignora
    }
  }
  return (
    <div className="space-y-1">
      <span className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">{label}</span>
      <div className="flex items-center gap-2">
        <code className="min-w-0 flex-1 truncate rounded-md border border-border/60 bg-muted/30 px-2 py-1.5 font-mono text-xs">{value}</code>
        <Button size="sm" variant="outline" onClick={() => void copy()} title={t('financial.charge.copy')}>
          {copied ? <Check size={14} className="text-success" /> : <Copy size={14} />}
        </Button>
      </div>
    </div>
  )
}

export default function ChargeDetailsModal({ open, onOpenChange, entry }: Props) {
  const { t } = useI18n()
  if (!entry) return null

  const chargeStatusLabel: Record<number, string> = {
    1: t('financial.charge.status.requested'),
    2: t('financial.charge.status.issued'),
    3: t('financial.charge.status.paid'),
    4: t('financial.charge.status.failed'),
    5: t('financial.charge.status.cancelled'),
  }
  const status = entry.chargeStatus ?? 0
  const boletoUrl = entry.chargeBankSlipUrl || entry.chargeUrl
  const hasAny = entry.chargeDigitableLine || entry.chargeBarCode || entry.chargeNossoNumero || entry.chargePixCopyPaste || entry.chargePixQrCodeUrl || boletoUrl

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent className="sm:max-w-lg">
        <ModalHeader>
          <ModalTitle className="flex items-center gap-2">
            {t('financial.charge.title')}
            {status > 0 && (
              <Badge variant={status === 3 ? 'success' : status === 4 || status === 5 ? 'destructive' : 'warning'}>
                {chargeStatusLabel[status] ?? '-'}
              </Badge>
            )}
          </ModalTitle>
        </ModalHeader>

        <div className="space-y-4 px-1 py-2">
          {!hasAny && <p className="text-sm text-muted-foreground">{t('financial.charge.empty')}</p>}

          {entry.chargeDigitableLine && <CopyField label={t('financial.charge.digitableLine')} value={entry.chargeDigitableLine} />}
          {entry.chargeBarCode && <CopyField label={t('financial.charge.barCode')} value={entry.chargeBarCode} />}
          {entry.chargeNossoNumero && <CopyField label={t('financial.charge.nossoNumero')} value={entry.chargeNossoNumero} />}
          {entry.chargePixCopyPaste && <CopyField label={t('financial.charge.pixCopyPaste')} value={entry.chargePixCopyPaste} />}
          {entry.chargeTxId && <CopyField label={t('financial.charge.txid')} value={entry.chargeTxId} />}

          {entry.chargePixQrCodeUrl && (
            <div className="space-y-1">
              <span className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">QR Code PIX</span>
              <div className="flex justify-center rounded-md border border-border/60 bg-white p-3">
                <img src={entry.chargePixQrCodeUrl} alt="QR Code PIX" className="h-44 w-44 object-contain" />
              </div>
            </div>
          )}

          {boletoUrl && (
            <a
              href={boletoUrl}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-1.5 text-sm font-medium text-primary hover:underline"
            >
              <ExternalLink size={14} />
              {t('financial.charge.openBoleto')}
            </a>
          )}
        </div>
      </ModalContent>
    </Modal>
  )
}
