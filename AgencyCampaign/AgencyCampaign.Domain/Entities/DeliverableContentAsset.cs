using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class DeliverableContentAsset : Entity
    {
        public long DeliverableContentVersionId { get; private set; }
        public ContentAssetType Type { get; private set; }
        public string Url { get; private set; } = string.Empty;
        public string? FileName { get; private set; }
        public string? ContentType { get; private set; }
        public int DisplayOrder { get; private set; }

        private DeliverableContentAsset()
        {
        }

        public DeliverableContentAsset(ContentAssetType type, string url, string? fileName, string? contentType, int displayOrder)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(url);
            Type = type;
            Url = url.Trim();
            FileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim();
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
            DisplayOrder = displayOrder;
        }
    }
}
