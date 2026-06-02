import { useEffect, useRef, useState } from 'react'
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription, Badge, Button, Input, useApi, useI18n } from 'archon-ui'
import { ClipboardCheck, ExternalLink, MessageSquare, Send, Upload } from 'lucide-react'
import { contentReviewService } from '../../services/contentReviewService'
import { deliverableShareLinkService } from '../../services/deliverableShareLinkService'
import type { ContentReview, ContentVersion, ReviewCommentVisibility } from '../../types/contentReview'
import { formatDateTime } from '../../lib/format'
import { resolveUploadUrl } from '../../lib/uploadUrl'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  deliverableId: number | null
}

function roleLabel(role: 1 | 2 | 3, t: (k: string) => string): string {
  if (role === 1) return t('contentReview.author.agency')
  if (role === 2) return t('contentReview.author.brand')
  return t('contentReview.author.creator')
}

function statusVariant(status: 1 | 2 | 3 | 4): 'warning' | 'info' | 'destructive' | 'success' {
  if (status === 1) return 'warning'
  if (status === 2) return 'info'
  if (status === 3) return 'destructive'
  return 'success'
}

function statusLabel(status: 1 | 2 | 3 | 4, t: (k: string) => string): string {
  if (status === 1) return t('contentReview.status.pendingInternal')
  if (status === 2) return t('contentReview.status.pendingBrand')
  if (status === 3) return t('contentReview.status.changesRequested')
  return t('contentReview.status.approved')
}

