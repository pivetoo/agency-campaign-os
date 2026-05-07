using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070012)]
    public sealed class Migration_202605070012_RenameAndExpandFinancialModule : Migration
    {
        public override void Up()
        {
            Create.Table("financialaccount")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(120).NotNullable()
                .WithColumn("type").AsInt32().NotNullable().WithDefaultValue(2)
                .WithColumn("bank").AsString(120).Nullable()
                .WithColumn("agency").AsString(50).Nullable()
                .WithColumn("number").AsString(50).Nullable()
                .WithColumn("initialbalance").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("color").AsString(32).NotNullable().WithDefaultValue("#6366f1")
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Insert.IntoTable("financialaccount").Row(new
            {
                name = "Caixa",
                type = 2,
                initialbalance = 0,
                color = "#6366f1",
                isactive = true,
                createdat = SystemMethods.CurrentUTCDateTime,
                updatedat = SystemMethods.CurrentUTCDateTime
            });

            Execute.Sql("ALTER TABLE campaignfinancialentry RENAME TO financialentry;");

            Alter.Table("financialentry")
                .AddColumn("accountid").AsInt64().Nullable();

            Execute.Sql("UPDATE financialentry SET accountid = (SELECT id FROM financialaccount ORDER BY id LIMIT 1);");

            Alter.Column("accountid").OnTable("financialentry").AsInt64().NotNullable();

            Execute.Sql("ALTER TABLE financialentry ALTER COLUMN campaignid DROP NOT NULL;");

            Create.ForeignKey("fkfinancialentryaccount")
                .FromTable("financialentry").ForeignColumn("accountid")
                .ToTable("financialaccount").PrimaryColumn("id");

            Create.Index("ixfinancialentryaccountiddueat")
                .OnTable("financialentry")
                .OnColumn("accountid").Ascending()
                .OnColumn("dueat").Descending();
        }

        public override void Down()
        {
            Delete.Index("ixfinancialentryaccountiddueat").OnTable("financialentry");
            Delete.ForeignKey("fkfinancialentryaccount").OnTable("financialentry");
            Execute.Sql("ALTER TABLE financialentry ALTER COLUMN campaignid SET NOT NULL;");
            Delete.Column("accountid").FromTable("financialentry");
            Execute.Sql("ALTER TABLE financialentry RENAME TO campaignfinancialentry;");
            Delete.Table("financialaccount");
        }
    }
}
