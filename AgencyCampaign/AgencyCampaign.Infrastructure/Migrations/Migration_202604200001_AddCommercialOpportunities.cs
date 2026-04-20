using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604200001)]
    public sealed class Migration_202604200001_AddCommercialOpportunities : Migration
    {
        public override void Up()
        {
            Create.Table("opportunity")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("brandid").AsInt64().NotNullable()
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("description").AsString(1000).Nullable()
                .WithColumn("stage").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("estimatedvalue").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("expectedcloseat").AsDateTimeOffset().Nullable()
                .WithColumn("internalownerid").AsInt64().Nullable()
                .WithColumn("internalownername").AsString(150).Nullable()
                .WithColumn("contactname").AsString(150).Nullable()
                .WithColumn("contactemail").AsString(255).Nullable()
                .WithColumn("notes").AsString(1000).Nullable()
                .WithColumn("closedat").AsDateTimeOffset().Nullable()
                .WithColumn("lossreason").AsString(1000).Nullable()
                .WithColumn("wonnotes").AsString(1000).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkopportunitybrand")
                .FromTable("opportunity").ForeignColumn("brandid")
                .ToTable("brand").PrimaryColumn("id");

            Create.Index("ixopportunitybrandid")
                .OnTable("opportunity")
                .OnColumn("brandid").Ascending();

            Create.Index("ixopportunitystage")
                .OnTable("opportunity")
                .OnColumn("stage").Ascending();

            Create.Index("ixopportunityexpectedcloseat")
                .OnTable("opportunity")
                .OnColumn("expectedcloseat").Ascending();

            Create.Table("opportunitynegotiation")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("opportunityid").AsInt64().NotNullable()
                .WithColumn("title").AsString(150).NotNullable()
                .WithColumn("amount").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("negotiatedat").AsDateTimeOffset().NotNullable()
                .WithColumn("notes").AsString(1000).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkopportunitynegotiationopportunity")
                .FromTable("opportunitynegotiation").ForeignColumn("opportunityid")
                .ToTable("opportunity").PrimaryColumn("id");

            Create.Index("ixopportunitynegotiationopportunityid")
                .OnTable("opportunitynegotiation")
                .OnColumn("opportunityid").Ascending();

            Create.Index("ixopportunitynegotiationnegotiatedat")
                .OnTable("opportunitynegotiation")
                .OnColumn("negotiatedat").Descending();

            Create.Table("opportunityfollowup")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("opportunityid").AsInt64().NotNullable()
                .WithColumn("subject").AsString(150).NotNullable()
                .WithColumn("dueat").AsDateTimeOffset().NotNullable()
                .WithColumn("notes").AsString(1000).Nullable()
                .WithColumn("iscompleted").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("completedat").AsDateTimeOffset().Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkopportunityfollowupopportunity")
                .FromTable("opportunityfollowup").ForeignColumn("opportunityid")
                .ToTable("opportunity").PrimaryColumn("id");

            Create.Index("ixopportunityfollowupopportunityid")
                .OnTable("opportunityfollowup")
                .OnColumn("opportunityid").Ascending();

            Create.Index("ixopportunityfollowupdueat")
                .OnTable("opportunityfollowup")
                .OnColumn("dueat").Ascending();

            Alter.Table("proposal")
                .AddColumn("opportunityid").AsInt64().Nullable();

            Create.ForeignKey("fkproposalopportunity")
                .FromTable("proposal").ForeignColumn("opportunityid")
                .ToTable("opportunity").PrimaryColumn("id");

            Create.Index("ixproposalopportunityid")
                .OnTable("proposal")
                .OnColumn("opportunityid").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ixproposalopportunityid").OnTable("proposal");
            Delete.ForeignKey("fkproposalopportunity").OnTable("proposal");
            Delete.Column("opportunityid").FromTable("proposal");

            Delete.Index("ixopportunityfollowupdueat").OnTable("opportunityfollowup");
            Delete.Index("ixopportunityfollowupopportunityid").OnTable("opportunityfollowup");
            Delete.ForeignKey("fkopportunityfollowupopportunity").OnTable("opportunityfollowup");
            Delete.Table("opportunityfollowup");

            Delete.Index("ixopportunitynegotiationnegotiatedat").OnTable("opportunitynegotiation");
            Delete.Index("ixopportunitynegotiationopportunityid").OnTable("opportunitynegotiation");
            Delete.ForeignKey("fkopportunitynegotiationopportunity").OnTable("opportunitynegotiation");
            Delete.Table("opportunitynegotiation");

            Delete.Index("ixopportunityexpectedcloseat").OnTable("opportunity");
            Delete.Index("ixopportunitystage").OnTable("opportunity");
            Delete.Index("ixopportunitybrandid").OnTable("opportunity");
            Delete.ForeignKey("fkopportunitybrand").OnTable("opportunity");
            Delete.Table("opportunity");
        }
    }
}
