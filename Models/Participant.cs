
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    [Table("Participant")]
    public class Participant
    {
        [Key]
        [Column("Participant_ID")]
        public int ParticipantId { get; set; }

        [Required]
        [StringLength(80)]
        [Column("First_Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        [Column("Last_Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Column("Date_Of_Birth", TypeName = "date")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [StringLength(20)]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Column("Phone_Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        // One participant can register for many races.
        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();
    }
}
