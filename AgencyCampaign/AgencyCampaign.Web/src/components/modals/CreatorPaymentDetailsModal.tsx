import { useEffect, useState } from 'react'
import {
  Badge,
  Button,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  useApi,
  useI18n,
} from 'archon-ui'
import { ExternalLink } from 'lucide-react'
import { creatorPaymentService } from '../../services/creatorPaymentService'
import {
  creatorPaymentEventTypeLabels,
  paymentMethodLabels,
  paymentStatusLabels,
  pixKeyTypeLabels,
  type CreatorPayment,
  type PaymentStatusValue,
} from '../../types/creatorPayment'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  paymentId: number | null
}

const STATUS_VARIANTS: Record<PaymentStatusValue, 'default' | 'success' | 'warning' | 'destructive' | 'outline'> = {
  1: 'outline',
  2: 'warning',
  3: 'success',
  4: 'destructive',
  5: 'destructive',
}

export default function CreatorPaymentDetailsModal({ open, onOpenChange, paymentId }: Props) {
  const { t } = useI18n()
  const [payment, setPayment] = useState<CreatorPayment | null>(null)
  const { execute, loading } = useApi<CreatorPayment | null>({ showErrorMessage: true })

  useEffect(() => {
    if (!open || !paymentId) {
      setPayment(null)
      return
    }
    void execute(() => creatorPaymentService.getById(paymentId)).then((result) => {
      if (result) setPayment(result)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, paymentId])

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '880px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{t('modal.creatorPayment.title.details')}</ModalTitle>
        </ModalHeader>

        {loading && <p className="text-sm text-muted-foreground">{t('common.loading')}</p>}

        {payment && (
          <div className="space-y-4">
            <div className="rounded-lg border bg-primary/5 p-4">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h3 className="text-base font-semibold">{payment.creatorName}</h3>
                  <p className="text-sm text-muted-foreground">
                    {payment.campaignName ?? 'Sem campanha'}
                    {payment.description ? ` · ${payment.description}` : ''}
                  </p>
                </div>
                <Badge variant={STATUS_VARIANTS[payment.status]}>
                  {paymentStatusLabels[payment.status]}
                </Badge>
              </div>
              <div className="mt-3 grid grid-cols-2 gap-2 text-sm md:grid-cols-4">
                <div>
                  <p className="text-xs text-muted-foreground">Valor bruto</p>
                  <p className="font-semibold">R$ {payment.grossAmount.toFixed(2)}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Descontos</p>
                  <p className="font-semibold">R$ {payment.discounts.toFixed(2)}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Líquido</p>
                  <p className="font-semibold">R$ {payment.netAmount.toFixed(2)}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Método</p>
                  <p className="font-semibold">{paymentMethodLabels[payment.method]}</p>
                </div>
              </div>
              {payment.failureReason && (
                <p className="mt-3 rounded bg-destructive/10 p-2 text-xs text-destructive">
                  Falha: {payment.failureReason}
                </p>
              )}
            </div>

            <Tabs defaultValue="info" className="pt-2">
              <TabsList className="mb-4 h-auto w-full justify-start gap-6 rounded-none border-b border-border bg-transparent p-0">
                <TabsTrigger value="info" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">{t('modal.creatorPayment.tab.data')}</TabsTrigger>
                <TabsTrigger value="invoice" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">{t('modal.creatorPayment.tab.invoice')}</TabsTrigger>
                <TabsTrigger value="events" className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none">
                  {t('modal.creatorPayment.tab.history')}
                  {payment.events.length > 0 && (
                    <span className="ml-0.5 text-[10px] bg-muted text-muted-foreground rounded-full px-1.5 py-0.5 font-medium group-data-[state=active]:bg-primary/15 group-data-[state=active]:text-primary">{payment.events.length}</span>
                  )}
                </TabsTrigger>
              </TabsList>

              <TabsContent value="info" className="mt-0 grid grid-cols-1 gap-3 md:grid-cols-2">
                <Field label="Chave PIX (snapshot)" value={payment.pixKey ?? payment.creatorPixKey ?? '—'} />
                <Field
                  label="Tipo PIX"
                  value={
                    payment.pixKeyType
                      ? pixKeyTypeLabels[payment.pixKeyType]
                      : payment.creatorPixKeyType
                        ? pixKeyTypeLabels[payment.creatorPixKeyType]
                        : '—'
                  }
                />
                <Field label="Provider" value={payment.provider ?? '—'} />
                <Field label="ID transação" value={payment.providerTransactionId ?? '—'} />
                <Field
                  label="Agendado para"
                  value={payment.scheduledFor ? new Date(payment.scheduledFor).toLocaleString('pt-BR') : '—'}
                />
                <Field
                  label="Pago em"
                  value={payment.paidAt ? new Date(payment.paidAt).toLocaleString('pt-BR') : '—'}
                />
              </TabsContent>

              <TabsContent value="invoice" className="mt-0 space-y-2">
                <Field label="Número" value={payment.invoiceNumber ?? '—'} />
                <Field
                  label="Emitida em"
                  value={payment.invoiceIssuedAt ? new Date(payment.invoiceIssuedAt).toLocaleDateString('pt-BR') : '—'}
                />
                {payment.invoiceUrl ? (
                  <a
                    href={payment.invoiceUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex items-center gap-1 text-sm text-primary hover:underline"
                  >
                    <ExternalLink className="h-4 w-4" /> Abrir PDF da NF
                  </a>
                ) : (
                  <p className="text-sm text-muted-foreground">Nenhuma URL de NF anexada.</p>
                )}
              </TabsContent>

              <TabsContent value="events" className="mt-0">
                {payment.events.length === 0 ? (
                  <p className="text-sm text-muted-foreground">{t('modal.creatorPayment.noEvents')}</p>
                ) : (
                  <div className="relative space-y-3 border-l-2 border-primary/15 pl-4">
                    {payment.events.map((event) => (
                      <div key={event.id} className="relative">
                        <div className="absolute -left-[1.4rem] top-1 h-3 w-3 rounded-full bg-primary/15" />
                        <div className="flex items-baseline justify-between gap-3">
                          <span className="text-sm font-medium">
                            {creatorPaymentEventTypeLabels[event.eventType]}
                          </span>
                          <span className="text-xs text-muted-foreground">
                            {new Date(event.occurredAt).toLocaleString('pt-BR')}
                          </span>
                        </div>
                        {event.description && (
                          <p className="text-xs text-muted-foreground">{event.description}</p>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </TabsContent>
            </Tabs>
          </div>
        )}

        <ModalFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t('common.action.close')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="text-sm font-medium">{value}</p>
    </div>
  )
}
