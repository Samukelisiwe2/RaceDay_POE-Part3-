
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    [Table("Event")]
    public class Event
    {
        [Key]
        [Column("Event_ID")]
        public int EventId { get; set; }

        [Required]
        [Column("Organiser_ID")]
        public int OrganiserId { get; set; }

        [Required]
        [StringLength(150)]
        [Column("Event_Name")]
        public string EventName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column("Event_Date", TypeName = "date")]
        public DateTime EventDate { get; set; }

        [Required]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        [Column("Event_Type")]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Planned";

        [Required]
        [Column("Closing_Date", TypeName = "date")]
        public DateTime ClosingDate { get; set; }

        // Each event belongs to an organiser.
        [ForeignKey(nameof(OrganiserId))]
        public Organiser Organiser { get; set; } = null!;

        // An event can contain multiple race categories.
        public ICollection<RaceCategory> RaceCategories { get; set; }
            = new List<RaceCategory>();
    }
}
