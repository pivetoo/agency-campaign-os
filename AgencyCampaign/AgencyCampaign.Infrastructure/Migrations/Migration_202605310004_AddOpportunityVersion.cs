using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605310004)]
    public sealed class Migration_202605310004_AddOpportunityVersion : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE opportunity ADD COLUMN IF NOT EXISTS version INTEGER NOT NULL DEFAULT 0;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE opportunity DROP COLUMN IF EXISTS version;");
        }
    }
}
