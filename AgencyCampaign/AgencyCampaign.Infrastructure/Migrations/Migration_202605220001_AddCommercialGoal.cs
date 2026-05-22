using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605220001)]
    public sealed class Migration_202605220001_AddCommercialGoal : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS commercialgoal (
                    id BIGSERIAL PRIMARY KEY,
                    userid BIGINT,
                    periodtype INT NOT NULL,
                    periodstart TIMESTAMPTZ NOT NULL,
                    targetamount NUMERIC(18,2) NOT NULL,
                    notes VARCHAR(1000),
                    isactive BOOLEAN NOT NULL DEFAULT TRUE,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ixcommercialgoaluserperiod
                    ON commercialgoal (COALESCE(userid, 0), periodtype, periodstart);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP INDEX IF EXISTS ixcommercialgoaluserperiod;
                DROP TABLE IF EXISTS commercialgoal;
            ");
        }
    }
}
