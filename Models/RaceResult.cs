
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    [Table("Race_Result")]
    public class RaceResult
    {
        [Key]
        [Column("Result_ID")]
        public int ResultId { get; set; }

        [Required]
        [Column("Enrollment_ID")]
        public int EnrollmentId { get; set; }

        [Column("Start_Time", TypeName = "datetime2")]
        public DateTime? StartTime { get; set; }

        [Column("Finish_Time", TypeName = "datetime2")]
        public DateTime? FinishTime { get; set; }

        [Column("Overall_Position")]
        public int? OverallPosition { get; set; }

        [Required]
        [StringLength(30)]
        [Column("Result_Status")]
        public string ResultStatus { get; set; } = "Pending";

        // Each result belongs to one enrollment.
        [ForeignKey(nameof(EnrollmentId))]
        public Enrollment Enrollment { get; set; } = null!;
    }
}
