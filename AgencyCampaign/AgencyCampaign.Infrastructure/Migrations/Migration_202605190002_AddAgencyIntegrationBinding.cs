using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605190002)]
    public sealed class Migration_202605190002_AddAgencyIntegrationBinding : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS agencyintegrationbinding (
                    id                  BIGSERIAL PRIMARY KEY,
                    intentkey           VARCHAR(80) NOT NULL,
                    connectorid         BIGINT NOT NULL,
                    pipelineid          BIGINT NOT NULL,
                    isactive            BOOLEAN NOT NULL DEFAULT TRUE,
                    createdbyuserid     BIGINT NULL,
                    createdbyusername   VARCHAR(150) NULL,
                    createdat           TIMESTAMPTZ NOT NULL,
                    updatedat           TIMESTAMPTZ NULL
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ux_agencyintegrationbinding_intentkey
                    ON agencyintegrationbinding (intentkey);
            ");
        }

        public override void Down()
        {
            Execute.Sql("DROP TABLE IF EXISTS agencyintegrationbinding;");
        }
    }
}
