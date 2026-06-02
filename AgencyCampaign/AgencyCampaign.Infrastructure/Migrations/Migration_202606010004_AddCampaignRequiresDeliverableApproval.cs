using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Gate de aprovacao de entregavel configuravel por campanha (default: obrigatorio).
    [Migration(202606010004)]
    public sealed class Migration_202606010004_AddCampaignRequiresDeliverableApproval : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE campaign ADD COLUMN requiresdeliverableapproval BOOLEAN NOT NULL DEFAULT TRUE;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE campaign DROP COLUMN IF EXISTS requiresdeliverableapproval;");
        }
    }
}
