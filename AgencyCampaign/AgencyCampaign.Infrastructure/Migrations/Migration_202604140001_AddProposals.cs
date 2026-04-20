using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604140001)]
    public sealed class Migration_202604140001_AddProposals : Migration
    {
        public override void Up()
        {
            Create.Table("proposal")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("brandid").AsInt64().NotNullable()
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("description").AsString(1000).Nullable()
                .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("validityuntil").AsDateTimeOffset().Nullable()
                .WithColumn("totalvalue").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("internalownerid").AsInt64().NotNullable()
                .WithColumn("internalownername").AsString(150).Nullable()
                .WithColumn("notes").AsString(1000).Nullable()
                .WithColumn("campaignid").AsInt64().Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkproposalbrand")
                .FromTable("proposal").ForeignColumn("brandid")
                .ToTable("brand").PrimaryColumn("id");

            Create.ForeignKey("fkproposalcampaign")
                .FromTable("proposal").ForeignColumn("campaignid")
                .ToTable("campaign").PrimaryColumn("id");

            Create.Index("ixproposalbrandid")
                .OnTable("proposal")
                .OnColumn("brandid").Ascending();

            Create.Index("ixproposalname")
                .OnTable("proposal")
                .OnColumn("name").Ascending();

            Create.Index("ixproposalstatus")
                .OnTable("proposal")
                .OnColumn("status").Ascending();

            Create.Table("proposalitem")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("proposalid").AsInt64().NotNullable()
                .WithColumn("description").AsString(500).NotNullable()
                .WithColumn("quantity").AsInt32().NotNullable()
                .WithColumn("unitprice").AsDecimal(18, 2).NotNullable()
                .WithColumn("deliverydeadline").AsDateTimeOffset().Nullable()
                .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("observations").AsString(1000).Nullable()
                .WithColumn("creatorid").AsInt64().Nullable();

            Create.ForeignKey("fkproposalitemproposal")
                .FromTable("proposalitem").ForeignColumn("proposalid")
                .ToTable("proposal").PrimaryColumn("id");

            Create.ForeignKey("fkproposalitemcreator")
                .FromTable("proposalitem").ForeignColumn("creatorid")
                .ToTable("creator").PrimaryColumn("id");

            Create.Index("ixproposalitemproposalid")
                .OnTable("proposalitem")
                .OnColumn("proposalid").Ascending();

            Create.Index("ixproposalitemcreatorid")
                .OnTable("proposalitem")
                .OnColumn("creatorid").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ixproposalitemcreatorid").OnTable("proposalitem");
            Delete.Index("ixproposalitemproposalid").OnTable("proposalitem");
            Delete.ForeignKey("fkproposalitemcreator").OnTable("proposalitem");
            Delete.ForeignKey("fkproposalitemproposal").OnTable("proposalitem");
            Delete.Table("proposalitem");

            Delete.Index("ixproposalstatus").OnTable("proposal");
            Delete.Index("ixproposalname").OnTable("proposal");
            Delete.Index("ixproposalbrandid").OnTable("proposal");
            Delete.ForeignKey("fkproposalcampaign").OnTable("proposal");
            Delete.ForeignKey("fkproposalbrand").OnTable("proposal");
            Delete.Table("proposal");
        }
    }
}