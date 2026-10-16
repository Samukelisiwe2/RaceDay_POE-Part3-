
using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class EventFormViewModel
    {
        public int EventId { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Run|Walk|Cycle)$",
            ErrorMessage = "Select Run, Walk or Cycle.")]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = "Run";

        [Required]
        [RegularExpression(
            "^(Planned|Open|Closed|Completed|Cancelled)$",
            ErrorMessage = "Select a valid event status.")]
        public string Status { get; set; } = "Planned";

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Registration Closing Date")]
        public DateTime ClosingDate { get; set; } = DateTime.Today;
    }
}

