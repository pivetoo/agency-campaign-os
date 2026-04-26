using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604260002)]
    public sealed class Migration_202604260002_RequireProposalOpportunityAndRemoveBrand : Migration
    {
        public override void Up()
        {
            Execute.Sql("DELETE FROM proposalitem WHERE proposalid IN (SELECT id FROM proposal WHERE opportunityid IS NULL);");
            Execute.Sql("DELETE FROM proposal WHERE opportunityid IS NULL;");

            Delete.Index("ixproposalbrandid").OnTable("proposal");
            Delete.ForeignKey("fkproposalbrand").OnTable("proposal");
            Delete.Column("brandid").FromTable("proposal");

            Alter.Column("opportunityid").OnTable("proposal").AsInt64().NotNullable();
        }

        public override void Down()
        {
            Alter.Table("proposal")
                .AddColumn("brandid").AsInt64().Nullable();

            Alter.Column("opportunityid").OnTable("proposal").AsInt64().Nullable();

            Create.ForeignKey("fkproposalbrand")
                .FromTable("proposal").ForeignColumn("brandid")
                .ToTable("brand").PrimaryColumn("id");

            Create.Index("ixproposalbrandid")
                .OnTable("proposal")
                .OnColumn("brandid").Ascending();
        }
    }
}
