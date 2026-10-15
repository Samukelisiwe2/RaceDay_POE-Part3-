
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    [Table("Payment")]
    public class Payment
    {
        [Key]
        [Column("Payment_ID")]
        public int PaymentId { get; set; }

        [Required]
        [Column("Enrollment_ID")]
        public int EnrollmentId { get; set; }

        [Required]
        [Column("Amount", TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column("Payment_Date", TypeName = "date")]
        public DateTime PaymentDate { get; set; }
            = DateTime.Today;

        [Required]
        [StringLength(50)]
        [Column("Payment_Method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        [Column("Payment_Status")]
        public string PaymentStatus { get; set; } = "Pending";

        // Each payment belongs to one enrollment.
        [ForeignKey(nameof(EnrollmentId))]
        public Enrollment Enrollment { get; set; } = null!;
    }
}
