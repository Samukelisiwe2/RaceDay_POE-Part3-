
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class OrganiserProfileViewModel
    {
        public int? OrganiserId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Organiser Name")]
        public string OrganiserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
