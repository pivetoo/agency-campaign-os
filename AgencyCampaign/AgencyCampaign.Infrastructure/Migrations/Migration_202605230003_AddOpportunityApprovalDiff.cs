using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605230003)]
    public sealed class Migration_202605230003_AddOpportunityApprovalDiff : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS opportunityapprovaldiff (
                    id BIGSERIAL PRIMARY KEY,
                    opportunityapprovalrequestid BIGINT NOT NULL REFERENCES opportunityapprovalrequest(id) ON DELETE CASCADE,
                    field VARCHAR(120) NOT NULL,
                    policyvalue VARCHAR(200),
                    requestedvalue VARCHAR(200),
                    delta VARCHAR(120),
                    kind INT NOT NULL DEFAULT 1,
                    displayorder INT NOT NULL DEFAULT 0,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ
                );

                CREATE INDEX IF NOT EXISTS ixopportunityapprovaldiffrequestorder
                    ON opportunityapprovaldiff (opportunityapprovalrequestid, displayorder);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP INDEX IF EXISTS ixopportunityapprovaldiffrequestorder;
                DROP TABLE IF EXISTS opportunityapprovaldiff;
            ");
        }
    }
}
