using QuickCartApi.Common;
using QuickCartApi.DTOs.Voucher;
using QuickCartApi.Entities;
using QuickCartApi.Enums;
using QuickCartApi.Repositories.Interfaces;
using QuickCartApi.Services.Interfaces;

namespace QuickCartApi.Services
{
    public class VoucherService(IVoucherRepository repository, TimeProvider clock) : IVoucherService
    {
        public async Task<ServiceResult<VoucherResponse>> CreateAsync(CreateVoucherRequest request)
        {
            var now = Now();
            var code = VoucherRules.NormalizeCode(request.Code);
            var start = request.StartsAt.ToUniversalTime();
            var end = request.EndsAt.ToUniversalTime();

            var error = VoucherRules.ValidateNew(
                code, request.Title, request.DiscountType, request.Value, request.MinSpend,
                request.MaxDiscount, request.UsageLimit, request.PerUserLimit, start, end, now);
            if (error is not null)
                return Result.Fail<VoucherResponse>(ErrorKind.Validation, error);

            if (await repository.CodeExistsAsync(code))
                return Result.Fail<VoucherResponse>(ErrorKind.Conflict, "That voucher code is already taken.");

            var voucher = new Voucher
            {
                Code = code,
                Title = request.Title.Trim(),
                DiscountType = request.DiscountType,
                Value = request.Value,
                MinSpend = request.MinSpend,
                MaxDiscount = request.MaxDiscount,
                UsageLimit = request.UsageLimit,
                PerUserLimit = request.PerUserLimit,
                StartsAtUtc = start,
                EndsAtUtc = end
            };

            await repository.AddAsync(voucher);
            return Result.Ok(ToResponse(voucher, now));
        }

        public async Task<List<VoucherResponse>> GetAllAsync(VoucherStatus? status)
        {
            var now = Now();
            var vouchers = await repository.GetRecentAsync();

            return vouchers
                .Where(v => status is null || v.StatusAt(now) == status)
                .Select(v => ToResponse(v, now))
                .ToList();
        }

        public async Task<ServiceResult<VoucherResponse>> SetActiveAsync(int id, bool isActive)
        {
            var voucher = await repository.GetByIdAsync(id);
            if (voucher is null)
                return Result.Fail<VoucherResponse>(ErrorKind.NotFound, "Voucher not found.");

            voucher.IsActive = isActive;
            await repository.UpdateAsync(voucher);
            return Result.Ok(ToResponse(voucher, Now()));
        }

        public async Task<ServiceResult<AppliedVoucher>> ApplyAsync(int userId, string code, decimal subTotal)
        {
            var now = Now();

            var voucher = await repository.GetByCodeAsync(VoucherRules.NormalizeCode(code));
            if (voucher is null)
                return Result.Fail<AppliedVoucher>(ErrorKind.Validation, "That voucher code doesn't exist.");

            var status = voucher.StatusAt(now);
            if (status != VoucherStatus.Active)
                return Result.Fail<AppliedVoucher>(ErrorKind.Validation, status switch
                {
                    VoucherStatus.Scheduled => "This voucher isn't active yet.",
                    VoucherStatus.Expired => "This voucher has expired.",
                    VoucherStatus.UsedUp => "This voucher has been fully claimed.",
                    _ => "This voucher isn't available."
                });

            if (subTotal < voucher.MinSpend)
                return Result.Fail<AppliedVoucher>(ErrorKind.Validation,
                    $"Spend at least ₱{voucher.MinSpend:N2} to use this voucher.");

            if (await repository.CountUserUsesAsync(voucher.Id, userId) >= voucher.PerUserLimit)
                return Result.Fail<AppliedVoucher>(ErrorKind.Validation, "You have already used this voucher.");

            if (!await repository.TryClaimUseAsync(voucher.Id))
                return Result.Fail<AppliedVoucher>(ErrorKind.Conflict, "This voucher has just been fully claimed.");

            return Result.Ok(new AppliedVoucher
            {
                VoucherId = voucher.Id,
                Code = voucher.Code,
                Discount = VoucherRules.Discount(voucher, subTotal)
            });
        }

        public Task ReleaseAsync(int voucherId) => repository.ReleaseUseAsync(voucherId);

        /**
         * HELPERS
         */

        private DateTime Now() => clock.GetUtcNow().UtcDateTime;

        private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

        private static VoucherResponse ToResponse(Voucher v, DateTime now) => new()
        {
            Id = v.Id,
            Code = v.Code,
            Title = v.Title,
            DiscountType = v.DiscountType,
            Value = v.Value,
            MinSpend = v.MinSpend,
            MaxDiscount = v.MaxDiscount,
            UsageLimit = v.UsageLimit,
            UsedCount = v.UsedCount,
            PerUserLimit = v.PerUserLimit,
            StartsAt = Utc(v.StartsAtUtc),
            EndsAt = Utc(v.EndsAtUtc),
            IsActive = v.IsActive,
            Status = v.StatusAt(now).ToString()
        };
    }
}