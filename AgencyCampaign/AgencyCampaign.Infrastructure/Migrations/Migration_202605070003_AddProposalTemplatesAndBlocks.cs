using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070003)]
    public sealed class Migration_202605070003_AddProposalTemplatesAndBlocks : Migration
    {
        public override void Up()
        {
            Create.Table("proposaltemplate")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("description").AsString(1000).Nullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdbyuserid").AsInt64().Nullable()
                .WithColumn("createdbyusername").AsString(255).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Table("proposaltemplateitem")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("proposaltemplateid").AsInt64().NotNullable()
                .WithColumn("description").AsString(1000).NotNullable()
                .WithColumn("defaultquantity").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("defaultunitprice").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("defaultdeliverydays").AsInt32().Nullable()
                .WithColumn("observations").AsString(1000).Nullable()
                .WithColumn("displayorder").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkproposaltemplateitemproposaltemplate")
                .FromTable("proposaltemplateitem").ForeignColumn("proposaltemplateid")
                .ToTable("proposaltemplate").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("ixproposaltemplateitemproposaltemplateiddisplayorder")
                .OnTable("proposaltemplateitem")
                .OnColumn("proposaltemplateid").Ascending()
                .OnColumn("displayorder").Ascending();

            Create.Table("proposalblock")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("body").AsCustom("text").NotNullable()
                .WithColumn("category").AsString(100).NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdbyuserid").AsInt64().Nullable()
                .WithColumn("createdbyusername").AsString(255).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ixproposalblockcategoryname")
                .OnTable("proposalblock")
                .OnColumn("category").Ascending()
                .OnColumn("name").Ascending();
        }

        public override void Down()
        {
            Delete.Table("proposalblock");
            Delete.Table("proposaltemplateitem");
            Delete.Table("proposaltemplate");
        }
    }
}
