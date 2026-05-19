using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605190001)]
    public sealed class Migration_202605190001_AddOpportunityCommentIsDeleted : Migration
    {
        public override void Up()
        {
            Execute.Sql("ALTER TABLE opportunitycomment ADD COLUMN IF NOT EXISTS isdeleted BOOLEAN NOT NULL DEFAULT FALSE;");
        }

        public override void Down()
        {
            Execute.Sql("ALTER TABLE opportunitycomment DROP COLUMN IF EXISTS isdeleted;");
        }
    }
}
