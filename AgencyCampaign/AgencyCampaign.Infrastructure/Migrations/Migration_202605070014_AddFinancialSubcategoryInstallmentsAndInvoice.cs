using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070014)]
    public sealed class Migration_202605070014_AddFinancialSubcategoryInstallmentsAndInvoice : Migration
    {
        public override void Up()
        {
            Create.Table("financialsubcategory")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(120).NotNullable()
                .WithColumn("macrocategory").AsInt32().NotNullable()
                .WithColumn("color").AsString(32).NotNullable().WithDefaultValue("#6366f1")
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ixfinancialsubcategorymacrocategory")
                .OnTable("financialsubcategory")
                .OnColumn("macrocategory").Ascending();

            Alter.Table("financialentry")
                .AddColumn("subcategoryid").AsInt64().Nullable()
                .AddColumn("parententryid").AsInt64().Nullable()
                .AddColumn("installmentnumber").AsInt32().Nullable()
                .AddColumn("installmenttotal").AsInt32().Nullable()
                .AddColumn("invoicenumber").AsString(60).Nullable()
                .AddColumn("invoiceurl").AsString(500).Nullable()
                .AddColumn("invoiceissuedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkfinancialentrysubcategory")
                .FromTable("financialentry").ForeignColumn("subcategoryid")
                .ToTable("financialsubcategory").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.SetNull);

            Create.ForeignKey("fkfinancialentryparent")
                .FromTable("financialentry").ForeignColumn("parententryid")
                .ToTable("financialentry").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("ixfinancialentryparententryid")
                .OnTable("financialentry")
                .OnColumn("parententryid").Ascending();

            Create.Index("ixfinancialentrysubcategoryid")
                .OnTable("financialentry")
                .OnColumn("subcategoryid").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ixfinancialentrysubcategoryid").OnTable("financialentry");
            Delete.Index("ixfinancialentryparententryid").OnTable("financialentry");
            Delete.ForeignKey("fkfinancialentryparent").OnTable("financialentry");
            Delete.ForeignKey("fkfinancialentrysubcategory").OnTable("financialentry");

            Delete.Column("invoiceissuedat").FromTable("financialentry");
            Delete.Column("invoiceurl").FromTable("financialentry");
            Delete.Column("invoicenumber").FromTable("financialentry");
            Delete.Column("installmenttotal").FromTable("financialentry");
            Delete.Column("installmentnumber").FromTable("financialentry");
            Delete.Column("parententryid").FromTable("financialentry");
            Delete.Column("subcategoryid").FromTable("financialentry");

            Delete.Index("ixfinancialsubcategorymacrocategory").OnTable("financialsubcategory");
            Delete.Table("financialsubcategory");
        }
    }
}
