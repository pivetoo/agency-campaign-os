using AgencyCampaign.Application.Models.Reports;
using AgencyCampaign.Domain.Entities;
using System.Globalization;
using System.Net;
using System.Text;

namespace AgencyCampaign.Infrastructure.Services
{
    internal static class ReportHtmlBuilder
    {
        private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

        public static string Build(ReportTable table, AgencySettings agency)
        {
            string primary = string.IsNullOrWhiteSpace(agency.PrimaryColor) ? "#6366f1" : agency.PrimaryColor;
            string display = agency.TradeName ?? agency.AgencyName;

            string[] contactParts = new[] { agency.PrimaryEmail, agency.Phone }
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => Encode(v!))
                .ToArray();
            string contact = string.Join(" · ", contactParts);

            string generatedAt = table.GeneratedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", PtBR);

            string logo = BuildLogoHtml(agency.LogoUrl, display, primary);

            string subtitleHtml = string.IsNullOrWhiteSpace(table.Subtitle)
                ? ""
                : $"<p class=\"subtitle\">{Encode(table.Subtitle)}</p>";

            string kpisHtml = BuildKpisHtml(table.Kpis);
            string tableHtml = BuildTableHtml(table.Columns, table.Rows);

            return Template
                .Replace("{{primary}}", primary)
                .Replace("{{logo}}", logo)
                .Replace("{{agencyName}}", Encode(display))
                .Replace("{{agencyContact}}", contact)
                .Replace("{{generatedAt}}", generatedAt)
                .Replace("{{title}}", Encode(table.Title))
                .Replace("{{subtitleHtml}}", subtitleHtml)
                .Replace("{{kpisHtml}}", kpisHtml)
                .Replace("{{tableHtml}}", tableHtml);
        }

        private static string BuildLogoHtml(string? logoUrl, string agencyDisplayName, string primaryColor)
        {
            if (string.IsNullOrWhiteSpace(logoUrl))
            {
                return $"<div class=\"header-logo-text\" style=\"color:{primaryColor}\">{Encode(agencyDisplayName)}</div>";
            }

            if (!logoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !logoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // LogoUrl carrega query de versão (?v=...); remover antes de resolver o caminho físico
                string relativePath = logoUrl.Split('?')[0].TrimStart('/');
                string physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                if (File.Exists(physicalPath))
                {
                    byte[] bytes = File.ReadAllBytes(physicalPath);
                    string mime = Path.GetExtension(physicalPath).ToLowerInvariant() switch
                    {
                        ".png" => "image/png",
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".webp" => "image/webp",
                        ".svg" => "image/svg+xml",
                        _ => "image/png"
                    };
                    return $"<img class=\"header-logo\" src=\"data:{mime};base64,{Convert.ToBase64String(bytes)}\" alt=\"Logo\" />";
                }
            }

            return $"<img class=\"header-logo\" src=\"{logoUrl}\" alt=\"Logo\" />";
        }

        private static string BuildKpisHtml(IReadOnlyList<ReportKpi> kpis)
        {
            if (kpis.Count == 0)
            {
                return "";
            }

            StringBuilder sb = new();
            sb.Append("<div class=\"kpis\">");
            foreach (ReportKpi kpi in kpis)
            {
                sb.Append("<div class=\"kpi\">");
                sb.Append($"<div class=\"kpi-label\">{Encode(kpi.Label)}</div>");
                sb.Append($"<div class=\"kpi-value\">{Encode(kpi.Value)}</div>");
                sb.Append("</div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string BuildTableHtml(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            if (columns.Count == 0)
            {
                return "<div class=\"empty\">Sem dados.</div>";
            }

            StringBuilder sb = new();
            sb.Append("<table><thead><tr>");
            foreach (string col in columns)
            {
                sb.Append($"<th>{Encode(col)}</th>");
            }
            sb.Append("</tr></thead><tbody>");

            foreach (IReadOnlyList<string> row in rows)
            {
                sb.Append("<tr>");
                for (int i = 0; i < columns.Count; i++)
                {
                    string cell = i < row.Count ? row[i] : "";
                    sb.Append($"<td>{Encode(cell)}</td>");
                }
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value);

        private const string Template = """
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
            <meta charset="UTF-8">
            <style>
            *,*::before,*::after{box-sizing:border-box}
            @page{size:A4;margin:0}
            html,body{margin:0;padding:0;background:#fff}
            body{font-family:-apple-system,'Segoe UI',Arial,Helvetica,sans-serif;font-size:10pt;color:#1f2933;line-height:1.45}
            .page{width:210mm;min-height:297mm;padding:0 0 20mm 0;position:relative}
            .brandbar{height:9px;background:linear-gradient(90deg,{{primary}},color-mix(in srgb,{{primary}} 50%,#00B3C7))}
            .content{padding:14mm 18mm 0 18mm}
            .header{display:flex;align-items:center;justify-content:space-between;margin-bottom:16px}
            .header-logo{height:40px;max-width:180px;object-fit:contain}
            .header-logo-text{font-size:15pt;font-weight:800;color:{{primary}}}
            .header-meta{text-align:right;font-size:8pt;color:#7b8794}
            .header-meta .agency{font-weight:700;color:{{primary}};font-size:9.5pt}
            .title{font-size:19pt;font-weight:800;color:#1f2933;margin:0}
            .subtitle{font-size:10pt;color:#7b8794;margin:3px 0 0}
            .divider{height:2px;background:color-mix(in srgb,{{primary}} 22%,#ffffff);margin:14px 0 18px;border-radius:2px}
            .kpis{display:flex;gap:10px;margin-bottom:18px;flex-wrap:wrap}
            .kpi{flex:1 1 0;min-width:120px;background:color-mix(in srgb,{{primary}} 8%,#ffffff);border:1px solid color-mix(in srgb,{{primary}} 16%,#ffffff);border-left:4px solid {{primary}};border-radius:9px;padding:11px 13px}
            .kpi-label{font-size:7.5pt;text-transform:uppercase;letter-spacing:.04em;color:#7b8794;font-weight:600}
            .kpi-value{font-size:15pt;font-weight:800;color:{{primary}};margin-top:3px}
            table{width:100%;border-collapse:collapse;font-size:8.5pt;margin-top:2px}
            thead th{background:{{primary}};color:#fff;text-align:left;padding:8px 10px;font-weight:600;font-size:8pt}
            thead th:first-child{border-top-left-radius:7px}
            thead th:last-child{border-top-right-radius:7px}
            tbody td{padding:7px 10px;border-bottom:1px solid #eef1f4}
            tbody tr:nth-child(even){background:#f7f9fb}
            .empty{padding:24px;text-align:center;color:#9aa5b1;font-size:9pt}
            .footer{position:absolute;bottom:9mm;left:18mm;right:18mm;display:flex;justify-content:space-between;font-size:7.5pt;color:#9aa5b1;border-top:1px solid #eef1f4;padding-top:6px}
            </style>
            </head>
            <body>
            <div class="page">
              <div class="brandbar"></div>
              <div class="content">
                <div class="header">
                  {{logo}}
                  <div class="header-meta">
                    <div class="agency">{{agencyName}}</div>
                    <div>{{agencyContact}}</div>
                    <div>Gerado em {{generatedAt}}</div>
                  </div>
                </div>
                <h1 class="title">{{title}}</h1>
                {{subtitleHtml}}
                <div class="divider"></div>
                {{kpisHtml}}
                {{tableHtml}}
              </div>
              <div class="footer"><span>{{agencyName}}</span><span>Kanvas &middot; by Mainstay</span></div>
            </div>
            </body>
            </html>
            """;
    }
}
