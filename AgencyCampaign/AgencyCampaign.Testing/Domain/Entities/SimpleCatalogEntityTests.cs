using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class AgencySettingsTests
    {
        [Test]
        public void Constructor_should_trim_agency_name()
        {
            AgencySettings subject = new("  Acme  ");
            subject.AgencyName.Should().Be("Acme");
        }

        [Test]
        public void Constructor_should_reject_blank_name()
        {
            Action act = () => _ = new AgencySettings(" ");
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_should_normalize_optional_fields_and_replace_state()
        {
            AgencySettings subject = new("Old");

            subject.Update("New", " trade ", " doc ", " a@x ", " 999 ", " addr ", " logo ", " #fff ", defaultEmailConnectorId: 5);

            subject.AgencyName.Should().Be("New");
            subject.TradeName.Should().Be("trade");
            subject.PrimaryEmail.Should().Be("a@x");
            subject.LogoUrl.Should().Be("logo");
            subject.PrimaryColor.Should().Be("#fff");
            subject.DefaultEmailConnectorId.Should().Be(5);
        }
    }

    [TestFixture]
    public sealed class EmailTemplateTests
    {
        [Test]
        public void Constructor_should_trim_inputs_but_preserve_html_body()
        {
            EmailTemplate subject = new("  Hello  ", EmailEventType.ProposalSent, "  Subject  ", "<p>Body</p>", createdByUserId: 5, createdByUserName: " Tester ");

            subject.Name.Should().Be("Hello");
            subject.Subject.Should().Be("Subject");
            subject.HtmlBody.Should().Be("<p>Body</p>");
            subject.CreatedByUserName.Should().Be("Tester");
            subject.IsActive.Should().BeTrue();
        }

        [Test]
        public void Constructor_should_reject_blank_required_fields()
        {
            Action blankName = () => _ = new EmailTemplate(" ", EmailEventType.ProposalSent, "x", "y", null, null);
            Action blankSubject = () => _ = new EmailTemplate("x", EmailEventType.ProposalSent, " ", "y", null, null);
            Action blankBody = () => _ = new EmailTemplate("x", EmailEventType.ProposalSent, "y", " ", null, null);

            blankName.Should().Throw<ArgumentException>();
            blankSubject.Should().Throw<ArgumentException>();
            blankBody.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_should_replace_state_and_toggle_active()
        {
            EmailTemplate subject = new("Old", EmailEventType.ProposalSent, "Subj", "<p>x</p>", null, null);

            subject.Update("New", EmailEventType.ProposalApproved, "New Subj", "<p>y</p>", isActive: false);

            subject.Name.Should().Be("New");
            subject.EventType.Should().Be(EmailEventType.ProposalApproved);
            subject.IsActive.Should().BeFalse();
        }
    }

    [TestFixture]
    public sealed class FinancialAccountTests
    {
        [Test]
        public void Constructor_should_trim_and_normalize()
        {
            FinancialAccount subject = new("  Conta  ", FinancialAccountType.Bank, 100m, "  #fff  ", bank: " Itau ", agency: "0001", number: "12345");

            subject.Name.Should().Be("Conta");
            subject.Color.Should().Be("#fff");
            subject.Bank.Should().Be("Itau");
            subject.IsActive.Should().BeTrue();
        }

        [Test]
        public void Constructor_should_reject_blank_required_fields()
        {
            Action blankName = () => _ = new FinancialAccount(" ", FinancialAccountType.Bank, 0m, "#fff");
            Action blankColor = () => _ = new FinancialAccount("x", FinancialAccountType.Bank, 0m, " ");

            blankName.Should().Throw<ArgumentException>();
            blankColor.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_should_replace_state()
        {
            FinancialAccount subject = new("Old", FinancialAccountType.Bank, 0m, "#fff");

            subject.Update("New", FinancialAccountType.Wallet, 500m, "#000", "Bank", "0001", "999", isActive: false);

            subject.Name.Should().Be("New");
            subject.Type.Should().Be(FinancialAccountType.Wallet);
            subject.IsActive.Should().BeFalse();
        }
    }

    [TestFixture]
    public sealed class FinancialSubcategoryTests
    {
        [Test]
        public void Constructor_should_trim()
        {
            FinancialSubcategory subject = new("  Hospedagem  ", FinancialEntryCategory.OperationalCost, "  #fff  ");
            subject.Name.Should().Be("Hospedagem");
            subject.Color.Should().Be("#fff");
            subject.IsActive.Should().BeTrue();
        }

        [Test]
        public void Update_should_replace_state()
        {
            FinancialSubcategory subject = new("x", FinancialEntryCategory.AgencyFee, "#fff");

            subject.Update("y", FinancialEntryCategory.Tax, "#000", isActive: false);

            subject.Name.Should().Be("y");
            subject.MacroCategory.Should().Be(FinancialEntryCategory.Tax);
            subject.IsActive.Should().BeFalse();
        }
    }

    [TestFixture]
    public sealed class DeliverableKindTests
    {
        [Test]
        public void Constructor_should_trim_and_default_active()
        {
            DeliverableKind subject = new("  Story  ", displayOrder: 2);
            subject.Name.Should().Be("Story");
            subject.DisplayOrder.Should().Be(2);
            subject.IsActive.Should().BeTrue();
        }

        [Test]
        public void Constructor_should_reject_blank_name()
        {
            Action act = () => _ = new DeliverableKind(" ");
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_should_replace_state()
        {
            DeliverableKind subject = new("Old");
            subject.Update("New", 5, isActive: false);
            subject.Name.Should().Be("New");
            subject.IsActive.Should().BeFalse();
        }
    }

    [TestFixture]
    public sealed class PlatformTests
    {
        [Test]
        public void Constructor_should_trim_and_default_active()
        {
            Platform subject = new("  Instagram  ", displayOrder: 1);
            subject.Name.Should().Be("Instagram");
            subject.IsActive.Should().BeTrue();
        }

        [Test]
        public void Constructor_should_reject_blank_name()
        {
            Action act = () => _ = new Platform(" ");
            act.Should().Throw<ArgumentException>();
        }
    }

    [TestFixture]
    public sealed class OpportunitySourceTests
    {
        [Test]
        public void Constructor_should_trim_inputs()
        {
            OpportunitySource subject = new("  Indicação  ", "  #fff  ", displayOrder: 2);
            subject.Name.Should().Be("Indicação");
            subject.Color.Should().Be("#fff");
            subject.IsActive.Should().BeTrue();
        }

        [Test]
        public void Update_should_replace_state()
        {
            OpportunitySource subject = new("x", "#fff", 1);
            subject.Update("y", "#000", 2, isActive: false);
            subject.Name.Should().Be("y");
            subject.IsActive.Should().BeFalse();
        }
    }

    [TestFixture]
    public sealed class OpportunityTagTests
    {
        [Test]
        public void Constructor_should_trim_inputs()
        {
            OpportunityTag subject = new("  vip  ", "  #fff  ");
            subject.Name.Should().Be("vip");
            subject.Color.Should().Be("#fff");
            subject.IsActive.Should().BeTrue();
        }

        [Test]
        public void Update_should_replace_state()
        {
            OpportunityTag subject = new("x", "#fff");
            subject.Update("y", "#000", isActive: false);
            subject.Name.Should().Be("y");
            subject.IsActive.Should().BeFalse();
        }
    }

    [TestFixture]
    public sealed class ProposalTemplateTests
    {
        [Test]
        public void Constructor_should_trim_and_default_active()
        {
            ProposalTemplate subject = new("  Pacote  ", " desc ", createdByUserId: 5, createdByUserName: " Tester ");
            subject.Name.Should().Be("Pacote");
            subject.Description.Should().Be("desc");
            subject.CreatedByUserName.Should().Be("Tester");
            subject.IsActive.Should().BeTrue();
        }

        [Test]
        public void Update_should_replace_state()
        {
            ProposalTemplate subject = new("Old", null, null, null);
            subject.Update("New", "desc", isActive: false);
            subject.Name.Should().Be("New");
            subject.IsActive.Should().BeFalse();
        }
    }

    [TestFixture]
    public sealed class ProposalBlockTests
    {
        [Test]
        public void Constructor_should_trim_inputs()
        {
            ProposalBlock subject = new("  Termos  ", "  body  ", "  cat  ", null, null);
            subject.Name.Should().Be("Termos");
            subject.Body.Should().Be("body");
            subject.Category.Should().Be("cat");
            subject.IsActive.Should().BeTrue();
        }

        [Test]
        public void Constructor_should_reject_blank_required_fields()
        {
            Action blankBody = () => _ = new ProposalBlock("x", " ", "y", null, null);
            Action blankCategory = () => _ = new ProposalBlock("x", "y", " ", null, null);

            blankBody.Should().Throw<ArgumentException>();
            blankCategory.Should().Throw<ArgumentException>();
        }
    }

    [TestFixture]
    public sealed class ProposalVersionTests
    {
        [Test]
        public void Constructor_should_persist_snapshot_and_default_sent_metadata()
        {
            ProposalVersion subject = new(proposalId: 1, versionNumber: 2, name: "v2", description: " desc ", totalValue: 100m,
                validityUntil: DateTimeOffset.Now, snapshotJson: "{\"x\":1}", sentByUserId: 5, sentByUserName: " Tester ");

            subject.VersionNumber.Should().Be(2);
            subject.Description.Should().Be("desc");
            subject.SnapshotJson.Should().Be("{\"x\":1}");
            subject.SentByUserName.Should().Be("Tester");
            subject.SentAt.Offset.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void Constructor_should_reject_invalid_proposal_or_version()
        {
            Action invalidProposal = () => _ = new ProposalVersion(0, 1, "x", null, 0m, null, "{}", null, null);
            Action invalidVersion = () => _ = new ProposalVersion(1, 0, "x", null, 0m, null, "{}", null, null);

            invalidProposal.Should().Throw<ArgumentOutOfRangeException>();
            invalidVersion.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Constructor_should_reject_blank_name_or_snapshot()
        {
            Action blankName = () => _ = new ProposalVersion(1, 1, " ", null, 0m, null, "{}", null, null);
            Action blankSnapshot = () => _ = new ProposalVersion(1, 1, "x", null, 0m, null, " ", null, null);

            blankName.Should().Throw<ArgumentException>();
            blankSnapshot.Should().Throw<ArgumentException>();
        }
    }
}
