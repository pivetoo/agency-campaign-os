using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // A campanha guardava apenas internalownername (desnormalizado); sem o id, a API nao devolvia
    // o vinculo e o editar-salvar do frontend apagava o responsavel. Backfill recupera o id a
    // partir da proposta de origem nas campanhas convertidas; campanhas manuais ficam nulas.
    [Migration(202606100002)]
    public sealed class Migration_202606100002_AddCampaignResponsibleUserId : Migration
    {
        public override void Up()
        {
            Alter.Table("campaign")
                .AddColumn("responsibleuserid").AsInt64().Nullable();

            Execute.Sql(@"
                UPDATE campaign
                SET responsibleuserid = proposal.internalownerid
                FROM proposal
                WHERE campaign.sourceproposalid = proposal.id
                  AND campaign.responsibleuserid IS NULL;
            ");
        }

        public override void Down()
        {
            Delete.Column("responsibleuserid").FromTable("campaign");
        }
    }
}
