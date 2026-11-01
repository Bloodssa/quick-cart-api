using QuickCartApi.Common;
using QuickCartApi.DTOs.Voucher;
using QuickCartApi.Enums;

namespace QuickCartApi.Services.Interfaces
{
    public interface IVoucherService
    {
        Task<ServiceResult<VoucherResponse>> CreateAsync(CreateVoucherRequest request);
        Task<List<VoucherResponse>> GetAllAsync(VoucherStatus? status);
        Task<ServiceResult<VoucherResponse>> SetActiveAsync(int id, bool isActive);

        // checkout: checks the code for this user and cart, claims one use, returns the discount.
        // Call it inside the transaction that creates the order.
        Task<ServiceResult<AppliedVoucher>> ApplyAsync(int userId, string code, decimal subTotal);

        // called when an order that used a voucher is cancelled
        Task ReleaseAsync(int voucherId);
    }
}