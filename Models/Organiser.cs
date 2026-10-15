
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    [Table("Organiser")]
    public class Organiser
    {
        [Key]
        [Column("Organiser_ID")]
        public int OrganiserId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("Organiser_Name")]
        public string OrganiserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Column("Phone_Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        // One organiser can create multiple events.
        public ICollection<Event> Events { get; set; }
            = new List<Event>();
    }
}
