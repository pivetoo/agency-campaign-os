using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605210001)]
    public sealed class Migration_202605210001_AddOpportunityWinLossReasons : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS opportunitywinreason (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(120) NOT NULL,
                    color VARCHAR(32) NOT NULL DEFAULT '#15803d',
                    displayorder INT NOT NULL DEFAULT 0,
                    isactive BOOLEAN NOT NULL DEFAULT TRUE,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ
                );

                CREATE TABLE IF NOT EXISTS opportunitylossreason (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(120) NOT NULL,
                    color VARCHAR(32) NOT NULL DEFAULT '#b91c1c',
                    displayorder INT NOT NULL DEFAULT 0,
                    isactive BOOLEAN NOT NULL DEFAULT TRUE,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ
                );

                ALTER TABLE opportunity ADD COLUMN IF NOT EXISTS winreasonid BIGINT;
                ALTER TABLE opportunity ADD COLUMN IF NOT EXISTS lossreasonid BIGINT;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_opportunity_winreason') THEN
                        ALTER TABLE opportunity ADD CONSTRAINT fk_opportunity_winreason FOREIGN KEY (winreasonid) REFERENCES opportunitywinreason(id) ON DELETE SET NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_opportunity_lossreason') THEN
                        ALTER TABLE opportunity ADD CONSTRAINT fk_opportunity_lossreason FOREIGN KEY (lossreasonid) REFERENCES opportunitylossreason(id) ON DELETE SET NULL;
                    END IF;
                END $$;

                INSERT INTO opportunitywinreason (name, color, displayorder)
                SELECT v.name, v.color, v.displayorder FROM (VALUES
                    ('Melhor proposta', '#15803d', 1),
                    ('Relacionamento existente', '#0f766e', 2),
                    ('Diferencial criativo', '#7c3aed', 3),
                    ('Preço competitivo', '#2563eb', 4),
                    ('Outro', '#6b7280', 99)
                ) AS v(name, color, displayorder)
                WHERE NOT EXISTS (SELECT 1 FROM opportunitywinreason);

                INSERT INTO opportunitylossreason (name, color, displayorder)
                SELECT v.name, v.color, v.displayorder FROM (VALUES
                    ('Preço', '#b91c1c', 1),
                    ('Concorrente', '#dc2626', 2),
                    ('Timing', '#d97706', 3),
                    ('Sem fit', '#6b7280', 4),
                    ('Sem resposta', '#94a3b8', 5),
                    ('Outro', '#475569', 99)
                ) AS v(name, color, displayorder)
                WHERE NOT EXISTS (SELECT 1 FROM opportunitylossreason);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE opportunity DROP CONSTRAINT IF EXISTS fk_opportunity_winreason;
                ALTER TABLE opportunity DROP CONSTRAINT IF EXISTS fk_opportunity_lossreason;
                ALTER TABLE opportunity DROP COLUMN IF EXISTS winreasonid;
                ALTER TABLE opportunity DROP COLUMN IF EXISTS lossreasonid;
                DROP TABLE IF EXISTS opportunitywinreason;
                DROP TABLE IF EXISTS opportunitylossreason;
            ");
        }
    }
}
