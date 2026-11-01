using QuickCartApi.Common;
using QuickCartApi.DTOs.Users;

namespace QuickCartApi.Services.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResult<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    }
}
