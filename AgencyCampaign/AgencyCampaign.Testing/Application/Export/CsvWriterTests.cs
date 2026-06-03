using AgencyCampaign.Application.Export;

namespace AgencyCampaign.Testing.Application.Export
{
    [TestFixture]
    public sealed class CsvWriterTests
    {
        [Test]
        public void Build_should_emit_header_then_rows_with_crlf_and_semicolon()
        {
            string csv = CsvWriter.Build(["A", "B"], [["1", "2"], ["3", "4"]]);

            csv.Should().Be("A;B\r\n1;2\r\n3;4\r\n");
        }

        [Test]
        public void Build_should_emit_header_only_when_no_rows()
        {
            string csv = CsvWriter.Build(["A", "B"], []);

            csv.Should().Be("A;B\r\n");
        }

        [Test]
        public void Build_should_quote_cells_with_delimiter_quote_or_newline()
        {
            string csv = CsvWriter.Build(["X"], [["a;b"], ["a\"b"], ["a\nb"]]);

            csv.Should().Be("X\r\n\"a;b\"\r\n\"a\"\"b\"\r\n\"a\nb\"\r\n");
        }

        [Test]
        public void Build_should_guard_formula_injection_with_leading_apostrophe()
        {
            string csv = CsvWriter.Build(["X"], [["=SUM(A1)"], ["+1"], ["@cmd"]]);

            csv.Should().Be("X\r\n'=SUM(A1)\r\n'+1\r\n'@cmd\r\n");
        }

        [Test]
        public void Build_should_not_guard_negative_numbers()
        {
            string csv = CsvWriter.Build(["X"], [["-100,50"]]);

            csv.Should().Be("X\r\n-100,50\r\n");
        }
    }
}
