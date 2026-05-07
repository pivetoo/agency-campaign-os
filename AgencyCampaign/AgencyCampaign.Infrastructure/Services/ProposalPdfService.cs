using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using PdfSharp.Fonts;
using System.Globalization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalPdfService : IProposalPdfService
    {
        private static readonly object FontResolverLock = new();
        private static bool fontResolverInstalled;

        private readonly DbContext dbContext;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public ProposalPdfService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
            EnsureFontResolver();
        }

        public async Task<byte[]> GenerateForProposalAsync(long proposalId, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await LoadProposalAsync(proposalId, cancellationToken);
            if (proposal is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            AgencySettings agency = await ResolveAgencyAsync(cancellationToken);
            return Render(proposal, agency);
        }

        public async Task<byte[]?> GenerateForShareTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            ProposalShareLink? shareLink = await dbContext.Set<ProposalShareLink>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Token == token, cancellationToken);

            if (shareLink is null || !shareLink.IsActive(DateTimeOffset.UtcNow))
            {
                return null;
            }

            Proposal? proposal = await LoadProposalAsync(shareLink.ProposalId, cancellationToken);
            if (proposal is null)
            {
                return null;
            }

            AgencySettings agency = await ResolveAgencyAsync(cancellationToken);
            return Render(proposal, agency);
        }

        private async Task<Proposal?> LoadProposalAsync(long proposalId, CancellationToken cancellationToken)
        {
            return await dbContext.Set<Proposal>()
                .AsNoTracking()
                .Include(item => item.Opportunity)
                    .ThenInclude(item => item!.Brand)
                .Include(item => item.Items)
                    .ThenInclude(item => item.Creator)
                .FirstOrDefaultAsync(item => item.Id == proposalId, cancellationToken);
        }

        private async Task<AgencySettings> ResolveAgencyAsync(CancellationToken cancellationToken)
        {
            return await dbContext.Set<AgencySettings>()
                .AsNoTracking()
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? new AgencySettings("Minha agência");
        }

        private static byte[] Render(Proposal proposal, AgencySettings agency)
        {
            Document document = BuildDocument(proposal, agency);

            PdfDocumentRenderer renderer = new()
            {
                Document = document
            };
            renderer.RenderDocument();

            using MemoryStream stream = new();
            renderer.PdfDocument.Save(stream, false);
            return stream.ToArray();
        }

        private static Document BuildDocument(Proposal proposal, AgencySettings agency)
        {
            CultureInfo culture = CultureInfo.GetCultureInfo("pt-BR");

            Document document = new()
            {
                Info =
                {
                    Title = proposal.Name,
                    Author = agency.AgencyName,
                    Subject = $"Proposta — {proposal.Opportunity?.Brand?.Name ?? string.Empty}"
                }
            };

            DefineStyles(document);

            Section section = document.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.TopMargin = Unit.FromCentimeter(2.2);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(1.8);
            section.PageSetup.LeftMargin = Unit.FromCentimeter(2.0);
            section.PageSetup.RightMargin = Unit.FromCentimeter(2.0);

            BuildHeader(section, agency);
            BuildFooter(section, agency);
            BuildCover(section, proposal, agency, culture);
            BuildDescription(section, proposal);
            BuildItemsTable(section, proposal, culture);
            BuildTotals(section, proposal, culture);
            BuildValidity(section, proposal, culture);
            BuildNotes(section, proposal);

            return document;
        }

        private static void DefineStyles(Document document)
        {
            Style normal = document.Styles[StyleNames.Normal]!;
            normal.Font.Name = "Arial";
            normal.Font.Size = 10;
            normal.Font.Color = Colors.Black;

            Style heading1 = document.Styles[StyleNames.Heading1]!;
            heading1.Font.Size = 22;
            heading1.Font.Bold = true;
            heading1.Font.Color = new Color(20, 20, 30);
            heading1.ParagraphFormat.SpaceAfter = Unit.FromMillimeter(4);

            Style heading2 = document.Styles[StyleNames.Heading2]!;
            heading2.Font.Size = 13;
            heading2.Font.Bold = true;
            heading2.Font.Color = new Color(80, 80, 95);
            heading2.ParagraphFormat.SpaceBefore = Unit.FromMillimeter(6);
            heading2.ParagraphFormat.SpaceAfter = Unit.FromMillimeter(2);

            Style header = document.Styles[StyleNames.Header]!;
            header.Font.Size = 9;
            header.Font.Color = new Color(120, 120, 130);

            Style footer = document.Styles[StyleNames.Footer]!;
            footer.Font.Size = 8;
            footer.Font.Color = new Color(140, 140, 150);
        }

        private static void BuildHeader(Section section, AgencySettings agency)
        {
            Paragraph paragraph = section.Headers.Primary.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.AddFormattedText(agency.TradeName ?? agency.AgencyName, TextFormat.Bold);
            if (!string.IsNullOrWhiteSpace(agency.PrimaryEmail))
            {
                paragraph.AddText($" · {agency.PrimaryEmail}");
            }
        }

        private static void BuildFooter(Section section, AgencySettings agency)
        {
            Paragraph paragraph = section.Footers.Primary.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Center;

            string contact = string.Join(" · ", new[]
            {
                agency.PrimaryEmail,
                agency.Phone,
                agency.Address
            }.Where(value => !string.IsNullOrWhiteSpace(value))!);

            if (!string.IsNullOrEmpty(contact))
            {
                paragraph.AddText(contact);
                paragraph.AddLineBreak();
            }

            paragraph.AddText("Página ");
            paragraph.AddPageField();
            paragraph.AddText(" de ");
            paragraph.AddNumPagesField();
        }

        private static void BuildCover(Section section, Proposal proposal, AgencySettings agency, CultureInfo culture)
        {
            Paragraph eyebrow = section.AddParagraph();
            eyebrow.Format.SpaceBefore = Unit.FromMillimeter(8);
            eyebrow.AddFormattedText("PROPOSTA COMERCIAL", new Font { Size = 9, Bold = true, Color = new Color(120, 120, 140) });

            Paragraph title = section.AddParagraph(proposal.Name, StyleNames.Heading1);
            title.Format.SpaceAfter = Unit.FromMillimeter(2);

            Paragraph metadata = section.AddParagraph();
            metadata.Format.Font.Size = 10;
            metadata.Format.Font.Color = new Color(70, 70, 90);

            string brand = proposal.Opportunity?.Brand?.Name ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(brand))
            {
                metadata.AddFormattedText("Cliente: ", TextFormat.Bold);
                metadata.AddText(brand);
                metadata.AddLineBreak();
            }

            metadata.AddFormattedText("Agência: ", TextFormat.Bold);
            metadata.AddText(agency.AgencyName);
            if (!string.IsNullOrWhiteSpace(agency.Document))
            {
                metadata.AddText($" ({agency.Document})");
            }
            metadata.AddLineBreak();

            metadata.AddFormattedText("Data de emissão: ", TextFormat.Bold);
            metadata.AddText(DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culture));

            if (!string.IsNullOrWhiteSpace(proposal.InternalOwnerName))
            {
                metadata.AddLineBreak();
                metadata.AddFormattedText("Responsável: ", TextFormat.Bold);
                metadata.AddText(proposal.InternalOwnerName);
            }
        }

        private static void BuildDescription(Section section, Proposal proposal)
        {
            if (string.IsNullOrWhiteSpace(proposal.Description))
            {
                return;
            }

            section.AddParagraph("Sobre a proposta", StyleNames.Heading2);
            Paragraph description = section.AddParagraph(proposal.Description);
            description.Format.SpaceAfter = Unit.FromMillimeter(4);
            description.Format.Alignment = ParagraphAlignment.Justify;
        }

        private static void BuildItemsTable(Section section, Proposal proposal, CultureInfo culture)
        {
            section.AddParagraph("Itens", StyleNames.Heading2);

            Table table = section.AddTable();
            table.Borders.Width = 0;
            table.Format.Font.Size = 9;
            table.TopPadding = Unit.FromMillimeter(2);
            table.BottomPadding = Unit.FromMillimeter(2);

            table.AddColumn(Unit.FromCentimeter(5.5));
            table.AddColumn(Unit.FromCentimeter(4.5));
            table.AddColumn(Unit.FromCentimeter(1.5));
            table.AddColumn(Unit.FromCentimeter(2.5));
            table.AddColumn(Unit.FromCentimeter(2.8));

            Row header = table.AddRow();
            header.HeadingFormat = true;
            header.Format.Font.Bold = true;
            header.Format.Font.Color = Colors.White;
            header.Shading.Color = new Color(40, 40, 60);
            header.Cells[0].AddParagraph("Descrição");
            header.Cells[1].AddParagraph("Creator");
            header.Cells[2].AddParagraph("Qtd");
            header.Cells[2].Format.Alignment = ParagraphAlignment.Right;
            header.Cells[3].AddParagraph("Valor unit.");
            header.Cells[3].Format.Alignment = ParagraphAlignment.Right;
            header.Cells[4].AddParagraph("Total");
            header.Cells[4].Format.Alignment = ParagraphAlignment.Right;

            bool alternate = false;
            foreach (ProposalItem item in proposal.Items)
            {
                Row row = table.AddRow();
                if (alternate)
                {
                    row.Shading.Color = new Color(245, 245, 250);
                }
                alternate = !alternate;

                row.Cells[0].AddParagraph(string.IsNullOrWhiteSpace(item.Description) ? "-" : item.Description);
                row.Cells[1].AddParagraph(item.Creator?.StageName ?? item.Creator?.Name ?? "-");
                row.Cells[2].AddParagraph(item.Quantity.ToString("0.##", culture));
                row.Cells[2].Format.Alignment = ParagraphAlignment.Right;
                row.Cells[3].AddParagraph(item.UnitPrice.ToString("C", culture));
                row.Cells[3].Format.Alignment = ParagraphAlignment.Right;
                row.Cells[4].AddParagraph(item.Total.ToString("C", culture));
                row.Cells[4].Format.Alignment = ParagraphAlignment.Right;
            }

            if (proposal.Items.Count == 0)
            {
                Row row = table.AddRow();
                row.Cells[0].MergeRight = 4;
                row.Cells[0].AddParagraph("Nenhum item registrado.");
                row.Cells[0].Format.Font.Color = new Color(120, 120, 130);
                row.Cells[0].Format.Alignment = ParagraphAlignment.Center;
            }
        }

        private static void BuildTotals(Section section, Proposal proposal, CultureInfo culture)
        {
            Paragraph total = section.AddParagraph();
            total.Format.SpaceBefore = Unit.FromMillimeter(6);
            total.Format.Alignment = ParagraphAlignment.Right;
            total.AddFormattedText("Total: ", new Font { Size = 11, Bold = true, Color = new Color(80, 80, 95) });
            total.AddFormattedText(proposal.TotalValue.ToString("C", culture), new Font { Size = 18, Bold = true, Color = new Color(20, 20, 30) });
        }

        private static void BuildValidity(Section section, Proposal proposal, CultureInfo culture)
        {
            if (!proposal.ValidityUntil.HasValue)
            {
                return;
            }

            Paragraph validity = section.AddParagraph();
            validity.Format.SpaceBefore = Unit.FromMillimeter(6);
            validity.AddFormattedText("Validade desta proposta: ", TextFormat.Bold);
            validity.AddText(proposal.ValidityUntil.Value.ToString("dd/MM/yyyy", culture));
        }

        private static void BuildNotes(Section section, Proposal proposal)
        {
            if (string.IsNullOrWhiteSpace(proposal.Notes))
            {
                return;
            }

            section.AddParagraph("Observações", StyleNames.Heading2);
            Paragraph notes = section.AddParagraph(proposal.Notes);
            notes.Format.Alignment = ParagraphAlignment.Justify;
        }

        private static void EnsureFontResolver()
        {
            lock (FontResolverLock)
            {
                if (fontResolverInstalled)
                {
                    return;
                }

                if (GlobalFontSettings.FontResolver is null)
                {
                    GlobalFontSettings.FontResolver = new DocumentFontResolver();
                }

                fontResolverInstalled = true;
            }
        }

        private sealed class DocumentFontResolver : IFontResolver
        {
            public byte[]? GetFont(string faceName)
            {
                string? path = ResolveFontPath(faceName);
                if (path is null || !File.Exists(path))
                {
                    return null;
                }

                return File.ReadAllBytes(path);
            }

            public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
            {
                string variant = $"{familyName}#{(isBold ? "B" : string.Empty)}{(isItalic ? "I" : string.Empty)}";
                return new FontResolverInfo(variant);
            }

            private static string? ResolveFontPath(string faceName)
            {
                string[] candidates = faceName switch
                {
                    "Arial#" or "Arial" => ["DejaVuSans.ttf", "LiberationSans-Regular.ttf"],
                    "Arial#B" => ["DejaVuSans-Bold.ttf", "LiberationSans-Bold.ttf"],
                    "Arial#I" => ["DejaVuSans-Oblique.ttf", "LiberationSans-Italic.ttf"],
                    "Arial#BI" => ["DejaVuSans-BoldOblique.ttf", "LiberationSans-BoldItalic.ttf"],
                    _ => ["DejaVuSans.ttf"]
                };

                string[] fontDirs =
                [
                    "/usr/share/fonts/truetype/dejavu",
                    "/usr/share/fonts/truetype/liberation",
                    "/usr/share/fonts/dejavu",
                    "/usr/share/fonts",
                    "/Library/Fonts",
                    "C:/Windows/Fonts"
                ];

                foreach (string candidate in candidates)
                {
                    foreach (string dir in fontDirs)
                    {
                        string path = Path.Combine(dir, candidate);
                        if (File.Exists(path))
                        {
                            return path;
                        }
                    }
                }

                return null;
            }
        }
    }
}
