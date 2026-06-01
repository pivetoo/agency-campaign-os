using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605310011)]
    public sealed class Migration_202605310011_AddProposalItemPricingModel : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE proposalitem ADD COLUMN IF NOT EXISTS pricingmodel INTEGER NOT NULL DEFAULT 0;");
            Execute.Sql(@"ALTER TABLE proposalitem ADD COLUMN IF NOT EXISTS variablerate NUMERIC(18,2) NULL;");
            Execute.Sql(@"ALTER TABLE proposalitem ADD COLUMN IF NOT EXISTS variablebasis NUMERIC(18,2) NULL;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE proposalitem DROP COLUMN IF EXISTS variablebasis;");
            Execute.Sql(@"ALTER TABLE proposalitem DROP COLUMN IF EXISTS variablerate;");
            Execute.Sql(@"ALTER TABLE proposalitem DROP COLUMN IF EXISTS pricingmodel;");
        }
    }
}
