using AgencyCampaign.Infrastructure.Options;
using AgencyCampaign.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class MediaAccessTokenServiceTests
    {
        private static MediaAccessTokenService Build(string secret = "media-signing-secret-0123456789")
        {
            return new MediaAccessTokenService(Options.Create(new MediaStorageOptions { SigningKey = secret, SignedUrlMinutes = 120 }));
        }

        private static string ExtractToken(string url)
        {
            return url.Substring(url.IndexOf("t=", StringComparison.Ordinal) + 2);
        }

        [Test]
        public void BuildSignedUrl_then_TryRead_should_roundtrip_the_key()
        {
            MediaAccessTokenService service = Build();
            string url = service.BuildSignedUrl("content/tenant-1/10/abc.png");

            url.Should().StartWith("/api/media?t=");
            service.TryReadStorageKey(ExtractToken(url), out string key).Should().BeTrue();
            key.Should().Be("content/tenant-1/10/abc.png");
        }

        [Test]
        public void TryReadStorageKey_should_reject_expired_token()
        {
            MediaAccessTokenService service = Build();
            string url = service.BuildSignedUrl("content/tenant-1/10/abc.png", TimeSpan.FromSeconds(-5));

            service.TryReadStorageKey(ExtractToken(url), out _).Should().BeFalse();
        }

        [Test]
        public void TryReadStorageKey_should_reject_tampered_signature()
        {
            MediaAccessTokenService service = Build();
            string token = ExtractToken(service.BuildSignedUrl("content/tenant-1/10/abc.png"));
            char last = token[^1];
            string tampered = token[..^1] + (last == 'A' ? 'B' : 'A');

            service.TryReadStorageKey(tampered, out _).Should().BeFalse();
        }

        [Test]
        public void TryReadStorageKey_should_reject_token_signed_with_other_secret()
        {
            string token = ExtractToken(Build("secret-one-aaaaaaaaaaaaaaaaaa").BuildSignedUrl("content/x/1/a.png"));

            Build("secret-two-bbbbbbbbbbbbbbbbbb").TryReadStorageKey(token, out _).Should().BeFalse();
        }

        [Test]
        public void TryReadStorageKey_should_reject_garbage()
        {
            MediaAccessTokenService service = Build();

            service.TryReadStorageKey("not-a-valid-token", out _).Should().BeFalse();
            service.TryReadStorageKey("", out _).Should().BeFalse();
        }

        [Test]
        public void BuildSignedUrl_should_throw_when_secret_missing()
        {
            MediaAccessTokenService service = Build(secret: "");

            Action act = () => service.BuildSignedUrl("content/x/1/a.png");

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
