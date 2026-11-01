using QuickCartApi.Entities;

namespace QuickCartApi.Repositories.Interfaces
{
    public interface IVoucherRepository
    {
        Task<bool> CodeExistsAsync(string code);
        Task AddAsync(Voucher voucher);
        Task<Voucher?> GetByIdAsync(int id);
        Task<Voucher?> GetByCodeAsync(string code);
        Task UpdateAsync(Voucher voucher); // the voucher is tracked: this just saves the changes
        Task<List<Voucher>> GetRecentAsync();

        // how many non-cancelled orders this user already placed with the voucher
        Task<int> CountUserUsesAsync(int voucherId, int userId);

        // false when the usage limit is already reached
        Task<bool> TryClaimUseAsync(int voucherId);
        Task ReleaseUseAsync(int voucherId);
    }
}