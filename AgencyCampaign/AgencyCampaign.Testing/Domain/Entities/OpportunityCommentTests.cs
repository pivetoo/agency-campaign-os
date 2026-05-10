using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class OpportunityCommentTests
    {
        [Test]
        public void Constructor_should_trim_inputs_and_persist_author()
        {
            OpportunityComment subject = new(opportunityId: 1, body: "  hello  ", authorUserId: 7, authorName: "  Alice  ");

            subject.Body.Should().Be("hello");
            subject.AuthorName.Should().Be("Alice");
            subject.AuthorUserId.Should().Be(7);
        }

        [Test]
        public void Update_should_throw_when_requesting_user_is_not_author()
        {
            OpportunityComment subject = new(1, "body", authorUserId: 7, authorName: "Alice");

            Action act = () => subject.Update("new body", requestingUserId: 99);

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Update_should_succeed_when_requesting_user_matches_author()
        {
            OpportunityComment subject = new(1, "body", authorUserId: 7, authorName: "Alice");

            subject.Update("  edited  ", requestingUserId: 7);

            subject.Body.Should().Be("edited");
        }

        [Test]
        public void Update_should_succeed_when_author_is_null()
        {
            OpportunityComment subject = new(1, "body", authorUserId: null, authorName: "system");

            subject.Update("edited", requestingUserId: 99);

            subject.Body.Should().Be("edited");
        }

        [Test]
        public void CanBeDeletedBy_should_be_true_when_author_or_requester_is_null()
        {
            OpportunityComment anonymous = new(1, "body", authorUserId: null, authorName: "system");
            anonymous.CanBeDeletedBy(99).Should().BeTrue();

            OpportunityComment withAuthor = new(1, "body", authorUserId: 7, authorName: "Alice");
            withAuthor.CanBeDeletedBy(null).Should().BeTrue();
        }

        [Test]
        public void CanBeDeletedBy_should_match_author_id()
        {
            OpportunityComment subject = new(1, "body", authorUserId: 7, authorName: "Alice");

            subject.CanBeDeletedBy(7).Should().BeTrue();
            subject.CanBeDeletedBy(99).Should().BeFalse();
        }
    }
}
