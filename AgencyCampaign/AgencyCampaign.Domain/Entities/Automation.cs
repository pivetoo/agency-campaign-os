using System.Text.Json;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Automation : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string Trigger { get; private set; } = string.Empty;

        public string? TriggerCondition { get; private set; }

        public long ConnectorId { get; private set; }

        public long PipelineId { get; private set; }

        public string VariableMappingJson { get; private set; } = "{}";

        public bool IsActive { get; private set; } = true;

        private Automation()
        {
        }

        public Automation(string name, string trigger, long connectorId, long pipelineId, string? triggerCondition = null, Dictionary<string, string>? variableMapping = null, bool isActive = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(trigger);

            Name = name.Trim();
            Trigger = trigger.Trim();
            ConnectorId = connectorId;
            PipelineId = pipelineId;
            TriggerCondition = Normalize(triggerCondition);
            VariableMappingJson = variableMapping is not null ? JsonSerializer.Serialize(variableMapping) : "{}";
            IsActive = isActive;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string name, string trigger, long connectorId, long pipelineId, string? triggerCondition = null, Dictionary<string, string>? variableMapping = null, bool? isActive = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(trigger);

            Name = name.Trim();
            Trigger = trigger.Trim();
            ConnectorId = connectorId;
            PipelineId = pipelineId;
            TriggerCondition = Normalize(triggerCondition);
            VariableMappingJson = variableMapping is not null ? JsonSerializer.Serialize(variableMapping) : VariableMappingJson;
            IsActive = isActive ?? IsActive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public Dictionary<string, string> GetVariableMapping()
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(VariableMappingJson) ?? [];
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
