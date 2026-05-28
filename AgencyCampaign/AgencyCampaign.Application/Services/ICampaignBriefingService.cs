using AgencyCampaign.Application.Requests.CampaignBriefings;
using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Application.Services
{
    public interface ICampaignBriefingService
    {
        Task<CampaignBriefingModel?> GetByCampaign(long campaignId, CancellationToken cancellationToken = default);

        Task<CampaignBriefingModel> Upsert(long campaignId, UpsertCampaignBriefingRequest request, CancellationToken cancellationToken = default);
    }

    public sealed class CampaignBriefingModel
    {
        public long CampaignId { get; init; }
        public string? KeyMessage { get; init; }
        public string? Dos { get; init; }
        public string? Donts { get; init; }
        public string? Hashtags { get; init; }
        public string? Mentions { get; init; }
        public string? ReferenceLinks { get; init; }

        public static CampaignBriefingModel FromEntity(CampaignBriefing entity) => new()
        {
            CampaignId = entity.CampaignId,
            KeyMessage = entity.KeyMessage,
            Dos = entity.Dos,
            Donts = entity.Donts,
            Hashtags = entity.Hashtags,
            Mentions = entity.Mentions,
            ReferenceLinks = entity.ReferenceLinks
        };
    }
}
