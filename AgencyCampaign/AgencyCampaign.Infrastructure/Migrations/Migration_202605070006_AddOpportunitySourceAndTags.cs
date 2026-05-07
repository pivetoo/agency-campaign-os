using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070006)]
    public sealed class Migration_202605070006_AddOpportunitySourceAndTags : Migration
    {
        public override void Up()
        {
            Create.Table("opportunitysource")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(120).NotNullable()
                .WithColumn("color").AsString(32).NotNullable().WithDefaultValue("#6366f1")
                .WithColumn("displayorder").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Insert.IntoTable("opportunitysource").Row(new { name = "Inbound", color = "#22c55e", displayorder = 1, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("opportunitysource").Row(new { name = "Outbound", color = "#0ea5e9", displayorder = 2, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("opportunitysource").Row(new { name = "Indicação", color = "#8b5cf6", displayorder = 3, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("opportunitysource").Row(new { name = "Parceiro", color = "#f59e0b", displayorder = 4, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("opportunitysource").Row(new { name = "Evento", color = "#ec4899", displayorder = 5, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });

            Create.Table("opportunitytag")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(80).NotNullable()
                .WithColumn("color").AsString(32).NotNullable().WithDefaultValue("#6366f1")
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Table("opportunitytagassignment")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("opportunityid").AsInt64().NotNullable()
                .WithColumn("opportunitytagid").AsInt64().NotNullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkopportunitytagassignmentopportunity")
                .FromTable("opportunitytagassignment").ForeignColumn("opportunityid")
                .ToTable("opportunity").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.ForeignKey("fkopportunitytagassignmenttag")
                .FromTable("opportunitytagassignment").ForeignColumn("opportunitytagid")
                .ToTable("opportunitytag").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("ixopportunitytagassignmentopportunityidtagid")
                .OnTable("opportunitytagassignment")
                .OnColumn("opportunityid").Ascending()
                .OnColumn("opportunitytagid").Ascending()
                .WithOptions().Unique();

            Alter.Table("opportunity")
                .AddColumn("opportunitysourceid").AsInt64().Nullable();

            Create.ForeignKey("fkopportunityopportunitysource")
                .FromTable("opportunity").ForeignColumn("opportunitysourceid")
                .ToTable("opportunitysource").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.SetNull);
        }

        public override void Down()
        {
            Delete.ForeignKey("fkopportunityopportunitysource").OnTable("opportunity");
            Delete.Column("opportunitysourceid").FromTable("opportunity");
            Delete.Table("opportunitytagassignment");
            Delete.Table("opportunitytag");
            Delete.Table("opportunitysource");
        }
    }
}
