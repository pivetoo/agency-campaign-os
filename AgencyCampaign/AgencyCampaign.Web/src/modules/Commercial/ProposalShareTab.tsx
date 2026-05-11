import { useEffect, useState } from 'react'
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, Input, useApi, useToast, useI18n } from 'archon-ui'
import { Copy, Eye, Link as LinkIcon, Link2, ShieldOff } from 'lucide-react'
import {
  proposalService,
  type ProposalShareLink,
  type ProposalVersion,
} from '../../services/proposalService'

interface ProposalShareTabProps {
  proposalId: number
}

function formatDateTime(value?: string): string {
  if (!value) return '-'
  const date = new Date(value)
  return `${date.toLocaleDateString('pt-BR')} ${date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`
}

function buildPublicUrl(token: string): string {
  return `${window.location.origin}/p/${token}`
}

function maskToken(token: string): string {
  return `${token.slice(0, 8)}...${token.slice(-4)}`
}

export default function ProposalShareTab({ proposalId }: ProposalShareTabProps) {
  const { t } = useI18n()
  const [shareLinks, setShareLinks] = useState<ProposalShareLink[]>([])
  const [versions, setVersions] = useState<ProposalVersion[]>([])
  const [expiresAt, setExpiresAt] = useState('')

  const { execute: load, loading } = useApi<unknown>({ showErrorMessage: true })
  const { execute: runMutation, loading: mutating } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })
  const { toast } = useToast()

  const reload = async () => {
    const [links, versionList] = await Promise.all([
      proposalService.getShareLinks(proposalId),
      proposalService.getVersions(proposalId),
    ])
    setShareLinks(links)
    setVersions(versionList)
  }

  useEffect(() => {
    void load(async () => {
      await reload()
      return null
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [proposalId])

  const generateLink = async () => {
    const expiresAtIso = expiresAt ? new Date(expiresAt).toISOString() : undefined
    const result = await runMutation(() =>
      proposalService.createShareLink(proposalId, expiresAtIso ? { expiresAt: expiresAtIso } : {})
    )
    if (result !== null) {
      setExpiresAt('')
      await reload()
    }
  }

  const revokeLink = async (linkId: number) => {
    if (!window.confirm(t('proposalShare.confirm.revoke'))) return
    const result = await runMutation(() => proposalService.revokeShareLink(linkId))
    if (result !== null) {
      await reload()
    }
  }

  const copyLink = async (token: string) => {
    const url = buildPublicUrl(token)
    try {
      await navigator.clipboard.writeText(url)
      toast({ title: t('proposalShare.toast.copied.title'), description: url, variant: 'success' })
    } catch {
      toast({ title: t('proposalShare.toast.copyFailed.title'), description: t('proposalShare.toast.copyFailed.description'), variant: 'destructive' })
    }
  }

  const statusBadge = (link: ProposalShareLink) => {
    if (link.revokedAt) return <Badge variant="destructive">{t('proposalShare.status.revoked')}</Badge>
    if (!link.isActive) return <Badge variant="warning">{t('proposalShare.status.expired')}</Badge>
    return <Badge variant="success">{t('proposalShare.status.active')}</Badge>
  }

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader className="border-b bg-muted/20 py-3">
          <CardTitle className="flex items-center gap-2 text-sm">
            <LinkIcon className="h-4 w-4 text-primary" />
            {t('proposalShare.publicLinks.title')}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4 p-4">
          <div className="rounded-md border border-dashed border-border/70 bg-muted/20 p-4">
            <div className="text-sm font-medium text-foreground">{t('proposalShare.generate.title')}</div>
            <p className="mt-1 text-xs text-muted-foreground">
              {t('proposalShare.generate.description')}
            </p>
            <div className="mt-3 space-y-2">
              <div>
                <label className="text-xs font-medium text-muted-foreground">
                  {t('proposalShare.generate.expiresLabel')}
                </label>
                <Input
                  type="datetime-local"
                  value={expiresAt}
                  onChange={(e) => setExpiresAt(e.target.value)}
                  className="mt-1 w-full"
                  disabled={mutating}
                />
              </div>
              <Button
                icon={<Link2 className="h-4 w-4" />}
                onClick={() => void generateLink()}
                disabled={mutating}
                fullWidth
              >
                {t('proposalShare.generate.button')}
              </Button>
            </div>
          </div>

          {loading && shareLinks.length === 0 ? (
            <div className="text-sm text-muted-foreground">{t('proposalShare.loading')}</div>
          ) : shareLinks.length === 0 ? (
            <div className="text-sm text-muted-foreground">{t('proposalShare.empty')}</div>
          ) : (
            <div className="space-y-2">
              {shareLinks.map((link) => (
                <div key={link.id} className="rounded-md border border-border/60 p-3">
                  <div className="flex flex-wrap items-center gap-2">
                    {statusBadge(link)}
                    <span className="flex items-center gap-1 text-xs text-muted-foreground">
                      <Eye className="h-3 w-3" /> {link.viewCount}
                    </span>
                  </div>
                  <div className="mt-2 truncate font-mono text-[11px] text-muted-foreground">
                    {maskToken(link.token)}
                  </div>
                  <dl className="mt-2 space-y-1 text-xs text-muted-foreground">
                    <div className="flex items-baseline justify-between gap-2">
                      <dt className="text-[10px] uppercase tracking-wide">{t('proposalShare.field.created')}</dt>
                      <dd className="text-foreground">{formatDateTime(link.createdAt)}</dd>
                    </div>
                    <div className="flex items-baseline justify-between gap-2">
                      <dt className="text-[10px] uppercase tracking-wide">{t('proposalShare.field.expires')}</dt>
                      <dd className="text-foreground">
                        {link.expiresAt ? formatDateTime(link.expiresAt) : t('proposalShare.field.noExpiration')}
                      </dd>
                    </div>
                    {link.lastViewedAt ? (
                      <div className="flex items-baseline justify-between gap-2">
                        <dt className="text-[10px] uppercase tracking-wide">{t('proposalShare.field.lastView')}</dt>
                        <dd className="text-foreground">{formatDateTime(link.lastViewedAt)}</dd>
                      </div>
                    ) : null}
                  </dl>
                  <div className="mt-3 flex flex-wrap items-center gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      icon={<Copy className="h-3.5 w-3.5" />}
                      onClick={() => void copyLink(link.token)}
                      disabled={!link.isActive}
                    >
                      {t('proposalShare.action.copy')}
                    </Button>
                    {link.isActive ? (
                      <Button
                        size="sm"
                        variant="outline-danger"
                        icon={<ShieldOff className="h-3.5 w-3.5" />}
                        onClick={() => void revokeLink(link.id)}
                        disabled={mutating}
                      >
                        {t('proposalShare.action.revoke')}
                      </Button>
                    ) : null}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="border-b bg-muted/20 py-3">
          <CardTitle className="text-sm">{t('proposalShare.versions.title')}</CardTitle>
        </CardHeader>
        <CardContent className="p-4">
          {versions.length === 0 ? (
            <div className="text-sm text-muted-foreground">{t('proposalShare.versions.empty')}</div>
          ) : (
            <ol className="space-y-2">
              {versions.map((version) => (
                <li key={version.id} className="rounded-md border border-border/60 p-3">
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-medium text-foreground">v{version.versionNumber}</span>
                    <span className="text-xs text-muted-foreground">{formatDateTime(version.sentAt)}</span>
                  </div>
                  <div className="mt-1 flex flex-wrap gap-3 text-xs text-muted-foreground">
                    <span>
                      {t('proposalShare.versions.totalLabel')}: <strong className="text-foreground">R$ {version.totalValue.toFixed(2)}</strong>
                    </span>
                    {version.sentByUserName ? <span>{t('proposalShare.versions.byUser').replace('{0}', version.sentByUserName)}</span> : null}
                    {version.validityUntil ? (
                      <span>{t('proposalShare.versions.validUntil').replace('{0}', new Date(version.validityUntil).toLocaleDateString('pt-BR'))}</span>
                    ) : null}
                  </div>
                </li>
              ))}
            </ol>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
