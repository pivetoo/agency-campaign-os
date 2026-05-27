using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605270002)]
    public sealed class Migration_202605270002_AddContentReview : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE deliverablecontentversion (
                    id BIGSERIAL PRIMARY KEY,
                    campaigndeliverableid BIGINT NOT NULL REFERENCES campaigndeliverable (id) ON DELETE CASCADE,
                    roundnumber INTEGER NOT NULL,
                    submittedbyrole INTEGER NOT NULL,
                    submittedbyname VARCHAR(200) NOT NULL,
                    note VARCHAR(2000) NULL,
                    status INTEGER NOT NULL,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ NULL
                );
                CREATE UNIQUE INDEX uxdeliverablecontentversionround ON deliverablecontentversion (campaigndeliverableid, roundnumber);

                CREATE TABLE deliverablecontentasset (
                    id BIGSERIAL PRIMARY KEY,
                    deliverablecontentversionid BIGINT NOT NULL REFERENCES deliverablecontentversion (id) ON DELETE CASCADE,
                    type INTEGER NOT NULL,
                    url VARCHAR(1000) NOT NULL,
                    filename VARCHAR(300) NULL,
                    contenttype VARCHAR(120) NULL,
                    displayorder INTEGER NOT NULL DEFAULT 0,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ NULL
                );
                CREATE INDEX ixdeliverablecontentassetversion ON deliverablecontentasset (deliverablecontentversionid);

                CREATE TABLE deliverablereviewcomment (
                    id BIGSERIAL PRIMARY KEY,
                    campaigndeliverableid BIGINT NOT NULL REFERENCES campaigndeliverable (id) ON DELETE CASCADE,
                    deliverablecontentversionid BIGINT NULL REFERENCES deliverablecontentversion (id) ON DELETE SET NULL,
                    authorrole INTEGER NOT NULL,
                    authorname VARCHAR(200) NOT NULL,
                    body VARCHAR(4000) NOT NULL,
                    visibility INTEGER NOT NULL,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ NULL
                );
                CREATE INDEX ixdeliverablereviewcommentdeliverable ON deliverablereviewcomment (campaigndeliverableid);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP TABLE IF EXISTS deliverablereviewcomment;
                DROP TABLE IF EXISTS deliverablecontentasset;
                DROP TABLE IF EXISTS deliverablecontentversion;
            ");
        }
    }
}
