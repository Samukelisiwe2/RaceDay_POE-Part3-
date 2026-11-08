
using System;

namespace RaceDay.Part3.Models
{
    public class EventResultViewModel
    {
        public int ResultId { get; set; }
        public int EnrollmentId { get; set; }

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string RaceNumber { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string ResultStatus { get; set; } = "";

        public DateTime? StartTime { get; set; }
        public DateTime? FinishTime { get; set; }
        public int? OverallPosition { get; set; }

        public string FullName =>
            $"{FirstName} {LastName}".Trim();
    }
}
