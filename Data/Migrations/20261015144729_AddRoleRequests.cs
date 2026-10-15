
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Data.Migrations
{
    public partial class AddRoleRequests : Migration
    {
        // Create the role requests table
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    UserId = table.Column<string>(
                        type: "nvarchar(max)", nullable: false),

                    RequestedRole = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false),

                    Status = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false),

                    RequestedAt = table.Column<DateTime>(
                        type: "datetime2", nullable: false),

                    ReviewedAt = table.Column<DateTime>(
                        type: "datetime2", nullable: true),

                    ReviewedByUserId = table.Column<string>(
                        type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleRequests", x => x.Id);
                });
        }

        // Remove the table if the migration is rolled back
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleRequests");
        }
    }
}
