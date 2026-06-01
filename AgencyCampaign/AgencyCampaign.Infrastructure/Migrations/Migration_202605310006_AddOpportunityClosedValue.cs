using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605310006)]
    public sealed class Migration_202605310006_AddOpportunityClosedValue : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE opportunity ADD COLUMN IF NOT EXISTS closedvalue NUMERIC(18, 2);");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE opportunity DROP COLUMN IF EXISTS closedvalue;");
        }
    }
}
