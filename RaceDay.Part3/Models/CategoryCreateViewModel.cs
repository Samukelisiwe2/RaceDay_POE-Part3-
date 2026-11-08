
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Part3.Models
{
    public class CategoryCreateViewModel
    {
        [Range(1, int.MaxValue)]
        public int EventId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = "";

        [Range(typeof(decimal), "0.01", "9999.99")]
        [Display(Name = "Distance (km)")]
        public decimal DistanceKm { get; set; }

        [Range(typeof(decimal), "0", "99999999.99")]
        [Display(Name = "Entry Fee (R)")]
        public decimal EntryFee { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Maximum Participants")]
        public int MaxParticipants { get; set; }
    }
}
