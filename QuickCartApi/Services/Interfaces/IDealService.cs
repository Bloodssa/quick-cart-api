using QuickCartApi.Common;
using QuickCartApi.DTOs.Deal;
using QuickCartApi.Enums;

namespace QuickCartApi.Services.Interfaces
{
    public interface IDealService
    {
        Task<ServiceResult<DealResponse>> CreateAsync(CreateDealRequest request);
        Task<List<DealResponse>> GetAllAsync(DealStatus? status);
        Task<ServiceResult> EndAsync(int id);

        // storefront: every product on discount right now
        Task<List<ActiveDealResponse>> GetActiveAsync();

        // checkout: the discounted price for these products (products without a deal are not in the result)
        Task<Dictionary<int, ActiveDealResponse>> GetActivePricesAsync(List<int> productIds);
    }
}