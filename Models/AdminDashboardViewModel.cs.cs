
namespace RaceDay.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalOrganisers { get; set; }
        public int TotalParticipants { get; set; }
        public int UnassignedUsers { get; set; }

        public List<AdminUserViewModel> Users { get; set; } = new();
    }

    public class AdminUserViewModel
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public List<string> Roles { get; set; } = new();
    }
}
