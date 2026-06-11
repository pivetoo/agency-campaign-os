using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // A FK de follow-up nasceu sem ON DELETE, entao excluir uma oportunidade com follow-ups falhava com
    // violacao de FK opaca (historico/comentarios/tags ja cascateiam; proposta e bloqueada no servico).
    [Migration(202606100003)]
    public sealed class Migration_202606100003_CascadeOpportunityFollowUpDelete : Migration
    {
        public override void Up()
        {
            Delete.ForeignKey("fkopportunityfollowupopportunity").OnTable("opportunityfollowup");

            Create.ForeignKey("fkopportunityfollowupopportunity")
                .FromTable("opportunityfollowup").ForeignColumn("opportunityid")
                .ToTable("opportunity").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);
        }

        public override void Down()
        {
            Delete.ForeignKey("fkopportunityfollowupopportunity").OnTable("opportunityfollowup");

            Create.ForeignKey("fkopportunityfollowupopportunity")
                .FromTable("opportunityfollowup").ForeignColumn("opportunityid")
                .ToTable("opportunity").PrimaryColumn("id");
        }
    }
}
