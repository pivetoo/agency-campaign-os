import { useEffect, useRef, useState } from 'react'
import { ChevronDown, ChevronUp, ExternalLink, ImagePlus } from 'lucide-react'
import { useI18n } from 'archon-ui'
import { creatorPortalService } from '../../services/creatorPortalService'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'
import type { ContentReview, ContentAssetInput } from '../../types/contentReview'
import { formatDateTime } from '../../lib/format'
import { resolveUploadUrl } from '../../lib/uploadUrl'
import { usePortalContext } from './hooks'

const VERSION_STATUS_COLOR: Record<number, string> = {
  1: 'bg-amber-500/15 text-amber-700',
  2: 'bg-blue-500/15 text-blue-700',
  3: 'bg-destructive/15 text-destructive',
  4: 'bg-emerald-500/15 text-emerald-700',
}

export default function CreatorPortalContent() {
  const { t } = useI18n()
  const { token } = usePortalContext()
  const [deliverables, setDeliverables] = useState<CampaignDeliverable[]>([])
  const [loading, setLoading] = useState(true)
  const [openId, setOpenId] = useState<number | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    creatorPortalService.getDeliverables(token).then((res) => {
      if (cancelled) return
      setDeliverables(res)
      setLoading(false)
    })
    return () => {
      cancelled = true
    }
  }, [token])

  const toggle = (id: number) => setOpenId((prev) => (prev === id ? null : id))

  if (loading) return <p className="text-sm text-muted-foreground">Carregando...</p>
  if (deliverables.length === 0) return <p className="text-sm text-muted-foreground">{t('contentReview.empty')}</p>

  return (
    <div className="space-y-3">
      <h2 className="text-base font-semibold">{t('contentReview.title')}</h2>
      {deliverables.map((item) => (
        <DeliverableCard
          key={item.id}
          token={token}
          deliverable={item}
          open={openId === item.id}
          onToggle={() => toggle(item.id)}
        />
      ))}
    </div>
  )
}

interface DeliverableCardProps {
  token: string
  deliverable: CampaignDeliverable
  open: boolean
  onToggle: () => void
}

function DeliverableCard({ token, deliverable, open, onToggle }: DeliverableCardProps) {
  const { t } = useI18n()
  const [review, setReview] = useState<ContentReview | null>(null)
  const [reviewLoading, setReviewLoading] = useState(false)
  const [reviewLoaded, setReviewLoaded] = useState(false)

  useEffect(() => {
    if (!open || reviewLoaded) return
    let cancelled = false
    setReviewLoading(true)
    creatorPortalService.getDeliverableReview(token, deliverable.id).then((res) => {
      if (cancelled) return
      setReview(res)
      setReviewLoaded(true)
      setReviewLoading(false)
    })
    return () => {
      cancelled = true
    }
  }, [open, reviewLoaded, token, deliverable.id])

  const latest = review?.versions.at(-1) ?? null
  const canSubmit = !latest || latest.status === 3

  return (
    <div className="rounded-lg border bg-background">
      <button type="button" onClick={onToggle} className="flex w-full items-center justify-between p-3 text-left">
        <div className="min-w-0">
          <p className="font-medium">{deliverable.title}</p>
          <p className="text-xs text-muted-foreground">
            {deliverable.campaign?.name}
            {deliverable.platform?.name ? ` · ${deliverable.platform.name}` : ''}
          </p>
        </div>
        <div className="ml-2 flex shrink-0 items-center gap-2">
          {latest && (
            <span className={`rounded-full px-2 py-0.5 text-[10px] font-medium ${VERSION_STATUS_COLOR[latest.status] ?? 'bg-muted text-muted-foreground'}`}>
              {t(`contentReview.status.${statusKey(latest.status)}`)}
            </span>
          )}
          {open ? <ChevronUp size={16} className="text-muted-foreground" /> : <ChevronDown size={16} className="text-muted-foreground" />}
        </div>
      </button>

      {open && (
        <div className="border-t px-3 pb-3 pt-3 space-y-4">
          {reviewLoading && <p className="text-sm text-muted-foreground">Carregando...</p>}
          {!reviewLoading && review && (
            <>
              <VersionList review={review} />
              <CommentSection token={token} deliverableId={deliverable.id} review={review} onUpdate={setReview} />
              {canSubmit && (
                <SubmitVersionForm
                  token={token}
                  deliverableId={deliverable.id}
                  onSubmitted={(updated) => {
                    setReview(updated)
                  }}
                />
              )}
            </>
          )}
          {!reviewLoading && !review && (
            <SubmitVersionForm
              token={token}
              deliverableId={deliverable.id}
              onSubmitted={(updated) => {
                setReview(updated)
                setReviewLoaded(true)
              }}
            />
          )}
        </div>
      )}
    </div>
  )
}

