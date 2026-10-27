
namespace RaceDay.Part3.Models.Events
{
    public class EventViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = "";

        public string Description { get; set; } = "";

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = "";

        public string EventType { get; set; } = "";

        public string Status { get; set; } = "";

        public DateTime ClosingDate { get; set; }
    }
}

