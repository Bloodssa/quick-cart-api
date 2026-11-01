using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuickCartApi.Common;
using QuickCartApi.DTOs.Voucher;
using QuickCartApi.Enums;
using QuickCartApi.Extensions;
using QuickCartApi.Services.Interfaces;

namespace QuickCartApi.Controllers
{
    [ApiVersion(1)]
    [Route("api/v{version:apiVersion}/vouchers")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitingExtension.ReadPolicy)]
    public class VouchersController(IVoucherService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<VoucherResponse>>> GetAll([FromQuery] VoucherStatus? status) =>
            Ok(await service.GetAllAsync(status));

        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        [HttpPost]
        public async Task<ActionResult<VoucherResponse>> Create(CreateVoucherRequest request)
        {
            var result = await service.CreateAsync(request);
            return result.Success ? Ok(result.Value) : result.ToProblem();
        }

        // enable or disable a voucher without deleting it
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        [HttpPost("{id:int}/active")]
        public async Task<ActionResult<VoucherResponse>> SetActive(int id, SetVoucherActiveRequest request)
        {
            var result = await service.SetActiveAsync(id, request.IsActive);
            return result.Success ? Ok(result.Value) : result.ToProblem();
        }
    }
}