
namespace RaceDay.Part3.Models
{
    public class EventEnrolmentViewModel
    {
        public int EnrollmentId { get; set; }
        public int ParticipantId { get; set; }
        public int CategoryId { get; set; }

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string RaceNumber { get; set; } = "";
        public string EnrollmentStatus { get; set; } = "";

        public string FullName =>
            $"{FirstName} {LastName}".Trim();
    }
}
