using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Lastro de nao-adulteracao (D1i): hash SHA-256 (hex, 64 chars) do corpo do documento,
    // selado no envio para assinatura. Permite detectar adulteracao do conteudo depois do envio.
    [Migration(202606020004)]
    public sealed class Migration_202606020004_AddCampaignDocumentContentHash : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE campaigndocument ADD COLUMN contenthash VARCHAR(64) NULL;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE campaigndocument DROP COLUMN IF EXISTS contenthash;");
        }
    }
}
