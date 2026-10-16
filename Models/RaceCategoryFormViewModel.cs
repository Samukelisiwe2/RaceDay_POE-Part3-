
using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class RaceCategoryFormViewModel
    {
        public int CategoryId { get; set; }

        public int EventId { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [Range(typeof(decimal), "0.01", "9999.99",
            ErrorMessage = "Distance must be greater than zero.")]
        [Display(Name = "Distance (KM)")]
        public decimal DistanceKm { get; set; }

        [Required]
        [Range(typeof(decimal), "0", "99999999.99",
            ErrorMessage = "Entry fee cannot be negative.")]
        [Display(Name = "Entry Fee (R)")]
        public decimal EntryFee { get; set; }

        [Required]
        [Range(1, int.MaxValue,
            ErrorMessage = "Maximum participants must be at least 1.")]
        [Display(Name = "Maximum Participants")]
        public int MaxParticipants { get; set; }
    }
}
