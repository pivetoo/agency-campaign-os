using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180003)]
    public sealed class Migration_202605180003_DropCampaignCreatorStatusIsFinal : Migration
    {
        public override void Up()
        {
            Execute.Sql("ALTER TABLE IF EXISTS campaigncreatorstatus DROP COLUMN IF EXISTS isfinal;");
        }

        public override void Down()
        {
        }
    }
}
