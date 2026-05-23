using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605230002)]
    public sealed class Migration_202605230002_AddOpportunityApprovalReviewer : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS opportunityapprovalreviewer (
                    id BIGSERIAL PRIMARY KEY,
                    opportunityapprovalrequestid BIGINT NOT NULL REFERENCES opportunityapprovalrequest(id) ON DELETE CASCADE,
                    userid BIGINT,
                    username VARCHAR(150) NOT NULL,
                    role VARCHAR(120),
                    required BOOLEAN NOT NULL DEFAULT FALSE,
                    status INT NOT NULL DEFAULT 1,
                    decidedat TIMESTAMPTZ,
                    decisionnotes VARCHAR(1000),
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ
                );

                CREATE INDEX IF NOT EXISTS ixopportunityapprovalreviewerrequestuser
                    ON opportunityapprovalreviewer (opportunityapprovalrequestid, userid);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP INDEX IF EXISTS ixopportunityapprovalreviewerrequestuser;
                DROP TABLE IF EXISTS opportunityapprovalreviewer;
            ");
        }
    }
}
