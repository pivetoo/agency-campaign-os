using AgencyCampaign.Infrastructure.Options;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class MediaAccessTokenServiceTests
    {
        private static MediaAccessTokenService Build(string secret = "media-signing-secret-0123456789", string? tenantId = "tenant-1")
        {
            return new MediaAccessTokenService(Options.Create(new MediaStorageOptions { SigningKey = secret, SignedUrlMinutes = 120 }), TenantContextMock.Create(tenantId));
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

            // O ultimo char base64 do HMAC-SHA256 (32 bytes = 43 chars) codifica apenas 4 bits
            // significativos; trocar so os 2 bits de padding nao altera os bytes decodificados.
            // Usamos o primeiro char da parte de assinatura, que sempre codifica 6 bits completos.
            int dot = token.LastIndexOf('.');
            char first = token[dot + 1];
            string tampered = token[..(dot + 1)] + (first == 'A' ? 'B' : 'A') + token[(dot + 2)..];

            service.TryReadStorageKey(tampered, out _).Should().BeFalse();
        }

        [Test]
        public void TryReadStorageKey_should_reject_token_signed_with_other_secret()
        {
            string token = ExtractToken(Build("secret-one-aaaaaaaaaaaaaaaaaa").BuildSignedUrl("content/tenant-1/1/a.png"));

            Build("secret-two-bbbbbbbbbbbbbbbbbb").TryReadStorageKey(token, out _).Should().BeFalse();
        }

        [Test]
        public void TryReadStorageKey_should_reject_storage_key_outside_token_tenant_scope()
        {
            // IDOR multi-tenant: um token cunhado no contexto de tenant-a nao pode liberar a leitura de
            // midia de outro tenant, mesmo que a chave seja injetada na hora de assinar.
            MediaAccessTokenService service = Build(tenantId: "tenant-a");
            string token = ExtractToken(service.BuildSignedUrl("content/tenant-b/1/secret.png"));

            service.TryReadStorageKey(token, out _).Should().BeFalse();
        }

        [Test]
        public void TryReadStorageKey_should_roundtrip_within_same_tenant_scope()
        {
            MediaAccessTokenService service = Build(tenantId: "tenant-a");
            string token = ExtractToken(service.BuildSignedUrl("content/tenant-a/1/x.png"));

            service.TryReadStorageKey(token, out string key).Should().BeTrue();
            key.Should().Be("content/tenant-a/1/x.png");
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

        [Test]
        public void ResolveDisplayUrl_should_sign_a_storage_key()
        {
            MediaAccessTokenService service = Build();

            string resolved = service.ResolveDisplayUrl("content/tenant-1/5/nf.pdf");

            resolved.Should().StartWith("/api/media?t=");
            service.TryReadStorageKey(ExtractToken(resolved), out string key).Should().BeTrue();
            key.Should().Be("content/tenant-1/5/nf.pdf");
        }

        [Test]
        public void ResolveDisplayUrl_should_pass_through_external_and_legacy_urls()
        {
            MediaAccessTokenService service = Build();

            service.ResolveDisplayUrl("https://nf.example/123.pdf").Should().Be("https://nf.example/123.pdf");
            service.ResolveDisplayUrl("/uploads/content/old.png").Should().Be("/uploads/content/old.png");
            service.ResolveDisplayUrl("").Should().BeEmpty();
            service.ResolveDisplayUrl(null).Should().BeEmpty();
        }

        [Test]
        public void ResolveDisplayUrl_should_not_throw_without_secret()
        {
            MediaAccessTokenService service = Build(secret: "");

            service.ResolveDisplayUrl("content/tenant-1/5/nf.pdf").Should().Be("content/tenant-1/5/nf.pdf");
        }
    }
}
