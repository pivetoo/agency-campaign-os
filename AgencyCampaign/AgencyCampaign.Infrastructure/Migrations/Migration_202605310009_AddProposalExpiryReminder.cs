using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605310009)]
    public sealed class Migration_202605310009_AddProposalExpiryReminder : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE proposal ADD COLUMN IF NOT EXISTS expiryremindersentat TIMESTAMPTZ;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE proposal DROP COLUMN IF EXISTS expiryremindersentat;");
        }
    }
}
