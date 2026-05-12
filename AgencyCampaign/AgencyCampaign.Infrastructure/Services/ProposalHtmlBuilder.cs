using AgencyCampaign.Domain.Entities;
using System.Globalization;
using System.Net;
using System.Text;

namespace AgencyCampaign.Infrastructure.Services
{
    internal static class ProposalHtmlBuilder
    {
        private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

        public static string Build(Proposal proposal, AgencySettings agency)
        {
            string primaryColor = string.IsNullOrWhiteSpace(agency.PrimaryColor) ? "#6366f1" : agency.PrimaryColor;
            string agencyDisplayName = agency.TradeName ?? agency.AgencyName;

            return Template
                .Replace("{PRIMARY_COLOR}", primaryColor)
                .Replace("{LOGO_HTML}", BuildLogoHtml(agency.LogoUrl, agencyDisplayName))
                .Replace("{AGENCY_DISPLAY_NAME}", Encode(agencyDisplayName))
                .Replace("{AGENCY_EMAIL_HTML}", BuildAgencyEmailHtml(agency.PrimaryEmail))
                .Replace("{PROPOSAL_NAME}", Encode(proposal.Name))
                .Replace("{DESCRIPTION_HTML}", BuildDescriptionHtml(proposal.Description))
                .Replace("{CLIENT_ROW}", BuildClientRow(proposal))
                .Replace("{AGENCY_NAME}", Encode(agency.AgencyName))
                .Replace("{AGENCY_DOCUMENT}", BuildAgencyDocument(agency.Document))
                .Replace("{EMISSION_DATE}", DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", PtBR))
                .Replace("{OWNER_ROW}", BuildOwnerRow(proposal.InternalOwnerName))
                .Replace("{ITEMS_TABLE}", BuildItemsTable(proposal))
                .Replace("{TOTALS_HTML}", BuildTotals(proposal))
                .Replace("{VALIDITY_HTML}", BuildValidity(proposal))
                .Replace("{NOTES_HTML}", BuildNotes(proposal))
                .Replace("{FOOTER_CONTACT}", BuildFooterContact(agency));
        }

        private static string BuildLogoHtml(string? logoUrl, string agencyDisplayName)
        {
            if (string.IsNullOrWhiteSpace(logoUrl))
            {
                return $"<div class=\"header-logo-text\">{Encode(agencyDisplayName)}</div>";
            }

            if (!logoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !logoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                string physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", logoUrl.TrimStart('/'));
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

        private static string BuildAgencyEmailHtml(string? email) =>
            string.IsNullOrWhiteSpace(email)
                ? ""
                : $"<div class=\"header-agency-email\">{Encode(email)}</div>";

        private static string BuildDescriptionHtml(string? description) =>
            string.IsNullOrWhiteSpace(description)
                ? ""
                : $"<p class=\"proposal-description\">{Encode(description)}</p>";

        private static string BuildClientRow(Proposal proposal)
        {
            string? brandName = proposal.Opportunity?.Brand?.Name;
            return string.IsNullOrWhiteSpace(brandName)
                ? ""
                : $"<tr><td class=\"label\">Cliente:</td><td>{Encode(brandName)}</td></tr>";
        }

        private static string BuildAgencyDocument(string? document) =>
            string.IsNullOrWhiteSpace(document) ? "" : $" ({Encode(document)})";

        private static string BuildOwnerRow(string? owner) =>
            string.IsNullOrWhiteSpace(owner)
                ? ""
                : $"<tr><td class=\"label\">Responsável:</td><td>{Encode(owner)}</td></tr>";

        private static string BuildItemsTable(Proposal proposal)
        {
            if (proposal.Items.Count == 0)
            {
                return "<p class=\"empty-items\">Nenhum item registrado.</p>";
            }

            StringBuilder sb = new();
            sb.Append("<table class=\"items\"><thead><tr>");
            sb.Append("<th>Creator</th><th>Descrição</th>");
            sb.Append("<th class=\"right\">Qtd</th><th class=\"right\">Valor unit.</th><th class=\"right\">Total</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (ProposalItem item in proposal.Items)
            {
                string creator = item.Creator?.StageName ?? item.Creator?.Name ?? "—";
                string description = item.Description ?? "—";
                sb.Append("<tr>");
                sb.Append($"<td>{Encode(creator)}</td>");
                sb.Append($"<td>{Encode(description)}</td>");
                sb.Append($"<td class=\"right\">{item.Quantity.ToString("0.##", PtBR)}</td>");
                sb.Append($"<td class=\"right\">{item.UnitPrice.ToString("C", PtBR)}</td>");
                sb.Append($"<td class=\"right\"><strong>{item.Total.ToString("C", PtBR)}</strong></td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        private static string BuildTotals(Proposal proposal) =>
            $"""
            <div class="total-row">
              <span class="total-label">Total da proposta</span>
              <span class="total-value">{proposal.TotalValue.ToString("C", PtBR)}</span>
            </div>
            """;

        private static string BuildValidity(Proposal proposal) =>
            proposal.ValidityUntil.HasValue
                ? $"<p class=\"validity\">Validade desta proposta: <strong>{proposal.ValidityUntil.Value.ToString("dd/MM/yyyy", PtBR)}</strong></p>"
                : "";

        private static string BuildNotes(Proposal proposal) =>
            string.IsNullOrWhiteSpace(proposal.Notes)
                ? ""
                : $"""
                  <div class="section-heading">Observações</div>
                  <div class="notes-box">{Encode(proposal.Notes)}</div>
                  """;

        private static string BuildFooterContact(AgencySettings agency)
        {
            string[] parts = new[] { agency.PrimaryEmail, agency.Phone, agency.Address }
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => Encode(v!))
                .ToArray();

            return parts.Length == 0 ? "" : $"<div>{string.Join(" · ", parts)}</div>";
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value);

        private const string Template = """
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
            <meta charset="UTF-8">
            <style>
            *, *::before, *::after { box-sizing: border-box; }
            @page { size: A4; margin: 0; }
            html, body { margin: 0; padding: 0; background: #fff; }
            body {
              font-family: -apple-system, 'Segoe UI', Arial, Helvetica, sans-serif;
              font-size: 10pt;
              color: #1a1a2e;
              line-height: 1.5;
            }
            .page {
              width: 210mm;
              min-height: 297mm;
              padding: 18mm 20mm 16mm 20mm;
            }
            .header {
              display: flex;
              align-items: center;
              justify-content: space-between;
              padding-bottom: 12px;
              border-bottom: 2.5px solid {PRIMARY_COLOR};
              margin-bottom: 26px;
            }
            .header-logo { height: 36px; max-width: 160px; object-fit: contain; }
            .header-logo-text { font-size: 13pt; font-weight: 700; color: {PRIMARY_COLOR}; }
            .header-agency { text-align: right; }
            .header-agency-name { font-size: 10pt; font-weight: 700; color: {PRIMARY_COLOR}; }
            .header-agency-email { font-size: 8pt; color: #888; margin-top: 2px; }
            .eyebrow {
              font-size: 7.5pt;
              font-weight: 700;
              letter-spacing: 0.14em;
              color: {PRIMARY_COLOR};
              text-transform: uppercase;
              margin-bottom: 5px;
            }
            .proposal-title {
              font-size: 21pt;
              font-weight: 800;
              color: #0f0f1a;
              line-height: 1.2;
              margin-bottom: 4px;
            }
            .proposal-description {
              font-size: 9.5pt;
              color: #555;
              margin-top: 6px;
              max-width: 520px;
            }
            .metadata { margin-top: 14px; }
            .metadata table { border: none; border-collapse: collapse; }
            .metadata td { padding: 2px 14px 2px 0; border: none; font-size: 9.5pt; color: #444; vertical-align: top; }
            .metadata td.label { font-weight: 600; color: #222; white-space: nowrap; }
            .divider { border: none; border-top: 1px solid #e5e7eb; margin: 20px 0; }
            .section-heading {
              font-size: 8pt;
              font-weight: 700;
              letter-spacing: 0.1em;
              text-transform: uppercase;
              color: #777;
              margin: 20px 0 10px 0;
            }
            table.items { width: 100%; border-collapse: collapse; font-size: 9pt; }
            table.items thead tr { background: {PRIMARY_COLOR}; color: #fff; }
            table.items thead th { padding: 8px 10px; font-weight: 600; text-align: left; font-size: 8.5pt; }
            table.items thead th.right { text-align: right; }
            table.items tbody tr:nth-child(even) { background: #f7f8fc; }
            table.items tbody td { padding: 8px 10px; border-bottom: 1px solid #eee; vertical-align: top; }
            table.items tbody td.right { text-align: right; }
            .empty-items { font-size: 9pt; color: #aaa; font-style: italic; margin-top: 8px; }
            .total-row {
              display: flex;
              justify-content: flex-end;
              align-items: baseline;
              gap: 12px;
              margin-top: 14px;
              padding-top: 12px;
              border-top: 2px solid {PRIMARY_COLOR};
            }
            .total-label { font-size: 10pt; font-weight: 600; color: #666; }
            .total-value { font-size: 20pt; font-weight: 800; color: #0f0f1a; }
            .validity { font-size: 9pt; color: #666; margin-top: 14px; }
            .validity strong { color: #222; }
            .notes-box {
              background: #f7f8fc;
              border-left: 3px solid {PRIMARY_COLOR};
              border-radius: 3px;
              padding: 10px 14px;
              font-size: 9pt;
              color: #444;
              white-space: pre-wrap;
              margin-top: 6px;
            }
            .footer {
              margin-top: 36px;
              padding-top: 10px;
              border-top: 1px solid #e5e7eb;
              font-size: 7.5pt;
              color: #aaa;
              text-align: center;
            }
            .footer > div { margin-bottom: 2px; }
            </style>
            </head>
            <body>
            <div class="page">

              <div class="header">
                {LOGO_HTML}
                <div class="header-agency">
                  <div class="header-agency-name">{AGENCY_DISPLAY_NAME}</div>
                  {AGENCY_EMAIL_HTML}
                </div>
              </div>

              <div class="eyebrow">Proposta Comercial</div>
              <div class="proposal-title">{PROPOSAL_NAME}</div>
              {DESCRIPTION_HTML}

              <div class="metadata">
                <table>
                  {CLIENT_ROW}
                  <tr><td class="label">Agência:</td><td>{AGENCY_NAME}{AGENCY_DOCUMENT}</td></tr>
                  <tr><td class="label">Data de emissão:</td><td>{EMISSION_DATE}</td></tr>
                  {OWNER_ROW}
                </table>
              </div>

              <hr class="divider">

              <div class="section-heading">Itens</div>
              {ITEMS_TABLE}

              {TOTALS_HTML}
              {VALIDITY_HTML}
              {NOTES_HTML}

              <div class="footer">
                {FOOTER_CONTACT}
                <div>Documento gerado automaticamente.</div>
              </div>

            </div>
            </body>
            </html>
            """;
    }
}
