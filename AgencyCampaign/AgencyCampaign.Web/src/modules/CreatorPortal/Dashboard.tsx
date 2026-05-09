import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Calendar, FileText, Receipt, AlertTriangle } from 'lucide-react'
import { creatorPortalService, type PortalCampaign } from '../../services/creatorPortalService'
import type { CampaignDocument } from '../../types/campaignDocument'
import type { CreatorPayment } from '../../types/creatorPayment'
import { CampaignDocumentStatus } from '../../types/campaignDocument'
import { PaymentStatus } from '../../types/creatorPayment'
import { usePortalContext } from './hooks'

export default function CreatorPortalDashboard() {
  const { token, session } = usePortalContext()
  const [campaigns, setCampaigns] = useState<PortalCampaign[]>([])
  const [documents, setDocuments] = useState<CampaignDocument[]>([])
  const [payments, setPayments] = useState<CreatorPayment[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    Promise.all([
      creatorPortalService.getCampaigns(token),
      creatorPortalService.getDocuments(token),
      creatorPortalService.getPayments(token),
    ])
      .then(([c, d, p]) => {
        if (cancelled) return
        setCampaigns(c)
        setDocuments(d)
        setPayments(p)
      })
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [token])

  const pendingDocs = documents.filter(
    (d) => d.status === CampaignDocumentStatus.Sent || d.status === CampaignDocumentStatus.Viewed,
  )
  const pendingPayments = payments.filter(
    (p) => p.status === PaymentStatus.Pending || p.status === PaymentStatus.Scheduled,
  )
  const paymentsWithoutInvoice = payments.filter(
    (p) => !p.invoiceUrl && (p.status === PaymentStatus.Scheduled || p.status === PaymentStatus.Pending),
  )

  const missingPix = !session.creator.pixKey || !session.creator.pixKeyType

  if (loading) {
    return <p className="text-sm text-muted-foreground">Carregando...</p>
  }

  return (
    <div className="space-y-4">
      {missingPix && (
        <Link
          to={`/portal/${token}/perfil`}
          className="flex items-start gap-2 rounded-lg border border-destructive/30 bg-destructive/5 p-3 text-sm"
        >
          <AlertTriangle size={18} className="mt-0.5 shrink-0 text-destructive" />
          <div>
            <p className="font-medium">Cadastre sua chave PIX</p>
            <p className="text-xs text-muted-foreground">Sem PIX a agência não consegue enviar seus repasses.</p>
          </div>
        </Link>
      )}

      <div className="grid grid-cols-3 gap-2">
        <SummaryCard label="Campanhas" value={campaigns.length} icon={<Calendar size={16} />} />
        <SummaryCard label="Contratos pendentes" value={pendingDocs.length} icon={<FileText size={16} />} />
        <SummaryCard label="Repasses pendentes" value={pendingPayments.length} icon={<Receipt size={16} />} />
      </div>

      {pendingDocs.length > 0 && (
        <Section title="Contratos esperando você">
          {pendingDocs.slice(0, 3).map((doc) => (
            <Link
              key={doc.id}
              to={`/portal/${token}/contratos`}
              className="block rounded-md border bg-background p-3 text-sm hover:border-primary/40"
            >
              <p className="font-medium">{doc.title}</p>
              <p className="text-xs text-muted-foreground">Aguardando sua assinatura</p>
            </Link>
          ))}
        </Section>
      )}

      {paymentsWithoutInvoice.length > 0 && (
        <Section title="Anexe nota fiscal">
          {paymentsWithoutInvoice.slice(0, 3).map((p) => (
            <Link
              key={p.id}
              to={`/portal/${token}/pagamentos`}
              className="block rounded-md border bg-background p-3 text-sm hover:border-primary/40"
            >
              <p className="font-medium">R$ {p.netAmount.toFixed(2)}</p>
              <p className="text-xs text-muted-foreground">{p.campaignName ?? 'Sem campanha'} · sem NF</p>
            </Link>
          ))}
        </Section>
      )}
    </div>
  )
}

function SummaryCard({ label, value, icon }: { label: string; value: number; icon: React.ReactNode }) {
  return (
    <div className="rounded-lg border bg-primary/5 p-3">
      <div className="mb-1 flex items-center gap-1.5 text-xs text-muted-foreground">
        {icon}
        <span>{label}</span>
      </div>
      <p className="text-xl font-semibold">{value}</p>
    </div>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="space-y-2">
      <h3 className="text-sm font-medium">{title}</h3>
      <div className="space-y-2">{children}</div>
    </div>
  )
}
