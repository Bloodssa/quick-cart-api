using System.ComponentModel.DataAnnotations;

namespace QuickCartApi.DTOs.Users
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [MaxLength(100)]
        [RegularExpression(@"^(?=.*\d).+$", ErrorMessage = "Password must contain at least one number.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the new password.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
