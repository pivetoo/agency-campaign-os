using AgencyCampaign.Application.Models;
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
            string template = !string.IsNullOrWhiteSpace(agency.ProposalHtmlTemplate)
                ? agency.ProposalHtmlTemplate
                : PadraoTemplate;

            AgencyData agencyData = MapAgency(agency);
            ProposalData proposalData = MapProposal(proposal);
            Dictionary<string, string> vars = BuildVariables(proposalData, agencyData);
            return ApplyVariables(template, vars);
        }

        public static string BuildPreview(string template, AgencySettings agency)
        {
            AgencyData agencyData = MapAgency(agency);
            ProposalData proposalData = BuildMockProposal();
            Dictionary<string, string> vars = BuildVariables(proposalData, agencyData);
            return ApplyVariables(template, vars);
        }

        public static IReadOnlyList<ProposalLayoutModel> GetLayouts()
        {
            return
            [
                new ProposalLayoutModel { Key = "padrao", Name = "Padrão", Template = PadraoTemplate },
            ];
        }

        // --- mapping ---

        private static AgencyData MapAgency(AgencySettings agency)
        {
            string primaryColor = string.IsNullOrWhiteSpace(agency.PrimaryColor) ? "#6366f1" : agency.PrimaryColor;
            string displayName = agency.TradeName ?? agency.AgencyName;
            return new AgencyData(agency.AgencyName, displayName, primaryColor, agency.LogoUrl, agency.PrimaryEmail, agency.Phone, agency.Address, agency.Document);
        }

        private static ProposalData MapProposal(Proposal proposal)
        {
            string? brandName = proposal.Opportunity?.Brand?.Name;
            IReadOnlyList<ProposalItemData> items = proposal.Items
                .Select(i => new ProposalItemData(
                    i.Creator?.StageName ?? i.Creator?.Name ?? "—",
                    i.Description,
                    i.Quantity,
                    i.UnitPrice,
                    i.Total))
                .ToList();
            return new ProposalData(
                proposal.Name,
                proposal.Description,
                string.IsNullOrWhiteSpace(brandName) ? null : brandName,
                proposal.InternalOwnerName,
                items,
                proposal.TotalValue,
                proposal.ValidityUntil,
                proposal.Notes);
        }

        private static ProposalData BuildMockProposal()
        {
            IReadOnlyList<ProposalItemData> items =
            [
                new ProposalItemData("Ana Silva (@anasilva)", "Reels patrocinado + Stories", 4, 1200m, 4800m),
                new ProposalItemData("João Costa (@jcosta)", "Vídeo YouTube — 10 min", 2, 3500m, 7000m),
                new ProposalItemData("Lua Mendes (@luamendes)", "Post no feed + carrossel", 3, 800m, 2400m),
            ];
            return new ProposalData(
                Name: "Campanha de Lançamento — Produto XYZ",
                Description: "Proposta comercial para campanha digital completa, incluindo produção de conteúdo no Instagram, TikTok e YouTube.",
                Client: "Empresa ABC",
                Owner: "Maria Oliveira",
                Items: items,
                TotalValue: 14200m,
                ValidityUntil: DateTimeOffset.UtcNow.AddDays(30),
                Notes: "Valores sujeitos ao briefing final aprovado. Pagamento: 50% na assinatura e 50% na entrega.");
        }

        // --- variable building ---

        private static Dictionary<string, string> BuildVariables(ProposalData proposal, AgencyData agency)
        {
            return new Dictionary<string, string>
            {
                ["agency.primaryColor"] = agency.PrimaryColor,
                ["agency.name"] = Encode(agency.AgencyName),
                ["agency.displayName"] = Encode(agency.DisplayName),
                ["agency.email"] = Encode(agency.PrimaryEmail ?? ""),
                ["agency.phone"] = Encode(agency.Phone ?? ""),
                ["agency.address"] = Encode(agency.Address ?? ""),
                ["agency.document"] = Encode(agency.Document ?? ""),
                ["agency.documentSuffix"] = string.IsNullOrWhiteSpace(agency.Document) ? "" : $" ({Encode(agency.Document)})",
                ["agency.nameWithDocument"] = string.IsNullOrWhiteSpace(agency.Document)
                    ? Encode(agency.AgencyName)
                    : $"{Encode(agency.AgencyName)} ({Encode(agency.Document)})",
                ["agency.contactLine"] = BuildContactLine(agency),
                ["agency.logo"] = BuildLogoHtml(agency.LogoUrl, agency.DisplayName, agency.PrimaryColor),
                ["agency.emailHtml"] = BuildAgencyEmailHtml(agency.PrimaryEmail),
                ["proposal.name"] = Encode(proposal.Name),
                ["proposal.description"] = Encode(proposal.Description ?? ""),
                ["proposal.descriptionHtml"] = BuildDescriptionHtml(proposal.Description),
                ["proposal.date"] = DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", PtBR),
                ["proposal.client"] = Encode(proposal.Client ?? ""),
                ["proposal.clientRow"] = BuildClientRow(proposal.Client),
                ["proposal.owner"] = Encode(proposal.Owner ?? ""),
                ["proposal.ownerRow"] = BuildOwnerRow(proposal.Owner),
                ["proposal.items"] = BuildItemsTable(proposal.Items, agency.PrimaryColor),
                ["proposal.totalFormatted"] = proposal.TotalValue.ToString("C", PtBR),
                ["proposal.totals"] = BuildTotals(proposal.TotalValue, agency.PrimaryColor),
                ["proposal.validityHtml"] = BuildValidity(proposal.ValidityUntil),
                ["proposal.notesHtml"] = BuildNotes(proposal.Notes, agency.PrimaryColor),
            };
        }

        private static string ApplyVariables(string template, Dictionary<string, string> vars)
        {
            foreach (KeyValuePair<string, string> entry in vars)
            {
                template = template.Replace($"{{{{{entry.Key}}}}}", entry.Value);
            }
            return template;
        }

        // --- HTML fragment builders ---

        private static string BuildLogoHtml(string? logoUrl, string agencyDisplayName, string primaryColor)
        {
            if (string.IsNullOrWhiteSpace(logoUrl))
            {
                return $"<div class=\"header-logo-text\" style=\"color:{primaryColor}\">{Encode(agencyDisplayName)}</div>";
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

        private static string BuildClientRow(string? client) =>
            string.IsNullOrWhiteSpace(client)
                ? ""
                : $"<tr><td class=\"label\">Cliente:</td><td>{Encode(client)}</td></tr>";

        private static string BuildOwnerRow(string? owner) =>
            string.IsNullOrWhiteSpace(owner)
                ? ""
                : $"<tr><td class=\"label\">Responsável:</td><td>{Encode(owner)}</td></tr>";

        private static string BuildContactLine(AgencyData agency)
        {
            string[] parts = new[] { agency.PrimaryEmail, agency.Phone, agency.Address }
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => Encode(v!))
                .ToArray();
            return parts.Length == 0 ? "" : $"<div>{string.Join(" · ", parts)}</div>";
        }

        private static string BuildItemsTable(IReadOnlyList<ProposalItemData> items, string primaryColor)
        {
            if (items.Count == 0)
            {
                return "<p class=\"empty-items\">Nenhum item registrado.</p>";
            }

            StringBuilder sb = new();
            sb.Append($"<table class=\"items\"><thead><tr style=\"background:{primaryColor};color:#fff\">");
            sb.Append("<th>Creator</th><th>Descrição</th>");
            sb.Append("<th class=\"right\">Qtd</th><th class=\"right\">Valor unit.</th><th class=\"right\">Total</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (ProposalItemData item in items)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{Encode(item.Creator)}</td>");
                sb.Append($"<td>{Encode(item.Description ?? "—")}</td>");
                sb.Append($"<td class=\"right\">{item.Quantity.ToString("0.##", PtBR)}</td>");
                sb.Append($"<td class=\"right\">{item.UnitPrice.ToString("C", PtBR)}</td>");
                sb.Append($"<td class=\"right\"><strong>{item.Total.ToString("C", PtBR)}</strong></td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        private static string BuildTotals(decimal totalValue, string primaryColor) =>
            $"""
            <div class="total-row" style="border-top:2px solid {primaryColor}">
              <span class="total-label">Total da proposta</span>
              <span class="total-value">{totalValue.ToString("C", PtBR)}</span>
            </div>
            """;

        private static string BuildValidity(DateTimeOffset? validityUntil) =>
            validityUntil.HasValue
                ? $"<p class=\"validity\">Validade desta proposta: <strong>{validityUntil.Value.ToString("dd/MM/yyyy", PtBR)}</strong></p>"
                : "";

        private static string BuildNotes(string? notes, string primaryColor) =>
            string.IsNullOrWhiteSpace(notes)
                ? ""
                : $"""
                  <div class="section-heading">Observações</div>
                  <div class="notes-box" style="border-left:3px solid {primaryColor}">{Encode(notes)}</div>
                  """;

        private static string Encode(string value) => WebUtility.HtmlEncode(value);

        // --- inner data types ---

        private sealed record AgencyData(
            string AgencyName,
            string DisplayName,
            string PrimaryColor,
            string? LogoUrl,
            string? PrimaryEmail,
            string? Phone,
            string? Address,
            string? Document);

        private sealed record ProposalItemData(
            string Creator,
            string? Description,
            decimal Quantity,
            decimal UnitPrice,
            decimal Total);

        private sealed record ProposalData(
            string Name,
            string? Description,
            string? Client,
            string? Owner,
            IReadOnlyList<ProposalItemData> Items,
            decimal TotalValue,
            DateTimeOffset? ValidityUntil,
            string? Notes);

        // --- layout templates ---

        private const string PadraoTemplate = """
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
              border-bottom: 2.5px solid {{agency.primaryColor}};
              margin-bottom: 26px;
            }
            .header-logo { height: 36px; max-width: 160px; object-fit: contain; }
            .header-logo-text { font-size: 13pt; font-weight: 700; }
            .header-agency { text-align: right; }
            .header-agency-name { font-size: 10pt; font-weight: 700; color: {{agency.primaryColor}}; }
            .header-agency-email { font-size: 8pt; color: #888; margin-top: 2px; }
            .eyebrow {
              font-size: 7.5pt;
              font-weight: 700;
              letter-spacing: 0.14em;
              color: {{agency.primaryColor}};
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
            }
            .total-label { font-size: 10pt; font-weight: 600; color: #666; }
            .total-value { font-size: 20pt; font-weight: 800; color: #0f0f1a; }
            .validity { font-size: 9pt; color: #666; margin-top: 14px; }
            .validity strong { color: #222; }
            .notes-box {
              background: #f7f8fc;
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
                {{agency.logo}}
                <div class="header-agency">
                  <div class="header-agency-name">{{agency.displayName}}</div>
                  {{agency.emailHtml}}
                </div>
              </div>

              <div class="eyebrow">Proposta Comercial</div>
              <div class="proposal-title">{{proposal.name}}</div>
              {{proposal.descriptionHtml}}

              <div class="metadata">
                <table>
                  {{proposal.clientRow}}
                  <tr><td class="label">Agência:</td><td>{{agency.name}}{{agency.documentSuffix}}</td></tr>
                  <tr><td class="label">Data de emissão:</td><td>{{proposal.date}}</td></tr>
                  {{proposal.ownerRow}}
                </table>
              </div>

              <hr class="divider">

              <div class="section-heading">Itens</div>
              {{proposal.items}}

              {{proposal.totals}}
              {{proposal.validityHtml}}
              {{proposal.notesHtml}}

              <div class="footer">
                {{agency.contactLine}}
                <div>Documento gerado automaticamente.</div>
              </div>

            </div>
            </body>
            </html>
            """;
    }
}
