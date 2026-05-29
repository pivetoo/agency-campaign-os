import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, Card, CardContent, useApi, useI18n } from 'archon-ui'
import { campaignDeliverableService } from '../../../services/campaignDeliverableService'
import type { CampaignDeliverable } from '../../../types/campaignDeliverable'
import CampaignDeliverableCalendar from '../../../components/CampaignDeliverableCalendar'

export default function OperationsCalendar() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [deliverables, setDeliverables] = useState<CampaignDeliverable[]>([])
  const { execute: fetchAll } = useApi<CampaignDeliverable[]>({ showErrorMessage: true })

  const load = () => {
    void fetchAll(() => campaignDeliverableService.getForCalendar()).then((result) => {
      if (result) setDeliverables(result)
    })
  }

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <PageLayout title={t('campaignCalendar.pageTitle')} subtitle={t('campaignCalendar.pageSubtitle')} showDefaultActions={false} onRefresh={load}>
      <Card>
        <CardContent className="pt-5 pb-5">
          <CampaignDeliverableCalendar
            deliverables={deliverables}
            showCampaignName
            onSelectDeliverable={(deliverable) => navigate(`/campanhas/${deliverable.campaignId}`)}
          />
        </CardContent>
      </Card>
    </PageLayout>
  )
}
