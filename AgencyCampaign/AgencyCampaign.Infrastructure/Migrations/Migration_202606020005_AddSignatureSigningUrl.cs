using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // C2: URL de assinatura do provedor por signatario, capturada do callback, para o creator
    // assinar a partir do portal (botao "Assinar") em vez de depender so do e-mail do provedor.
    [Migration(202606020005)]
    public sealed class Migration_202606020005_AddSignatureSigningUrl : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE campaigndocumentsignature ADD COLUMN signingurl VARCHAR(1000) NULL;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE campaigndocumentsignature DROP COLUMN IF EXISTS signingurl;");
        }
    }
}
