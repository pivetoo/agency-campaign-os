using AgencyCampaign.Application.Models.Reports;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using System.Net;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ReportHtmlBuilderTests
    {
        private static readonly DateTimeOffset FixedDate = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        private static ReportTable BuildTable()
        {
            return new ReportTable
            {
                Title = "Relatório Teste",
                Subtitle = "Periodo X",
                GeneratedAt = FixedDate,
                Kpis =
                [
                    new ReportKpi { Label = "Receita", Value = "R$ 100" },
                    new ReportKpi { Label = "Margem", Value = "20%" },
                ],
                Columns = ["Campanha", "Valor"],
                Rows =
                [
                    ["Alpha", "R$ 60"],
                    ["Beta", "R$ 40"],
                ],
            };
        }

        [Test]
        public void Build_should_contain_title_and_subtitle()
        {
            AgencySettings agency = new("Acme");
            agency.Update("Acme", null, null, null, null, null, null, "#3b82f6", null, null);
            ReportTable table = BuildTable();

            string html = ReportHtmlBuilder.Build(table, agency);

            html.Should().Contain(WebUtility.HtmlEncode("Relatório Teste"));
            html.Should().Contain("Periodo X");
        }

        [Test]
        public void Build_should_contain_agency_name()
        {
            AgencySettings agency = new("Acme");
            agency.Update("Acme", null, null, null, null, null, null, "#3b82f6", null, null);
            ReportTable table = BuildTable();

            string html = ReportHtmlBuilder.Build(table, agency);

            html.Should().Contain("Acme");
        }

        [Test]
        public void Build_should_apply_primary_color()
        {
            AgencySettings agency = new("Acme");
            agency.Update("Acme", null, null, null, null, null, null, "#3b82f6", null, null);
            ReportTable table = BuildTable();

            string html = ReportHtmlBuilder.Build(table, agency);

            html.Should().Contain("#3b82f6");
        }

        [Test]
        public void Build_should_fall_back_to_default_color_when_primary_color_is_null()
        {
            AgencySettings agency = new("Acme");
            ReportTable table = BuildTable();

            string html = ReportHtmlBuilder.Build(table, agency);

            html.Should().Contain("#6366f1");
        }

        [Test]
        public void Build_should_contain_kpi_labels_and_values()
        {
            AgencySettings agency = new("Acme");
            agency.Update("Acme", null, null, null, null, null, null, "#3b82f6", null, null);
            ReportTable table = BuildTable();

            string html = ReportHtmlBuilder.Build(table, agency);

            html.Should().Contain("Receita");
            html.Should().Contain("R$ 100");
        }

        [Test]
        public void Build_should_contain_table_columns_and_rows()
        {
            AgencySettings agency = new("Acme");
            agency.Update("Acme", null, null, null, null, null, null, "#3b82f6", null, null);
            ReportTable table = BuildTable();

            string html = ReportHtmlBuilder.Build(table, agency);

            html.Should().Contain("Campanha");
            html.Should().Contain("Alpha");
            html.Should().Contain("R$ 60");
        }

        [Test]
        public void Build_should_reject_malicious_primary_color_and_use_fallback()
        {
            AgencySettings agency = new("Acme");
            agency.Update("Acme", null, null, null, null, null, null, "red}</style><script>x</script>", null, null);
            ReportTable table = BuildTable();

            string html = ReportHtmlBuilder.Build(table, agency);

            html.Should().NotContain("</script>");
            html.Should().NotContain("</style><script");
            html.Should().Contain("#6366f1");
        }

        [Test]
        public void Build_should_pass_through_valid_hex_color()
        {
            AgencySettings agency = new("Acme");
            agency.Update("Acme", null, null, null, null, null, null, "#1F3B61", null, null);
            ReportTable table = BuildTable();

            string html = ReportHtmlBuilder.Build(table, agency);

            html.Should().Contain("#1F3B61");
            html.Should().NotContain("#6366f1");
        }
    }
}