export default function ContentReviewSheet({ open, onOpenChange, deliverableId }: Props) {
  const { t } = useI18n()
  const [review, setReview] = useState<ContentReview | null>(null)

  const [commentBody, setCommentBody] = useState('')
  const [commentVisibility, setCommentVisibility] = useState<ReviewCommentVisibility>(1)

  const [submitOpen, setSubmitOpen] = useState(false)
  const [submitNote, setSubmitNote] = useState('')
  const [submitUrl, setSubmitUrl] = useState('')
  const [submitFile, setSubmitFile] = useState<File | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [changesBody, setChangesBody] = useState('')
  const [changesOpen, setChangesOpen] = useState(false)

  const { execute: fetchReview, loading: loadingReview } = useApi<ContentReview | null>({ showErrorMessage: true })
  const { execute: runUpload } = useApi({ showErrorMessage: true })
  const { execute: runAddVersion, loading: addingVersion } = useApi({ showErrorMessage: true, showSuccessMessage: true })
  const { execute: runRequestChanges, loading: requestingChanges } = useApi({ showErrorMessage: true, showSuccessMessage: true })
  const { execute: runSendToBrand, loading: sendingToBrand } = useApi({ showErrorMessage: true, showSuccessMessage: true })
  const { execute: runAgencyApprove, loading: approvingInternally } = useApi({ showErrorMessage: true, showSuccessMessage: true })
  const { execute: runAddComment, loading: addingComment } = useApi({ showErrorMessage: true })
  const { execute: runCopyLink, loading: copyingLink } = useApi({ showErrorMessage: true })

  useEffect(() => {
    if (open && deliverableId) {
      void loadReview()
    }
    if (!open) {
      setReview(null)
      resetSubmitForm()
      resetChangesForm()
      setCommentBody('')
      setCommentVisibility(1)
    }
  }, [open, deliverableId])

  async function loadReview() {
    if (!deliverableId) return
    const result = await fetchReview(() => contentReviewService.get(deliverableId))
    setReview(result ?? null)
  }

  function resetSubmitForm() {
    setSubmitOpen(false)
    setSubmitNote('')
    setSubmitUrl('')
    setSubmitFile(null)
  }

  function resetChangesForm() {
    setChangesOpen(false)
    setChangesBody('')
  }

  const latestVersion: ContentVersion | undefined = review
    ? [...review.versions].sort((a, b) => b.roundNumber - a.roundNumber)[0]
    : undefined

  const canSubmitVersion = !latestVersion || latestVersion.status === 3
  const canRequestChanges = latestVersion?.status === 1
  const canSendToBrand = latestVersion?.status === 1
  const canAgencyApprove = latestVersion?.status === 1

  async function handleAddVersion() {
    if (!deliverableId) return
    const assets: Array<{ type: 1 | 2; url: string; fileName?: string; contentType?: string }> = []

    if (submitFile) {
      const uploadRes = await runUpload(() => contentReviewService.uploadFile(deliverableId, submitFile))
      if (!uploadRes) return
      assets.push({ type: 1, url: uploadRes.url, fileName: uploadRes.fileName, contentType: uploadRes.contentType })
    }

    if (submitUrl.trim()) {
      assets.push({ type: 2, url: submitUrl.trim() })
    }

    const res = await runAddVersion(() => contentReviewService.addVersion(deliverableId, assets, submitNote.trim() || undefined))
    if (res) {
      setReview(res)
      resetSubmitForm()
    }
  }

  async function handleRequestChanges() {
    if (!latestVersion || !changesBody.trim()) return
    const res = await runRequestChanges(() => contentReviewService.requestChanges(latestVersion.id, changesBody.trim()))
    if (res) {
      setReview(res)
      resetChangesForm()
    }
  }

  async function handleSendToBrand() {
    if (!latestVersion) return
    const res = await runSendToBrand(() => contentReviewService.sendToBrand(latestVersion.id))
    if (res) {
      setReview(res)
    }
  }

  async function handleAgencyApprove() {
    if (!latestVersion) return
    const res = await runAgencyApprove(() => contentReviewService.agencyApprove(latestVersion.id))
    if (res) {
      setReview(res)
    }
  }

  async function handleAddComment() {
    if (!deliverableId || !commentBody.trim()) return
    const res = await runAddComment(() => contentReviewService.addComment(deliverableId, commentBody.trim(), commentVisibility))
    if (res) {
      setReview(res)
      setCommentBody('')
    }
  }

  async function handleCopyLink() {
    if (!deliverableId) return
    const links = await runCopyLink(() => deliverableShareLinkService.getByDeliverable(deliverableId))
    if (!links) return
    const existing = Array.isArray(links) ? links[0] : null
    if (existing) {
      const url = `${window.location.origin}/d/${existing.token}`
      try {
        await navigator.clipboard.writeText(url)
      } catch {
        // ignore clipboard failure
      }
    }
  }

  const internalComments = review?.comments.filter((c) => c.visibility === 1) ?? []
  const sharedComments = review?.comments.filter((c) => c.visibility === 2) ?? []
  const sortedVersions = review ? [...review.versions].sort((a, b) => a.roundNumber - b.roundNumber) : []

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full sm:max-w-2xl flex flex-col gap-0 p-0">
        <SheetHeader className="px-6 pt-6 pb-4 border-b">
          <div className="flex items-center gap-2">
            <ClipboardCheck size={18} className="text-muted-foreground" />
            <SheetTitle>{t('contentReview.title')}</SheetTitle>
          </div>
          <SheetDescription className="sr-only">{t('contentReview.title')}</SheetDescription>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto">
          {loadingReview && !review ? (
            <div className="flex items-center justify-center py-16 text-muted-foreground text-sm">
              {t('common.loading')}
            </div>
          ) : sortedVersions.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 gap-2 text-muted-foreground">
              <ClipboardCheck size={36} />
              <p className="text-sm">{t('contentReview.empty')}</p>
            </div>
          ) : (
            <div className="divide-y">
              {sortedVersions.map((version) => (
                <div key={version.id} className="px-6 py-5 space-y-3">
                  <div className="flex items-center justify-between gap-3 flex-wrap">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-sm font-semibold text-foreground">
                        {t('contentReview.round').replace('{0}', String(version.roundNumber))}
                      </span>
                      <Badge variant={statusVariant(version.status)}>
                        {statusLabel(version.status, t)}
                      </Badge>
                    </div>
                    <span className="text-xs text-muted-foreground">{formatDateTime(version.createdAt)}</span>
                  </div>

                  <p className="text-xs text-muted-foreground">
                    {roleLabel(version.submittedByRole, t)} &mdash; {t('contentReview.submittedBy').replace('{0}', version.submittedByName)}
                  </p>

                  {version.note && (
                    <p className="text-sm text-foreground bg-muted/40 rounded px-3 py-2">{version.note}</p>
                  )}

                  {version.assets.length > 0 && (
                    <div className="flex flex-wrap gap-3">
                      {version.assets.map((asset, idx) => {
                        if (asset.type === 1) {
                          const src = resolveUploadUrl(asset.url)
                          return src ? (
                            <a key={idx} href={src} target="_blank" rel="noopener noreferrer">
                              <img src={src} alt={asset.fileName ?? ''} className="h-24 w-auto rounded border object-cover" />
                            </a>
                          ) : null
                        }
                        return (
                          <a key={idx} href={asset.url} target="_blank" rel="noopener noreferrer" className="flex items-center gap-1.5 text-xs text-primary underline underline-offset-2">
                            <ExternalLink size={13} />
                            {t('contentReview.asset.externalLink')}
                          </a>
                        )
                      })}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}

          {review && (
            <div className="border-t px-6 py-5 space-y-5">
              <div className="space-y-2">
                <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  {t('contentReview.comments.internal')}
                  <span className="ml-2 text-[10px] normal-case text-primary/70 bg-primary/10 px-1.5 py-0.5 rounded">
                    {t('contentReview.visibility.internalHint')}
                  </span>
                </p>
                {internalComments.length === 0 ? (
                  <p className="text-xs text-muted-foreground italic">{t('contentReview.comments.empty')}</p>
                ) : (
                  <ul className="space-y-2">
                    {internalComments.map((c) => (
                      <li key={c.id} className="bg-muted/40 rounded px-3 py-2 space-y-0.5">
                        <div className="flex items-center justify-between gap-2">
                          <span className="text-xs font-medium">{roleLabel(c.authorRole, t)} &mdash; {c.authorName}</span>
                          <span className="text-[10px] text-muted-foreground">{formatDateTime(c.createdAt)}</span>
                        </div>
                        <p className="text-xs text-foreground">{c.body}</p>
                      </li>
                    ))}
                  </ul>
                )}
              </div>

              <div className="space-y-2">
                <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{t('contentReview.comments.shared')}</p>
                {sharedComments.length === 0 ? (
                  <p className="text-xs text-muted-foreground italic">{t('contentReview.comments.empty')}</p>
                ) : (
                  <ul className="space-y-2">
                    {sharedComments.map((c) => (
                      <li key={c.id} className="bg-muted/40 rounded px-3 py-2 space-y-0.5">
                        <div className="flex items-center justify-between gap-2">
                          <span className="text-xs font-medium">{roleLabel(c.authorRole, t)} &mdash; {c.authorName}</span>
                          <span className="text-[10px] text-muted-foreground">{formatDateTime(c.createdAt)}</span>
                        </div>
                        <p className="text-xs text-foreground">{c.body}</p>
                      </li>
                    ))}
                  </ul>
                )}
              </div>

              <div className="space-y-2">
                <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  <MessageSquare size={13} className="inline-block mr-1" />
                  {t('contentReview.action.addComment')}
                </p>
                <textarea
                  className="w-full rounded border bg-background text-sm px-3 py-2 resize-none focus:outline-none focus:ring-2 focus:ring-primary/30"
                  rows={2}
                  placeholder={t('contentReview.comments.placeholder')}
                  value={commentBody}
                  onChange={(e) => setCommentBody(e.target.value)}
                />
                <div className="flex items-center justify-between gap-2 flex-wrap">
                  <div className="flex items-center gap-3">
                    <label className="flex items-center gap-1.5 text-xs cursor-pointer">
                      <input type="radio" name="visibility" value="1" checked={commentVisibility === 1} onChange={() => setCommentVisibility(1)} />
                      {t('contentReview.comments.internal')}
                    </label>
                    <label className="flex items-center gap-1.5 text-xs cursor-pointer">
                      <input type="radio" name="visibility" value="2" checked={commentVisibility === 2} onChange={() => setCommentVisibility(2)} />
                      {t('contentReview.comments.shared')}
                    </label>
                  </div>
                  <Button size="sm" variant="outline" disabled={!commentBody.trim() || addingComment} onClick={() => void handleAddComment()}>
                    <Send size={13} className="mr-1.5" />
                    {t('contentReview.action.addComment')}
                  </Button>
                </div>
              </div>
            </div>
          )}
        </div>

        <div className="border-t px-6 py-4 bg-background space-y-3">
          {canSubmitVersion && (
            <div className="space-y-3">
              {!submitOpen ? (
                <Button size="sm" className="w-full" onClick={() => setSubmitOpen(true)}>
                  <Upload size={14} className="mr-1.5" />
                  {t('contentReview.action.submitVersion')}
                </Button>
              ) : (
                <div className="space-y-2 rounded border bg-muted/20 px-4 py-3">
                  <p className="text-xs font-medium">{t('contentReview.action.submitVersion')}</p>

                  <div className="space-y-1">
                    <label className="text-xs text-muted-foreground">{t('contentReview.field.uploadImage')}</label>
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept="image/*"
                      className="hidden"
                      onChange={(e) => setSubmitFile(e.target.files?.[0] ?? null)}
                    />
                    <Button size="sm" variant="outline" type="button" onClick={() => fileInputRef.current?.click()}>
                      <Upload size={13} className="mr-1.5" />
                      {submitFile ? submitFile.name : t('contentReview.field.uploadImage')}
                    </Button>
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs text-muted-foreground">{t('contentReview.field.externalUrl')}</label>
                    <Input size={1} className="h-8 text-xs" placeholder="https://" value={submitUrl} onChange={(e) => setSubmitUrl(e.target.value)} />
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs text-muted-foreground">{t('contentReview.field.note')}</label>
                    <textarea className="w-full rounded border bg-background text-xs px-3 py-2 resize-none focus:outline-none focus:ring-2 focus:ring-primary/30" rows={2} value={submitNote} onChange={(e) => setSubmitNote(e.target.value)} />
                  </div>

                  <div className="flex gap-2 justify-end">
                    <Button size="sm" variant="outline" onClick={resetSubmitForm}>{t('common.action.cancel')}</Button>
                    <Button size="sm" disabled={addingVersion || (!submitFile && !submitUrl.trim())} onClick={() => void handleAddVersion()}>
                      {addingVersion ? t('common.action.saving') : t('common.action.save')}
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}

          {canRequestChanges && (
            <div className="space-y-2">
              {!changesOpen ? (
                <Button size="sm" variant="outline" className="w-full" onClick={() => setChangesOpen(true)}>
                  {t('contentReview.action.requestChanges')}
                </Button>
              ) : (
                <div className="space-y-2 rounded border bg-muted/20 px-4 py-3">
                  <p className="text-xs font-medium">{t('contentReview.action.requestChanges')}</p>
                  <textarea className="w-full rounded border bg-background text-xs px-3 py-2 resize-none focus:outline-none focus:ring-2 focus:ring-primary/30" rows={3} placeholder={t('contentReview.comments.placeholder')} value={changesBody} onChange={(e) => setChangesBody(e.target.value)} />
                  <div className="flex gap-2 justify-end">
                    <Button size="sm" variant="outline" onClick={resetChangesForm}>{t('common.action.cancel')}</Button>
                    <Button size="sm" disabled={requestingChanges || !changesBody.trim()} onClick={() => void handleRequestChanges()}>
                      {t('contentReview.action.requestChanges')}
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}

          {canSendToBrand && (
            <Button size="sm" variant="outline" className="w-full" disabled={sendingToBrand} onClick={() => void handleSendToBrand()}>
              <Send size={13} className="mr-1.5" />
              {t('contentReview.action.sendToBrand')}
            </Button>
          )}

          {canAgencyApprove && (
            <Button size="sm" variant="outline" className="w-full" disabled={approvingInternally} onClick={() => void handleAgencyApprove()}>
              {t('contentReview.action.agencyApprove')}
            </Button>
          )}

          <Button size="sm" variant="outline" className="w-full" disabled={copyingLink || !deliverableId} onClick={() => void handleCopyLink()}>
            {t('contentReview.action.copyLink')}
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  )
}
