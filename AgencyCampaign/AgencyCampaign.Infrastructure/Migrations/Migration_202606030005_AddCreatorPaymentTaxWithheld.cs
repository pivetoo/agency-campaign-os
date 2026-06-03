using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Imposto retido na fonte no pagamento ao creator (bruto - descontos - retido = liquido), base do
    // relatorio de retencoes para o contador (camada fiscal - fase 1).
    [Migration(202606030005)]
    public sealed class Migration_202606030005_AddCreatorPaymentTaxWithheld : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE creatorpayment ADD COLUMN taxwithheld DECIMAL(18,2) NOT NULL DEFAULT 0;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE creatorpayment DROP COLUMN IF EXISTS taxwithheld;
            ");
        }
    }
}
