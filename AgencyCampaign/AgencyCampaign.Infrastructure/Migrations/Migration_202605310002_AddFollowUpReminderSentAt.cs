using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605310002)]
    public sealed class Migration_202605310002_AddFollowUpReminderSentAt : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE opportunityfollowup ADD COLUMN IF NOT EXISTS remindersentat TIMESTAMPTZ;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE opportunityfollowup DROP COLUMN IF EXISTS remindersentat;");
        }
    }
}
