using QuickCartApi.Common;
using QuickCartApi.DTOs.Order;
using QuickCartApi.DTOs.Order.Customer;

namespace QuickCartApi.Services.Interfaces
{
    public interface IOrderService
    {
        // customer service
        Task<ServiceResult<OrderResponse>> PlaceOrderAsync(int userId, PlaceOrderRequest request);
        Task<MyOrderListResponse> GetMyOrdersAsync(int userId, OrderQuery query);
        Task<ServiceResult<OrderResponse>> GetMyOrderAsync(int userId, int orderId);
        Task<ServiceResult<OrderResponse>> CancelMyOrderAsync(int userId, int orderId);

        // admin service
        Task<OrderListResponse> GetAllAsync(OrderQuery query);
        Task<ServiceResult<OrderResponse>> GetByIdAsync(int orderId);
        Task<ServiceResult<OrderResponse>> UpdateStatusAsync(int orderId, UpdateOrderStatusRequest request);
        Task<ServiceResult<OrderPreviewResponse>> PreviewAsync(int userId, OrderPreviewRequest request);
    }
}
