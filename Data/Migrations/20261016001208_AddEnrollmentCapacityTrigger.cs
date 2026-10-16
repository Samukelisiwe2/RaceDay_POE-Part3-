
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Data.Migrations
{
    public partial class AddEnrollmentCapacityTrigger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Prevent overbooking and maintain participant counts.
            migrationBuilder.Sql(@"
                CREATE TRIGGER dbo.Trigger_Enrollment_Category_Capacity
                ON dbo.Enrollment
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Identify affected race categories.
                    SELECT DISTINCT Category_ID
                    INTO #AffectedCategories
                    FROM
                    (
                        SELECT Category_ID FROM inserted
                        UNION
                        SELECT Category_ID FROM deleted
                    ) AS Changes;

                    -- Process affected categories in a consistent order.
                    DECLARE @CategoryId INT;
                    DECLARE @Maximum INT;
                    DECLARE @Current INT;

                    DECLARE CategoryCursor CURSOR LOCAL FAST_FORWARD
                    FOR
                        SELECT Category_ID
                        FROM #AffectedCategories
                        ORDER BY Category_ID;

                    OPEN CategoryCursor;

                    FETCH NEXT FROM CategoryCursor INTO @CategoryId;

                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        -- Lock the category during validation.
                        SELECT @Maximum = Max_Participants
                        FROM dbo.Race_Category WITH (UPDLOCK, HOLDLOCK)
                        WHERE Category_ID = @CategoryId;

                        -- Count active enrolments.
                        SELECT @Current = COUNT(*)
                        FROM dbo.Enrollment
                        WHERE Category_ID = @CategoryId
                          AND Enrollment_Status <> 'Cancelled';

                        -- Reject enrolments exceeding capacity.
                        IF @Current > @Maximum
                        BEGIN
                            CLOSE CategoryCursor;
                            DEALLOCATE CategoryCursor;

                            THROW 50003,
                                'Race category capacity has been exceeded.',
                                1;
                        END;

                        -- Update the stored participant count.
                        UPDATE dbo.Race_Category
                        SET Current_Participants = @Current
                        WHERE Category_ID = @CategoryId;

                        FETCH NEXT FROM CategoryCursor INTO @CategoryId;
                    END;

                    CLOSE CategoryCursor;
                    DEALLOCATE CategoryCursor;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the trigger if migration is rolled back.
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS
                    dbo.Trigger_Enrollment_Category_Capacity;
            ");
        }
    }
}
