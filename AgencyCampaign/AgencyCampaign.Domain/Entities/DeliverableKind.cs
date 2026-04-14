using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class DeliverableKind : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public int DisplayOrder { get; private set; }

        private DeliverableKind()
        {
        }

        public DeliverableKind(string name, int displayOrder = 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            DisplayOrder = displayOrder;
            Name = name.Trim();
        }

        public void Update(string name, int displayOrder, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name.Trim();
            DisplayOrder = displayOrder;
            IsActive = isActive;
        }
    }
}
