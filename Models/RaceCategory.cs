
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    [Table("Race_Category")]
    public class RaceCategory
    {
        [Key]
        [Column("Category_ID")]
        public int CategoryId { get; set; }

        [Required]
        [Column("Event_ID")]
        public int EventId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("Category_Name")]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [Column("Distance_KM", TypeName = "decimal(6,2)")]
        public decimal DistanceKm { get; set; }

        [Required]
        [Column("Entry_Fee", TypeName = "decimal(10,2)")]
        public decimal EntryFee { get; set; }

        [Required]
        [Column("Max_Participants")]
        public int MaxParticipants { get; set; }

        [Required]
        [Column("Current_Participants")]
        public int CurrentParticipants { get; set; } = 0;

        // Each category belongs to one event.
        [ForeignKey(nameof(EventId))]
        public Event Event { get; set; } = null!;

        // One category can have many enrolments.
        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();
    }
}

