using Microsoft.AspNetCore.Identity;
using QuickCartApi.Common;
using QuickCartApi.DTOs.Users;
using QuickCartApi.Entities;
using QuickCartApi.Repositories.Interfaces;
using QuickCartApi.Services.Interfaces;

namespace QuickCartApi.Services;
    public class UserService(
        IUserRepository userRepository,
        IPasswordHasher<User> hasher
    ) : IUserService
    {
        public async Task<ServiceResult<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return Result.Fail<bool>(ErrorKind.Validation, "The new passwords do not match.");

            var user = await userRepository.FindUserByIdAsync(userId);
            if (user is null)
                return Result.Fail<bool>(ErrorKind.NotFound, "User not found.");

            // Validation (400), not Unauthorized (401): the frontend treats a 401 as an expired
            // session, tries a token refresh and may log the admin out.
            if (!Verify(user, request.CurrentPassword))
                return Result.Fail<bool>(ErrorKind.Validation, "Current password is incorrect.");

            if (Verify(user, request.NewPassword))
                return Result.Fail<bool>(ErrorKind.Validation, "The new password must be different from the current one.");

            user.PasswordHash = Hash(user, request.NewPassword);
            await userRepository.UpdateUserAsync(user);

            return Result.Ok(true);
        }

         private bool Verify(User user, string password) =>
             hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
        private string Hash(User user, string password) => hasher.HashPassword(user, password);
}