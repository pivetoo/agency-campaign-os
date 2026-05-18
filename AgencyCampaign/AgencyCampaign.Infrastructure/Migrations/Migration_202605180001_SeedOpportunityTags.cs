using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180001)]
    public sealed class Migration_202605180001_SeedOpportunityTags : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                INSERT INTO opportunitytag (name, color, isactive, createdat)
                SELECT v.name, v.color, true, NOW()
                FROM (VALUES
                    ('Hot Lead',           '#ef4444'),
                    ('Urgente',            '#f97316'),
                    ('Briefing Pendente',  '#f59e0b'),
                    ('Cliente Recorrente', '#10b981'),
                    ('Novo Cliente',       '#3b82f6'),
                    ('Alto Valor',         '#8b5cf6'),
                    ('Sazonal',            '#ec4899'),
                    ('Cross-Sell',         '#06b6d4')
                ) AS v(name, color)
                WHERE NOT EXISTS (
                    SELECT 1 FROM opportunitytag t WHERE t.name = v.name
                );
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DELETE FROM opportunitytag
                WHERE name IN (
                    'Hot Lead',
                    'Urgente',
                    'Briefing Pendente',
                    'Cliente Recorrente',
                    'Novo Cliente',
                    'Alto Valor',
                    'Sazonal',
                    'Cross-Sell'
                );
            ");
        }
    }
}
