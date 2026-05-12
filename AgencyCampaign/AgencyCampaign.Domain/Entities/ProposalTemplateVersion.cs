using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class ProposalTemplateVersion : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string Template { get; private set; } = string.Empty;

        public bool IsActive { get; private set; }

        private ProposalTemplateVersion()
        {
        }

        public ProposalTemplateVersion(string name, string template)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(template);
            Name = name.Trim();
            Template = template;
            SetCreatedAt(DateTimeOffset.UtcNow);
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Rename(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name.Trim();
        }
    }
}
