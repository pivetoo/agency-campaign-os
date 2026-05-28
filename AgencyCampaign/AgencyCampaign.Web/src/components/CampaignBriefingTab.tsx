import { useEffect, useState } from 'react'
import { Card, CardContent, Button, Input, useApi, useI18n } from 'archon-ui'
import { campaignBriefingService } from '../services/campaignBriefingService'
import type { CampaignBriefing } from '../types/campaignBriefing'

interface Props {
  campaignId: number
}

const textareaClass = 'w-full rounded border bg-background text-sm px-3 py-2 resize-none focus:outline-none focus:ring-2 focus:ring-primary/30'

export default function CampaignBriefingTab({ campaignId }: Props) {
  const { t } = useI18n()
  const [keyMessage, setKeyMessage] = useState('')
  const [dos, setDos] = useState('')
  const [donts, setDonts] = useState('')
  const [hashtags, setHashtags] = useState('')
  const [mentions, setMentions] = useState('')
  const [referenceLinks, setReferenceLinks] = useState('')
  const { execute: fetchBriefing, loading } = useApi<CampaignBriefing | null>({ showErrorMessage: true })
  const { execute: runSave, loading: saving } = useApi({ showErrorMessage: true, showSuccessMessage: true })

  useEffect(() => {
    if (campaignId > 0) {
      void fetchBriefing(() => campaignBriefingService.getByCampaign(campaignId)).then((result) => {
        if (result) {
          setKeyMessage(result.keyMessage ?? '')
          setDos(result.dos ?? '')
          setDonts(result.donts ?? '')
          setHashtags(result.hashtags ?? '')
          setMentions(result.mentions ?? '')
          setReferenceLinks(result.referenceLinks ?? '')
        }
      })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [campaignId])

  async function handleSave() {
    await runSave(() => campaignBriefingService.upsert(campaignId, {
      keyMessage: keyMessage.trim() || null,
      dos: dos.trim() || null,
      donts: donts.trim() || null,
      hashtags: hashtags.trim() || null,
      mentions: mentions.trim() || null,
      referenceLinks: referenceLinks.trim() || null,
    }))
  }

  return (
    <Card>
      <CardContent className="pt-5 pb-5 space-y-4">
        <p className="text-xs text-muted-foreground">{t('campaignBriefing.hint')}</p>

        <div className="space-y-1">
          <label className="text-sm font-medium">{t('campaignBriefing.field.keyMessage')}</label>
          <textarea className={textareaClass} rows={2} value={keyMessage} onChange={(e) => setKeyMessage(e.target.value)} />
        </div>

        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-1">
            <label className="text-sm font-medium">{t('campaignBriefing.field.dos')}</label>
            <textarea className={textareaClass} rows={4} value={dos} onChange={(e) => setDos(e.target.value)} />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">{t('campaignBriefing.field.donts')}</label>
            <textarea className={textareaClass} rows={4} value={donts} onChange={(e) => setDonts(e.target.value)} />
          </div>
        </div>

        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-1">
            <label className="text-sm font-medium">{t('campaignBriefing.field.hashtags')}</label>
            <Input value={hashtags} onChange={(e) => setHashtags(e.target.value)} placeholder="#marca #verao" />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">{t('campaignBriefing.field.mentions')}</label>
            <Input value={mentions} onChange={(e) => setMentions(e.target.value)} placeholder="@marca" />
          </div>
        </div>

        <div className="space-y-1">
          <label className="text-sm font-medium">{t('campaignBriefing.field.referenceLinks')}</label>
          <textarea className={textareaClass} rows={2} value={referenceLinks} onChange={(e) => setReferenceLinks(e.target.value)} placeholder="https://..." />
        </div>

        <div className="flex justify-end">
          <Button size="sm" disabled={loading || saving} onClick={() => void handleSave()}>{saving ? t('common.action.saving') : t('common.action.save')}</Button>
        </div>
      </CardContent>
    </Card>
  )
}
