using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605310005)]
    public sealed class Migration_202605310005_AddProposalClientDecision : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE proposal ADD COLUMN IF NOT EXISTS clientdecisionbyname TEXT;
                ALTER TABLE proposal ADD COLUMN IF NOT EXISTS clientdecisionbyemail TEXT;
                ALTER TABLE proposal ADD COLUMN IF NOT EXISTS clientdecisionat TIMESTAMPTZ;
                ALTER TABLE proposal ADD COLUMN IF NOT EXISTS clientdecisionversionnumber INTEGER;
                ALTER TABLE proposal ADD COLUMN IF NOT EXISTS clientdecisioncontenthash TEXT;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE proposal DROP COLUMN IF EXISTS clientdecisionbyname;
                ALTER TABLE proposal DROP COLUMN IF EXISTS clientdecisionbyemail;
                ALTER TABLE proposal DROP COLUMN IF EXISTS clientdecisionat;
                ALTER TABLE proposal DROP COLUMN IF EXISTS clientdecisionversionnumber;
                ALTER TABLE proposal DROP COLUMN IF EXISTS clientdecisioncontenthash;
            ");
        }
    }
}
