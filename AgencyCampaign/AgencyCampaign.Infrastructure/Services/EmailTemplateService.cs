using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.EmailTemplates;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Clients;
using Archon.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class EmailTemplateService : IEmailTemplateService
    {
        private readonly DbContext dbContext;
        private readonly ICurrentUser currentUser;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public EmailTemplateService(DbContext dbContext, ICurrentUser currentUser, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.currentUser = currentUser;
            this.localizer = localizer;
        }

        public async Task<IReadOnlyCollection<EmailTemplateModel>> GetAll(bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<EmailTemplate> query = dbContext.Set<EmailTemplate>().AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            return await query
                .OrderBy(item => item.EventType)
                .ThenBy(item => item.Name)
                .Select(item => Map(item))
                .ToArrayAsync(cancellationToken);
        }

        public async Task<EmailTemplateModel?> GetById(long id, CancellationToken cancellationToken = default)
        {
            EmailTemplate? template = await dbContext.Set<EmailTemplate>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return template is null ? null : Map(template);
        }

        public async Task<EmailTemplateModel> Create(CreateEmailTemplateRequest request, CancellationToken cancellationToken = default)
        {
            EmailTemplate template = new(request.Name, request.EventType, request.Subject, request.HtmlBody, currentUser.UserId, currentUser.UserName);
            dbContext.Set<EmailTemplate>().Add(template);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(template);
        }

        public async Task<EmailTemplateModel> Update(long id, UpdateEmailTemplateRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            EmailTemplate? template = await dbContext.Set<EmailTemplate>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (template is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            template.Update(request.Name, request.EventType, request.Subject, request.HtmlBody, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(template);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            EmailTemplate? template = await dbContext.Set<EmailTemplate>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (template is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            dbContext.Set<EmailTemplate>().Remove(template);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static EmailTemplateModel Map(EmailTemplate template) => new()
        {
            Id = template.Id,
            Name = template.Name,
            EventType = template.EventType,
            Subject = template.Subject,
            HtmlBody = template.HtmlBody,
            IsActive = template.IsActive,
            CreatedByUserName = template.CreatedByUserName,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    public sealed class EmailService : IEmailService
    {
        private static readonly Regex PlaceholderRegex = new(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled);

        private readonly DbContext dbContext;
        private readonly IntegrationPlatformClient integrationPlatformClient;

        public EmailService(DbContext dbContext, IntegrationPlatformClient integrationPlatformClient)
        {
            this.dbContext = dbContext;
            this.integrationPlatformClient = integrationPlatformClient;
        }

        public async Task SendForEvent(EmailEventType eventType, IReadOnlyCollection<string> recipients, IReadOnlyDictionary<string, object?> payload, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(recipients);
            ArgumentNullException.ThrowIfNull(payload);

            if (recipients.Count == 0)
            {
                return;
            }

            EmailTemplate? template = await dbContext.Set<EmailTemplate>()
                .AsNoTracking()
                .Where(item => item.EventType == eventType && item.IsActive)
                .OrderByDescending(item => item.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (template is null)
            {
                Console.WriteLine($"[EmailService] no active template for event {eventType}, skipping.");
                return;
            }

            AgencySettings? settings = await dbContext.Set<AgencySettings>()
                .AsNoTracking()
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (settings?.DefaultEmailConnectorId is null || settings.DefaultEmailPipelineId is null)
            {
                Console.WriteLine($"[EmailService] email connector/pipeline not configured, skipping event {eventType}.");
                return;
            }

            string subject = Render(template.Subject, payload);
            string htmlBody = Render(template.HtmlBody, payload);

            string enqueuePayload = JsonSerializer.Serialize(new
            {
                to = string.Join(",", recipients),
                subject,
                body = htmlBody
            });

            EnqueuePipelineRequest request = new()
            {
                ConnectorId = settings.DefaultEmailConnectorId.Value,
                PipelineId = settings.DefaultEmailPipelineId.Value,
                Payload = enqueuePayload,
                Priority = 0
            };

            await integrationPlatformClient.EnqueuePipelineAsync(request, cancellationToken);
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
    }
}
