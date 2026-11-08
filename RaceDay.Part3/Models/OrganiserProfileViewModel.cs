
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Part3.Models
{
    public class OrganiserProfileViewModel
    {
        public int? OrganiserId { get; set; }

        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Organisation name is required.")]
        [StringLength(100)]
        [Display(Name = "Organisation Name")]
        public string OrganiserName { get; set; } = "";

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = "";
    }
}
