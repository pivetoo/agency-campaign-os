using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070002)]
    public sealed class Migration_202605070002_AddProposalVersionsAndShareLinks : Migration
    {
        public override void Up()
        {
            Create.Table("proposalversion")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("proposalid").AsInt64().NotNullable()
                .WithColumn("versionnumber").AsInt32().NotNullable()
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("description").AsString(1000).Nullable()
                .WithColumn("totalvalue").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("validityuntil").AsDateTimeOffset().Nullable()
                .WithColumn("snapshotjson").AsCustom("text").NotNullable()
                .WithColumn("sentat").AsDateTimeOffset().NotNullable()
                .WithColumn("sentbyuserid").AsInt64().Nullable()
                .WithColumn("sentbyusername").AsString(255).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkproposalversionproposal")
                .FromTable("proposalversion").ForeignColumn("proposalid")
                .ToTable("proposal").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("ixproposalversionproposalidversionnumber")
                .OnTable("proposalversion")
                .OnColumn("proposalid").Ascending()
                .OnColumn("versionnumber").Ascending()
                .WithOptions().Unique();

            Create.Table("proposalsharelink")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("proposalid").AsInt64().NotNullable()
                .WithColumn("token").AsString(64).NotNullable()
                .WithColumn("expiresat").AsDateTimeOffset().Nullable()
                .WithColumn("revokedat").AsDateTimeOffset().Nullable()
                .WithColumn("createdbyuserid").AsInt64().Nullable()
                .WithColumn("createdbyusername").AsString(255).Nullable()
                .WithColumn("lastviewedat").AsDateTimeOffset().Nullable()
                .WithColumn("viewcount").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkproposalsharelinkproposal")
                .FromTable("proposalsharelink").ForeignColumn("proposalid")
                .ToTable("proposal").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("ixproposalsharelinktoken")
                .OnTable("proposalsharelink")
                .OnColumn("token").Ascending()
                .WithOptions().Unique();

            Create.Table("proposalview")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("proposalsharelinkid").AsInt64().NotNullable()
                .WithColumn("viewedat").AsDateTimeOffset().NotNullable()
                .WithColumn("ipaddress").AsString(64).Nullable()
                .WithColumn("useragent").AsString(500).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkproposalviewproposalsharelink")
                .FromTable("proposalview").ForeignColumn("proposalsharelinkid")
                .ToTable("proposalsharelink").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("ixproposalviewproposalsharelinkidviewedat")
                .OnTable("proposalview")
                .OnColumn("proposalsharelinkid").Ascending()
                .OnColumn("viewedat").Descending();
        }

        public override void Down()
        {
            Delete.Table("proposalview");
            Delete.Table("proposalsharelink");
            Delete.Table("proposalversion");
        }
    }
}
