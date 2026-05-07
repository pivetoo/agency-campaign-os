using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.EmailTemplates;

namespace AgencyCampaign.Application.Services
{
    public interface IEmailTemplateService
    {
        Task<IReadOnlyCollection<EmailTemplateModel>> GetAll(bool includeInactive, CancellationToken cancellationToken = default);

        Task<EmailTemplateModel?> GetById(long id, CancellationToken cancellationToken = default);

        Task<EmailTemplateModel> Create(CreateEmailTemplateRequest request, CancellationToken cancellationToken = default);

        Task<EmailTemplateModel> Update(long id, UpdateEmailTemplateRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }

    public interface IEmailService
    {
        Task SendForEvent(AgencyCampaign.Domain.ValueObjects.EmailEventType eventType, IReadOnlyCollection<string> recipients, IReadOnlyDictionary<string, object?> payload, CancellationToken cancellationToken = default);
    }
}
