using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Notifications;
using Archon.Core.ValueObjects;

namespace AgencyCampaign.Application.Notifications
{
    public static class KanvasNotifications
    {
        public static CreateNotificationRequest ProposalViewedByBrand(Proposal proposal, string? brandName)
        {
            string subject = string.IsNullOrWhiteSpace(brandName) ? "A marca" : $"A marca {brandName}";
            return new CreateNotificationRequest
            {
                UserId = proposal.InternalOwnerId,
                Type = NotificationType.Success,
                Title = "Proposta visualizada",
                Message = $"{subject} acabou de visualizar a proposta \"{proposal.Name}\".",
                Link = $"/comercial/propostas/{proposal.Id}",
                Source = "proposal",
                ReferenceEntityName = nameof(Proposal),
                ReferenceEntityId = proposal.Id.ToString()
            };
        }

        public static CreateNotificationRequest ProposalApproved(Proposal proposal)
        {
            return new CreateNotificationRequest
            {
                UserId = proposal.InternalOwnerId,
                Type = NotificationType.Success,
                Title = "Proposta aprovada",
                Message = $"A proposta \"{proposal.Name}\" foi aprovada.",
                Link = $"/comercial/propostas/{proposal.Id}",
                Source = "proposal",
                ReferenceEntityName = nameof(Proposal),
                ReferenceEntityId = proposal.Id.ToString()
            };
        }

        public static CreateNotificationRequest ProposalRejected(Proposal proposal)
        {
            return new CreateNotificationRequest
            {
                UserId = proposal.InternalOwnerId,
                Type = NotificationType.Warning,
                Title = "Proposta rejeitada",
                Message = $"A proposta \"{proposal.Name}\" foi rejeitada.",
                Link = $"/comercial/propostas/{proposal.Id}",
                Source = "proposal",
                ReferenceEntityName = nameof(Proposal),
                ReferenceEntityId = proposal.Id.ToString()
            };
        }

        public static CreateNotificationRequest ProposalConverted(Proposal proposal, long campaignId)
        {
            return new CreateNotificationRequest
            {
                UserId = proposal.InternalOwnerId,
                Type = NotificationType.Success,
                Title = "Proposta convertida em campanha",
                Message = $"A proposta \"{proposal.Name}\" gerou uma nova campanha.",
                Link = $"/campanhas/{campaignId}",
                Source = "proposal",
                ReferenceEntityName = nameof(Proposal),
                ReferenceEntityId = proposal.Id.ToString()
            };
        }

        public static CreateNotificationRequest OpportunityApprovalRequested(OpportunityApprovalRequest request, long? opportunityId, string opportunityName)
        {
            return new CreateNotificationRequest
            {
                UserId = null,
                Type = NotificationType.Info,
                Title = "Aprovação pendente",
                Message = $"{request.RequestedByUserName} solicitou aprovação para a oportunidade \"{opportunityName}\".",
                Link = opportunityId.HasValue ? $"/comercial/oportunidades/{opportunityId.Value}" : "/comercial/aprovacoes",
                Source = "opportunity",
                ReferenceEntityName = nameof(OpportunityApprovalRequest),
                ReferenceEntityId = request.Id.ToString()
            };
        }

        public static CreateNotificationRequest OpportunityApprovalDecided(OpportunityApprovalRequest request, long? opportunityId, string opportunityName, bool approved)
        {
            return new CreateNotificationRequest
            {
                UserId = request.RequestedByUserId,
                Type = approved ? NotificationType.Success : NotificationType.Warning,
                Title = approved ? "Aprovação concedida" : "Aprovação negada",
                Message = approved
                    ? $"Sua solicitação de aprovação para \"{opportunityName}\" foi aprovada."
                    : $"Sua solicitação de aprovação para \"{opportunityName}\" foi negada.",
                Link = opportunityId.HasValue ? $"/comercial/oportunidades/{opportunityId.Value}" : "/comercial/aprovacoes",
                Source = "opportunity",
                ReferenceEntityName = nameof(OpportunityApprovalRequest),
                ReferenceEntityId = request.Id.ToString()
            };
        }

