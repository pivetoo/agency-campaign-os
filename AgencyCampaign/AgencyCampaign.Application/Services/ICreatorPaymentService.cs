using AgencyCampaign.Application.Requests.CreatorPayments;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface ICreatorPaymentService : ICrudService<CreatorPayment>
    {
        Task<PagedResult<CreatorPayment>> GetPayments(PagedRequest request, CancellationToken cancellationToken = default);
        Task<CreatorPayment?> GetPaymentById(long id, CancellationToken cancellationToken = default);
        Task<List<CreatorPayment>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default);
        Task<List<CreatorPayment>> GetByStatus(int status, CancellationToken cancellationToken = default);
        Task<CreatorPayment> CreatePayment(CreateCreatorPaymentRequest request, CancellationToken cancellationToken = default);
        Task<CreatorPayment> UpdatePayment(long id, UpdateCreatorPaymentRequest request, CancellationToken cancellationToken = default);
        Task<CreatorPayment> AttachInvoice(long id, AttachInvoiceRequest request, CancellationToken cancellationToken = default);
        Task<CreatorPayment> MarkPaid(long id, MarkCreatorPaymentPaidRequest request, CancellationToken cancellationToken = default);
        Task<CreatorPayment> Cancel(long id, CancellationToken cancellationToken = default);
        Task<List<CreatorPayment>> SchedulePaymentBatch(SchedulePaymentBatchRequest request, CancellationToken cancellationToken = default);
        Task<CreatorPayment> HandleProviderCallback(CreatorPaymentProviderCallbackRequest request, CancellationToken cancellationToken = default);
    }
}
