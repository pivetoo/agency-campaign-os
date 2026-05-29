using System.Net.Mail;
using AgencyCampaign.Application.Requests.BrandContacts;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class BrandContactService : IBrandContactService
    {
        private readonly DbContext dbContext;

        public BrandContactService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IReadOnlyList<BrandContactModel>> GetByBrand(long brandId, CancellationToken cancellationToken = default)
        {
            List<BrandContact> contacts = await dbContext.Set<BrandContact>()
                .AsNoTracking()
                .Where(item => item.BrandId == brandId)
                .OrderBy(item => item.Type)
                .ThenByDescending(item => item.IsPrimary)
                .ThenBy(item => item.Id)
                .ToListAsync(cancellationToken);

            return contacts.Select(BrandContactModel.FromEntity).ToList();
        }

        public async Task<BrandContactModel> Add(long brandId, AddBrandContactRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureBrandExists(brandId, cancellationToken);
            ValidateValue(request.Type, request.Value);

            bool hasOfType = await dbContext.Set<BrandContact>()
                .AsNoTracking()
                .AnyAsync(item => item.BrandId == brandId && item.Type == request.Type, cancellationToken);

            BrandContact contact = new(brandId, request.Type, request.Value, request.Label, !hasOfType);
            dbContext.Set<BrandContact>().Add(contact);
            await dbContext.SaveChangesAsync(cancellationToken);
            await SyncMirror(brandId, cancellationToken);
            return BrandContactModel.FromEntity(contact);
        }

        public async Task<BrandContactModel> Update(long contactId, UpdateBrandContactRequest request, CancellationToken cancellationToken = default)
        {
            BrandContact contact = await Load(contactId, cancellationToken);
            ValidateValue(contact.Type, request.Value);
            contact.Update(request.Value, request.Label);
            await dbContext.SaveChangesAsync(cancellationToken);
            await SyncMirror(contact.BrandId, cancellationToken);
            return BrandContactModel.FromEntity(contact);
        }

        public async Task Delete(long contactId, CancellationToken cancellationToken = default)
        {
            BrandContact contact = await Load(contactId, cancellationToken);
            long brandId = contact.BrandId;
            BrandContactType type = contact.Type;
            bool wasPrimary = contact.IsPrimary;

            dbContext.Set<BrandContact>().Remove(contact);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (wasPrimary)
            {
                BrandContact? next = await dbContext.Set<BrandContact>()
                    .AsTracking()
                    .Where(item => item.BrandId == brandId && item.Type == type)
                    .OrderBy(item => item.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (next is not null)
                {
                    next.SetPrimary(true);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            await SyncMirror(brandId, cancellationToken);
        }

        public async Task<BrandContactModel> SetPrimary(long contactId, CancellationToken cancellationToken = default)
        {
            BrandContact contact = await Load(contactId, cancellationToken);

            List<BrandContact> sameType = await dbContext.Set<BrandContact>()
                .AsTracking()
                .Where(item => item.BrandId == contact.BrandId && item.Type == contact.Type)
                .ToListAsync(cancellationToken);

            foreach (BrandContact item in sameType)
            {
                item.SetPrimary(item.Id == contactId);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await SyncMirror(contact.BrandId, cancellationToken);
            return BrandContactModel.FromEntity(contact);
        }

        private async Task SyncMirror(long brandId, CancellationToken cancellationToken)
        {
            List<BrandContact> contacts = await dbContext.Set<BrandContact>()
                .AsNoTracking()
                .Where(item => item.BrandId == brandId)
                .ToListAsync(cancellationToken);

            string? email = contacts
                .Where(item => item.Type == BrandContactType.Email && item.IsPrimary)
                .Select(item => item.Value)
                .FirstOrDefault();
            string? phone = contacts
                .Where(item => item.Type == BrandContactType.Phone && item.IsPrimary)
                .Select(item => item.Value)
                .FirstOrDefault();

            Brand? brand = await dbContext.Set<Brand>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == brandId, cancellationToken);

            if (brand is not null)
            {
                brand.SetContactChannels(email, phone);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task EnsureBrandExists(long brandId, CancellationToken cancellationToken)
        {
            bool exists = await dbContext.Set<Brand>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == brandId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("record.notFound");
            }
        }

        private async Task<BrandContact> Load(long contactId, CancellationToken cancellationToken)
        {
            BrandContact? contact = await dbContext.Set<BrandContact>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == contactId, cancellationToken);

            if (contact is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            return contact;
        }

        private static void ValidateValue(BrandContactType type, string value)
        {
            if (type == BrandContactType.Email && !MailAddress.TryCreate(value.Trim(), out _))
            {
                throw new InvalidOperationException("brandContact.invalidEmail");
            }
        }
    }
}
