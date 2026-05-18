using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180004)]
    public sealed class Migration_202605180004_AddProposalLayoutId : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE proposal
                    ADD COLUMN IF NOT EXISTS proposallayoutid BIGINT NULL;
            ");

            Execute.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE constraint_name = 'fk_proposal_proposallayout'
                    ) THEN
                        ALTER TABLE proposal
                            ADD CONSTRAINT fk_proposal_proposallayout
                            FOREIGN KEY (proposallayoutid)
                            REFERENCES proposaltemplateversion(id)
                            ON DELETE SET NULL;
                    END IF;
                END $$;
            ");

            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_proposal_proposallayoutid ON proposal(proposallayoutid);");
        }

        public override void Down()
        {
            Execute.Sql("DROP INDEX IF EXISTS ix_proposal_proposallayoutid;");
            Execute.Sql("ALTER TABLE proposal DROP CONSTRAINT IF EXISTS fk_proposal_proposallayout;");
            Execute.Sql("ALTER TABLE proposal DROP COLUMN IF EXISTS proposallayoutid;");
        }
    }
}
