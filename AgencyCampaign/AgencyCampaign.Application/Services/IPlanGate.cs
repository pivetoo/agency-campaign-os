using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Services
{
    // Atalho de gating por plano para os services. Resolve o tier do tenant e consulta o IEntitlementService.
    // RequireFeatureAsync/RequireWithinLimitAsync LANCAM (acoes de usuario -> 403 com mensagem de upgrade).
    // HasFeatureAsync NAO lanca (jobs de fundo e leitura publica usam para PULAR/suprimir quando a feature esta off).
    public interface IPlanGate
    {
        Task RequireFeatureAsync(PlanFeature feature, CancellationToken cancellationToken = default);

        Task<bool> HasFeatureAsync(PlanFeature feature, CancellationToken cancellationToken = default);

        Task RequireWithinLimitAsync(PlanLimit limit, int currentUsage, CancellationToken cancellationToken = default);
    }
}
