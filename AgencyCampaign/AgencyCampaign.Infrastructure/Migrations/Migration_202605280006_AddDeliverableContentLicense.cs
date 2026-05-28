using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605280006)]
    public sealed class Migration_202605280006_AddDeliverableContentLicense : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE deliverablecontentlicense (
                    id BIGSERIAL PRIMARY KEY,
                    campaigndeliverableid BIGINT NOT NULL REFERENCES campaigndeliverable (id) ON DELETE CASCADE,
                    type INTEGER NOT NULL,
                    channels VARCHAR(500) NULL,
                    startsat TIMESTAMPTZ NULL,
                    expiresat TIMESTAMPTZ NULL,
                    value NUMERIC(14,2) NULL,
                    notes VARCHAR(2000) NULL,
                    campaigndocumentid BIGINT NULL REFERENCES campaigndocument (id) ON DELETE SET NULL,
                    lastalertedthresholddays INTEGER NULL,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ NULL
                );
                CREATE INDEX ixdeliverablecontentlicensedeliverable ON deliverablecontentlicense (campaigndeliverableid);
                CREATE INDEX ixdeliverablecontentlicenseexpiresat ON deliverablecontentlicense (expiresat);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"DROP TABLE IF EXISTS deliverablecontentlicense;");
        }
    }
}
