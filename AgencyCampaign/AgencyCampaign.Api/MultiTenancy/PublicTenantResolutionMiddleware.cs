using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.MultiTenancy;
using Archon.Infrastructure.MultiTenancy;

namespace AgencyCampaign.Api.MultiTenancy
{
    // Resolve o tenant de requisicoes ANONIMAS de link publico a partir do prefixo de tenant embutido no token.
    // Guardas: so atua (1) nas rotas publicas da allow-list e (2) quando nenhum tenant foi resolvido ainda
    // (request autenticado ou modo single-tenant ja tem tenant -> este middleware nao toca em nada).
    public sealed class PublicTenantResolutionMiddleware
    {
        private static readonly string[] PublicPrefixes =
        {
            "/api/proposal-public/",
            "/api/campaign-report-public/",
            "/api/deliverable-public/",
            "/api/creatorportal/",
            // Callback de pagamento multi-tenant: o segmento apos o prefixo e o callbackToken (com prefixo
            // de tenant). O pipeline do IntegrationPlatform deve ecoar esse token na URL do callback.
            "/api/creatorpayments/provider-callback/",
            // Callback de assinatura de documento multi-tenant (mesmo padrao do pagamento).
            "/api/campaigndocuments/provider-callback/",
            // Callback de cobranca de recebivel multi-tenant (mesmo padrao do pagamento).
            "/api/financialentries/provider-callback/"
        };

        private readonly RequestDelegate next;

        public PublicTenantResolutionMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver, ITenantContext tenantContext)
        {
            if (!tenantContext.HasTenant
                && tenantContext is MultiTenantContext multiTenantContext
                && TryGetPublicToken(context.Request.Path, out string token))
            {
                string? tenantId = PublicLinkToken.ExtractTenantId(token);
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    TenantInfo? tenant = await tenantResolver.ResolveAsync(tenantId, context.RequestAborted);
                    if (tenant is not null)
                    {
                        multiTenantContext.SetTenant(tenant);
                    }
                }
            }

            await next(context);
        }

        private static bool TryGetPublicToken(PathString path, out string token)
        {
            token = string.Empty;
            string value = path.Value ?? string.Empty;

            foreach (string prefix in PublicPrefixes)
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string rest = value[prefix.Length..];
                    int slash = rest.IndexOf('/');
                    token = slash >= 0 ? rest[..slash] : rest;
                    return token.Length > 0;
                }
            }

            return false;
        }
    }

    public static class PublicTenantResolutionMiddlewareExtensions
    {
        public static IApplicationBuilder UsePublicTenantResolution(this IApplicationBuilder app)
        {
            return app.UseMiddleware<PublicTenantResolutionMiddleware>();
        }
    }
}
