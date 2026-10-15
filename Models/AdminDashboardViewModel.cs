

using System.Collections.Generic;

namespace RaceDay.Models
{
    public class AdminDashboardViewModel
    {
        // Dashboard statistics
        public int TotalUsers { get; set; }
        public int TotalOrganisers { get; set; }
        public int TotalParticipants { get; set; }
        public int UnassignedUsers { get; set; }

        // Registered users
        public List<AdminUserViewModel> Users { get; set; } = new();

        // Pending role approval requests
        public List<RoleRequestViewModel> PendingRequests { get; set; } = new();
    }

    public class AdminUserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
