export type ContentVersionStatus = 1 | 2 | 3 | 4
export type ReviewParticipant = 1 | 2 | 3
export type ContentAssetType = 1 | 2
export type ReviewCommentVisibility = 1 | 2

export interface ContentAsset { type: ContentAssetType; url: string; fileName?: string }
export interface ContentVersion { id: number; roundNumber: number; submittedByRole: ReviewParticipant; submittedByName: string; note?: string; status: ContentVersionStatus; createdAt: string; assets: ContentAsset[] }
export interface ReviewComment { id: number; versionId?: number; authorRole: ReviewParticipant; authorName: string; body: string; visibility: ReviewCommentVisibility; createdAt: string }
export interface ContentReview { deliverableId: number; versions: ContentVersion[]; comments: ReviewComment[] }
export interface ContentAssetInput { type: ContentAssetType; url: string; fileName?: string; contentType?: string }
