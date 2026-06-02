using AgencyCampaign.Infrastructure.Options;
using AgencyCampaign.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.TestSupport
{
    public static class MediaTokenTestFactory
    {
        public static MediaAccessTokenService Create()
        {
            return new MediaAccessTokenService(Options.Create(new MediaStorageOptions { SigningKey = "content-review-test-signing-secret-0123456789" }));
        }
    }
}
