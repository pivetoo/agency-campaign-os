namespace AgencyCampaign.Domain.ValueObjects
{
    // Ciclo da cobranca (boleto/PIX) emitida para um recebivel, independente do status financeiro do
    // lancamento: None (sem cobranca) -> Requested (enfileirada no IntegrationPlatform) -> Issued
    // (provedor confirmou e devolveu o link) -> Paid (provedor confirmou o pagamento) ou Failed/Cancelled.
    public enum ChargeStatus
    {
        None = 0,
        Requested = 1,
        Issued = 2,
        Paid = 3,
        Failed = 4,
        Cancelled = 5
    }
}
