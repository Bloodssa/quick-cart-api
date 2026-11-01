using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuickCartApi.Common;
using QuickCartApi.DTOs.Deal;
using QuickCartApi.Enums;
using QuickCartApi.Extensions;
using QuickCartApi.Services.Interfaces;

namespace QuickCartApi.Controllers
{
    [ApiVersion(1)]
    [Route("api/v{version:apiVersion}/deals")]
    [ApiController]
    [EnableRateLimiting(RateLimitingExtension.ReadPolicy)]
    public class DealsController(IDealService service) : ControllerBase
    {
        // storefront: products on discount right now (anyone can call it)
        [AllowAnonymous]
        [HttpGet("active")]
        public async Task<ActionResult<List<ActiveDealResponse>>> GetActive() =>
            Ok(await service.GetActiveAsync());

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<List<DealResponse>>> GetAll([FromQuery] DealStatus? status) =>
            Ok(await service.GetAllAsync(status));

        [Authorize(Roles = "Admin")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        [HttpPost]
        public async Task<ActionResult<DealResponse>> Create(CreateDealRequest request)
        {
            var result = await service.CreateAsync(request);
            return result.Success ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        [HttpPost("{id:int}/end")]
        public async Task<IActionResult> End(int id)
        {
            var result = await service.EndAsync(id);
            return result.Success ? NoContent() : result.ToProblem();
        }
    }
}