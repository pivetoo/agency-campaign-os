using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CampaignDocumentTests
    {
        private static CampaignDocument BuildDefault()
        {
            return new CampaignDocument(campaignId: 1, documentType: CampaignDocumentType.CreatorAgreement, title: "  Contrato  ");
        }

        [Test]
        public void Constructor_should_initialize_as_draft()
        {
            CampaignDocument subject = BuildDefault();

            subject.Status.Should().Be(CampaignDocumentStatus.Draft);
            subject.Title.Should().Be("Contrato");
        }

        [Test]
        public void MarkReadyToSend_should_transition_status()
        {
            CampaignDocument subject = BuildDefault();
            subject.MarkReadyToSend();
            subject.Status.Should().Be(CampaignDocumentStatus.ReadyToSend);
        }

        [Test]
        public void MarkSent_should_persist_recipient_and_timestamps()
        {
            CampaignDocument subject = BuildDefault();
            DateTimeOffset sentAt = DateTimeOffset.Now;

            subject.MarkSent(" creator@x ", " Assunto ", " corpo ", sentAt);

            subject.Status.Should().Be(CampaignDocumentStatus.Sent);
            subject.RecipientEmail.Should().Be("creator@x");
            subject.EmailSubject.Should().Be("Assunto");
            subject.EmailBody.Should().Be("corpo");
            subject.SentAt!.Value.Offset.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void MarkSent_should_reject_blank_recipient_or_subject()
        {
            CampaignDocument subject = BuildDefault();

            Action blankRecipient = () => subject.MarkSent("   ", "x", null, DateTimeOffset.UtcNow);
            Action blankSubject = () => subject.MarkSent("x", "  ", null, DateTimeOffset.UtcNow);

            blankRecipient.Should().Throw<ArgumentException>();
            blankSubject.Should().Throw<ArgumentException>();
        }

        [Test]
        public void AttachToProvider_should_promote_draft_to_sent_with_default_timestamp()
        {
            CampaignDocument subject = BuildDefault();

            subject.AttachToProvider("Clicksign", "abc-123");

            subject.Provider.Should().Be("Clicksign");
            subject.ProviderDocumentId.Should().Be("abc-123");
            subject.Status.Should().Be(CampaignDocumentStatus.Sent);
            subject.SentAt.Should().NotBeNull();
        }

        [Test]
        public void AttachToProvider_should_not_change_status_when_already_signed()
        {
            CampaignDocument subject = BuildDefault();
            subject.MarkSigned(DateTimeOffset.UtcNow);

            subject.AttachToProvider("Clicksign", "abc");

            subject.Status.Should().Be(CampaignDocumentStatus.Signed);
        }

        [Test]
        public void AddSignature_should_register_a_pending_signature()
        {
            CampaignDocument subject = BuildDefault();

            CampaignDocumentSignature signature = subject.AddSignature(CampaignDocumentSignerRole.Creator, "  Foo  ", "  foo@x  ");

            signature.SignerName.Should().Be("Foo");
            signature.SignerEmail.Should().Be("foo@x");
            signature.IsSigned.Should().BeFalse();
            subject.Signatures.Should().ContainSingle();
        }

        [Test]
        public void RegisterSignerSigned_should_match_by_email_case_insensitively()
        {
            CampaignDocument subject = BuildDefault();
            subject.AddSignature(CampaignDocumentSignerRole.Creator, "Foo", "foo@x");

            CampaignDocumentSignature? returned = subject.RegisterSignerSigned("FOO@X", DateTimeOffset.UtcNow);

            returned.Should().NotBeNull();
            returned!.IsSigned.Should().BeTrue();
        }

        [Test]
        public void RegisterSignerSigned_should_fall_back_to_provider_signer_id_when_email_not_found()
        {
            CampaignDocument subject = BuildDefault();
            subject.AddSignature(CampaignDocumentSignerRole.Creator, "Foo", "foo@x", providerSignerId: "p-1");

            CampaignDocumentSignature? returned = subject.RegisterSignerSigned("not-found@x", DateTimeOffset.UtcNow, providerSignerId: "p-1");

            returned.Should().NotBeNull();
            returned!.IsSigned.Should().BeTrue();
        }

        [Test]
        public void RegisterSignerSigned_should_return_null_when_no_match()
        {
            CampaignDocument subject = BuildDefault();
            CampaignDocumentSignature? returned = subject.RegisterSignerSigned("missing@x", DateTimeOffset.UtcNow);
            returned.Should().BeNull();
        }

        [Test]
        public void MarkViewed_should_only_promote_status_when_currently_sent()
        {
            CampaignDocument draft = BuildDefault();
            draft.MarkViewed(DateTimeOffset.UtcNow);
            draft.Status.Should().Be(CampaignDocumentStatus.Draft);

            CampaignDocument sent = BuildDefault();
            sent.MarkSent("x@y", "s", null, DateTimeOffset.UtcNow);
            sent.MarkViewed(DateTimeOffset.UtcNow);
            sent.Status.Should().Be(CampaignDocumentStatus.Viewed);
        }

        [Test]
        public void MarkSigned_should_persist_signed_url_when_provided()
        {
            CampaignDocument subject = BuildDefault();

            subject.MarkSigned(DateTimeOffset.UtcNow, "  https://signed  ");

            subject.Status.Should().Be(CampaignDocumentStatus.Signed);
            subject.SignedDocumentUrl.Should().Be("https://signed");
        }

        [Test]
        public void AllSigned_should_be_true_only_when_all_signatures_signed()
        {
            CampaignDocument subject = BuildDefault();
            subject.AllSigned().Should().BeFalse();

            CampaignDocumentSignature s1 = subject.AddSignature(CampaignDocumentSignerRole.Creator, "A", "a@x");
            CampaignDocumentSignature s2 = subject.AddSignature(CampaignDocumentSignerRole.Brand, "B", "b@x");

            subject.AllSigned().Should().BeFalse();

            s1.MarkSigned(DateTimeOffset.UtcNow);
            s2.MarkSigned(DateTimeOffset.UtcNow);

            subject.AllSigned().Should().BeTrue();
        }

        [Test]
        public void RegisterEvent_should_append_to_events()
        {
            CampaignDocument subject = BuildDefault();

            subject.RegisterEvent(CampaignDocumentEventType.Created);
            subject.RegisterEvent(CampaignDocumentEventType.Sent);

            subject.Events.Should().HaveCount(2);
        }
    }
}
