
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    [Table("Enrollment")]
    public class Enrollment
    {
        [Key]
        [Column("Enrollment_ID")]
        public int EnrollmentId { get; set; }

        [Required]
        [Column("Participant_ID")]
        public int ParticipantId { get; set; }

        [Required]
        [Column("Category_ID")]
        public int CategoryId { get; set; }

        [Required]
        [Column("Enrollment_Date", TypeName = "date")]
        public DateTime EnrollmentDate { get; set; }
            = DateTime.Today;

        [Required]
        [StringLength(30)]
        [Column("Race_Number")]
        public string RaceNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        [Column("Enrollment_Status")]
        public string EnrollmentStatus { get; set; } = "Pending";

        // Each enrolment belongs to one participant.
        [ForeignKey(nameof(ParticipantId))]
        public Participant Participant { get; set; } = null!;

        // Each enrolment belongs to one race category.
        [ForeignKey(nameof(CategoryId))]
        public RaceCategory RaceCategory { get; set; } = null!;

        // One enrolment can have multiple payment attempts.
        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();

        // One enrolment can have at most one race result.
        public RaceResult? RaceResult { get; set; }
    }
}

