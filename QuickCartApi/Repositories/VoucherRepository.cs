using Microsoft.EntityFrameworkCore;
using QuickCartApi.Data;
using QuickCartApi.Entities;
using QuickCartApi.Enums;
using QuickCartApi.Repositories.Interfaces;

namespace QuickCartApi.Repositories
{
    public class VoucherRepository(AppDbContext context) : IVoucherRepository
    {
        public Task<bool> CodeExistsAsync(string code) =>
            context.Vouchers.AnyAsync(v => v.Code == code);

        public async Task AddAsync(Voucher voucher)
        {
            context.Vouchers.Add(voucher);
            await context.SaveChangesAsync();
        }

        public Task<Voucher?> GetByIdAsync(int id) =>
            context.Vouchers.FirstOrDefaultAsync(v => v.Id == id);

        public Task<Voucher?> GetByCodeAsync(string code) =>
            context.Vouchers.AsNoTracking().FirstOrDefaultAsync(v => v.Code == code);

        public async Task UpdateAsync(Voucher voucher) => await context.SaveChangesAsync();

        public Task<List<Voucher>> GetRecentAsync() =>
            context.Vouchers.AsNoTracking()
                .OrderByDescending(v => v.StartsAtUtc)
                .Take(200)
                .ToListAsync();

        public Task<int> CountUserUsesAsync(int voucherId, int userId) =>
            context.Orders.CountAsync(o => o.VoucherId == voucherId
                                           && o.UserId == userId
                                           && o.Status != OrderStatus.Cancelled);

        // one conditional UPDATE: two shoppers can never take the last use
        public async Task<bool> TryClaimUseAsync(int voucherId) =>
            await context.Vouchers
                .Where(v => v.Id == voucherId && (v.UsageLimit == null || v.UsedCount < v.UsageLimit))
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.UsedCount, v => v.UsedCount + 1)) == 1;

        public async Task ReleaseUseAsync(int voucherId) =>
            await context.Vouchers
                .Where(v => v.Id == voucherId && v.UsedCount > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.UsedCount, v => v.UsedCount - 1));
    }
}