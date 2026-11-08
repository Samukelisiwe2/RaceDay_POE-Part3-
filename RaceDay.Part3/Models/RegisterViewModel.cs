
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Part3.Models
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password",
            ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";

        [Required]
        [RegularExpression("^(Organiser|Participant)$",
            ErrorMessage = "Choose a valid account type.")]
        public string RequestedRole { get; set; } = "";
    }
}
