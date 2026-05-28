using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605280009)]
    public sealed class Migration_202605280009_AddCampaignBriefing : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE campaignbriefing (
                    id BIGSERIAL PRIMARY KEY,
                    campaignid BIGINT NOT NULL REFERENCES campaign (id) ON DELETE CASCADE,
                    keymessage TEXT NULL,
                    dos TEXT NULL,
                    donts TEXT NULL,
                    hashtags TEXT NULL,
                    mentions TEXT NULL,
                    referencelinks TEXT NULL,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ NULL
                );
                CREATE UNIQUE INDEX ixcampaignbriefingcampaignid ON campaignbriefing (campaignid);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"DROP TABLE IF EXISTS campaignbriefing;");
        }
    }
}
