using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace SpaBookingWeb.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("ConfirmNewPassword", ErrorMessage = "Passwords do not match.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmNewPassword { get; set; }
    }
}