using QuickCartApi.Common;
using QuickCartApi.DTOs.Reviews;
using QuickCartApi.DTOs.Reviews.Admin;

namespace QuickCartApi.Services.Interfaces;

public interface IReviewService
{
    Task<ServiceResult<ReviewSummaryResponse>> GetSummaryAsync(int productId);
    Task<ServiceResult<ReviewListResponse>> GetReviewsAsync(int productId, ReviewQuery query, int? currentUserId);
    Task<ServiceResult<MyReviewResponse>> GetMineAsync(int productId, int userId);
    Task<ServiceResult<bool>> CreateAsync(int productId, int userId, SaveReviewRequest request);
    Task<ServiceResult<bool>> UpdateAsync(int productId, int userId, SaveReviewRequest request);
    Task<ServiceResult<bool>> DeleteMineAsync(int productId, int userId);
    Task<ServiceResult<bool>> DeleteAsync(int reviewId); // admin moderation
    Task<AdminReviewListResponse> GetAdminReviewsAsync(AdminReviewQuery query);
    Task<ServiceResult<AdminReviewResponse>> SetReplyAsync(int reviewId, string reply);
    Task<ServiceResult<bool>> DeleteReplyAsync(int reviewId);
}