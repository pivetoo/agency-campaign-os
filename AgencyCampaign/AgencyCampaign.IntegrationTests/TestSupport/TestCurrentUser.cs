using Archon.Application.Abstractions;

namespace AgencyCampaign.IntegrationTests
{
    // ICurrentUser fixo para a suite: varios servicos leem UserId/UserName para auditoria e historico
    // (ex.: CampaignService.RegisterStatusHistory). No app real vem do AddArchonApi (contexto HTTP);
    // aqui e um usuario de teste estavel para os inserts nao quebrarem por falta de identidade.
    internal sealed class TestCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public long? UserId => 1;

        public string? UserName => "integration-tester";

        public string? Email => "integration@test.local";

        public string? ClientId => "integration-tests";
    }
}
