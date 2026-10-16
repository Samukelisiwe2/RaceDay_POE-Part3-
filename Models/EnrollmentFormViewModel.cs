
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class EnrollmentFormViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a race category.")]
        [Display(Name = "Race Category")]
        public int CategoryId { get; set; }
    }
}

