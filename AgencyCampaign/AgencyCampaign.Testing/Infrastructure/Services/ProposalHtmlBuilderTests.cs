using AgencyCampaign.Application.Models;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ProposalHtmlBuilderTests
    {
        private static AgencySettings BuildAgency(
            string name = "Acme",
            string? tradeName = null,
            string? document = null,
            string? primaryEmail = null,
            string? phone = null,
            string? address = null,
            string? logoUrl = null,
            string? primaryColor = "#123456")
        {
            AgencySettings agency = new(name);
            agency.Update(name, tradeName, document, primaryEmail, phone, address, logoUrl, primaryColor, null);
            return agency;
        }

        private static Proposal BuildProposal(
            string name = "Proposta X",
            string? description = null,
            string? notes = null,
            DateTimeOffset? validityUntil = null,
            string? ownerName = "Maria",
            string? brandName = "Cliente Acme")
        {
            Proposal proposal = new(opportunityId: 1, name: name, internalOwnerId: 1, description: description, validityUntil: validityUntil, notes: notes);
            if (!string.IsNullOrWhiteSpace(ownerName))
            {
                proposal.SetInternalOwner(1, ownerName);
            }

            if (!string.IsNullOrWhiteSpace(brandName))
            {
                Brand brand = new Brand(brandName).WithId(1);
                Opportunity opportunity = new(brand.Id, 1, "x", 0);
                typeof(Opportunity).GetProperty(nameof(Opportunity.Brand))!.SetValue(opportunity, brand);
                typeof(Proposal).GetProperty(nameof(Proposal.Opportunity))!.SetValue(proposal, opportunity);
            }

            return proposal;
        }

        private const string AllVariablesTemplate = """
            <html>
            <body style="color:{{agency.primaryColor}}">
            <div class="logo">{{agency.logo}}</div>
            <div class="email">{{agency.emailHtml}}</div>
            <div class="name">{{agency.name}}</div>
            <div class="display">{{agency.displayName}}</div>
            <div class="phone">{{agency.phone}}</div>
            <div class="address">{{agency.address}}</div>
            <div class="document">{{agency.document}}</div>
            <div class="docSuffix">{{agency.documentSuffix}}</div>
            <div class="nameWithDoc">{{agency.nameWithDocument}}</div>
            <div class="contactLine">{{agency.contactLine}}</div>
            <h1>{{proposal.name}}</h1>
            <div class="desc">{{proposal.description}}</div>
            <div class="descHtml">{{proposal.descriptionHtml}}</div>
            <div class="date">{{proposal.date}}</div>
            <div class="client">{{proposal.client}}</div>
            <div class="clientRow">{{proposal.clientRow}}</div>
            <div class="owner">{{proposal.owner}}</div>
            <div class="ownerRow">{{proposal.ownerRow}}</div>
            <div class="items">{{proposal.items}}</div>
            <div class="total">{{proposal.totalFormatted}}</div>
            <div class="totals">{{proposal.totals}}</div>
            <div class="validity">{{proposal.validityHtml}}</div>
            <div class="notes">{{proposal.notesHtml}}</div>
            </body>
            </html>
            """;

        [Test]
        public void GetLayouts_should_return_padrao_layout()
        {
            IReadOnlyList<ProposalLayoutModel> layouts = ProposalHtmlBuilder.GetLayouts();

            layouts.Should().ContainSingle();
            layouts[0].Key.Should().Be("padrao");
            layouts[0].Name.Should().Be("Padrão");
            layouts[0].Template.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public void Build_should_use_default_template_when_agency_has_none_and_no_explicit()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency);

            html.Should().Contain("Proposta Comercial");
            html.Should().Contain("Proposta X");
        }

        [Test]
        public void Build_should_prefer_explicit_template_over_agency_template()
        {
            AgencySettings agency = BuildAgency();
            agency.SetProposalHtmlTemplate("<div>agency-template</div>");
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "<div>explicit {{proposal.name}}</div>");

            html.Should().Be("<div>explicit Proposta X</div>");
        }

        [Test]
        public void Build_should_use_agency_template_when_no_explicit_provided()
        {
            AgencySettings agency = BuildAgency();
            agency.SetProposalHtmlTemplate("<div>{{proposal.name}}</div>");
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency);

            html.Should().Be("<div>Proposta X</div>");
        }

        [Test]
        public void Build_should_apply_primary_color_to_template()
        {
            AgencySettings agency = BuildAgency(primaryColor: "#abcdef");
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "color={{agency.primaryColor}}");

            html.Should().Be("color=#abcdef");
        }

        [Test]
        public void Build_should_fall_back_to_default_color_when_primary_color_is_empty()
        {
            AgencySettings agency = BuildAgency(primaryColor: null);
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "color={{agency.primaryColor}}");

            html.Should().Be("color=#6366f1");
        }

        [Test]
        public void Build_should_use_trade_name_as_display_name_when_provided()
        {
            AgencySettings agency = BuildAgency(name: "Razao Social", tradeName: "Acme Marketing");
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{agency.displayName}}|{{agency.name}}");

            html.Should().Be("Acme Marketing|Razao Social");
        }

        [Test]
        public void Build_should_use_agency_name_as_display_when_trade_name_is_null()
        {
            AgencySettings agency = BuildAgency(name: "Sem TradeName");
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{agency.displayName}}");

            html.Should().Be("Sem TradeName");
        }

        [Test]
        public void Build_should_render_logo_as_text_fallback_when_no_logo_url()
        {
            AgencySettings agency = BuildAgency(tradeName: "Marca", primaryColor: "#123456");
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{agency.logo}}");

            html.Should().Contain("header-logo-text");
            html.Should().Contain("Marca");
            html.Should().Contain("color:#123456");
        }

        [Test]
        public void Build_should_render_logo_as_img_when_logo_url_is_http()
        {
            AgencySettings agency = BuildAgency(logoUrl: "https://example.com/logo.png");
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{agency.logo}}");

            html.Should().Contain("<img class=\"header-logo\"");
            html.Should().Contain("src=\"https://example.com/logo.png\"");
        }

        [Test]
        public void Build_should_render_logo_as_img_when_local_path_does_not_exist()
        {
            AgencySettings agency = BuildAgency(logoUrl: "/uploads/inexistent-logo.png");
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{agency.logo}}");

            html.Should().Contain("<img class=\"header-logo\"");
            html.Should().Contain("src=\"/uploads/inexistent-logo.png\"");
        }

        [Test]
        public void Build_should_embed_local_logo_as_base64_ignoring_version_query_string()
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "agency", "test-tenant");
            Directory.CreateDirectory(folder);
            string filePath = Path.Combine(folder, "99.png");
            byte[] payload = [1, 2, 3, 4];
            File.WriteAllBytes(filePath, payload);

            try
            {
                AgencySettings agency = BuildAgency(logoUrl: "/uploads/agency/test-tenant/99.png?v=1716800000");
                Proposal proposal = BuildProposal();

                string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{agency.logo}}");

                html.Should().Contain($"src=\"data:image/png;base64,{Convert.ToBase64String(payload)}\"");
                html.Should().NotContain("?v=");
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Test]
        public void Build_should_render_email_html_when_email_is_present()
        {
            AgencySettings agency = BuildAgency(primaryEmail: "contato@acme.com");
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{agency.emailHtml}}");

            html.Should().Contain("header-agency-email");
            html.Should().Contain("contato@acme.com");
        }

        [Test]
        public void Build_should_render_empty_email_html_when_email_is_null()
        {
            AgencySettings agency = BuildAgency(primaryEmail: null);
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "[{{agency.emailHtml}}]");

            html.Should().Be("[]");
        }

        [Test]
        public void Build_should_render_document_suffix_when_document_is_present()
        {
            AgencySettings agency = BuildAgency(document: "12.345.678/0001-99");
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{agency.nameWithDocument}}{{agency.documentSuffix}}");

            html.Should().Contain("12.345.678/0001-99");
            html.Should().Contain("Acme (12.345.678/0001-99)");
        }

        [Test]
        public void Build_should_render_empty_document_suffix_when_document_is_null()
        {
            AgencySettings agency = BuildAgency(document: null);
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "[{{agency.documentSuffix}}][{{agency.nameWithDocument}}]");

            html.Should().Be("[][Acme]");
        }

        [Test]
        public void Build_should_render_contact_line_with_email_phone_and_address()
        {
            AgencySettings agency = BuildAgency(primaryEmail: "a@x", phone: "11-2222", address: "Rua X");
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{agency.contactLine}}");

            html.Should().Contain("a@x");
            html.Should().Contain("11-2222");
            html.Should().Contain("Rua X");
            html.Should().Contain(" · ");
        }

        [Test]
        public void Build_should_render_empty_contact_line_when_no_contacts()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "[{{agency.contactLine}}]");

            html.Should().Be("[]");
        }

        [Test]
        public void Build_should_render_description_html_when_description_present()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal(description: "Texto bonito");

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{proposal.descriptionHtml}}");

            html.Should().Contain("proposal-description");
            html.Should().Contain("Texto bonito");
        }

        [Test]
        public void Build_should_render_empty_description_html_when_description_blank()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal(description: null);

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "[{{proposal.descriptionHtml}}]");

            html.Should().Be("[]");
        }

        [Test]
        public void Build_should_render_client_row_when_brand_name_present()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal(brandName: "Cliente Y");

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{proposal.clientRow}}");

            html.Should().Contain("Cliente:");
            html.Should().Contain("Cliente Y");
        }

        [Test]
        public void Build_should_render_empty_client_row_when_no_brand()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal(brandName: null);

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "[{{proposal.clientRow}}]");

            html.Should().Be("[]");
        }

        [Test]
        public void Build_should_render_owner_row_when_owner_name_present()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal(ownerName: "Joao da Silva");

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{proposal.ownerRow}}");

            html.Should().Contain("class=\"label\"");
            html.Should().Contain("Joao da Silva");
        }

        [Test]
        public void Build_should_render_empty_owner_row_when_no_owner()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal(ownerName: null);

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "[{{proposal.ownerRow}}]");

            html.Should().Be("[]");
        }

        [Test]
        public void Build_should_render_empty_items_message_when_no_items()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{proposal.items}}");

            html.Should().Contain("Nenhum item registrado.");
            html.Should().Contain("empty-items");
        }

        [Test]
        public void Build_should_render_items_table_when_items_present()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal();
            Creator creator = new(name: "Fulano de Tal", stageName: "@fulano");
            ProposalItem item = new(proposalId: 1, description:"Reels", quantity: 2, unitPrice: 1500m);
            typeof(ProposalItem).GetProperty(nameof(ProposalItem.Creator))!.SetValue(item, creator);
            proposal.AddItem(item);

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{proposal.items}}");

            html.Should().Contain("@fulano");
            html.Should().Contain("Reels");
            html.Should().Contain("class=\"items\"");
        }

        [Test]
        public void Build_should_fall_back_to_creator_name_when_stage_name_is_null()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal();
            Creator creator = new(name: "Sem Stage Name");
            ProposalItem item = new(proposalId: 1, description:"Post", quantity: 1, unitPrice: 100m);
            typeof(ProposalItem).GetProperty(nameof(ProposalItem.Creator))!.SetValue(item, creator);
            proposal.AddItem(item);

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{proposal.items}}");

            html.Should().Contain("Sem Stage Name");
        }

        [Test]
        public void Build_should_show_dash_when_item_description_is_null()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal();
            Creator creator = new(name: "Anonimo");
            ProposalItem item = new(proposalId: 1, description:"qualquer", quantity: 1, unitPrice: 50m);
            typeof(ProposalItem).GetProperty(nameof(ProposalItem.Description))!.SetValue(item, null);
            typeof(ProposalItem).GetProperty(nameof(ProposalItem.Creator))!.SetValue(item, creator);
            proposal.AddItem(item);

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{proposal.items}}");

            html.Should().Contain("—");
        }

        [Test]
        public void Build_should_render_validity_html_when_validity_present()
        {
            AgencySettings agency = BuildAgency();
            DateTimeOffset validity = new(2026, 12, 31, 12, 0, 0, TimeSpan.Zero);
            Proposal proposal = BuildProposal(validityUntil: validity);

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{proposal.validityHtml}}");

            html.Should().Contain("Validade desta proposta");
            html.Should().Contain("31/12/2026");
        }

        [Test]
        public void Build_should_render_empty_validity_when_no_validity()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "[{{proposal.validityHtml}}]");

            html.Should().Be("[]");
        }

        [Test]
        public void Build_should_render_notes_html_when_notes_present()
        {
            AgencySettings agency = BuildAgency(primaryColor: "#aabbcc");
            Proposal proposal = BuildProposal(notes: "Observacao importante");

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{proposal.notesHtml}}");

            html.Should().Contain("section-heading");
            html.Should().Contain("Observacao importante");
            html.Should().Contain("border-left:3px solid #aabbcc");
        }

        [Test]
        public void Build_should_render_empty_notes_when_no_notes()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "[{{proposal.notesHtml}}]");

            html.Should().Be("[]");
        }

        [Test]
        public void Build_should_html_encode_user_supplied_values()
        {
            AgencySettings agency = BuildAgency(name: "Acme <Marketing>");
            Proposal proposal = BuildProposal(name: "<script>alert(1)</script>");

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{agency.name}}|{{proposal.name}}");

            html.Should().NotContain("<script>");
            html.Should().Contain("&lt;script&gt;");
            html.Should().Contain("&lt;Marketing&gt;");
        }

        [Test]
        public void Build_should_render_total_formatted_in_pt_br()
        {
            AgencySettings agency = BuildAgency();
            Proposal proposal = BuildProposal();
            Creator creator = new(name: "x");
            ProposalItem item = new(proposalId: 1, description:"y", quantity: 3, unitPrice: 1000m);
            typeof(ProposalItem).GetProperty(nameof(ProposalItem.Creator))!.SetValue(item, creator);
            proposal.AddItem(item);

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{proposal.totalFormatted}}");

            html.Should().MatchRegex(@"R\$\s*3\.000,00");
        }

        [Test]
        public void Build_should_render_totals_block_with_primary_color()
        {
            AgencySettings agency = BuildAgency(primaryColor: "#ff8800");
            Proposal proposal = BuildProposal();

            string html = ProposalHtmlBuilder.Build(proposal, agency, explicitTemplate: "{{proposal.totals}}");

            html.Should().Contain("border-top:2px solid #ff8800");
            html.Should().Contain("Total da proposta");
        }

        [Test]
        public void BuildPreview_should_replace_variables_with_mock_data()
        {
            AgencySettings agency = BuildAgency(tradeName: "Mainstay", primaryEmail: "preview@x");

            string html = ProposalHtmlBuilder.BuildPreview(AllVariablesTemplate, agency);

            html.Should().Contain("Campanha de Lan");
            html.Should().Contain("Empresa ABC");
            html.Should().Contain("Maria Oliveira");
            html.Should().Contain("Ana Silva (@anasilva)");
            html.Should().Contain("Reels patrocinado");
            html.Should().Contain("Mainstay");
            html.Should().Contain("preview@x");
        }

        [Test]
        public void BuildPreview_should_render_mock_proposal_items_and_total()
        {
            AgencySettings agency = BuildAgency();

            string html = ProposalHtmlBuilder.BuildPreview("{{proposal.items}}|{{proposal.totalFormatted}}", agency);

            html.Should().Contain("Ana Silva");
            html.Should().MatchRegex(@"R\$\s*14\.200,00");
        }

        [Test]
        public void BuildPreview_should_render_mock_validity_and_notes()
        {
            AgencySettings agency = BuildAgency();

            string html = ProposalHtmlBuilder.BuildPreview("{{proposal.validityHtml}}|{{proposal.notesHtml}}", agency);

            html.Should().Contain("Validade desta proposta");
            html.Should().Contain("Valores sujeitos ao briefing");
        }
    }
}
