using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuickCartApi.Common;
using QuickCartApi.DTOs.Reviews;
using QuickCartApi.DTOs.Reviews.Admin;
using QuickCartApi.Extensions;
using QuickCartApi.Services.Interfaces;
using System.Security.Claims;

namespace QuickCartApi.Controllers;

    [ApiVersion(1)]
    [Route("api/v{version:apiVersion}")]
    [ApiController]
    public class ReviewsController(IReviewService service) : ControllerBase
    {
        // the id comes from the token, never from the request body
        private int? CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingExtension.ReadPolicy)]
        [HttpGet("products/{productId:int}/reviews/summary")]
        public async Task<IActionResult> GetSummary(int productId)
        {
            var result = await service.GetSummaryAsync(productId);
            return result.Success ? Ok(result.Value) : result.ToProblem();
        }

        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingExtension.ReadPolicy)]
        [HttpGet("products/{productId:int}/reviews")]
        public async Task<IActionResult> GetReviews(int productId, [FromQuery] ReviewQuery query)
        {
            var result = await service.GetReviewsAsync(productId, query, CurrentUserId);
            return result.Success ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize]
        [EnableRateLimiting(RateLimitingExtension.ReadPolicy)]
        [HttpGet("products/{productId:int}/reviews/mine")]
        public async Task<IActionResult> GetMine(int productId)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            var result = await service.GetMineAsync(productId, userId);
            return result.Success ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        [HttpPost("products/{productId:int}/reviews")]
        public async Task<IActionResult> Create(int productId, [FromForm] SaveReviewRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            var result = await service.CreateAsync(productId, userId, request);
            return result.Success ? NoContent() : result.ToProblem();
        }

        [Authorize]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        [HttpPut("products/{productId:int}/reviews/mine")]
        public async Task<IActionResult> Update(int productId, [FromForm] SaveReviewRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            var result = await service.UpdateAsync(productId, userId, request);
            return result.Success ? NoContent() : result.ToProblem();
        }

        [Authorize]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        [HttpDelete("products/{productId:int}/reviews/mine")]
        public async Task<IActionResult> DeleteMine(int productId)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            var result = await service.DeleteMineAsync(productId, userId);
            return result.Success ? NoContent() : result.ToProblem();
        }

        // moderation: an admin removes any review
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        [HttpDelete("reviews/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await service.DeleteAsync(id);
            return result.Success ? NoContent() : result.ToProblem();
        }

        // GET api/v1/admin/reviews?filter=needsReply&withPhotos=false&search=lamp&page=1&pageSize=10
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting(RateLimitingExtension.ReadPolicy)]
        [HttpGet("admin/reviews")]
        public async Task<IActionResult> GetAdminReviews([FromQuery] AdminReviewQuery query)
            => Ok(await service.GetAdminReviewsAsync(query));

        // PUT api/v1/admin/reviews/5/reply   (creates the reply, or edits it)
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        [HttpPut("admin/reviews/{id:int}/reply")]
        public async Task<IActionResult> SetReply(int id, [FromBody] ReplyRequest request)
        {
            var result = await service.SetReplyAsync(id, request.Reply);
            return result.Success ? Ok(result.Value) : result.ToProblem();
        }

        // DELETE api/v1/admin/reviews/5/reply
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        [HttpDelete("admin/reviews/{id:int}/reply")]
        public async Task<IActionResult> DeleteReply(int id)
        {
            var result = await service.DeleteReplyAsync(id);
            return result.Success ? NoContent() : result.ToProblem();
        }
}