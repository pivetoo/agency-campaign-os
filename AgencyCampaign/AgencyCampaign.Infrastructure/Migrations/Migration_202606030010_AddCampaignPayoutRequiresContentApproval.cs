using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Pay-when-paid (E2/DP7): gate opt-in (default FALSE) por campanha. Quando ligado, o repasse ao creator
    // so e agendado apos todos os entregaveis daquele creator estarem aprovados. Coluna aditiva NOT NULL
    // com default FALSE - nao liga o gate em campanhas existentes.
    [Migration(202606030010)]
    public sealed class Migration_202606030010_AddCampaignPayoutRequiresContentApproval : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE campaign ADD COLUMN payoutrequirescontentapproval BOOLEAN NOT NULL DEFAULT FALSE;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE campaign DROP COLUMN IF EXISTS payoutrequirescontentapproval;");
        }
    }
}
