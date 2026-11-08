
namespace RaceDay.Part3.Models
{
    public class RaceCategoryViewModel
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string CategoryName { get; set; } = "";

        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }

        public int MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }

        public int AvailablePlaces =>
            Math.Max(0, MaxParticipants - CurrentParticipants);

        public bool IsFull =>
            CurrentParticipants >= MaxParticipants;
    }
}

