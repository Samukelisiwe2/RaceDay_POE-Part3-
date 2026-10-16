
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentValidationTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create a trigger to validate race payments.
            migrationBuilder.Sql(@"
                CREATE TRIGGER dbo.Trigger_Validate_Payment
                ON dbo.Payment
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Payment amount must match the
                    -- associated race category entry fee.
                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted i
                        INNER JOIN dbo.Enrollment e
                            ON e.Enrollment_ID = i.Enrollment_ID
                        INNER JOIN dbo.Race_Category c
                            ON c.Category_ID = e.Category_ID
                        WHERE i.Amount <> c.Entry_Fee
                    )
                    BEGIN
                        THROW 50001,
                            'Payment amount must match the race category entry fee.',
                            1;
                    END;

                    -- Allow multiple attempts, but only
                    -- one successful payment per enrollment.
                    IF EXISTS
                    (
                        SELECT p.Enrollment_ID
                        FROM dbo.Payment p
                        INNER JOIN
                        (
                            SELECT DISTINCT Enrollment_ID
                            FROM inserted
                        ) i
                            ON p.Enrollment_ID = i.Enrollment_ID
                        WHERE p.Payment_Status = 'Paid'
                        GROUP BY p.Enrollment_ID
                        HAVING COUNT(*) > 1
                    )
                    BEGIN
                        THROW 50002,
                            'An enrollment can only have one successful Paid payment.',
                            1;
                    END;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the trigger if the migration is rolled back.
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS dbo.Trigger_Validate_Payment;
            ");
        }
    }
}
