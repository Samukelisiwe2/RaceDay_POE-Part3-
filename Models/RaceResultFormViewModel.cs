
using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class RaceResultFormViewModel
    {
        public int ResultId { get; set; }

        public int EventId { get; set; }

        [Required]
        public int EnrollmentId { get; set; }

        public string ParticipantName { get; set; }
            = string.Empty;

        public string EventName { get; set; }
            = string.Empty;

        public string CategoryName { get; set; }
            = string.Empty;

        public string RaceNumber { get; set; }
            = string.Empty;

        [DataType(DataType.DateTime)]
        [Display(Name = "Start Time")]
        public DateTime? StartTime { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Finish Time")]
        public DateTime? FinishTime { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Finishing position must be greater than zero.")]
        [Display(Name = "Overall Position")]
        public int? OverallPosition { get; set; }

        [Required(ErrorMessage = "Select a result status.")]
        [RegularExpression(
            "^(Pending|Completed|Did Not Finish|Disqualified)$",
            ErrorMessage = "Select a valid result status.")]
        [Display(Name = "Result Status")]
        public string ResultStatus { get; set; } = "Pending";
    }
}


