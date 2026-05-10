using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDocuments;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using CampaignCreatorStatusEntity = AgencyCampaign.Domain.Entities.CampaignCreatorStatus;
using AgencyCampaign.Infrastructure.Clients;
using AgencyCampaign.Infrastructure.Options;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignDocumentService : CrudService<CampaignDocument>, ICampaignDocumentService
    {
        private static readonly Regex PlaceholderRegex = new(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled);

        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly DocumentEmailOptions emailOptions;
        private readonly IntegrationPlatformClient integrationPlatformClient;

        public CampaignDocumentService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer, IOptions<DocumentEmailOptions> emailOptions, IntegrationPlatformClient integrationPlatformClient) : base(dbContext)
        {
            this.localizer = localizer;
            this.emailOptions = emailOptions.Value;
            this.integrationPlatformClient = integrationPlatformClient;
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

        public async Task<CampaignDocument?> GetByProviderDocumentId(string provider, string providerDocumentId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);
            ArgumentException.ThrowIfNullOrWhiteSpace(providerDocumentId);

            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Provider == provider && item.ProviderDocumentId == providerDocumentId, cancellationToken);
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

            // Registrar eventos somente apos o Insert: CampaignDocumentEvent exige campaignDocumentId > 0,
            // que so e atribuido pelo SaveChanges do Insert.
            await RegisterCreationEventsAsync(document.Id, "Documento criado manualmente.", cancellationToken);

            return await GetDocumentById(document.Id, cancellationToken) ?? document;
        }

        public async Task<CampaignDocument> GenerateFromTemplate(GenerateCampaignDocumentFromTemplateRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureReferencesExist(request.CampaignId, request.CampaignCreatorId, cancellationToken);

            CampaignDocumentTemplate? template = await DbContext.Set<CampaignDocumentTemplate>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.TemplateId, cancellationToken);

            if (template is null || !template.IsActive)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            Dictionary<string, object?> values = await BuildTemplateVariables(request.CampaignId, request.CampaignCreatorId, request.Overrides, cancellationToken);
            string body = Render(template.Body, values);

            CampaignDocument document = new(
                request.CampaignId,
                template.DocumentType,
                request.Title,
                documentUrl: null,
                notes: null,
                campaignCreatorId: request.CampaignCreatorId,
                templateId: template.Id,
                body: body);

            document.MarkReadyToSend();

            bool success = await Insert(cancellationToken, document);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            // Registrar eventos somente apos o Insert: CampaignDocumentEvent exige campaignDocumentId > 0,
            // que so e atribuido pelo SaveChanges do Insert.
            await RegisterCreationEventsAsync(document.Id, $"Documento gerado a partir do template '{template.Name}'.", cancellationToken);

            return await GetDocumentById(document.Id, cancellationToken) ?? document;
        }

        private async Task RegisterCreationEventsAsync(long documentId, string createdDescription, CancellationToken cancellationToken)
        {
            CampaignDocument? tracked = await DbContext.Set<CampaignDocument>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == documentId, cancellationToken);

            if (tracked is null)
            {
                return;
            }

            tracked.RegisterEvent(CampaignDocumentEventType.Created, createdDescription);
            tracked.RegisterEvent(CampaignDocumentEventType.ReadyToSend);
            await DbContext.SaveChangesAsync(cancellationToken);
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
            document.RegisterEvent(CampaignDocumentEventType.Sent, $"Enviado por email para {request.RecipientEmail}.");

            CampaignDocument? result = await Update(document, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDocumentById(result.Id, cancellationToken) ?? result;
        }

        public async Task<CampaignDocument> SendForSignature(long id, SendCampaignDocumentForSignatureRequest request, CancellationToken cancellationToken = default)
        {
            CampaignDocument? document = await DbContext.Set<CampaignDocument>()
                .AsTracking()
                .Include(item => item.Signatures)
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            if (string.IsNullOrWhiteSpace(document.Body) && string.IsNullOrWhiteSpace(document.DocumentUrl))
            {
                throw new InvalidOperationException(localizer["campaignDocument.body.required"]);
            }

            foreach (SendCampaignDocumentForSignatureRequest.SignerInput signer in request.Signers)
            {
                bool alreadyExists = document.Signatures.Any(item =>
                    string.Equals(item.SignerEmail, signer.Email, StringComparison.OrdinalIgnoreCase));

                if (!alreadyExists)
                {
                    document.AddSignature(signer.Role, signer.Name, signer.Email, signer.DocumentNumber);
                }
            }

            string payload = JsonSerializer.Serialize(new
            {
                campaignDocumentId = document.Id,
                title = document.Title,
                body = document.Body,
                documentUrl = document.DocumentUrl,
                signers = request.Signers.Select(item => new
                {
                    role = item.Role.ToString(),
                    name = item.Name,
                    email = item.Email,
                    documentNumber = item.DocumentNumber,
                }),
            });

            EnqueuePipelineRequest enqueueRequest = new()
            {
                ConnectorId = request.ConnectorId,
                PipelineId = request.PipelineId,
                Payload = payload,
                Priority = 1,
            };

            try
            {
                await integrationPlatformClient.EnqueuePipelineAsync(enqueueRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                document.RegisterEvent(CampaignDocumentEventType.ProviderSyncError, ex.Message);
                await Update(document, cancellationToken);
                throw;
            }

            document.MarkReadyToSend();
            document.RegisterEvent(CampaignDocumentEventType.Sent, $"Enviado para assinatura via pipeline {request.PipelineId}.");

            CampaignDocument? result = await Update(document, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDocumentById(result.Id, cancellationToken) ?? result;
        }

        public async Task<CampaignDocument> HandleProviderCallback(CampaignDocumentProviderCallbackRequest request, CancellationToken cancellationToken = default)
        {
            CampaignDocument? document = await DbContext.Set<CampaignDocument>()
                .AsTracking()
                .Include(item => item.Signatures)
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Provider == request.Provider && item.ProviderDocumentId == request.ProviderDocumentId, cancellationToken);

            if (document is null)
            {
                document = await DbContext.Set<CampaignDocument>()
                    .AsTracking()
                    .Include(item => item.Signatures)
                    .Include(item => item.Events)
                    .FirstOrDefaultAsync(item => item.ProviderDocumentId == request.ProviderDocumentId && item.Provider == null, cancellationToken);

                if (document is null)
                {
                    throw new InvalidOperationException(localizer["record.notFound"]);
                }

                document.AttachToProvider(request.Provider, request.ProviderDocumentId);
            }

            DateTimeOffset occurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow;
            string normalizedEvent = request.EventType.Trim().ToLowerInvariant();

            switch (normalizedEvent)
            {
                case "created":
                case "document.created":
                    document.RegisterEvent(CampaignDocumentEventType.Created, request.EventType, request.Metadata, occurredAt);
                    break;

                case "sent":
                case "document.sent":
                    if (document.Status == CampaignDocumentStatus.Draft || document.Status == CampaignDocumentStatus.ReadyToSend)
                    {
                        document.MarkSent(
                            request.SignerEmail ?? document.RecipientEmail ?? string.Empty,
                            document.EmailSubject ?? document.Title,
                            document.EmailBody,
                            occurredAt);
                    }
                    document.RegisterEvent(CampaignDocumentEventType.Sent, request.EventType, request.Metadata, occurredAt);
                    break;

                case "viewed":
                case "document.viewed":
                    document.MarkViewed(occurredAt);
                    document.RegisterEvent(CampaignDocumentEventType.Viewed, request.EventType, request.Metadata, occurredAt);
                    break;

                case "signed":
                case "signer.signed":
                    if (!string.IsNullOrWhiteSpace(request.SignerEmail))
                    {
                        document.RegisterSignerSigned(request.SignerEmail, occurredAt, request.IpAddress, request.UserAgent, request.ProviderSignerId);
                    }
                    document.RegisterEvent(CampaignDocumentEventType.SignerSigned, $"{request.EventType} - {request.SignerEmail}", request.Metadata, occurredAt);

                    if (document.AllSigned())
                    {
                        document.MarkSigned(occurredAt, request.SignedDocumentUrl);
                        document.RegisterEvent(CampaignDocumentEventType.Signed, "Todas as partes assinaram.", request.Metadata, occurredAt);
                        await PromoteCampaignCreatorOnSign(document, occurredAt, cancellationToken);
                    }
                    break;

                case "completed":
                case "document.signed":
                case "document.completed":
                    document.MarkSigned(occurredAt, request.SignedDocumentUrl);
                    document.RegisterEvent(CampaignDocumentEventType.Signed, request.EventType, request.Metadata, occurredAt);
                    await PromoteCampaignCreatorOnSign(document, occurredAt, cancellationToken);
                    break;

                case "rejected":
                case "document.rejected":
                    document.MarkRejected(request.Metadata);
                    document.RegisterEvent(CampaignDocumentEventType.Rejected, request.EventType, request.Metadata, occurredAt);
                    break;

                case "cancelled":
                case "document.cancelled":
                    document.MarkCancelled(request.Metadata);
                    document.RegisterEvent(CampaignDocumentEventType.Cancelled, request.EventType, request.Metadata, occurredAt);
                    break;

                default:
                    document.RegisterEvent(CampaignDocumentEventType.ProviderSyncError, $"Evento desconhecido: {request.EventType}", request.Metadata, occurredAt);
                    break;
            }

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
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            document.MarkSigned(request.SignedAt);
            document.RegisterEvent(CampaignDocumentEventType.Signed, "Marcado manualmente como assinado.", null, request.SignedAt);

            await PromoteCampaignCreatorOnSign(document, request.SignedAt, cancellationToken);

            CampaignDocument? result = await Update(document, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDocumentById(result.Id, cancellationToken) ?? result;
        }

        private async Task PromoteCampaignCreatorOnSign(CampaignDocument document, DateTimeOffset signedAt, CancellationToken cancellationToken)
        {
            if (!document.CampaignCreatorId.HasValue)
            {
                return;
            }

            CampaignCreator? campaignCreator = await DbContext.Set<CampaignCreator>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == document.CampaignCreatorId.Value, cancellationToken);

            if (campaignCreator is null || campaignCreator.CampaignCreatorStatus is null || campaignCreator.CampaignCreatorStatus.Category != CampaignCreatorStatusCategory.InProgress)
            {
                return;
            }

            CampaignCreatorStatusEntity? successStatus = await DbContext.Set<CampaignCreatorStatusEntity>()
                .AsTracking()
                .Where(s => s.IsActive && s.Category == CampaignCreatorStatusCategory.Success)
                .OrderBy(s => s.DisplayOrder)
                .FirstOrDefaultAsync(cancellationToken);

            if (successStatus is not null)
            {
                campaignCreator.ChangeStatus(successStatus, signedAt);
            }
        }

        private async Task<Dictionary<string, object?>> BuildTemplateVariables(long campaignId, long? campaignCreatorId, Dictionary<string, string>? overrides, CancellationToken cancellationToken)
        {
            Campaign? campaign = await DbContext.Set<Campaign>()
                .AsNoTracking()
                .Include(item => item.Brand)
                .FirstOrDefaultAsync(item => item.Id == campaignId, cancellationToken);

            CampaignCreator? campaignCreator = null;
            if (campaignCreatorId.HasValue)
            {
                campaignCreator = await DbContext.Set<CampaignCreator>()
                    .AsNoTracking()
                    .Include(item => item.Creator)
                    .FirstOrDefaultAsync(item => item.Id == campaignCreatorId.Value, cancellationToken);
            }

            CultureInfo culture = CultureInfo.GetCultureInfo("pt-BR");
            Dictionary<string, object?> values = new(StringComparer.OrdinalIgnoreCase)
            {
                ["today"] = DateTimeOffset.Now.ToString("dd/MM/yyyy", culture),
                ["campaignId"] = campaign?.Id,
                ["campaignName"] = campaign?.Name,
                ["campaignDescription"] = campaign?.Description,
                ["campaignObjective"] = campaign?.Objective,
                ["campaignBriefing"] = campaign?.Briefing,
                ["campaignStartDate"] = campaign?.StartsAt.ToString("dd/MM/yyyy", culture),
                ["campaignEndDate"] = campaign?.EndsAt?.ToString("dd/MM/yyyy", culture),
                ["campaignBudget"] = campaign?.Budget.ToString("C", culture),
                ["brandName"] = campaign?.Brand?.Name,
                ["brandTradeName"] = campaign?.Brand?.TradeName,
                ["brandDocument"] = campaign?.Brand?.Document,
                ["brandContactName"] = campaign?.Brand?.ContactName,
                ["brandContactEmail"] = campaign?.Brand?.ContactEmail,
                ["creatorName"] = campaignCreator?.Creator?.Name,
                ["creatorStageName"] = campaignCreator?.Creator?.StageName,
                ["creatorEmail"] = campaignCreator?.Creator?.Email,
                ["creatorDocument"] = campaignCreator?.Creator?.Document,
                ["creatorAgreedAmount"] = campaignCreator?.AgreedAmount.ToString("C", culture),
                ["creatorAgencyFeePercent"] = campaignCreator?.AgencyFeePercent.ToString("F2", culture),
                ["creatorAgencyFeeAmount"] = campaignCreator?.AgencyFeeAmount.ToString("C", culture),
                ["scopeNotes"] = campaignCreator?.Notes,
            };

            if (overrides is not null)
            {
                foreach (KeyValuePair<string, string> kv in overrides)
                {
                    values[kv.Key] = kv.Value;
                }
            }

            return values;
        }

        private static string Render(string template, IReadOnlyDictionary<string, object?> values)
        {
            return PlaceholderRegex.Replace(template, match =>
            {
                string key = match.Groups[1].Value;
                if (!values.TryGetValue(key, out object? value) || value is null)
                {
                    return string.Empty;
                }

                return value.ToString() ?? string.Empty;
            });
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
                    .ThenInclude(item => item.Creator)
                .Include(item => item.Template)
                .Include(item => item.Signatures)
                .Include(item => item.Events.OrderByDescending(evt => evt.OccurredAt));
        }
    }
}
