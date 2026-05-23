using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605230006)]
    public sealed class Migration_202605230006_AddCommercialPolicy : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS commercialpolicy (
                    id BIGSERIAL PRIMARY KEY,
                    maxdiscountpercent NUMERIC(5,2),
                    minmarginpercent NUMERIC(5,2),
                    defaultpaymenttermdays INT,
                    maxpaymenttermdays INT,
                    notes VARCHAR(1000),
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ
                );
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"DROP TABLE IF EXISTS commercialpolicy;");
        }
    }
}
