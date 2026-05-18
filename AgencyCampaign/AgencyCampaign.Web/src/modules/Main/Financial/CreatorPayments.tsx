import { useEffect, useMemo, useState } from 'react'
import {
  Badge,
  Button,
  ConfirmModal,
  DataTable,
  PageLayout,
  SearchableSelect,
  useApi,
  useI18n,
} from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Ban, Eye, Pencil, Plus, Receipt, Send, Signature } from 'lucide-react'
import { campaignService } from '../../services/campaignService'
import { creatorPaymentService } from '../../services/creatorPaymentService'
import type { Campaign } from '../../types/campaign'
import {
  PaymentStatus,
  paymentMethodLabels,
  paymentStatusLabels,
  type CreatorPayment,
  type PaymentStatusValue,
} from '../../types/creatorPayment'
import CreatorPaymentFormModal from '../../components/modals/CreatorPaymentFormModal'
import CreatorPaymentInvoiceModal from '../../components/modals/CreatorPaymentInvoiceModal'
import CreatorPaymentMarkPaidModal from '../../components/modals/CreatorPaymentMarkPaidModal'
import CreatorPaymentScheduleBatchModal from '../../components/modals/CreatorPaymentScheduleBatchModal'
import CreatorPaymentDetailsModal from '../../components/modals/CreatorPaymentDetailsModal'

const STATUS_VARIANTS: Record<PaymentStatusValue, 'default' | 'success' | 'warning' | 'destructive' | 'outline'> = {
  1: 'outline',
  2: 'warning',
  3: 'success',
  4: 'destructive',
  5: 'destructive',
}

