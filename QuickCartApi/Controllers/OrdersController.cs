using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuickCartApi.Common;
using QuickCartApi.DTOs.Order;
using QuickCartApi.Extensions;
using QuickCartApi.Services.Interfaces;

namespace QuickCartApi.Controllers
{
    [Authorize]
    [ApiVersion(1)]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [EnableRateLimiting(RateLimitingExtension.ReadPolicy)]
    public class OrdersController(IOrderService orderService, ShippingCalculator shipping) : ControllerBase
    {
        // POST api/v1/orders  { addressId, paymentMethod, cartItemIds }  ->  201 + the new order
        [HttpPost]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        public async Task<ActionResult<OrderResponse>> Place([FromBody] PlaceOrderRequest request)
        {
            var result = await orderService.PlaceOrderAsync(User.GetUserId(), request);
            if (!result.Success) return result.ToProblem();

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        // GET api/v1/orders?status=Pending&page=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<OrderListResponse>> Get([FromQuery] OrderQuery query)
            => Ok(await orderService.GetMyOrdersAsync(User.GetUserId(), query));

        // GET api/v1/orders/42: 403 when it belongs to someone else
        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderResponse>> GetById(int id)
        {
            var result = await orderService.GetMyOrderAsync(User.GetUserId(), id);
            return result.Success ? Ok(result.Value) : result.ToProblem();
        }

        // POST api/v1/orders/42/cancel: an ACTION on the order, not a delete: the record stays
        [HttpPost("{id:int}/cancel")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        public async Task<ActionResult<OrderResponse>> Cancel(int id)
        {
            var result = await orderService.CancelMyOrderAsync(User.GetUserId(), id);
            return result.Success ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("shipping-quote")]
        [EnableRateLimiting(RateLimitingExtension.ReadPolicy)]
        public ActionResult<ShippingQuote> GetShippingQuote([FromQuery] string? province, [FromQuery] string? city, [FromQuery] decimal subtotal)
             => Ok(shipping.Quote(province, city, subtotal));
        }
}
