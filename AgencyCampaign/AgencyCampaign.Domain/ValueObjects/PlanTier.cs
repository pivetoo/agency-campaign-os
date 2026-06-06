namespace AgencyCampaign.Domain.ValueObjects
{
    // Tier de assinatura do tenant. Define quais funcionalidades e limites a agencia tem.
    // Internal e o tenant da propria Mainstay: acesso total e sem limites, isento de gate
    // (precondicao de ligar o enforcement sem cortar features em producao).
    public enum PlanTier
    {
        Essencial = 1,
        Pro = 2,
        Scale = 3,
        Internal = 99
    }
}
