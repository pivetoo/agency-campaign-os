using AgencyCampaign.Application.Requests.BrandContacts;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Services
{
    public interface IBrandContactService
    {
        Task<IReadOnlyList<BrandContactModel>> GetByBrand(long brandId, CancellationToken cancellationToken = default);

        Task<BrandContactModel> Add(long brandId, AddBrandContactRequest request, CancellationToken cancellationToken = default);

        Task<BrandContactModel> Update(long contactId, UpdateBrandContactRequest request, CancellationToken cancellationToken = default);

        Task Delete(long contactId, CancellationToken cancellationToken = default);

        Task<BrandContactModel> SetPrimary(long contactId, CancellationToken cancellationToken = default);
    }

    public sealed class BrandContactModel
    {
        public long Id { get; init; }
        public long BrandId { get; init; }
        public BrandContactType Type { get; init; }
        public string Value { get; init; } = string.Empty;
        public string? Label { get; init; }
        public bool IsPrimary { get; init; }

        public static BrandContactModel FromEntity(BrandContact entity) => new()
        {
            Id = entity.Id,
            BrandId = entity.BrandId,
            Type = entity.Type,
            Value = entity.Value,
            Label = entity.Label,
            IsPrimary = entity.IsPrimary
        };
    }
}
