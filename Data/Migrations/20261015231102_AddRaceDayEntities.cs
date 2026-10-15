using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRaceDayEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organiser",
                columns: table => new
                {
                    Organiser_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Organiser_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone_Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organiser", x => x.Organiser_ID);
                });

            migrationBuilder.CreateTable(
                name: "Participant",
                columns: table => new
                {
                    Participant_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    First_Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Last_Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Date_Of_Birth = table.Column<DateTime>(type: "date", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone_Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participant", x => x.Participant_ID);
                });

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    Event_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Organiser_ID = table.Column<int>(type: "int", nullable: false),
                    Event_Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Event_Date = table.Column<DateTime>(type: "date", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Event_Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Planned"),
                    Closing_Date = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Event_ID);
                    table.CheckConstraint("CK_Event_Closing_Date", "[Closing_Date] <= [Event_Date]");
                    table.CheckConstraint("CK_Event_Status", "[Status] IN ('Planned', 'Open', 'Closed', 'Completed', 'Cancelled')");
                    table.CheckConstraint("CK_Event_Type", "[Event_Type] IN ('Run', 'Walk', 'Cycle')");
                    table.ForeignKey(
                        name: "FK_Event_Organiser_Organiser_ID",
                        column: x => x.Organiser_ID,
                        principalTable: "Organiser",
                        principalColumn: "Organiser_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Race_Category",
                columns: table => new
                {
                    Category_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Event_ID = table.Column<int>(type: "int", nullable: false),
                    Category_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Distance_KM = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Entry_Fee = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 0.00m),
                    Max_Participants = table.Column<int>(type: "int", nullable: false),
                    Current_Participants = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Race_Category", x => x.Category_ID);
                    table.CheckConstraint("CK_Race_Category_Current_Participants", "[Current_Participants] >= 0 AND [Current_Participants] <= [Max_Participants]");
                    table.CheckConstraint("CK_Race_Category_Distance", "[Distance_KM] > 0");
                    table.CheckConstraint("CK_Race_Category_Entry_Fee", "[Entry_Fee] >= 0");
                    table.CheckConstraint("CK_Race_Category_Max_Participants", "[Max_Participants] > 0");
                    table.ForeignKey(
                        name: "FK_Race_Category_Event_Event_ID",
                        column: x => x.Event_ID,
                        principalTable: "Event",
                        principalColumn: "Event_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrollment",
                columns: table => new
                {
                    Enrollment_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Participant_ID = table.Column<int>(type: "int", nullable: false),
                    Category_ID = table.Column<int>(type: "int", nullable: false),
                    Enrollment_Date = table.Column<DateTime>(type: "date", nullable: false, defaultValueSql: "CONVERT(date, GETDATE())"),
                    Race_Number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Enrollment_Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollment", x => x.Enrollment_ID);
                    table.CheckConstraint("CK_Enrollment_Status", "[Enrollment_Status] IN ('Pending', 'Confirmed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Enrollment_Participant_Participant_ID",
                        column: x => x.Participant_ID,
                        principalTable: "Participant",
                        principalColumn: "Participant_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollment_Race_Category_Category_ID",
                        column: x => x.Category_ID,
                        principalTable: "Race_Category",
                        principalColumn: "Category_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Payment_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enrollment_ID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Payment_Date = table.Column<DateTime>(type: "date", nullable: false, defaultValueSql: "CONVERT(date, GETDATE())"),
                    Payment_Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Payment_Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Payment_ID);
                    table.CheckConstraint("CK_Payment_Amount", "[Amount] >= 0");
                    table.CheckConstraint("CK_Payment_Status", "[Payment_Status] IN ('Pending', 'Paid', 'Failed', 'Refunded')");
                    table.ForeignKey(
                        name: "FK_Payment_Enrollment_Enrollment_ID",
                        column: x => x.Enrollment_ID,
                        principalTable: "Enrollment",
                        principalColumn: "Enrollment_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Race_Result",
                columns: table => new
                {
                    Result_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enrollment_ID = table.Column<int>(type: "int", nullable: false),
                    Start_Time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Finish_Time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Overall_Position = table.Column<int>(type: "int", nullable: true),
                    Result_Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Race_Result", x => x.Result_ID);
                    table.CheckConstraint("CK_Race_Result_Completed_Data", "[Result_Status] <> 'Completed' OR ([Start_Time] IS NOT NULL AND [Finish_Time] IS NOT NULL AND [Overall_Position] IS NOT NULL)");
                    table.CheckConstraint("CK_Race_Result_Position", "[Overall_Position] IS NULL OR [Overall_Position] > 0");
                    table.CheckConstraint("CK_Race_Result_Status", "[Result_Status] IN ('Pending', 'Completed', 'Did Not Finish', 'Disqualified')");
                    table.CheckConstraint("CK_Race_Result_Time", "[Finish_Time] IS NULL OR [Start_Time] IS NULL OR [Finish_Time] >= [Start_Time]");
                    table.ForeignKey(
                        name: "FK_Race_Result_Enrollment_Enrollment_ID",
                        column: x => x.Enrollment_ID,
                        principalTable: "Enrollment",
                        principalColumn: "Enrollment_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_Category_ID",
                table: "Enrollment",
                column: "Category_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_Participant_ID_Category_ID",
                table: "Enrollment",
                columns: new[] { "Participant_ID", "Category_ID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_Race_Number",
                table: "Enrollment",
                column: "Race_Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Event_Organiser_ID",
                table: "Event",
                column: "Organiser_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Organiser_Email",
                table: "Organiser",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participant_Email",
                table: "Participant",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_Enrollment_ID",
                table: "Payment",
                column: "Enrollment_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Race_Category_Event_ID_Category_Name",
                table: "Race_Category",
                columns: new[] { "Event_ID", "Category_Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Race_Result_Enrollment_ID",
                table: "Race_Result",
                column: "Enrollment_ID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "Race_Result");

            migrationBuilder.DropTable(
                name: "Enrollment");

            migrationBuilder.DropTable(
                name: "Participant");

            migrationBuilder.DropTable(
                name: "Race_Category");

            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "Organiser");
        }
    }
}
