using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070015)]
    public sealed class Migration_202605070015_AddAgencySettings : Migration
    {
        public override void Up()
        {
            Create.Table("agencysettings")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("agencyname").AsString(150).NotNullable()
                .WithColumn("tradename").AsString(150).Nullable()
                .WithColumn("document").AsString(50).Nullable()
                .WithColumn("primaryemail").AsString(255).Nullable()
                .WithColumn("phone").AsString(50).Nullable()
                .WithColumn("address").AsString(500).Nullable()
                .WithColumn("logourl").AsString(500).Nullable()
                .WithColumn("primarycolor").AsString(32).Nullable()
                .WithColumn("defaultemailconnectorid").AsInt64().Nullable()
                .WithColumn("defaultemailpipelineid").AsInt64().Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Insert.IntoTable("agencysettings").Row(new
            {
                agencyname = "Minha agência",
                createdat = SystemMethods.CurrentUTCDateTime,
                updatedat = SystemMethods.CurrentUTCDateTime
            });
        }

        public override void Down()
        {
            Delete.Table("agencysettings");
        }
    }
}
