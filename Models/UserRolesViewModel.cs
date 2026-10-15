using System.Collections.Generic;

namespace RaceDay.Models
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public IList<string> Roles { get; set; } = new List<string>();
    }
}
