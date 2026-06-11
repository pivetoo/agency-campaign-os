using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDocuments;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using CampaignCreatorStatusEntity = AgencyCampaign.Domain.Entities.CampaignCreatorStatus;
using AgencyCampaign.Infrastructure.Clients;
using Archon.Application.MultiTenancy;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignDocumentService : CrudService<CampaignDocument>, ICampaignDocumentService
    {
        private static readonly Regex PlaceholderRegex = new(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled);

        private readonly IntegrationPlatformClient integrationPlatformClient;
        private readonly IIntegrationCapabilityService integrationCapabilityService;
        private readonly ISignedDocumentDownloader signedDocumentDownloader;
        private readonly IContentFileStorage fileStorage;
        private readonly IPlanGate planGate;
        private readonly ITenantContext? tenantContext;
        private readonly ILogger<CampaignDocumentService>? logger;

        public CampaignDocumentService(DbContext dbContext, IntegrationPlatformClient integrationPlatformClient, IIntegrationCapabilityService integrationCapabilityService, ISignedDocumentDownloader signedDocumentDownloader, IContentFileStorage fileStorage, IPlanGate planGate, ITenantContext? tenantContext = null, ILogger<CampaignDocumentService>? logger = null) : base(dbContext)
        {
            this.integrationPlatformClient = integrationPlatformClient;
            this.integrationCapabilityService = integrationCapabilityService;
            this.signedDocumentDownloader = signedDocumentDownloader;
            this.fileStorage = fileStorage;
            this.planGate = planGate;
            this.tenantContext = tenantContext;
            this.logger = logger;
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
                throw new InvalidOperationException("record.notFound");
            }

            Dictionary<string, object?> values = await BuildTemplateVariables(request.CampaignId, request.CampaignCreatorId, request.Overrides, cancellationToken);
            EnsureNoUnknownVariables(template.Body, values);
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
                throw new InvalidOperationException("request.route.idMismatch");
            }

            CampaignDocument? document = await DbContext.Set<CampaignDocument>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException("record.notFound");
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
                throw new InvalidOperationException("record.notFound");
            }

            ResolvedCapability capability = await integrationCapabilityService.ResolveForExecution(Application.Catalogs.IntegrationIntents.CampaignDocumentSendEmail, cancellationToken);

            string attachmentUrl = !string.IsNullOrWhiteSpace(document.SignedDocumentUrl)
                ? document.SignedDocumentUrl!
                : document.DocumentUrl ?? string.Empty;

            object[] attachments = !string.IsNullOrWhiteSpace(attachmentUrl)
                ? [new { filename = $"{document.Title}.pdf", url = attachmentUrl }]
                : [];

            string payload = JsonSerializer.Serialize(new
            {
                campaignDocumentId = document.Id,
                title = document.Title,
                to = new[] { request.RecipientEmail },
                subject = request.Subject,
                body = request.Body ?? string.Empty,
                isHtml = true,
                attachments,
            });

            try
            {
                await integrationPlatformClient.EnqueueServiceAsync(capability.ServiceContractIdentifier, capability.ConnectorId, payload, priority: 1, ct: cancellationToken);
            }
            catch (Exception ex)
            {
                document.RegisterEvent(CampaignDocumentEventType.ProviderSyncError, ex.Message);
                await Update(document, cancellationToken);
                throw;
            }

            document.MarkSent(request.RecipientEmail, request.Subject, request.Body, DateTimeOffset.UtcNow);
            document.RegisterEvent(CampaignDocumentEventType.Sent, $"Enviado por email para {request.RecipientEmail} via conector {capability.ConnectorId}.");

            CampaignDocument? result = await Update(document, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDocumentById(result.Id, cancellationToken) ?? result;
        }

        public async Task<CampaignDocument> SendDocumentWhatsapp(long id, SendCampaignDocumentWhatsappRequest request, CancellationToken cancellationToken = default)
        {
            CampaignDocument? document = await DbContext.Set<CampaignDocument>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            ResolvedCapability capability = await integrationCapabilityService.ResolveForExecution(Application.Catalogs.IntegrationIntents.CampaignDocumentSendWhatsapp, cancellationToken);

            string payload = JsonSerializer.Serialize(new
            {
                campaignDocumentId = document.Id,
                title = document.Title,
                to = request.RecipientPhone,
                channel = "whatsapp",
                body = request.Body,
            });

            try
            {
                await integrationPlatformClient.EnqueueServiceAsync(capability.ServiceContractIdentifier, capability.ConnectorId, payload, priority: 1, ct: cancellationToken);
            }
            catch (Exception ex)
            {
                document.RegisterEvent(CampaignDocumentEventType.ProviderSyncError, ex.Message);
                await Update(document, cancellationToken);
                throw;
            }

            document.MarkSent(request.RecipientPhone, $"WhatsApp: {document.Title}", request.Body, DateTimeOffset.UtcNow);
            document.RegisterEvent(CampaignDocumentEventType.Sent, $"Enviado por WhatsApp para {request.RecipientPhone} via conector {capability.ConnectorId}.");

            CampaignDocument? result = await Update(document, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDocumentById(result.Id, cancellationToken) ?? result;
        }

        public async Task<CampaignDocument> SendForSignature(long id, SendCampaignDocumentForSignatureRequest request, CancellationToken cancellationToken = default)
        {
            await planGate.RequireFeatureAsync(PlanFeature.DigitalSignature, cancellationToken);

            CampaignDocument? document = await DbContext.Set<CampaignDocument>()
                .AsTracking()
                .Include(item => item.Signatures)
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            if (string.IsNullOrWhiteSpace(document.Body) && string.IsNullOrWhiteSpace(document.DocumentUrl))
            {
                throw new InvalidOperationException("campaignDocument.body.required");
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

            ResolvedCapability capability = await integrationCapabilityService.ResolveForExecution(Application.Catalogs.IntegrationIntents.CampaignDocumentSendSignature, cancellationToken);

            // O provedor de assinatura precisa dos bytes do documento (PDF). Usa o arquivo enviado
            // (DocumentUrl) quando disponivel; senao renderiza o corpo HTML para PDF.
            byte[]? documentBytes = null;
            if (!string.IsNullOrWhiteSpace(document.DocumentUrl))
            {
                documentBytes = await signedDocumentDownloader.DownloadAsync(document.DocumentUrl!, cancellationToken);
            }

            if ((documentBytes is null || documentBytes.Length == 0) && !string.IsNullOrWhiteSpace(document.Body))
            {
                documentBytes = await PuppeteerPdfRenderer.RenderToPdfAsync(document.Body!);
            }

            if (documentBytes is null || documentBytes.Length == 0)
            {
                throw new InvalidOperationException("campaignDocument.content.required");
            }

            string payload = JsonSerializer.Serialize(new
            {
                campaignDocumentId = document.Id,
                title = document.Title,
                documentBase64 = Convert.ToBase64String(documentBytes),
                documentName = $"{document.Title}.pdf",
                mimeType = "application/pdf",
                message = document.EmailBody,
                // Token tenant-scoped para o pipeline ECOAR na URL do callback
                // (/api/campaigndocuments/provider-callback/{callbackToken}), permitindo a resolucao
                // de tenant em requisicoes anonimas multi-tenant.
                callbackToken = PublicLinkToken.Compose(tenantContext?.TenantId, document.Id.ToString(CultureInfo.InvariantCulture)),
                signers = request.Signers.Select(item => new
                {
                    role = item.Role.ToString(),
                    name = item.Name,
                    email = item.Email,
                    documentNumber = item.DocumentNumber,
                }),
            });

            try
            {
                await integrationPlatformClient.EnqueueServiceAsync(capability.ServiceContractIdentifier, capability.ConnectorId, payload, priority: 1, ct: cancellationToken);
            }
            catch (Exception ex)
            {
                document.RegisterEvent(CampaignDocumentEventType.ProviderSyncError, ex.Message);
                await Update(document, cancellationToken);
                throw;
            }

            document.MarkReadyToSend();
            document.RegisterEvent(CampaignDocumentEventType.Sent, $"Enviado para assinatura via conector {capability.ConnectorId}.");

            string? contentHash = document.SealContentForSignature();
            if (contentHash is not null)
            {
                document.RegisterEvent(CampaignDocumentEventType.ContentSealed, $"Hash SHA-256 do conteudo selado para assinatura: {contentHash}");
            }

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
            }

            // Correlacao inicial: o primeiro callback do envio carrega o nosso campaignDocumentId junto do
            // providerDocumentId (uuid do envelope). Permite vincular o documento ao provedor mesmo antes de
            // ter o ProviderDocumentId gravado; os callbacks seguintes ja localizam por Provider + ProviderDocumentId.
            if (document is null && request.CampaignDocumentId.HasValue)
            {
                document = await DbContext.Set<CampaignDocument>()
                    .AsTracking()
                    .Include(item => item.Signatures)
                    .Include(item => item.Events)
                    .FirstOrDefaultAsync(item => item.Id == request.CampaignDocumentId.Value, cancellationToken);
            }

            if (document is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            if (string.IsNullOrWhiteSpace(document.ProviderDocumentId) && !string.IsNullOrWhiteSpace(request.ProviderDocumentId))
            {
                document.AttachToProvider(request.Provider, request.ProviderDocumentId);
            }

            DateTimeOffset occurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow;
            string normalizedEvent = request.EventType.Trim().ToLowerInvariant();

            // Captura a URL de assinatura do signatario em qualquer evento que a carregue (tipicamente
            // "sent"/"signer.created"): habilita o botao "Assinar" no portal do creator (C2).
            if (!string.IsNullOrWhiteSpace(request.SigningUrl)
                && (!string.IsNullOrWhiteSpace(request.SignerEmail) || !string.IsNullOrWhiteSpace(request.ProviderSignerId)))
            {
                document.AssignSignerSigningUrl(request.SignerEmail, request.ProviderSignerId, request.SigningUrl!);
            }

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

            await TryStoreSignedCopyAsync(document, cancellationToken);

            CampaignDocument? result = await Update(document, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDocumentById(result.Id, cancellationToken) ?? result;
        }

        // Lastro/durabilidade (D1i): assim que o documento esta assinado e ha URL do provedor, baixa
        // (best-effort) o PDF e guarda nossa copia privada. Falha aqui nunca quebra o callback de
        // assinatura - a assinatura ja ocorreu e a URL do provedor continua valida; a copia e secundaria.
        private async Task TryStoreSignedCopyAsync(CampaignDocument document, CancellationToken cancellationToken)
        {
            if (document.Status != CampaignDocumentStatus.Signed
                || string.IsNullOrWhiteSpace(document.SignedDocumentUrl)
                || !string.IsNullOrWhiteSpace(document.SignedDocumentStorageKey))
            {
                return;
            }

            try
            {
                byte[]? bytes = await signedDocumentDownloader.DownloadAsync(document.SignedDocumentUrl!, cancellationToken);
                if (bytes is null || bytes.Length == 0)
                {
                    return;
                }

                await using MemoryStream stream = new(bytes);
                ContentFileResult stored = await fileStorage.SaveAsync(document.Id, stream, $"{document.Title}-assinado.pdf", "application/pdf", cancellationToken);
                document.AttachSignedDocumentCopy(stored.StorageKey);
            }
            catch (Exception exception)
            {
                logger?.LogWarning(exception, "Failed to store private copy of signed document {DocumentId}.", document.Id);
            }
        }

        public async Task<CampaignDocument> MarkAsSigned(long id, MarkCampaignDocumentSignedRequest request, CancellationToken cancellationToken = default)
        {
            CampaignDocument? document = await DbContext.Set<CampaignDocument>()
                .AsTracking()
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException("record.notFound");
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

        public async Task<DocumentIntegrityStatus> VerifyContentIntegrity(long id, CancellationToken cancellationToken = default)
        {
            CampaignDocument? document = await DbContext.Set<CampaignDocument>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            return document.VerifyContentIntegrity();
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

            // Consolidacao do briefing (D5): o briefing estruturado e a fonte de verdade; o campo
            // livre legado da campanha so e usado como fallback para campanhas antigas sem estrutura.
            CampaignBriefing? structuredBriefing = await DbContext.Set<CampaignBriefing>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.CampaignId == campaignId, cancellationToken);
            string? briefingText = !string.IsNullOrWhiteSpace(structuredBriefing?.KeyMessage)
                ? structuredBriefing!.KeyMessage
                : campaign?.Briefing;

            CultureInfo culture = CultureInfo.GetCultureInfo("pt-BR");
            Dictionary<string, object?> values = new(StringComparer.OrdinalIgnoreCase)
            {
                ["today"] = DateTimeOffset.Now.ToString("dd/MM/yyyy", culture),
                ["campaignId"] = campaign?.Id,
                ["campaignName"] = campaign?.Name,
                ["campaignDescription"] = campaign?.Description,
                ["campaignObjective"] = campaign?.Objective,
                ["campaignBriefing"] = briefingText,
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

        private static void EnsureNoUnknownVariables(string template, IReadOnlyDictionary<string, object?> values)
        {
            List<string> unknown = PlaceholderRegex.Matches(template)
                .Select(match => match.Groups[1].Value)
                .Where(name => !values.ContainsKey(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (unknown.Count > 0)
            {
                throw new InvalidOperationException($"campaignDocument.template.unknownVariables: {string.Join(", ", unknown)}");
            }
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
                throw new InvalidOperationException("record.notFound");
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
                throw new InvalidOperationException("record.notFound");
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
