using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Lembrete proativo de prazo de entregavel: dedup por entregavel via deadlineremindersentat
    // (resetado quando o prazo e remarcado), espelhando o lembrete de follow-up do comercial.
    [Migration(202606020001)]
    public sealed class Migration_202606020001_AddDeliverableDeadlineReminder : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE campaigndeliverable ADD COLUMN deadlineremindersentat TIMESTAMPTZ NULL;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE campaigndeliverable DROP COLUMN IF EXISTS deadlineremindersentat;");
        }
    }
}
