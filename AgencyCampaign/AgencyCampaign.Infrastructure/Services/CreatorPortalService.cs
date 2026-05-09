using AgencyCampaign.Application.Localization;
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
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public CreatorPortalService(DbContext dbContext, ICreatorAccessTokenService accessTokenService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.accessTokenService = accessTokenService;
            this.localizer = localizer;
        }

        public async Task<CreatorPortalContext> ResolveContext(string token, CancellationToken cancellationToken = default)
        {
            CreatorAccessToken? accessToken = await accessTokenService.ValidateToken(token, cancellationToken);
            if (accessToken is null || accessToken.Creator is null)
            {
                throw new InvalidOperationException(localizer["creatorPortal.token.invalid"]);
            }
            return new CreatorPortalContext(accessToken.Creator, accessToken);
        }

        public async Task<List<CampaignCreator>> GetCampaigns(long creatorId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                    .ThenInclude(item => item!.Brand)
                .Include(item => item.CampaignCreatorStatus)
                .Where(item => item.CreatorId == creatorId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<CampaignDocument>> GetDocuments(long creatorId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Set<CampaignDocument>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                .Include(item => item.CampaignCreator)
                .Include(item => item.Signatures)
                .Where(item => item.CampaignCreator != null && item.CampaignCreator.CreatorId == creatorId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<CreatorPayment>> GetPayments(long creatorId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Set<CreatorPayment>()
                .AsNoTracking()
                .Include(item => item.CampaignCreator)
                    .ThenInclude(item => item!.Campaign)
                .Where(item => item.CreatorId == creatorId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<Creator> UpdateBankInfo(long creatorId, UpdateCreatorBankInfoRequest request, CancellationToken cancellationToken = default)
        {
            Creator? creator = await dbContext.Set<Creator>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == creatorId, cancellationToken);

            if (creator is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

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
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            payment.AttachInvoice(request.InvoiceNumber, request.InvoiceUrl, request.IssuedAt);
            payment.RegisterEvent(CreatorPaymentEventType.InvoiceAttached, $"NF anexada via portal pelo creator.");

            await dbContext.SaveChangesAsync(cancellationToken);
            return payment;
        }
    }
}
