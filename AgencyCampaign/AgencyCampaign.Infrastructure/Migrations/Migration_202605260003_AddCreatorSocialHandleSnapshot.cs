using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605260003)]
    public sealed class Migration_202605260003_AddCreatorSocialHandleSnapshot : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS creatorsocialhandlesnapshot (
                    id BIGSERIAL PRIMARY KEY,
                    creatorsocialhandleid BIGINT NOT NULL REFERENCES creatorsocialhandle(id) ON DELETE CASCADE,
                    year INT NOT NULL,
                    month INT NOT NULL,
                    followers BIGINT,
                    engagementrate NUMERIC(5,2),
                    source VARCHAR(50) NOT NULL,
                    collectedat TIMESTAMPTZ NOT NULL,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ,
                    CONSTRAINT uxcreatorsocialhandlesnapshotperiod UNIQUE (creatorsocialhandleid, year, month)
                );
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"DROP TABLE IF EXISTS creatorsocialhandlesnapshot;");
        }
    }
}
