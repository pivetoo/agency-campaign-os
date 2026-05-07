using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070001)]
    public sealed class Migration_202605070001_AddOpportunityComments : Migration
    {
        public override void Up()
        {
            Create.Table("opportunitycomment")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("opportunityid").AsInt64().NotNullable()
                .WithColumn("authoruserid").AsInt64().Nullable()
                .WithColumn("authorname").AsString(255).NotNullable()
                .WithColumn("body").AsString(4000).NotNullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkopportunitycommentopportunity")
                .FromTable("opportunitycomment").ForeignColumn("opportunityid")
                .ToTable("opportunity").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("ixopportunitycommentopportunityidcreatedat")
                .OnTable("opportunitycomment")
                .OnColumn("opportunityid").Ascending()
                .OnColumn("createdat").Descending();
        }

        public override void Down()
        {
            Delete.Table("opportunitycomment");
        }
    }
}
