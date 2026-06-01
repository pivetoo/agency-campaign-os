using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605310007)]
    public sealed class Migration_202605310007_AddRateCardItem : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS ratecarditem (
                    id BIGSERIAL PRIMARY KEY,
                    creatorid BIGINT NOT NULL REFERENCES creator (id),
                    label VARCHAR(160) NOT NULL,
                    unitprice NUMERIC(18,2) NOT NULL DEFAULT 0,
                    displayorder INT NOT NULL DEFAULT 0,
                    isactive BOOLEAN NOT NULL DEFAULT TRUE,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ
                );

                CREATE INDEX IF NOT EXISTS ixratecarditemcreator ON ratecarditem (creatorid);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP INDEX IF EXISTS ixratecarditemcreator;
                DROP TABLE IF EXISTS ratecarditem;
            ");
        }
    }
}
