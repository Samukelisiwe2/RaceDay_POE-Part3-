
using RaceDay.Part3.Models.Events;

namespace RaceDay.Part3.Models
{
    public class RaceEntryPageViewModel
    {
        public EventViewModel Event { get; set; } = new();

        public List<RaceCategoryViewModel> Categories
        { get; set; } = new();
    }
}
