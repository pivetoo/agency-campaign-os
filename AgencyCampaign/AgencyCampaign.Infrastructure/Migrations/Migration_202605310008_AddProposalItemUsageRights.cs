using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605310008)]
    public sealed class Migration_202605310008_AddProposalItemUsageRights : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE proposalitem ADD COLUMN IF NOT EXISTS kind INT NOT NULL DEFAULT 0;
                ALTER TABLE proposalitem ADD COLUMN IF NOT EXISTS usagedurationmonths INT;
                ALTER TABLE proposalitem ADD COLUMN IF NOT EXISTS usagescope VARCHAR(160);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE proposalitem DROP COLUMN IF EXISTS kind;
                ALTER TABLE proposalitem DROP COLUMN IF EXISTS usagedurationmonths;
                ALTER TABLE proposalitem DROP COLUMN IF EXISTS usagescope;
            ");
        }
    }
}
