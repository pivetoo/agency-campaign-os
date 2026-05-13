using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class AutomationExecutionLog : Entity
    {
        public long AutomationId { get; private set; }

        public string AutomationName { get; private set; } = string.Empty;

        public string Trigger { get; private set; } = string.Empty;

        public bool Succeeded { get; private set; }

        public string? RenderedPayload { get; private set; }

        public string? ErrorMessage { get; private set; }

        public Automation? Automation { get; private set; }

        private AutomationExecutionLog() { }

        public AutomationExecutionLog(long automationId, string automationName, string trigger, bool succeeded, string? renderedPayload, string? errorMessage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(automationName);
            ArgumentException.ThrowIfNullOrWhiteSpace(trigger);

            AutomationId = automationId;
            AutomationName = automationName;
            Trigger = trigger;
            Succeeded = succeeded;
            RenderedPayload = renderedPayload;
            ErrorMessage = errorMessage;
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}
