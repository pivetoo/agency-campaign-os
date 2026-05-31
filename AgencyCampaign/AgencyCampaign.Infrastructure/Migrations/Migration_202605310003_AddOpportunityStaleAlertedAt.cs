using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605310003)]
    public sealed class Migration_202605310003_AddOpportunityStaleAlertedAt : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE opportunity ADD COLUMN IF NOT EXISTS stalealertedat TIMESTAMPTZ;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE opportunity DROP COLUMN IF EXISTS stalealertedat;");
        }
    }
}
