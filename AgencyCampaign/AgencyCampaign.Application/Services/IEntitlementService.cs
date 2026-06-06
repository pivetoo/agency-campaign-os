using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Services
{
    // Porteiro de plano: responde, a partir do tier do tenant, quais funcionalidades estao liberadas
    // e se o uso atual cabe no limite. A resolucao do tier do tenant (a partir da assinatura) e
    // responsabilidade de quem chama; este servico e a fonte de verdade da matriz plano -> features/limites.
    public interface IEntitlementService
    {
        // Valor de limite que representa "ilimitado".
        public const int Unlimited = int.MaxValue;

        // O tier tem acesso a esta funcionalidade?
        bool HasFeature(PlanTier tier, PlanFeature feature);

        // Limite do tier para esta metrica (Unlimited quando nao ha teto).
        int GetLimit(PlanTier tier, PlanLimit limit);

        // Avalia o uso atual contra o limite do tier (Allowed=false quando o uso ja atingiu o teto).
        EntitlementCheck CheckLimit(PlanTier tier, PlanLimit limit, int currentUsage);
    }

    // Resultado de uma checagem de limite. Allowed indica se ainda cabe mais um.
    public sealed record EntitlementCheck(bool Allowed, int Limit, int CurrentUsage)
    {
        public bool IsUnlimited => Limit >= IEntitlementService.Unlimited;
    }
}
