using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605260001)]
    public sealed class Migration_202605260001_AddDeliverableMetrics : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE campaigndeliverable ADD COLUMN IF NOT EXISTS likes INT;
                ALTER TABLE campaigndeliverable ADD COLUMN IF NOT EXISTS comments INT;
                ALTER TABLE campaigndeliverable ADD COLUMN IF NOT EXISTS views BIGINT;
                ALTER TABLE campaigndeliverable ADD COLUMN IF NOT EXISTS reach BIGINT;
                ALTER TABLE campaigndeliverable ADD COLUMN IF NOT EXISTS impressions BIGINT;
                ALTER TABLE campaigndeliverable ADD COLUMN IF NOT EXISTS saves INT;
                ALTER TABLE campaigndeliverable ADD COLUMN IF NOT EXISTS shares INT;
                ALTER TABLE campaigndeliverable ADD COLUMN IF NOT EXISTS engagementrate NUMERIC(7,2);
                ALTER TABLE campaigndeliverable ADD COLUMN IF NOT EXISTS metricscollectedat TIMESTAMPTZ;
                ALTER TABLE campaigndeliverable ADD COLUMN IF NOT EXISTS metricssource INT NOT NULL DEFAULT 0;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE campaigndeliverable DROP COLUMN IF EXISTS likes;
                ALTER TABLE campaigndeliverable DROP COLUMN IF EXISTS comments;
                ALTER TABLE campaigndeliverable DROP COLUMN IF EXISTS views;
                ALTER TABLE campaigndeliverable DROP COLUMN IF EXISTS reach;
                ALTER TABLE campaigndeliverable DROP COLUMN IF EXISTS impressions;
                ALTER TABLE campaigndeliverable DROP COLUMN IF EXISTS saves;
                ALTER TABLE campaigndeliverable DROP COLUMN IF EXISTS shares;
                ALTER TABLE campaigndeliverable DROP COLUMN IF EXISTS engagementrate;
                ALTER TABLE campaigndeliverable DROP COLUMN IF EXISTS metricscollectedat;
                ALTER TABLE campaigndeliverable DROP COLUMN IF EXISTS metricssource;
            ");
        }
    }
}
