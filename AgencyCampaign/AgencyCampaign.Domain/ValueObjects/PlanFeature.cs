namespace AgencyCampaign.Domain.ValueObjects
{
    // Funcionalidades gateadas por plano. As funcionalidades centrais (CRM/pipeline, propostas com
    // aceite digital, producao e financeiro com auto-geracao) NAO entram aqui: por decisao de produto
    // o valor central nunca e gateado e fica disponivel em todos os tiers desde o Essencial.
    public enum PlanFeature
    {
        // Incluidas a partir do Pro
        DigitalSignature = 1,
        PixPayout = 2,
        Automations = 3,
        Portals = 4,
        CommercialAnalytics = 5,
        ApprovalPolicy = 6,
        ProposalEngagementTracking = 7,

        // Incluidas a partir do Scale
        ApifySync = 100,
        EmvRoi = 101,
        ContentLicensing = 102,
        PixGovernance = 103,
        AdvancedFinancialReports = 104
    }
}
