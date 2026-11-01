using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuickCartApi.Common;
using QuickCartApi.DTOs.Admin.Customer;
using QuickCartApi.DTOs.Admin.Dashboard;
using QuickCartApi.DTOs.Admin.Reports;
using QuickCartApi.Extensions;
using QuickCartApi.Services.Interfaces;

namespace QuickCartApi.Controllers
{
    [ApiVersion(1)]
    [EnableRateLimiting(RateLimitingExtension.ReadPolicy)]
    [Route("api/v{version:apiVersion}/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        // GET api/v1/admin/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardResponse>> Dashboard()
            => await adminService.GetDashboardDataAsync();

        // GET api/v1/admin/reports/sales?from=2026-10-01&to=2026-10-31&page=1&pageSize=10
        [HttpGet("reports/sales")]
        public async Task<ActionResult<SalesReportResponse>> GetSalesReport(
            [FromQuery] DateOnly from, [FromQuery] DateOnly to,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await adminService.GetSalesReportAsync(from, to, page, pageSize);
            return result.Success ? Ok(result.Value) : result.ToProblem();
        }

        // GET api/v1/admin/reports/sales/pdf?from=2026-10-01&to=2026-10-31
        [HttpGet("reports/sales/pdf")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        public async Task<IActionResult> DownloadSalesReportPdf([FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            var result = await adminService.GetSalesReportPdfAsync(from, to);
            if (!result.Success) return result.ToProblem();

            return File(result.Value!, "application/pdf", $"sales-report_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.pdf");
        }

        // GET api/v1/admin/customers?search=maria&page=1&pageSize=10
        [HttpGet("customers")]
        public async Task<ActionResult<CustomerListResponse>> GetCustomers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await adminService.GetCustomersAsync(search, page, pageSize));

        // GET api/v1/admin/customers/5
        [HttpGet("customers/{id:int}")]
        public async Task<ActionResult<CustomerDetailResponse>> GetCustomer(int id)
        {
            var result = await adminService.GetCustomerAsync(id);
            return result.Success ? Ok(result.Value) : result.ToProblem();
        }
    }
}