        public static CreateNotificationRequest DeliverableApprovedByBrand(CampaignDeliverable deliverable, string reviewerName)
        {
            return new CreateNotificationRequest
            {
                UserId = null,
                Type = NotificationType.Success,
                Title = "Marca aprovou entrega",
                Message = $"{reviewerName} aprovou a entrega \"{deliverable.Title}\".",
                Link = $"/campanhas/{deliverable.CampaignId}",
                Source = "deliverable",
                ReferenceEntityName = nameof(CampaignDeliverable),
                ReferenceEntityId = deliverable.Id.ToString()
            };
        }

        public static CreateNotificationRequest DeliverableRejectedByBrand(CampaignDeliverable deliverable, string reviewerName, string? comment)
        {
            string suffix = string.IsNullOrWhiteSpace(comment) ? string.Empty : $" Comentário: {comment}";
            return new CreateNotificationRequest
            {
                UserId = null,
                Type = NotificationType.Error,
                Title = "Marca rejeitou entrega",
                Message = $"{reviewerName} rejeitou a entrega \"{deliverable.Title}\".{suffix}",
                Link = $"/campanhas/{deliverable.CampaignId}",
                Source = "deliverable",
                ReferenceEntityName = nameof(CampaignDeliverable),
                ReferenceEntityId = deliverable.Id.ToString()
            };
        }

        public static CreateNotificationRequest CampaignCreatorConfirmed(CampaignCreator campaignCreator, string creatorName, string campaignName)
        {
            return new CreateNotificationRequest
            {
                UserId = null,
                Type = NotificationType.Success,
                Title = "Creator confirmou participação",
                Message = $"{creatorName} confirmou participação na campanha \"{campaignName}\".",
                Link = $"/campanhas/{campaignCreator.CampaignId}",
                Source = "campaign",
                ReferenceEntityName = nameof(CampaignCreator),
                ReferenceEntityId = campaignCreator.Id.ToString()
            };
        }

        public static CreateNotificationRequest CampaignCreatorCancelled(CampaignCreator campaignCreator, string creatorName, string campaignName)
        {
            return new CreateNotificationRequest
            {
                UserId = null,
                Type = NotificationType.Warning,
                Title = "Creator cancelou participação",
                Message = $"{creatorName} cancelou participação na campanha \"{campaignName}\".",
                Link = $"/campanhas/{campaignCreator.CampaignId}",
                Source = "campaign",
                ReferenceEntityName = nameof(CampaignCreator),
                ReferenceEntityId = campaignCreator.Id.ToString()
            };
        }

        public static CreateNotificationRequest PayoutGenerationFailed(CampaignDeliverable deliverable)
        {
            return new CreateNotificationRequest
            {
                UserId = null,
                Type = NotificationType.Error,
                Title = "Falha ao gerar pagamento",
                Message = $"Não foi possível gerar o pagamento da entrega \"{deliverable.Title}\" automaticamente. Verifique manualmente.",
                Link = $"/campanhas/{deliverable.CampaignId}",
                Source = "deliverable",
                ReferenceEntityName = nameof(CampaignDeliverable),
                ReferenceEntityId = deliverable.Id.ToString()
            };
        }

        public static CreateNotificationRequest FinancialEntrySettled(FinancialEntry entry)
        {
            bool isReceivable = entry.Type == FinancialEntryType.Receivable;
            return new CreateNotificationRequest
            {
                UserId = null,
                Type = NotificationType.Success,
                Title = isReceivable ? "Pagamento recebido" : "Pagamento realizado",
                Message = isReceivable
                    ? $"Recebido R$ {entry.Amount:N2} de {entry.CounterpartyName ?? "marca"} ({entry.Description})."
                    : $"Pago R$ {entry.Amount:N2} para {entry.CounterpartyName ?? "fornecedor"} ({entry.Description}).",
                Link = isReceivable ? "/financeiro/receber" : "/financeiro/pagar",
                Source = "financial",
                ReferenceEntityName = nameof(FinancialEntry),
                ReferenceEntityId = entry.Id.ToString()
            };
        }
    }
}