export default function CreatorPaymentsPage() {
  const { t } = useI18n()
  const [campaigns, setCampaigns] = useState<Campaign[]>([])
  const [campaignId, setCampaignId] = useState<number | undefined>()
  const [statusFilter, setStatusFilter] = useState<PaymentStatusValue>(PaymentStatus.Pending)
  const [payments, setPayments] = useState<CreatorPayment[]>([])
  const [selected, setSelected] = useState<CreatorPayment[]>([])

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingPayment, setEditingPayment] = useState<CreatorPayment | null>(null)
  const [isInvoiceOpen, setIsInvoiceOpen] = useState(false)
  const [isMarkPaidOpen, setIsMarkPaidOpen] = useState(false)
  const [isBatchOpen, setIsBatchOpen] = useState(false)
  const [isDetailsOpen, setIsDetailsOpen] = useState(false)
  const [detailsPaymentId, setDetailsPaymentId] = useState<number | null>(null)
  const [isConfirmCancelOpen, setIsConfirmCancelOpen] = useState(false)

  const { execute: fetchPayments, loading } = useApi<CreatorPayment[]>({ showErrorMessage: true })
  const { execute: fetchCampaigns } = useApi<Campaign[]>({ showErrorMessage: true })
  const { execute: runCancel, loading: cancelling } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    void loadCampaigns()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    void loadPayments()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [statusFilter, campaignId])

  const loadCampaigns = async () => {
    const result = await fetchCampaigns(() => campaignService.getAll({ pageSize: 10 }))
    if (result) setCampaigns(result)
  }

  const loadPayments = async () => {
    const result = await fetchPayments(() => {
      if (campaignId) {
        return creatorPaymentService.getByCampaign(campaignId)
      }
      return creatorPaymentService.getByStatus(statusFilter)
    })
    if (result) {
      const filtered = campaignId ? result.filter((p) => p.status === statusFilter) : result
      setPayments(filtered)
      setSelected([])
    }
  }

  const handleCancel = async () => {
    if (selected.length !== 1) return
    const result = await runCancel(() => creatorPaymentService.cancel(selected[0].id))
    if (result !== null) {
      setIsConfirmCancelOpen(false)
      void loadPayments()
    }
  }

  const totals = useMemo(() => {
    const all = payments.reduce(
      (acc, p) => ({
        gross: acc.gross + (p.grossAmount || 0),
        net: acc.net + (p.netAmount || 0),
      }),
      { gross: 0, net: 0 },
    )
    const sel = selected.reduce(
      (acc, p) => ({
        gross: acc.gross + (p.grossAmount || 0),
        net: acc.net + (p.netAmount || 0),
      }),
      { gross: 0, net: 0 },
    )
    return { all, sel }
  }, [payments, selected])

  const columns: DataTableColumn<CreatorPayment>[] = [
    {
      key: 'creatorName',
      title: t('financial.creatorPayments.field.creator'),
      dataIndex: 'creatorName',
      render: (value: string | undefined) => value || '—',
    },
    {
      key: 'campaignName',
      title: t('common.field.campaign'),
      dataIndex: 'campaignName',
      render: (value: string | undefined) => (
        <span className="text-sm text-muted-foreground">{value ?? '—'}</span>
      ),
    },
    {
      key: 'netAmount',
      title: t('financial.creatorPayments.field.netAmount'),
      dataIndex: 'netAmount',
      width: 120,
      render: (value: number) => `R$ ${value.toFixed(2)}`,
    },
    {
      key: 'method',
      title: t('financial.creatorPayments.field.method'),
      dataIndex: 'method',
      width: 90,
      render: (value: number) => (
        <Badge variant="outline">{paymentMethodLabels[value as 1 | 2 | 3]}</Badge>
      ),
    },
    {
      key: 'status',
      title: t('common.field.status'),
      dataIndex: 'status',
      width: 110,
      render: (value: PaymentStatusValue) => (
        <Badge variant={STATUS_VARIANTS[value]}>{paymentStatusLabels[value]}</Badge>
      ),
    },
    {
      key: 'invoiceNumber',
      title: t('financial.creatorPayments.field.invoiceNumber'),
      dataIndex: 'invoiceNumber',
      width: 90,
      hiddenBelow: 'md',
      render: (value: string | undefined) =>
        value ? (
          <Badge variant="success">#{value}</Badge>
        ) : (
          <Badge variant="outline">{t('financial.creatorPayments.badge.noNF')}</Badge>
        ),
    },
    {
      key: 'scheduledFor',
      title: t('financial.creatorPayments.field.scheduledFor'),
      dataIndex: 'scheduledFor',
      width: 130,
      hiddenBelow: 'md',
      render: (value?: string) => (value ? new Date(value).toLocaleDateString('pt-BR') : '—'),
    },
    {
      key: 'paidAt',
      title: t('common.field.paidAt'),
      dataIndex: 'paidAt',
      width: 130,
      hiddenBelow: 'lg',
      render: (value?: string) => (value ? new Date(value).toLocaleDateString('pt-BR') : '—'),
    },
    {
      key: 'actions',
      title: '',
      width: 48,
      render: (_: unknown, record: CreatorPayment) => (
        <button
          className="inline-flex items-center justify-center rounded p-1 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
          onClick={() => {
            setDetailsPaymentId(record.id)
            setIsDetailsOpen(true)
          }}
          title={t('financial.creatorPayments.action.details')}
        >
          <Eye size={14} />
        </button>
      ),
    },
  ]

  const selectedPayment = selected.length === 1 ? selected[0] : null
  const canSchedule = selected.length > 0 && selected.every((p) => p.status === PaymentStatus.Pending || p.status === PaymentStatus.Failed)

  return (
    <>
      <PageLayout
        title={t('financial.creatorPayments.title')}
        subtitle={t('financial.creatorPayments.subtitle')}
        showDefaultActions={false}
      >
        <div className="flex flex-wrap items-end gap-3">
          <div className="space-y-1 min-w-[220px]">
            <label className="text-xs text-muted-foreground">{t('financial.creatorPayments.filter.status')}</label>
            <SearchableSelect
              value={String(statusFilter)}
              onValueChange={(value) => setStatusFilter(Number(value) as PaymentStatusValue)}
              options={Object.values(PaymentStatus).map((value) => ({
                value: String(value),
                label: paymentStatusLabels[value as PaymentStatusValue],
              }))}
              placeholder={t('financial.creatorPayments.placeholder.status')}
              searchPlaceholder={t('common.placeholder.search')}
            />
          </div>
          <div className="space-y-1 min-w-[260px]">
            <label className="text-xs text-muted-foreground">{t('financial.creatorPayments.filter.campaign')}</label>
            <SearchableSelect
              value={campaignId ? String(campaignId) : ''}
              onValueChange={(value) => setCampaignId(value ? Number(value) : undefined)}
              options={[{ value: '', label: t('financial.creatorPayments.placeholder.allCampaigns') }, ...campaigns.map((c) => ({ value: String(c.id), label: c.name }))]}
              placeholder={t('financial.creatorPayments.placeholder.allCampaigns')}
              searchPlaceholder={t('common.placeholder.search')}
              onSearch={async (term) => {
                const r = await campaignService.getAll({ search: term, pageSize: 10 })
                return (r.data ?? []).map((c) => ({ value: String(c.id), label: c.name }))
              }}
            />
          </div>
          <div className="flex flex-wrap gap-2 ml-auto">
            <Button size="sm" variant="outline" onClick={() => { setEditingPayment(selectedPayment); setIsFormOpen(true) }} disabled={!selectedPayment}>
              <Pencil size={14} className="mr-1" /> {t('common.action.edit')}
            </Button>
            <Button size="sm" variant="outline" onClick={() => setIsInvoiceOpen(true)} disabled={!selectedPayment}>
              <Receipt size={14} className="mr-1" /> {t('financial.creatorPayments.action.attachInvoice')}
            </Button>
            <Button size="sm" variant="outline" onClick={() => setIsMarkPaidOpen(true)} disabled={!selectedPayment}>
              <Signature size={14} className="mr-1" /> {t('financial.creatorPayments.action.markPaid')}
            </Button>
            <Button size="sm" variant="outline" onClick={() => setIsConfirmCancelOpen(true)} disabled={!selectedPayment || cancelling}>
              <Ban size={14} className="mr-1" /> {t('common.action.cancel')}
            </Button>
            <Button size="sm" variant="outline" onClick={() => setIsBatchOpen(true)} disabled={!canSchedule}>
              <Send size={14} className="mr-1" /> {t('financial.creatorPayments.action.scheduleBatch').replace('{0}', String(selected.length))}
            </Button>
            <Button size="sm" onClick={() => { setEditingPayment(null); setIsFormOpen(true) }}>
              <Plus size={14} className="mr-1" /> {t('financial.creatorPayments.action.new')}
            </Button>
          </div>
        </div>

        <div className="mt-3 grid grid-cols-2 gap-2 text-sm md:grid-cols-4">
          <Stat label={t('financial.creatorPayments.stat.payments')} value={payments.length.toString()} />
          <Stat label={t('financial.creatorPayments.stat.grossTotal')} value={`R$ ${totals.all.gross.toFixed(2)}`} />
          <Stat label={t('financial.creatorPayments.stat.netTotal')} value={`R$ ${totals.all.net.toFixed(2)}`} />
          <Stat
            label={t('financial.creatorPayments.stat.selected')}
            value={selected.length === 0 ? '—' : `${selected.length} · R$ ${totals.sel.net.toFixed(2)}`}
          />
        </div>

        <div className="mt-4">
          <DataTable
            columns={columns}
            data={payments}
            rowKey="id"
            selectedRows={selected}
            onSelectionChange={(rows) => setSelected(rows)}
            emptyText={loading ? t('common.loading') : t('financial.creatorPayments.empty')}
            loading={loading}
            pageSize={10}
            pageSizeOptions={[10, 20, 50, 100]}
          />
        </div>
      </PageLayout>

      <CreatorPaymentFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        payment={editingPayment}
        campaignId={campaignId}
        onSuccess={() => {
          setIsFormOpen(false)
          setEditingPayment(null)
          void loadPayments()
        }}
      />

      <CreatorPaymentInvoiceModal
        open={isInvoiceOpen}
        onOpenChange={setIsInvoiceOpen}
        payment={selectedPayment}
        onSuccess={() => {
          setIsInvoiceOpen(false)
          void loadPayments()
        }}
      />

      <CreatorPaymentMarkPaidModal
        open={isMarkPaidOpen}
        onOpenChange={setIsMarkPaidOpen}
        payment={selectedPayment}
        onSuccess={() => {
          setIsMarkPaidOpen(false)
          void loadPayments()
        }}
      />

      <CreatorPaymentScheduleBatchModal
        open={isBatchOpen}
        onOpenChange={setIsBatchOpen}
        payments={selected}
        onSuccess={() => {
          setIsBatchOpen(false)
          void loadPayments()
        }}
      />

      <CreatorPaymentDetailsModal
        open={isDetailsOpen}
        onOpenChange={setIsDetailsOpen}
        paymentId={detailsPaymentId}
      />

      <ConfirmModal
        open={isConfirmCancelOpen}
        onOpenChange={setIsConfirmCancelOpen}
        description={t('financial.creatorPayments.confirm.cancel').replace('{0}', selectedPayment?.creatorName ?? '')}
        variant="warning"
        onConfirm={() => void handleCancel()}
        loading={cancelling}
      />
    </>
  )
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border bg-primary/5 p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="text-sm font-semibold">{value}</p>
    </div>
  )
}
