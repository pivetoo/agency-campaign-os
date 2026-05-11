using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604270002)]
    public sealed class Migration_202604270002_FixCommercialResponsibleColumnNames : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("commercial_responsible").Exists())
                return;

            if (Schema.Table("commercial_responsible").Column("created_at").Exists())
            {
                Delete.Column("created_at").FromTable("commercial_responsible");
            }

            if (Schema.Table("commercial_responsible").Column("updated_at").Exists())
            {
                Delete.Column("updated_at").FromTable("commercial_responsible");
            }

            if (Schema.Table("commercial_responsible").Column("is_active").Exists())
            {
                Delete.Column("is_active").FromTable("commercial_responsible");
            }

            Alter.Table("commercial_responsible")
                .AddColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true);

            Alter.Table("commercial_responsible")
                .AddColumn("createdat").AsDateTimeOffset().NotNullable();

            Alter.Table("commercial_responsible")
                .AddColumn("updatedat").AsDateTimeOffset().Nullable();

            Execute.Sql("UPDATE commercial_responsible SET createdat = CURRENT_TIMESTAMP, updatedat = CURRENT_TIMESTAMP;");

            if (Schema.Table("opportunity").Column("commercial_responsible_id").Exists())
            {
                Delete.ForeignKey("fk_opportunity_commercial_responsible").OnTable("opportunity");
                Delete.Index("ix_opportunity_commercial_responsible_id").OnTable("opportunity");

                Execute.Sql("ALTER TABLE opportunity RENAME COLUMN commercial_responsible_id TO commercialresponsibleid;");

                Create.Index("ixopportunitycommercialresponsibleid")
                    .OnTable("opportunity")
                    .OnColumn("commercialresponsibleid").Ascending();

                Create.ForeignKey("fkopportunitycommercialresponsible")
                    .FromTable("opportunity").ForeignColumn("commercialresponsibleid")
                    .ToTable("commercial_responsible").PrimaryColumn("id")
                    .OnDelete(System.Data.Rule.SetNull);
            }
        }

        public override void Down()
        {
            if (Schema.Table("opportunity").Column("commercialresponsibleid").Exists())
            {
                Delete.ForeignKey("fkopportunitycommercialresponsible").OnTable("opportunity");
                Delete.Index("ixopportunitycommercialresponsibleid").OnTable("opportunity");

                Execute.Sql("ALTER TABLE opportunity RENAME COLUMN commercialresponsibleid TO commercial_responsible_id;");

                Create.Index("ix_opportunity_commercial_responsible_id")
                    .OnTable("opportunity")
                    .OnColumn("commercial_responsible_id").Ascending();

                Create.ForeignKey("fk_opportunity_commercial_responsible")
                    .FromTable("opportunity").ForeignColumn("commercial_responsible_id")
                    .ToTable("commercial_responsible").PrimaryColumn("id")
                    .OnDelete(System.Data.Rule.SetNull);
            }

            if (Schema.Table("commercial_responsible").Column("isactive").Exists())
            {
                Delete.Column("isactive").FromTable("commercial_responsible");
            }

            if (Schema.Table("commercial_responsible").Column("createdat").Exists())
            {
                Delete.Column("createdat").FromTable("commercial_responsible");
            }

            if (Schema.Table("commercial_responsible").Column("updatedat").Exists())
            {
                Delete.Column("updatedat").FromTable("commercial_responsible");
            }

            Alter.Table("commercial_responsible")
                .AddColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true);

            Alter.Table("commercial_responsible")
                .AddColumn("created_at").AsDateTimeOffset().NotNullable();

            Alter.Table("commercial_responsible")
                .AddColumn("updated_at").AsDateTimeOffset().Nullable();
        }
    }
}
