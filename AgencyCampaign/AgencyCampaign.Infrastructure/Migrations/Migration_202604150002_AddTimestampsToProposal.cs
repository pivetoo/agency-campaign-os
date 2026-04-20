using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604150002)]
    public sealed class Migration_202604150002_AddTimestampsToProposal : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("proposal").Column("createdat").Exists())
            {
                Alter.Table("proposal")
                    .AddColumn("createdat").AsDateTimeOffset().NotNullable().WithDefaultValue(DateTimeOffset.UtcNow);
            }

            if (!Schema.Table("proposal").Column("updatedat").Exists())
            {
                Alter.Table("proposal")
                    .AddColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("proposalitem").Column("createdat").Exists())
            {
                Alter.Table("proposalitem")
                    .AddColumn("createdat").AsDateTimeOffset().NotNullable().WithDefaultValue(DateTimeOffset.UtcNow);
            }

            if (!Schema.Table("proposalitem").Column("updatedat").Exists())
            {
                Alter.Table("proposalitem")
                    .AddColumn("updatedat").AsDateTimeOffset().Nullable();
            }
        }

        public override void Down()
        {
            Delete.Column("updatedat").FromTable("proposalitem");
            Delete.Column("createdat").FromTable("proposalitem");
            Delete.Column("updatedat").FromTable("proposal");
            Delete.Column("createdat").FromTable("proposal");
        }
    }
}