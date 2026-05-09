import { useEffect, useMemo, useState } from 'react'
import {
  Badge,
  Button,
  DataTable,
  PageLayout,
  SearchableSelect,
  useApi,
} from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Ban, Eye, FileText, Pencil, Plus, Receipt, Send, Signature } from 'lucide-react'
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
    const result = await fetchCampaigns(() => campaignService.getAll())
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
    if (!window.confirm(`Cancelar o pagamento de ${selected[0].creatorName}?`)) return
    const result = await runCancel(() => creatorPaymentService.cancel(selected[0].id))
    if (result !== null) void loadPayments()
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
      title: 'Creator',
      dataIndex: 'creatorName',
      render: (value: string | undefined) => value || '—',
    },
    {
      key: 'campaignName',
      title: 'Campanha',
      dataIndex: 'campaignName',
      render: (value: string | undefined) => (
        <span className="text-sm text-muted-foreground">{value ?? '—'}</span>
      ),
    },
    {
      key: 'netAmount',
      title: 'Líquido',
      dataIndex: 'netAmount',
      width: 120,
      render: (value: number) => `R$ ${value.toFixed(2)}`,
    },
    {
      key: 'method',
      title: 'Método',
      dataIndex: 'method',
      width: 90,
      render: (value: number) => (
        <Badge variant="outline">{paymentMethodLabels[value as 1 | 2 | 3]}</Badge>
      ),
    },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      width: 110,
      render: (value: PaymentStatusValue) => (
        <Badge variant={STATUS_VARIANTS[value]}>{paymentStatusLabels[value]}</Badge>
      ),
    },
    {
      key: 'invoiceNumber',
      title: 'NF',
      dataIndex: 'invoiceNumber',
      width: 90,
      render: (value: string | undefined) =>
        value ? (
          <Badge variant="success">#{value}</Badge>
        ) : (
          <Badge variant="outline">Sem NF</Badge>
        ),
    },
    {
      key: 'scheduledFor',
      title: 'Agendado',
      dataIndex: 'scheduledFor',
      width: 130,
      render: (value?: string) => (value ? new Date(value).toLocaleDateString('pt-BR') : '—'),
    },
    {
      key: 'paidAt',
      title: 'Pago em',
      dataIndex: 'paidAt',
      width: 130,
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
          title="Ver detalhes"
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
        title="Repasses para creators"
        subtitle="Gerencie pagamentos individuais e em lote, anexe NF e dispare via gateway."
        showDefaultActions={false}
      >
        <div className="flex flex-wrap items-end gap-3">
          <div className="space-y-1 min-w-[220px]">
            <label className="text-xs text-muted-foreground">Filtrar por status</label>
            <SearchableSelect
              value={String(statusFilter)}
              onValueChange={(value) => setStatusFilter(Number(value) as PaymentStatusValue)}
              options={Object.values(PaymentStatus).map((value) => ({
                value: String(value),
                label: paymentStatusLabels[value as PaymentStatusValue],
              }))}
              placeholder="Status"
              searchPlaceholder="Buscar"
            />
          </div>
          <div className="space-y-1 min-w-[260px]">
            <label className="text-xs text-muted-foreground">Filtrar por campanha</label>
            <SearchableSelect
              value={campaignId ? String(campaignId) : ''}
              onValueChange={(value) => setCampaignId(value ? Number(value) : undefined)}
              options={[{ value: '', label: 'Todas as campanhas' }, ...campaigns.map((c) => ({ value: String(c.id), label: c.name }))]}
              placeholder="Todas as campanhas"
              searchPlaceholder="Buscar"
            />
          </div>
          <div className="flex flex-wrap gap-2 ml-auto">
            <Button size="sm" variant="outline" onClick={() => { setEditingPayment(selectedPayment); setIsFormOpen(true) }} disabled={!selectedPayment}>
              <Pencil size={14} className="mr-1" /> Editar
            </Button>
            <Button size="sm" variant="outline" onClick={() => setIsInvoiceOpen(true)} disabled={!selectedPayment}>
              <Receipt size={14} className="mr-1" /> Anexar NF
            </Button>
            <Button size="sm" variant="outline" onClick={() => setIsMarkPaidOpen(true)} disabled={!selectedPayment}>
              <Signature size={14} className="mr-1" /> Marcar pago
            </Button>
            <Button size="sm" variant="outline" onClick={() => void handleCancel()} disabled={!selectedPayment || cancelling}>
              <Ban size={14} className="mr-1" /> Cancelar
            </Button>
            <Button size="sm" variant="outline" onClick={() => setIsBatchOpen(true)} disabled={!canSchedule}>
              <Send size={14} className="mr-1" /> Agendar lote ({selected.length})
            </Button>
            <Button size="sm" onClick={() => { setEditingPayment(null); setIsFormOpen(true) }}>
              <Plus size={14} className="mr-1" /> Novo
            </Button>
          </div>
        </div>

        <div className="mt-3 grid grid-cols-2 gap-2 text-sm md:grid-cols-4">
          <Stat label="Pagamentos" value={payments.length.toString()} />
          <Stat label="Total bruto" value={`R$ ${totals.all.gross.toFixed(2)}`} />
          <Stat label="Total líquido" value={`R$ ${totals.all.net.toFixed(2)}`} />
          <Stat
            label="Selecionados"
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
            multiSelect
            emptyText={loading ? 'Carregando...' : 'Nenhum pagamento neste filtro'}
            loading={loading}
            pageSize={20}
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
