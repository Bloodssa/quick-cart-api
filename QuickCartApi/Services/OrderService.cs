using Microsoft.EntityFrameworkCore;
using QuickCartApi.Common;
using QuickCartApi.Data;
using QuickCartApi.DTOs.Address;
using QuickCartApi.DTOs.Order;
using QuickCartApi.DTOs.Order.Customer;
using QuickCartApi.DTOs.Voucher;
using QuickCartApi.Entities;
using QuickCartApi.Enums;
using QuickCartApi.Repositories.Interfaces;
using QuickCartApi.Services.Interfaces;

namespace QuickCartApi.Services
{
    public class OrderService(
        IOrderRepository orderRepository,
        ICartItemRepository cartItemRepository,
        IAddressRepository addressRepository,
        IProductRepository productRepository,
        IDealService dealService,
        IVoucherService voucherService,
        ILogger<OrderService> logger,
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ShippingCalculator shipping
    ) : IOrderService
    {
        private const decimal ShippingFee = 0m;

        public async Task<ServiceResult<OrderResponse>> PlaceOrderAsync(int userId, PlaceOrderRequest request)
        {
            // validate if address exists
            // must exists and belong to the user -> userid
            var address = await addressRepository.GetByIdForUserAsync(request.AddressId, userId);
            if (address is null)
                return Result.Fail<OrderResponse>(ErrorKind.Validation, "Please select a valid delivery address.");

            var cartItemIds = request.CartItemIds.Distinct().ToList(); // get the query param from ?items=1,2,3
            var cartItems = await cartItemRepository.GetByIdsForUserAsync(userId, cartItemIds); // get the details of the ids

            if (cartItems.Count != cartItemIds.Count)
                return Result.Fail<OrderResponse>(ErrorKind.Conflict, "Some selected items are no longer in your cart. Please review your cart and try again.");

            // a product with an active discount is charged the discounted price
            var dealPrices = await dealService.GetActivePricesAsync(cartItems.Select(ci => ci.ProductId).Distinct().ToList());

            // LIST<OrderItems>
            // make a list of the select cart item can be specific or one checkout or multiple from the cart
            var orderItems = cartItems.Select(ci =>
            {
                var unitPrice = dealPrices.TryGetValue(ci.ProductId, out var deal) ? deal.DealPrice : ci.Product.Price;

                return new OrderItem
                {
                    ProductId = ci.ProductId,
                    UnitPrice = unitPrice,
                    Quantity = ci.Quantity,
                    SubTotal = unitPrice * ci.Quantity
                };
            }).ToList();

            var subTotal = orderItems.Sum(i => i.SubTotal);

            // begin transaction for multiple operations
            await using var transaction = await context.Database.BeginTransactionAsync();

            // optional voucher: checked and claimed inside the transaction, so a failed order gives it back
            AppliedVoucher? voucher = null;
            if (!string.IsNullOrWhiteSpace(request.VoucherCode))
            {
                var applied = await voucherService.ApplyAsync(userId, request.VoucherCode, subTotal);
                if (!applied.Success)
                    return Result.Fail<OrderResponse>(applied.Kind, applied.Error!); // disposing rolls back

                voucher = applied.Value;
            }

            var discount = voucher?.Discount ?? 0m;
            var total = subTotal - discount + ShippingFee;

            var order = new Order
            {
                UserId = userId,
                AddressId = address.Id,
                OrderNumber = await GenerateOrderNumberAsync(),
                SubTotal = subTotal,
                VoucherId = voucher?.VoucherId,
                VoucherCode = voucher?.Code,
                DiscountAmount = discount,
                Total = total,
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow,
                Items = orderItems,
                Transaction = new Transaction
                {
                    TransactionReference = $"QC-TRN-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}",
                    Amount = total,
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = PaymentStatus.Pending,
                    TransactionDate = DateTime.UtcNow
                }
            };

            var removed = await cartItemRepository.DeleteByIdsAsync(userId, cartItemIds);
            if (removed != cartItemIds.Count)
                return Result.Fail<OrderResponse>(ErrorKind.Conflict, "Your cart changed");

            foreach (var item in cartItems.OrderBy(i => i.ProductId))
            {
                // quantity exceds
                if (!await productRepository.TryDecrementStockAsync(item.ProductId, item.Quantity))
                    return Result.Fail<OrderResponse>(ErrorKind.Validation, $"Insufficient stock for '{item.Product.Name}'.");
            }

            await orderRepository.AddAsync(order);

            await transaction.CommitAsync();

            var created = await orderRepository.GetByIdWithDetailsAsync(order.Id);
            return Result.Ok(ToResponse(created!));
        }

        public async Task<MyOrderListResponse> GetMyOrdersAsync(int userId, OrderQuery query)
        {
            Normalize(query);
            var (items, total) = await orderRepository.GetByUserIdAsync(userId, query);
            return ToMyListResponse(items, total, query);
        }

        public async Task<ServiceResult<OrderResponse>> GetMyOrderAsync(int userId, int orderId)
        {
            var order = await orderRepository.GetByIdWithDetailsAsync(orderId);
            if (order is null)
                return Result.Fail<OrderResponse>(ErrorKind.NotFound, "Order not found");

            // ERR-QC-107
            if (order.UserId != userId)
            {
                logger.LogWarning("User {UserId} tried to open order {OrderId} that belongs to another user.", userId, orderId);
                return Result.Fail<OrderResponse>(ErrorKind.Forbidden, "You are not authorized to view this order.");
            }

            return Result.Ok(ToResponse(order));
        }

        public async Task<ServiceResult<OrderResponse>> CancelMyOrderAsync(int userId, int orderId)
        {
            var order = await orderRepository.GetByIdWithDetailsAsync(orderId);
            if (order is null)
                return Result.Fail<OrderResponse>(ErrorKind.NotFound, "Order not found.");

            if (order.UserId != userId)
            {
                logger.LogWarning("User {UserId} tried to cancel order {OrderId} that belongs to another user.", userId, orderId);
                return Result.Fail<OrderResponse>(ErrorKind.Forbidden, "You are not authorized to cancel this order.");
            }

            if (order.Status != OrderStatus.Pending || order.Transaction?.PaymentStatus == PaymentStatus.Paid)
                return Result.Fail<OrderResponse>(ErrorKind.Validation, "Only pending, unpaid orders can be cancelled.");

            // status change + restock succeed together or not at all
            await using var transaction = await context.Database.BeginTransactionAsync();

            var changed = await ApplyOrderStatusAsync(order, OrderStatus.Cancelled); // restocks the items
            if (!changed.Success)
                return Result.Fail<OrderResponse>(changed.Kind, changed.Error!);    // disposing rolls back

            await orderRepository.UpdateAsync(order);

            await transaction.CommitAsync();

            return Result.Ok(ToResponse(order));
        }


        // admin service
        public async Task<ServiceResult<OrderResponse>> UpdateStatusAsync(int orderId, UpdateOrderStatusRequest request)
        {
            if (request.Status is null && request.PaymentStatus is null)
                return Result.Fail<OrderResponse>(ErrorKind.Validation, "Provide a status or a paymentStatus to update.");

            var order = await orderRepository.GetByIdWithDetailsAsync(orderId);
            if (order is null)
                return Result.Fail<OrderResponse>(ErrorKind.NotFound, "Order not found.");

            // order status + payment status + restock succeed together or not at all
            await using var transaction = await context.Database.BeginTransactionAsync();

            // order status first, so "cancel + paid" in one request is judged against the NEW status
            if (request.Status is { } newStatus)
            {
                var changed = await ApplyOrderStatusAsync(order, newStatus);
                if (!changed.Success)
                    return Result.Fail<OrderResponse>(changed.Kind, changed.Error!); // disposing rolls back
            }

            if (request.PaymentStatus is { } newPayment
                && order.Transaction is not null
                && order.Transaction.PaymentStatus != newPayment)
            {
                if (!order.Transaction.PaymentStatus.CanTransitionTo(newPayment))
                    return Result.Fail<OrderResponse>(ErrorKind.Validation,
                        $"Cannot change a {order.Transaction.PaymentStatus} payment to {newPayment}.");

                if (newPayment == PaymentStatus.Paid && order.Status == OrderStatus.Cancelled)
                    return Result.Fail<OrderResponse>(ErrorKind.Validation, "A cancelled order cannot be marked as paid.");

                order.Transaction.PaymentStatus = newPayment;

                // the ERD has one date column, so for a Paid payment it records when the payment was confirmed
                if (newPayment == PaymentStatus.Paid)
                    order.Transaction.TransactionDate = DateTime.UtcNow;
            }

            await orderRepository.UpdateAsync(order); // the order is tracked: this just saves the changes

            await transaction.CommitAsync();

            return Result.Ok(ToResponse(order));
        }

        public async Task<OrderListResponse> GetAllAsync(OrderQuery query)
        {
            Normalize(query);
            var (items, total) = await orderRepository.GetAllAsync(query);
            return ToListResponse(items, total, query);
        }

        public async Task<ServiceResult<OrderResponse>> GetByIdAsync(int orderId)
        {
            var order = await orderRepository.GetByIdWithDetailsAsync(orderId);

            return order is null
                ? Result.Fail<OrderResponse>(ErrorKind.NotFound, "Order not found.")
                : Result.Ok(ToResponse(order));
        }

        /**
         * HELPERS
         */

        // Moves a TRACKED order to a new status if the flow allows it. Cancelling puts the stock back.
        // Does not save: the caller does, inside its transaction.
        private async Task<ServiceResult> ApplyOrderStatusAsync(Order order, OrderStatus newStatus)
        {
            if (order.Status == newStatus) return Result.Ok();

            if (!order.Status.CanTransitionTo(newStatus))
                return Result.Fail(ErrorKind.Validation, $"Cannot change an order from {order.Status} to {newStatus}.");

            if (newStatus == OrderStatus.Cancelled)
            {
                foreach (var item in order.Items)
                    await productRepository.IncrementStockAsync(item.ProductId, item.Quantity);

                // the voucher use goes back, so the customer can use it on another order
                if (order.VoucherId is { } voucherId)
                    await voucherService.ReleaseAsync(voucherId);
            }

            order.Status = newStatus;
            return Result.Ok();
        }

        // loop until it is done getting all order then generate the ordernumber
        // SAMPLE: QC-20261019-A1B2C3
        private async Task<string> GenerateOrderNumberAsync()
        {
            string number;
            do
            {
                number = $"QC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            }
            while (await orderRepository.OrderNumberExistsAsync(number));

            return number;
        }

        private static void Normalize(OrderQuery query)
        {
            query.Page = query.Page < 1 ? 1 : query.Page;
            query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;
        }

        private static OrderListResponse ToListResponse(List<Order> items, int total, OrderQuery query) => new()
        {
            Orders = items.Select(o => ToSummary(o)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalResults = total
        };

        private static OrderSummaryResponse ToSummary(Order order) => new()
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.User?.FullName ?? string.Empty,
            Status = order.Status.ToString(),
            PaymentStatus = order.Transaction?.PaymentStatus.ToString() ?? string.Empty,
            Total = order.Total,
            ItemCount = order.Items.Sum(i => i.Quantity),
            OrderDate = order.OrderDate
        };

        private OrderResponse ToResponse(Order order) => new()
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            CustomerName = order.User.FullName,
            SubTotal = order.SubTotal,
            ShippingFee = order.ShippingFee,
            ShippingDiscount = order.ShippingDiscount,
            Total = order.Total,
            OrderDate = order.OrderDate,
            DiscountAmount = order.DiscountAmount,
            VoucherCode = order.VoucherCode,

            Address = new AddressResponse
            {
                Id = order.Address.Id,
                RecipientName = order.Address.RecipientName,
                ContactNumber = order.Address.ContactNumber,
                AddressLine = order.Address.AddressLine,
                Barangay = order.Address.Barangay,
                City = order.Address.City,
                Province = order.Address.Province,
                PostalCode = order.Address.PostalCode,
                IsDefault = order.Address.IsDefault
            },

            Items = order.Items.Select(i => new OrderItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                ImageUrl = GetFullImageUrl(i.Product.Images.OrderByDescending(img => img.IsDefault).Select(img => img.ImageUrl).FirstOrDefault()),
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                SubTotal = i.SubTotal
            }).ToList(),

            Payment = order.Transaction is null ? null : new PaymentResponse
            {
                TransactionReference = order.Transaction.TransactionReference,
                PaymentMethod = order.Transaction.PaymentMethod,
                PaymentStatus = order.Transaction.PaymentStatus.ToString(),
                Amount = order.Transaction.Amount,
                TransactionDate = order.Transaction.TransactionDate
            }
        };

        private MyOrderListResponse ToMyListResponse(List<Order> items, int total, OrderQuery query) => new()
        {
            Orders = items.Select(ToMySummary).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalResults = total
        };

        private MyOrderSummaryResponse ToMySummary(Order order) => new()
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            PaymentStatus = order.Transaction?.PaymentStatus.ToString() ?? string.Empty,
            Total = order.Total,
            ItemCount = order.Items.Sum(i => i.Quantity),
            Items = order.Items.Select(i => new MyOrderItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                ImageUrl = GetProductImageUrl(i.Product),
                Quantity = i.Quantity
            }).ToList(),
            OrderDate = order.OrderDate
        };

        private string? GetProductImageUrl(Product product) =>
            GetFullImageUrl(product.Images
                .OrderByDescending(img => img.IsDefault)
                .Select(img => img.ImageUrl)
                .FirstOrDefault());

        // todo: refactor and clean up
        private string? GetFullImageUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null; // product has no image

            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return url;

            var request = httpContextAccessor.HttpContext?.Request;
            if (request is null)
                return url;

            return $"{request.Scheme}://{request.Host}{url}";
        }
    }
}
