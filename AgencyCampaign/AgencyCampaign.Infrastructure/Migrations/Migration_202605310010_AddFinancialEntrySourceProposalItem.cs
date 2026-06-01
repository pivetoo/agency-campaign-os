using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605310010)]
    public sealed class Migration_202605310010_AddFinancialEntrySourceProposalItem : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE financialentry ADD COLUMN IF NOT EXISTS sourceproposalitemid BIGINT NULL;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE financialentry DROP COLUMN IF EXISTS sourceproposalitemid;");
        }
    }
}
