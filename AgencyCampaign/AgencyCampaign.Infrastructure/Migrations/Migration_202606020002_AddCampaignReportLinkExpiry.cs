using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Link publico de relatorio passa a expirar (TTL de 90 dias) em vez de ficar valido para sempre.
    // Backfill: links ja existentes recebem expiracao baseada na data de criacao.
    [Migration(202606020002)]
    public sealed class Migration_202606020002_AddCampaignReportLinkExpiry : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE campaignreportlink ADD COLUMN expiresat TIMESTAMPTZ NULL;");
            Execute.Sql(@"UPDATE campaignreportlink SET expiresat = createdat + INTERVAL '90 days' WHERE expiresat IS NULL;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE campaignreportlink DROP COLUMN IF EXISTS expiresat;");
        }
    }
}