function statusKey(status: number): string {
  if (status === 1) return 'pendingInternal'
  if (status === 2) return 'pendingBrand'
  if (status === 3) return 'changesRequested'
  if (status === 4) return 'approved'
  return 'pendingInternal'
}

function authorKey(role: number): string {
  if (role === 1) return 'agency'
  if (role === 2) return 'brand'
  return 'creator'
}

interface VersionListProps {
  review: ContentReview
}

function VersionList({ review }: VersionListProps) {
  const { t } = useI18n()

  if (review.versions.length === 0) return null

  return (
    <div className="space-y-2">
      {review.versions.map((version) => (
        <div key={version.id} className="rounded-md border bg-muted/30 p-2.5 space-y-2">
          <div className="flex items-center justify-between gap-2">
            <span className="text-xs font-medium">{t('contentReview.round').replace('{0}', String(version.roundNumber))}</span>
            <span className={`rounded-full px-2 py-0.5 text-[10px] font-medium ${VERSION_STATUS_COLOR[version.status] ?? 'bg-muted text-muted-foreground'}`}>
              {t(`contentReview.status.${statusKey(version.status)}`)}
            </span>
          </div>
          <p className="text-[11px] text-muted-foreground">
            {t('contentReview.submittedBy').replace('{0}', version.submittedByName)} · {t(`contentReview.author.${authorKey(version.submittedByRole)}`)} · {formatDateTime(version.createdAt)}
          </p>
          {version.note && <p className="text-xs text-foreground/80">{version.note}</p>}
          {version.assets.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {version.assets.map((asset, idx) => {
                if (asset.type === 1) {
                  const src = resolveUploadUrl(asset.url)
                  if (!src) return null
                  if (/\.(mp4|mov|webm)$/i.test(asset.fileName ?? '')) {
                    return <video key={idx} src={src} controls className="h-20 w-auto rounded border" />
                  }
                  return (
                    <a key={idx} href={src} target="_blank" rel="noreferrer">
                      <img src={src} alt={asset.fileName ?? ''} className="h-20 w-20 rounded border object-cover" />
                    </a>
                  )
                }
                return (
                  <a key={idx} href={asset.url} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-xs text-primary hover:underline">
                    <ExternalLink size={12} /> {t('contentReview.asset.externalLink')}
                  </a>
                )
              })}
            </div>
          )}
        </div>
      ))}
    </div>
  )
}

interface CommentSectionProps {
  token: string
  deliverableId: number
  review: ContentReview
  onUpdate: (review: ContentReview) => void
}

function CommentSection({ token, deliverableId, review, onUpdate }: CommentSectionProps) {
  const { t } = useI18n()
  const [body, setBody] = useState('')
  const [sending, setSending] = useState(false)

  const send = async () => {
    const text = body.trim()
    if (!text) return
    setSending(true)
    try {
      const response = await creatorPortalService.addReviewComment(token, deliverableId, text)
      const updated = response.data
      if (updated) {
        onUpdate(updated)
        setBody('')
      }
    } finally {
      setSending(false)
    }
  }

  const sharedComments = review.comments.filter((c) => c.visibility === 2)

  return (
    <div className="space-y-2">
      <p className="text-[10px] font-medium uppercase tracking-wide text-muted-foreground">{t('contentReview.comments.shared')}</p>
      {sharedComments.length === 0 ? (
        <p className="text-xs text-muted-foreground">{t('contentReview.comments.empty')}</p>
      ) : (
        <div className="space-y-2">
          {sharedComments.map((comment) => (
            <div key={comment.id} className="rounded-md bg-muted/40 p-2.5">
              <div className="flex items-center gap-1 text-[10px] text-muted-foreground">
                <span className="font-medium">{comment.authorName}</span>
                <span>·</span>
                <span>{t(`contentReview.author.${authorKey(comment.authorRole)}`)}</span>
                <span>·</span>
                <span>{formatDateTime(comment.createdAt)}</span>
              </div>
              <p className="mt-0.5 text-xs">{comment.body}</p>
            </div>
          ))}
        </div>
      )}
      <textarea
        value={body}
        onChange={(event) => setBody(event.target.value)}
        placeholder={t('contentReview.comments.placeholder')}
        rows={2}
        className="w-full resize-none rounded-md border bg-background px-2.5 py-2 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-primary"
      />
      <div className="flex justify-end">
        <button
          type="button"
          onClick={() => void send()}
          disabled={sending || !body.trim()}
          className="rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-white disabled:opacity-50"
        >
          {sending ? t('common.action.sending') : t('contentReview.action.addComment')}
        </button>
      </div>
    </div>
  )
}

