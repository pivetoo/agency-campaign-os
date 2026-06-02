using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.ContentReview;
using AgencyCampaign.Application.Requests.CreatorPortal;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CreatorPortalService : ICreatorPortalService
    {
        private readonly DbContext dbContext;
        private readonly ICreatorAccessTokenService accessTokenService;
        private readonly IContentReviewService contentReview;

        public CreatorPortalService(DbContext dbContext, ICreatorAccessTokenService accessTokenService, IContentReviewService contentReview)
        {
            this.dbContext = dbContext;
            this.accessTokenService = accessTokenService;
            this.contentReview = contentReview;
        }

        public async Task<CreatorPortalContext> ResolveContext(string token, CancellationToken cancellationToken = default)
        {
            CreatorAccessToken? accessToken = await accessTokenService.ValidateToken(token, cancellationToken);
            if (accessToken is null || accessToken.Creator is null)
            {
                throw new InvalidOperationException("creatorPortal.token.invalid");
            }
            return new CreatorPortalContext(accessToken.Creator, accessToken);
        }

        public async Task<List<CampaignCreator>> GetCampaigns(long creatorId, CancellationToken cancellationToken = default)
        {
            List<CampaignCreator> campaigns = await dbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                    .ThenInclude(item => item!.Brand)
                .Include(item => item.CampaignCreatorStatus)
                .Where(item => item.CreatorId == creatorId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);

            foreach (CampaignCreator campaign in campaigns)
            {
                campaign.RedactAgencyFee();
            }

            return campaigns;
        }

        public async Task<CampaignBriefingModel?> GetCampaignBriefing(long creatorId, long campaignId, CancellationToken cancellationToken = default)
        {
            bool participates = await dbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .AnyAsync(item => item.CreatorId == creatorId && item.CampaignId == campaignId, cancellationToken);

            if (!participates)
            {
                throw new InvalidOperationException("creatorPortal.token.invalid");
            }

            CampaignBriefing? briefing = await dbContext.Set<CampaignBriefing>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.CampaignId == campaignId, cancellationToken);

            return briefing is null ? null : CampaignBriefingModel.FromEntity(briefing);
        }

        public async Task<List<CampaignDocument>> GetDocuments(long creatorId, CancellationToken cancellationToken = default)
        {
            List<CampaignDocument> documents = await dbContext.Set<CampaignDocument>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                .Include(item => item.CampaignCreator)
                .Include(item => item.Signatures)
                .Where(item => item.CampaignCreator != null && item.CampaignCreator.CreatorId == creatorId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);

            string? creatorEmail = await dbContext.Set<Creator>()
                .AsNoTracking()
                .Where(item => item.Id == creatorId)
                .Select(item => item.Email)
                .FirstOrDefaultAsync(cancellationToken);

            foreach (CampaignDocument document in documents)
            {
                document.CampaignCreator?.RedactAgencyFee();

                // Seguranca: o creator so pode ver a URL de assinatura DELE - apagar a dos demais
                // signatarios (ex. a marca) para nao permitir assinar no lugar de outro.
                foreach (CampaignDocumentSignature signature in document.Signatures)
                {
                    if (string.IsNullOrWhiteSpace(creatorEmail)
                        || !string.Equals(signature.SignerEmail, creatorEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        signature.AssignSigningUrl(null);
                    }
                }
            }

            return documents;
        }

        public async Task<List<CampaignDeliverable>> GetDeliverables(long creatorId, CancellationToken cancellationToken = default)
        {
            List<CampaignDeliverable> deliverables = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Include(item => item.Platform)
                .Include(item => item.Campaign)
                .Include(item => item.CampaignCreator)
                .Where(item => item.CampaignCreator != null && item.CampaignCreator.CreatorId == creatorId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);

            foreach (CampaignDeliverable deliverable in deliverables)
            {
                deliverable.CampaignCreator?.RedactAgencyFee();
            }

            return deliverables;
        }

        public async Task<CampaignDeliverable> SubmitInsights(long creatorId, long deliverableId, SubmitDeliverableInsightsRequest request, CancellationToken cancellationToken = default)
        {
            CampaignDeliverable? deliverable = await dbContext.Set<CampaignDeliverable>()
                .AsTracking()
                .Include(item => item.CampaignCreator)
                .FirstOrDefaultAsync(item => item.Id == deliverableId, cancellationToken);

            if (deliverable is null || deliverable.CampaignCreator is null || deliverable.CampaignCreator.CreatorId != creatorId)
            {
                throw new InvalidOperationException("record.notFound");
            }

            deliverable.RegisterCreatorInsights(request.Reach, request.Impressions, request.Saves);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (deliverable.CampaignCreator is not null)
            {
                dbContext.Entry(deliverable.CampaignCreator).State = EntityState.Detached;
                deliverable.CampaignCreator.RedactAgencyFee();
            }

            return deliverable;
        }

        public async Task<List<CreatorPayment>> GetPayments(long creatorId, CancellationToken cancellationToken = default)
        {
            List<CreatorPayment> payments = await dbContext.Set<CreatorPayment>()
                .AsNoTracking()
                .Include(item => item.CampaignCreator)
                    .ThenInclude(item => item!.Campaign)
                .Where(item => item.CreatorId == creatorId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);

            foreach (CreatorPayment payment in payments)
            {
                payment.CampaignCreator?.RedactAgencyFee();
            }

            return payments;
        }

        public async Task<Creator> UpdateBankInfo(long creatorId, UpdateCreatorBankInfoRequest request, CancellationToken cancellationToken = default)
        {
            Creator? creator = await dbContext.Set<Creator>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == creatorId, cancellationToken);

            if (creator is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            bool bankInfoChanged = creator.PixKey != request.PixKey
                || creator.PixKeyType != request.PixKeyType
                || (request.Document is not null && request.Document != creator.Document);

            creator.Update(
                creator.Name,
                creator.StageName,
                creator.Email,
                creator.Phone,
                request.Document ?? creator.Document,
                request.PixKey,
                request.PixKeyType,
                creator.PrimaryNiche,
                creator.City,
                creator.State,
                creator.Notes,
                creator.DefaultAgencyFeePercent,
                creator.IsActive);

            if (bankInfoChanged)
            {
                dbContext.Set<CreatorEvent>().Add(new CreatorEvent(
                    creatorId,
                    CreatorEventType.BankInfoChanged,
                    "Dados bancarios (Pix) alterados via portal pelo creator."));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return creator;
        }

        public async Task<CreatorPayment> UploadInvoice(long creatorId, UploadInvoiceRequest request, CancellationToken cancellationToken = default)
        {
            CreatorPayment? payment = await dbContext.Set<CreatorPayment>()
                .AsTracking()
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Id == request.CreatorPaymentId && item.CreatorId == creatorId, cancellationToken);

            if (payment is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            payment.AttachInvoice(request.InvoiceNumber, request.InvoiceUrl, request.IssuedAt);
            payment.RegisterEvent(CreatorPaymentEventType.InvoiceAttached, $"NF anexada via portal pelo creator.");

            await dbContext.SaveChangesAsync(cancellationToken);
            return payment;
        }

        public async Task<ContentReviewModel> GetDeliverableReview(long creatorId, long deliverableId, CancellationToken cancellationToken = default)
        {
            await EnsureDeliverableBelongsToCreator(creatorId, deliverableId, cancellationToken);
            ContentReviewModel full = await contentReview.GetByDeliverable(deliverableId, cancellationToken);
            return FilterShared(full);
        }

        public async Task<ContentReviewModel> SubmitContentVersion(long creatorId, long deliverableId, AddContentVersionRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureDeliverableBelongsToCreator(creatorId, deliverableId, cancellationToken);
            string name = await CreatorName(creatorId, cancellationToken);
            ContentReviewModel full = await contentReview.AddVersion(deliverableId, ReviewParticipant.Creator, name, request, cancellationToken);
            return FilterShared(full);
        }

        public async Task<ContentReviewModel> AddReviewComment(long creatorId, long deliverableId, string body, CancellationToken cancellationToken = default)
        {
            await EnsureDeliverableBelongsToCreator(creatorId, deliverableId, cancellationToken);
            string name = await CreatorName(creatorId, cancellationToken);
            ContentReviewModel full = await contentReview.AddComment(deliverableId, ReviewParticipant.Creator, name, new AddReviewCommentRequest(null, body, ReviewCommentVisibility.Shared), cancellationToken);
            return FilterShared(full);
        }

        public Task EnsureCreatorOwnsDeliverable(long creatorId, long deliverableId, CancellationToken cancellationToken = default)
        {
            return EnsureDeliverableBelongsToCreator(creatorId, deliverableId, cancellationToken);
        }

        private async Task EnsureDeliverableBelongsToCreator(long creatorId, long deliverableId, CancellationToken cancellationToken)
        {
            bool exists = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == deliverableId && item.CampaignCreator != null && item.CampaignCreator.CreatorId == creatorId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("record.notFound");
            }
        }

        private async Task<string> CreatorName(long creatorId, CancellationToken cancellationToken)
        {
            Creator? creator = await dbContext.Set<Creator>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == creatorId, cancellationToken);

            return creator?.StageName ?? creator?.Name ?? string.Empty;
        }

        private static ContentReviewModel FilterShared(ContentReviewModel model)
        {
            return new ContentReviewModel
            {
                DeliverableId = model.DeliverableId,
                Versions = model.Versions,
                Comments = model.Comments.Where(item => item.Visibility == ReviewCommentVisibility.Shared).ToList()
            };
        }
    }
}
