using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605310001)]
    public sealed class Migration_202605310001_AddProposalVersionFrozenDiscount : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE proposalversion ADD COLUMN IF NOT EXISTS discountamount NUMERIC(18,2);
                ALTER TABLE proposalversion ADD COLUMN IF NOT EXISTS nettotalvalue NUMERIC(18,2);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE proposalversion DROP COLUMN IF EXISTS discountamount;
                ALTER TABLE proposalversion DROP COLUMN IF EXISTS nettotalvalue;
            ");
        }
    }
}
