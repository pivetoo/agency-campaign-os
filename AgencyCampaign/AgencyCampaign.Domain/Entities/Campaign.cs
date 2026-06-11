using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Campaign : Entity
    {
        private readonly List<CampaignCreator> campaignCreators = [];
        private readonly List<CampaignDeliverable> deliverables = [];

        public long BrandId { get; private set; }

        public Brand? Brand { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public string? Objective { get; private set; }

        public string? Briefing { get; private set; }

        public decimal Budget { get; private set; }

        public DateTimeOffset StartsAt { get; private set; }

        public DateTimeOffset? EndsAt { get; private set; }

        public CampaignStatus Status { get; private set; } = CampaignStatus.Draft;

        // Id do responsavel persistido junto ao nome desnormalizado; sem ele a API nao consegue
        // devolver o vinculo e o editar-salvar do frontend apaga o responsavel silenciosamente
        public long? ResponsibleUserId { get; private set; }

        public string? InternalOwnerName { get; private set; }

        public string? Notes { get; private set; }

        public bool IsActive { get; private set; } = true;

        // Gate de aprovacao: por padrao, publicar um entregavel exige aprovacao da marca. A agencia
        // pode desligar o gate por campanha (ex.: conteudo simples que nao precisa de aprovacao).
        public bool RequiresDeliverableApproval { get; private set; } = true;

        // Pay-when-paid (E2/DP7): gate OPT-IN (default false) que so libera o repasse ao creator apos TODOS
        // os entregaveis daquele creator na campanha estarem aprovados. Sem entregaveis = libera. Independente
        // do RequiresDeliverableApproval (que controla a PUBLICACAO).
        public bool PayoutRequiresContentApproval { get; private set; }

        public long? OpportunityId { get; private set; }

        public long? SourceProposalId { get; private set; }

        public IReadOnlyCollection<CampaignCreator> CampaignCreators => campaignCreators.AsReadOnly();

        public IReadOnlyCollection<CampaignDeliverable> Deliverables => deliverables.AsReadOnly();

        private Campaign()
        {
        }

        public Campaign(long brandId, string name, decimal budget, DateTimeOffset startsAt, string? description = null, string? objective = null, string? briefing = null, DateTimeOffset? endsAt = null, string? internalOwnerName = null, string? notes = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(brandId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(budget);

            BrandId = brandId;
            Name = name.Trim();
            Description = Normalize(description);
            Objective = Normalize(objective);
            Briefing = Normalize(briefing);
            Budget = budget;
            StartsAt = startsAt.ToUniversalTime();
            EndsAt = endsAt?.ToUniversalTime();
            InternalOwnerName = Normalize(internalOwnerName);
            Notes = Normalize(notes);
        }

        public void Update(long brandId, string name, decimal budget, DateTimeOffset startsAt, DateTimeOffset? endsAt, string? description, string? objective, string? briefing, CampaignStatus status, string? internalOwnerName, string? notes, bool isActive)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(brandId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(budget);

            EnsureNotResurrectingCancelled(status);

            BrandId = brandId;
            Name = name.Trim();
            Description = Normalize(description);
            Objective = Normalize(objective);
            Briefing = Normalize(briefing);
            Budget = budget;
            StartsAt = startsAt.ToUniversalTime();
            EndsAt = endsAt?.ToUniversalTime();
            Status = status;
            InternalOwnerName = Normalize(internalOwnerName);
            Notes = Normalize(notes);
            // Coerencia status/IsActive: status terminal (Cancelada/Concluida) forca inativo; em status
            // ativo respeita o flag (permite pausar). Evita a inconsistencia "Cancelada com IsActive=true".
            IsActive = IsTerminal(status) ? false : isActive;
        }

        public void SetResponsibleUserId(long? responsibleUserId)
        {
            ResponsibleUserId = responsibleUserId;
        }

        public void SetRequiresDeliverableApproval(bool value)
        {
            RequiresDeliverableApproval = value;
        }

        public void SetPayoutRequiresContentApproval(bool value)
        {
            PayoutRequiresContentApproval = value;
        }

        public void ChangeStatus(CampaignStatus status)
        {
            EnsureNotResurrectingCancelled(status);

            Status = status;

            if (IsTerminal(status))
            {
                IsActive = false;
            }
        }

        // Cancelada e terminal: nao ressuscita (maquina de estados M1). Demais transicoes seguem livres.
        private void EnsureNotResurrectingCancelled(CampaignStatus target)
        {
            if (Status == CampaignStatus.Cancelled && target != CampaignStatus.Cancelled)
            {
                throw new InvalidOperationException("campaign.status.cannotResurrectCancelled");
            }
        }

        private static bool IsTerminal(CampaignStatus status)
        {
            return status == CampaignStatus.Cancelled || status == CampaignStatus.Completed;
        }

        public void AttachOrigin(long opportunityId, long sourceProposalId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceProposalId);

            OpportunityId = opportunityId;
            SourceProposalId = sourceProposalId;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
