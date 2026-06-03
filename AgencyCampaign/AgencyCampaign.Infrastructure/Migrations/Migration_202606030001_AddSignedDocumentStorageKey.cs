using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Lastro/durabilidade (D1i): chave da nossa copia privada do PDF assinado, alem da URL do provedor.
    // Permite servir/recuperar o documento mesmo que o link do provedor expire ou fique indisponivel.
    [Migration(202606030001)]
    public sealed class Migration_202606030001_AddSignedDocumentStorageKey : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE campaigndocument ADD COLUMN signeddocumentstoragekey VARCHAR(500) NULL;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE campaigndocument DROP COLUMN IF EXISTS signeddocumentstoragekey;");
        }
    }
}
