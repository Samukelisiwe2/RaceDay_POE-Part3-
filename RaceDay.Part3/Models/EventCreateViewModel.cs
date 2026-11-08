
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Part3.Models
{
    public class EventCreateViewModel : IValidatableObject
    {
        [Required]
        [StringLength(150)]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Event Date")]
        public DateTime? EventDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Registration Closing Date")]
        public DateTime? ClosingDate { get; set; }

        [Required]
        public string Location { get; set; } = "";

        [Required]
        [RegularExpression("^(Run|Walk|Cycle)$")]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = "Run";

        [Required]
        [RegularExpression(
            "^(Planned|Open|Closed|Completed|Cancelled)$")]
        public string Status { get; set; } = "Planned";

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (EventDate.HasValue &&
                ClosingDate.HasValue &&
                ClosingDate.Value.Date > EventDate.Value.Date)
            {
                yield return new ValidationResult(
                    "Closing date cannot be after the event date.",
                    new[] { nameof(ClosingDate) });
            }
        }
    }
}

