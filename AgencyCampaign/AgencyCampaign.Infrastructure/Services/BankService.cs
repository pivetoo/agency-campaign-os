using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.Banks;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Abstractions;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class BankService : IBankService
    {
        private readonly DbContext dbContext;
        private readonly ICurrentUser currentUser;

        public BankService(DbContext dbContext, ICurrentUser currentUser)
        {
            this.dbContext = dbContext;
            this.currentUser = currentUser;
        }

        public async Task<PagedResult<BankModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<Bank> query = dbContext.Set<Bank>().AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lower = search.ToLower();
                query = query.Where(item =>
                    item.Name.ToLower().Contains(lower)
                    || item.ShortName.ToLower().Contains(lower)
                    || item.Compe.Contains(lower));
            }

            PagedResult<Bank> paged = await query
                .OrderBy(item => item.Compe)
                .ToPagedResultAsync(request, cancellationToken);

            return new PagedResult<BankModel>
            {
                Items = paged.Items.Select(ToModel).ToArray(),
                Pagination = paged.Pagination
            };
        }

        public async Task<List<BankModel>> GetActive(CancellationToken cancellationToken = default)
        {
            return await dbContext.Set<Bank>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.ShortName)
                .Select(item => new BankModel
                {
                    Id = item.Id,
                    Compe = item.Compe,
                    Ispb = item.Ispb,
                    Name = item.Name,
                    ShortName = item.ShortName,
                    LogoUrl = item.LogoUrl,
                    IsActive = item.IsActive,
                    IsSystem = item.IsSystem,
                    CreatedByUserName = item.CreatedByUserName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<BankModel?> GetById(long id, CancellationToken cancellationToken = default)
        {
            Bank? bank = await dbContext.Set<Bank>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return bank is null ? null : ToModel(bank);
        }

        public async Task<BankModel> Create(CreateBankRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureCompeIsUnique(request.Compe, ignoreId: null, cancellationToken);

            string? createdByUserName = currentUser.UserName ?? currentUser.Email;
            Bank bank = new(request.Compe, request.Name, request.ShortName, request.Ispb, request.LogoUrl, isSystem: false, createdByUserName: createdByUserName);
            dbContext.Set<Bank>().Add(bank);
            await dbContext.SaveChangesAsync(cancellationToken);
            return ToModel(bank);
        }

        public async Task<BankModel> Update(long id, UpdateBankRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            Bank? bank = await dbContext.Set<Bank>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (bank is null)
            {
                throw new InvalidOperationException("bank.notFound");
            }

            await EnsureCompeIsUnique(request.Compe, ignoreId: id, cancellationToken);

            bank.Update(request.Compe, request.Name, request.ShortName, request.Ispb, request.LogoUrl, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
            return ToModel(bank);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            Bank? bank = await dbContext.Set<Bank>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (bank is null)
            {
                throw new InvalidOperationException("bank.notFound");
            }

            if (bank.IsSystem)
            {
                throw new InvalidOperationException("bank.system.cannotDelete");
            }

            bool inUse = await dbContext.Set<FinancialAccount>()
                .AsNoTracking()
                .AnyAsync(item => item.BankId == id, cancellationToken);

            if (inUse)
            {
                throw new InvalidOperationException("bank.hasAccounts.cannotDelete");
            }

            dbContext.Set<Bank>().Remove(bank);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureCompeIsUnique(string compe, long? ignoreId, CancellationToken cancellationToken)
        {
            string normalized = compe.Trim();
            bool exists = await dbContext.Set<Bank>()
                .AsNoTracking()
                .AnyAsync(item => item.Compe == normalized && (ignoreId == null || item.Id != ignoreId), cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("bank.compe.duplicated");
            }
        }

        private static BankModel ToModel(Bank bank)
        {
            return new BankModel
            {
                Id = bank.Id,
                Compe = bank.Compe,
                Ispb = bank.Ispb,
                Name = bank.Name,
                ShortName = bank.ShortName,
                LogoUrl = bank.LogoUrl,
                IsActive = bank.IsActive,
                IsSystem = bank.IsSystem,
                CreatedByUserName = bank.CreatedByUserName
            };
        }
    }
}
