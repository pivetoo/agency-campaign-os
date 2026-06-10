using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // A Migration_202606010002 alargou as colunas de token publico para 128 mas deixou
    // proposalsharelink de fora. O token com prefixo de tenant ("{tenantId}~{aleatorio}")
    // passa de 64 chars e o insert falha em producao ao gerar link publico de proposta.
    [Migration(202606100001)]
    public sealed class Migration_202606100001_WidenProposalShareLinkToken : Migration
    {
        public override void Up()
        {
            Execute.Sql("ALTER TABLE proposalsharelink ALTER COLUMN token TYPE VARCHAR(128);");
        }

        public override void Down()
        {
            Execute.Sql("ALTER TABLE proposalsharelink ALTER COLUMN token TYPE VARCHAR(64);");
        }
    }
}