interface SubmitVersionFormProps {
  token: string
  deliverableId: number
  onSubmitted: (review: ContentReview) => void
}

function SubmitVersionForm({ token, deliverableId, onSubmitted }: SubmitVersionFormProps) {
  const { t } = useI18n()
  const fileRef = useRef<HTMLInputElement>(null)
  const [externalUrl, setExternalUrl] = useState('')
  const [note, setNote] = useState('')
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [imagePreview, setImagePreview] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const handleFile = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] ?? null
    setImageFile(file)
    if (file) {
      setImagePreview(URL.createObjectURL(file))
    } else {
      setImagePreview(null)
    }
  }

  const submit = async () => {
    const assets: ContentAssetInput[] = []

    if (imageFile) {
      const uploadResponse = await creatorPortalService.uploadReviewFile(token, deliverableId, imageFile)
      const uploaded = uploadResponse.data
      if (uploaded) {
        assets.push({ type: 1, url: uploaded.storageKey, fileName: uploaded.fileName, contentType: uploaded.contentType })
      }
    }

    const url = externalUrl.trim()
    if (url) {
      assets.push({ type: 2, url })
    }

    if (assets.length === 0 && !note.trim()) return

    const response = await creatorPortalService.submitContentVersion(token, deliverableId, assets, note.trim() || undefined)
    const updated = response.data
    if (updated) {
      onSubmitted(updated)
      setExternalUrl('')
      setNote('')
      setImageFile(null)
      setImagePreview(null)
      if (fileRef.current) fileRef.current.value = ''
    }
  }

  const handleSubmit = async () => {
    setSubmitting(true)
    try {
      await submit()
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="space-y-3 rounded-md border border-dashed p-3">
      <p className="text-xs font-medium">{t('contentReview.action.submitVersion')}</p>

      <div>
        <label className="text-[10px] uppercase tracking-wide text-muted-foreground">{t('contentReview.field.uploadImage')}</label>
        <div className="mt-1 flex items-center gap-2">
          <button
            type="button"
            onClick={() => fileRef.current?.click()}
            className="inline-flex items-center gap-1.5 rounded-md border px-2.5 py-1.5 text-xs text-muted-foreground hover:text-foreground"
          >
            <ImagePlus size={14} />
            {imageFile ? imageFile.name : t('common.action.attach')}
          </button>
          {imagePreview && (
            imageFile?.type.startsWith('video')
              ? <video src={imagePreview} className="h-10 w-10 rounded border object-cover" />
              : <img src={imagePreview} alt="" className="h-10 w-10 rounded border object-cover" />
          )}
        </div>
        <input ref={fileRef} type="file" accept="image/*,video/mp4,video/quicktime,video/webm" className="hidden" onChange={handleFile} />
      </div>

      <div>
        <label className="text-[10px] uppercase tracking-wide text-muted-foreground">{t('contentReview.field.externalUrl')}</label>
        <input
          type="url"
          value={externalUrl}
          onChange={(event) => setExternalUrl(event.target.value)}
          placeholder="https://"
          className="mt-1 w-full rounded-md border bg-background px-2.5 py-1.5 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-primary"
        />
      </div>

      <div>
        <label className="text-[10px] uppercase tracking-wide text-muted-foreground">{t('contentReview.field.note')}</label>
        <textarea
          value={note}
          onChange={(event) => setNote(event.target.value)}
          rows={2}
          className="mt-1 w-full resize-none rounded-md border bg-background px-2.5 py-2 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-primary"
        />
      </div>

      <div className="flex justify-end">
        <button
          type="button"
          onClick={() => void handleSubmit()}
          disabled={submitting || (!imageFile && !externalUrl.trim() && !note.trim())}
          className="rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-white disabled:opacity-50"
        >
          {submitting ? t('common.action.sending') : t('contentReview.action.submitVersion')}
        </button>
      </div>
    </div>
  )
}
