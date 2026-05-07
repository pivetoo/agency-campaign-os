using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605060001)]
    public sealed class Migration_202605060001_AddCommercialAuditTrail : Migration
    {
        public override void Up()
        {
            Create.Table("opportunitystagehistory")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("opportunityid").AsInt64().NotNullable()
                .WithColumn("fromstageid").AsInt64().Nullable()
                .WithColumn("tostageid").AsInt64().NotNullable()
                .WithColumn("changedat").AsDateTimeOffset().NotNullable()
                .WithColumn("changedbyuserid").AsInt64().Nullable()
                .WithColumn("changedbyusername").AsString(255).Nullable()
                .WithColumn("reason").AsString(500).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkopportunitystagehistoryopportunity")
                .FromTable("opportunitystagehistory").ForeignColumn("opportunityid")
                .ToTable("opportunity").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.ForeignKey("fkopportunitystagehistoryfromstage")
                .FromTable("opportunitystagehistory").ForeignColumn("fromstageid")
                .ToTable("commercialpipelinestage").PrimaryColumn("id");

            Create.ForeignKey("fkopportunitystagehistorytostage")
                .FromTable("opportunitystagehistory").ForeignColumn("tostageid")
                .ToTable("commercialpipelinestage").PrimaryColumn("id");

            Create.Index("ixopportunitystagehistoryopportunityidchangedat")
                .OnTable("opportunitystagehistory")
                .OnColumn("opportunityid").Ascending()
                .OnColumn("changedat").Descending();

            Create.Table("proposalstatushistory")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("proposalid").AsInt64().NotNullable()
                .WithColumn("fromstatus").AsInt32().Nullable()
                .WithColumn("tostatus").AsInt32().NotNullable()
                .WithColumn("changedat").AsDateTimeOffset().NotNullable()
                .WithColumn("changedbyuserid").AsInt64().Nullable()
                .WithColumn("changedbyusername").AsString(255).Nullable()
                .WithColumn("reason").AsString(500).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkproposalstatushistoryproposal")
                .FromTable("proposalstatushistory").ForeignColumn("proposalid")
                .ToTable("proposal").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("ixproposalstatushistoryproposalidchangedat")
                .OnTable("proposalstatushistory")
                .OnColumn("proposalid").Ascending()
                .OnColumn("changedat").Descending();
        }

        public override void Down()
        {
            Delete.Table("proposalstatushistory");
            Delete.Table("opportunitystagehistory");
        }
    }
}
