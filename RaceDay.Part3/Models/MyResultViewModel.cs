
namespace RaceDay.Part3.Models
{
    public class MyResultViewModel
    {
        public int ResultId { get; set; }
        public int EnrollmentId { get; set; }
        public int EventId { get; set; }

        public string EventName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string RaceNumber { get; set; } = "";
        public string ResultStatus { get; set; } = "";

        public DateTime? StartTime { get; set; }
        public DateTime? FinishTime { get; set; }
        public int? OverallPosition { get; set; }

        public TimeSpan? Duration =>
            StartTime.HasValue && FinishTime.HasValue
                ? FinishTime.Value - StartTime.Value
                : null;
    }
}
