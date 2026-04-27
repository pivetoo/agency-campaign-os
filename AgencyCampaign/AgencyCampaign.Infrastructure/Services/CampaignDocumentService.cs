using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDocuments;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using CampaignCreatorStatusEntity = AgencyCampaign.Domain.Entities.CampaignCreatorStatus;
using AgencyCampaign.Infrastructure.Options;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignDocumentService : CrudService<CampaignDocument>, ICampaignDocumentService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly DocumentEmailOptions emailOptions;

        public CampaignDocumentService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer, IOptions<DocumentEmailOptions> emailOptions) : base(dbContext)
        {
            this.localizer = localizer;
            this.emailOptions = emailOptions.Value;
        }

        public async Task<PagedResult<CampaignDocument>> GetDocuments(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<CampaignDocument?> GetDocumentById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<CampaignDocument>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.CampaignId == campaignId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<CampaignDocument> CreateDocument(CreateCampaignDocumentRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureReferencesExist(request.CampaignId, request.CampaignCreatorId, cancellationToken);

            CampaignDocument document = new(request.CampaignId, request.DocumentType, request.Title, request.DocumentUrl, request.Notes, request.CampaignCreatorId);
            document.MarkReadyToSend();

            bool success = await Insert(cancellationToken, document);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDocumentById(document.Id, cancellationToken) ?? document;
        }

        public async Task<CampaignDocument> UpdateDocument(long id, UpdateCampaignDocumentRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            CampaignDocument? document = await DbContext.Set<CampaignDocument>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            await EnsureReferencesExist(document.CampaignId, request.CampaignCreatorId, cancellationToken);

            document.Update(request.DocumentType, request.Title, request.DocumentUrl, request.Notes, request.CampaignCreatorId);

            CampaignDocument? result = await Update(document, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDocumentById(result.Id, cancellationToken) ?? result;
        }

        public async Task<CampaignDocument> SendDocumentEmail(long id, SendCampaignDocumentEmailRequest request, CancellationToken cancellationToken = default)
        {
            CampaignDocument? document = await DbContext.Set<CampaignDocument>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            EnsureEmailConfigured();

            using SmtpClient smtpClient = new(emailOptions.Host, emailOptions.Port)
            {
                EnableSsl = emailOptions.EnableSsl,
                Credentials = new NetworkCredential(emailOptions.Username, emailOptions.Password),
            };

            using MailMessage mailMessage = new()
            {
                From = new MailAddress(emailOptions.FromEmail, emailOptions.FromName),
                Subject = request.Subject,
                Body = request.Body ?? string.Empty,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(request.RecipientEmail);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            document.MarkSent(request.RecipientEmail, request.Subject, request.Body, DateTimeOffset.UtcNow);

            CampaignDocument? result = await Update(document, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDocumentById(result.Id, cancellationToken) ?? result;
        }

        public async Task<CampaignDocument> MarkAsSigned(long id, MarkCampaignDocumentSignedRequest request, CancellationToken cancellationToken = default)
        {
            CampaignDocument? document = await DbContext.Set<CampaignDocument>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            document.MarkSigned(request.SignedAt);

            if (document.CampaignCreatorId.HasValue)
            {
                CampaignCreator? campaignCreator = await DbContext.Set<CampaignCreator>()
                    .AsTracking()
                    .FirstOrDefaultAsync(item => item.Id == document.CampaignCreatorId.Value, cancellationToken);

                if (campaignCreator is not null && campaignCreator.CampaignCreatorStatus is not null && campaignCreator.CampaignCreatorStatus.Category == CampaignCreatorStatusCategory.InProgress)
                {
                    CampaignCreatorStatusEntity? successStatus = await DbContext.Set<CampaignCreatorStatusEntity>()
                        .AsTracking()
                        .Where(s => s.IsActive && s.Category == CampaignCreatorStatusCategory.Success)
                        .OrderBy(s => s.DisplayOrder)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (successStatus is not null)
                    {
                        campaignCreator.ChangeStatus(successStatus, request.SignedAt);
                    }
                }
            }

            CampaignDocument? result = await Update(document, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDocumentById(result.Id, cancellationToken) ?? result;
        }

        private async Task EnsureReferencesExist(long campaignId, long? campaignCreatorId, CancellationToken cancellationToken)
        {
            bool campaignExists = await DbContext.Set<Campaign>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignId, cancellationToken);

            if (!campaignExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            if (!campaignCreatorId.HasValue)
            {
                return;
            }

            bool campaignCreatorExists = await DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignCreatorId.Value && item.CampaignId == campaignId, cancellationToken);

            if (!campaignCreatorExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private void EnsureEmailConfigured()
        {
            if (string.IsNullOrWhiteSpace(emailOptions.Host) || string.IsNullOrWhiteSpace(emailOptions.FromEmail))
            {
                throw new InvalidOperationException(localizer["email.configuration.invalid"]);
            }
        }

        private IQueryable<CampaignDocument> QueryWithDetails()
        {
            return DbContext.Set<CampaignDocument>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                .Include(item => item.CampaignCreator!)
                    .ThenInclude(item => item.Creator);
        }
    }
}
