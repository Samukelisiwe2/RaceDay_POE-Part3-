

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;

namespace RaceDay.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Authentication and role requests
        public DbSet<RoleRequest> RoleRequests { get; set; }

        // RaceDay database entities
        public DbSet<Organiser> Organisers { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<RaceCategory> RaceCategories { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<RaceResult> RaceResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Preserve ASP.NET Identity configuration
            base.OnModelCreating(modelBuilder);

            // ORGANISER
            modelBuilder.Entity<Organiser>(entity =>
            {
                entity.HasIndex(o => o.Email)
                    .IsUnique();

                entity.HasMany(o => o.Events)
                    .WithOne(e => e.Organiser)
                    .HasForeignKey(e => e.OrganiserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // EVENT
            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(e => e.Status)
                    .HasDefaultValue("Planned");

                entity.HasMany(e => e.RaceCategories)
                    .WithOne(c => c.Event)
                    .HasForeignKey(c => c.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_Event_Type",
                        "[Event_Type] IN ('Run', 'Walk', 'Cycle')");

                    t.HasCheckConstraint(
                        "CK_Event_Status",
                        "[Status] IN ('Planned', 'Open', 'Closed', 'Completed', 'Cancelled')");

                    t.HasCheckConstraint(
                        "CK_Event_Closing_Date",
                        "[Closing_Date] <= [Event_Date]");
                });
            });

            // RACE CATEGORY
            modelBuilder.Entity<RaceCategory>(entity =>
            {
                entity.HasIndex(c => new
                {
                    c.EventId,
                    c.CategoryName
                }).IsUnique();

                entity.Property(c => c.EntryFee)
                    .HasDefaultValue(0.00m);

                entity.Property(c => c.CurrentParticipants)
                    .HasDefaultValue(0);

                entity.HasMany(c => c.Enrollments)
                    .WithOne(e => e.RaceCategory)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_Race_Category_Distance",
                        "[Distance_KM] > 0");

                    t.HasCheckConstraint(
                        "CK_Race_Category_Entry_Fee",
                        "[Entry_Fee] >= 0");

                    t.HasCheckConstraint(
                        "CK_Race_Category_Max_Participants",
                        "[Max_Participants] > 0");

                    t.HasCheckConstraint(
                        "CK_Race_Category_Current_Participants",
                        "[Current_Participants] >= 0 AND [Current_Participants] <= [Max_Participants]");
                });
            });

            // PARTICIPANT
            modelBuilder.Entity<Participant>(entity =>
            {
                entity.HasIndex(p => p.Email)
                    .IsUnique();

                entity.HasMany(p => p.Enrollments)
                    .WithOne(e => e.Participant)
                    .HasForeignKey(e => e.ParticipantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ENROLLMENT
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasIndex(e => e.RaceNumber)
                    .IsUnique();

                entity.HasIndex(e => new
                {
                    e.ParticipantId,
                    e.CategoryId
                }).IsUnique();

                entity.Property(e => e.EnrollmentDate)
                    .HasDefaultValueSql("CONVERT(date, GETDATE())");

                entity.Property(e => e.EnrollmentStatus)
                    .HasDefaultValue("Pending");

                // One enrollment can have multiple payments
                entity.HasMany(e => e.Payments)
                    .WithOne(p => p.Enrollment)
                    .HasForeignKey(p => p.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // One enrollment can have one race result
                entity.HasOne(e => e.RaceResult)
                    .WithOne(r => r.Enrollment)
                    .HasForeignKey<RaceResult>(r => r.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    // Required for SQL Server AFTER triggers
                    t.UseSqlOutputClause(false);

                    t.HasCheckConstraint(
                        "CK_Enrollment_Status",
                        "[Enrollment_Status] IN ('Pending', 'Confirmed', 'Cancelled')");
                });
            });

            // PAYMENT
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.PaymentDate)
                    .HasDefaultValueSql("CONVERT(date, GETDATE())");

                entity.Property(p => p.PaymentStatus)
                    .HasDefaultValue("Pending");

                entity.ToTable(t =>
                {
                    // Required for SQL Server AFTER triggers
                    t.UseSqlOutputClause(false);

                    t.HasCheckConstraint(
                        "CK_Payment_Amount",
                        "[Amount] >= 0");

                    t.HasCheckConstraint(
                        "CK_Payment_Status",
                        "[Payment_Status] IN ('Pending', 'Paid', 'Failed', 'Refunded')");
                });
            });

            // RACE RESULT
            modelBuilder.Entity<RaceResult>(entity =>
            {
                entity.HasIndex(r => r.EnrollmentId)
                    .IsUnique();

                entity.Property(r => r.ResultStatus)
                    .HasDefaultValue("Pending");

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_Race_Result_Time",
                        "[Finish_Time] IS NULL OR [Start_Time] IS NULL OR [Finish_Time] >= [Start_Time]");

                    t.HasCheckConstraint(
                        "CK_Race_Result_Position",
                        "[Overall_Position] IS NULL OR [Overall_Position] > 0");

                    t.HasCheckConstraint(
                        "CK_Race_Result_Status",
                        "[Result_Status] IN ('Pending', 'Completed', 'Did Not Finish', 'Disqualified')");

                    t.HasCheckConstraint(
                        "CK_Race_Result_Completed_Data",
                        "[Result_Status] <> 'Completed' OR ([Start_Time] IS NOT NULL AND [Finish_Time] IS NOT NULL AND [Overall_Position] IS NOT NULL)");
                });
            });
        }
    }
}
