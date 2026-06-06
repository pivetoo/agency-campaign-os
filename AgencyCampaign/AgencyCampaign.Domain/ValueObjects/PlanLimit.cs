namespace AgencyCampaign.Domain.ValueObjects
{
    // Limites quantitativos por plano (fences). ActiveManagedCreators e o DRIVER unico de cobranca.
    // Seats e contado no IdentityManagement (nao existe entidade de usuario no AgencyCampaign);
    // aparece aqui apenas para o catalogo de limites ser completo.
    public enum PlanLimit
    {
        ActiveManagedCreators = 1,
        Seats = 2,
        ActiveCampaigns = 3
    }
}
