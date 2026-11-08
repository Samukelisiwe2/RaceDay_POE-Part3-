
namespace RaceDay.Part3.Models
{
    public class MyEnrolmentViewModel
    {
        public int EnrollmentId { get; set; }
        public int EventId { get; set; }
        public int CategoryId { get; set; }

        public string EventName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string RaceNumber { get; set; } = "";
        public string EnrollmentStatus { get; set; } = "";

        public DateTime EnrollmentDate { get; set; }
    }
}

