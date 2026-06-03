using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Maker-checker do pagamento ao creator: quem criou, quando/quem aprovou. Acima do teto da agencia o
    // repasse so e agendado apos aprovacao por um usuario DIFERENTE de quem criou (segregacao de funcoes).
    [Migration(202606030007)]
    public sealed class Migration_202606030007_AddCreatorPaymentApproval : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE creatorpayment ADD COLUMN createdbyuserid BIGINT NULL;
                ALTER TABLE creatorpayment ADD COLUMN approvedat TIMESTAMPTZ NULL;
                ALTER TABLE creatorpayment ADD COLUMN approvedbyuserid BIGINT NULL;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE creatorpayment DROP COLUMN IF EXISTS approvedbyuserid;
                ALTER TABLE creatorpayment DROP COLUMN IF EXISTS approvedat;
                ALTER TABLE creatorpayment DROP COLUMN IF EXISTS createdbyuserid;
            ");
        }
    }
}
