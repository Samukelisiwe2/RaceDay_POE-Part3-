
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Part3.Models
{
    public class ResultEntryViewModel : IValidatableObject
    {
        [Range(1, int.MaxValue)]
        public int EventId { get; set; }

        [Range(1, int.MaxValue)]
        public int EnrollmentId { get; set; }

        [Required]
        [RegularExpression(
            "^(Pending|Completed|Did Not Finish|Disqualified)$")]
        [Display(Name = "Result Status")]
        public string ResultStatus { get; set; } = "Pending";

        [DataType(DataType.DateTime)]
        [Display(Name = "Start Time")]
        public DateTime? StartTime { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Finish Time")]
        public DateTime? FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Overall Position")]
        public int? OverallPosition { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext context)
        {
            if (ResultStatus == "Completed")
            {
                if (!StartTime.HasValue ||
                    !FinishTime.HasValue ||
                    !OverallPosition.HasValue)
                {
                    yield return new ValidationResult(
                        "Completed results require start time, " +
                        "finish time and overall position.");
                }
            }

            if (StartTime.HasValue && FinishTime.HasValue &&
                FinishTime.Value < StartTime.Value)
            {
                yield return new ValidationResult(
                    "Finish time cannot be before start time.",
                    new[] { nameof(FinishTime) });
            }
        }
    }
}
