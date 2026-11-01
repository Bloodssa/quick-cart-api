using QuestPDF.Fluent;
using QuickCartApi.Common;
using QuickCartApi.DTOs.Admin.Dashboard;
using QuickCartApi.DTOs.Admin.Customer;
using QuickCartApi.DTOs.Admin.Reports;
using QuickCartApi.Reports;
using QuickCartApi.Repositories.Interfaces;
using QuickCartApi.Services.Interfaces;

namespace QuickCartApi.Services
{
    public class AdminService(
        IAdminRepository adminRepository
    ) : IAdminService
    {
        private const int MaxRangeDays = 366;
        private const int PdfMaxRows = 2000; // keeps the PDF and the memory use bounded

        public async Task<DashboardResponse> GetDashboardDataAsync()
        {
            return await adminRepository.GetDashboardAsync();
        }

        public async Task<ServiceResult<SalesReportResponse>> GetSalesReportAsync(DateOnly from, DateOnly to, int page, int pageSize)
        {
            var error = Validate(from, to);
            if (error is not null)
                return Result.Fail<SalesReportResponse>(ErrorKind.Validation, error);

            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

            var report = await BuildAsync(new ReportRange(from, to), (page - 1) * pageSize, pageSize);
            report.Page = page;
            report.PageSize = pageSize;

            return Result.Ok(report);
        }

        public async Task<ServiceResult<byte[]>> GetSalesReportPdfAsync(DateOnly from, DateOnly to)
        {
            var error = Validate(from, to);
            if (error is not null)
                return Result.Fail<byte[]>(ErrorKind.Validation, error);

            var report = await BuildAsync(new ReportRange(from, to), 0, PdfMaxRows);
            var pdf = new SalesReportDocument(report, report.Transactions, report.TotalResults > PdfMaxRows).GeneratePdf();

            return Result.Ok(pdf);
        }

        public async Task<CustomerListResponse> GetCustomersAsync(string? search, int page, int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

            var (items, total) = await adminRepository.GetCustomersAsync(search, page, pageSize);

            return new CustomerListResponse { Customers = items, Page = page, PageSize = pageSize, TotalResults = total };
        }

        public async Task<ServiceResult<CustomerDetailResponse>> GetCustomerAsync(int id)
        {
            var customer = await adminRepository.GetCustomerDetailAsync(id);

            return customer is null
                ? Result.Fail<CustomerDetailResponse>(ErrorKind.NotFound, "Customer not found.")
                : Result.Ok(customer);
        }


        // helpers function
        // Awaited one after another: a DbContext can't run queries in parallel
        private async Task<SalesReportResponse> BuildAsync(ReportRange range, int skip, int take)
        {
            var summary = await adminRepository.GetSummaryAsync(range);
            var revenueByDay = await adminRepository.GetRevenueByDayAsync(range);
            var topProducts = await adminRepository.GetTopProductsAsync(range, 5);
            var (rows, total) = await adminRepository.GetTransactionsAsync(range, skip, take);

            return new SalesReportResponse
            {
                From = range.From,
                To = range.To,
                Summary = summary,
                RevenueByDay = revenueByDay,
                TopProducts = topProducts,
                Transactions = rows,
                TotalResults = total
            };
        }

        private static string? Validate(DateOnly from, DateOnly to)
        {
            // a missing query parameter arrives as the default date (0001-01-01)
            if (from == default || to == default)
                return "Both a start date and an end date are required.";

            if (from.Year < 2000 || to.Year > 2100)
                return "Choose dates between the year 2000 and 2100.";

            if (from > to)
                return "The start date must be on or before the end date.";

            if (new ReportRange(from, to).Days > MaxRangeDays)
                return $"Choose a range of {MaxRangeDays} days or less.";

            return null;
        }
    }
}
