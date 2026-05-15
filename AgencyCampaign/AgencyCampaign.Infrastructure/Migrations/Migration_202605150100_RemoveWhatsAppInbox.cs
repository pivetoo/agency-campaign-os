using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605150100)]
    public sealed class Migration_202605150100_RemoveWhatsAppInbox : Migration
    {
        public override void Up()
        {
            Execute.Sql("ALTER TABLE IF EXISTS agencysettings DROP COLUMN IF EXISTS whatsappconnectorid;");
            Execute.Sql("DROP TABLE IF EXISTS whatsappmessage CASCADE;");
            Execute.Sql("DROP TABLE IF EXISTS whatsappconversation CASCADE;");
        }

        public override void Down()
        {
        }
    }
}
