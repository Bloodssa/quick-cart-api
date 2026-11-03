using System.ComponentModel.DataAnnotations;

namespace QuickCartApi.DTOs.Users
{
    public class UpdateProfileRequest
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(30, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers and underscores.")]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;
    }
}

public record UserProfileResponse(int Id, string FullName, string Username, string Email);