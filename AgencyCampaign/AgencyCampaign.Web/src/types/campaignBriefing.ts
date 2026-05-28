export interface CampaignBriefing {
  campaignId: number
  keyMessage?: string | null
  dos?: string | null
  donts?: string | null
  hashtags?: string | null
  mentions?: string | null
  referenceLinks?: string | null
}

export interface UpsertCampaignBriefingInput {
  keyMessage?: string | null
  dos?: string | null
  donts?: string | null
  hashtags?: string | null
  mentions?: string | null
  referenceLinks?: string | null
}
