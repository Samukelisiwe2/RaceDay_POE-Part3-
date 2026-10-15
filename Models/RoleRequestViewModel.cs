
using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class RoleRequestViewModel
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Requested Role")]
        public string RequestedRole { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        [Display(Name = "Date Requested")]
        public DateTime RequestedAt { get; set; }
    }
}
