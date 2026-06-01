using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Os tokens publicos passaram a embutir o prefixo de tenant ("{tenantId}~{aleatorio}").
    // Com tenantId de 36 chars (Guid) + 1 separador + 43 chars de aleatorio (32 bytes base64url),
    // o token chega a 80 chars - estourando creatoraccesstoken.token (64) e sem folga em
    // deliverablesharelink/campaignreportlink (80). Alargamos os tres para 128 com margem.
    [Migration(202606010002)]
    public sealed class Migration_202606010002_WidenPublicTokenColumns : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE creatoraccesstoken ALTER COLUMN token TYPE VARCHAR(128);
                ALTER TABLE deliverablesharelink ALTER COLUMN token TYPE VARCHAR(128);
                ALTER TABLE campaignreportlink ALTER COLUMN token TYPE VARCHAR(128);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE creatoraccesstoken ALTER COLUMN token TYPE VARCHAR(64);
                ALTER TABLE deliverablesharelink ALTER COLUMN token TYPE VARCHAR(80);
                ALTER TABLE campaignreportlink ALTER COLUMN token TYPE VARCHAR(80);
            ");
        }
    }
}
