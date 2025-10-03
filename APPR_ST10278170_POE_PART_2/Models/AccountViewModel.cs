using System.ComponentModel.DataAnnotations;

namespace APPR_ST10278170_POE_PART_2.Models
{
    public class AccountViewModel
    {
        // Shared for both login and registration
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // Only used during registration
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Action selector
        public bool IsRegistering { get; set; } = false;

        public string ReturnUrl { get; set; } = "/Dashboard/Index";
    }
}
