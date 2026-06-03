using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Idempotencia do payout: idempotencykey (gerada no agendamento, enviada ao IntegrationPlatform para
    // evitar Pix duplicado em retry) e endtoendid (e2eId do Pix, para conciliacao/comprovacao).
    [Migration(202606030002)]
    public sealed class Migration_202606030002_AddCreatorPaymentIdempotencyAndE2E : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE creatorpayment ADD COLUMN idempotencykey VARCHAR(64) NULL;
                ALTER TABLE creatorpayment ADD COLUMN endtoendid VARCHAR(140) NULL;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE creatorpayment DROP COLUMN IF EXISTS endtoendid;
                ALTER TABLE creatorpayment DROP COLUMN IF EXISTS idempotencykey;
            ");
        }
    }
}
