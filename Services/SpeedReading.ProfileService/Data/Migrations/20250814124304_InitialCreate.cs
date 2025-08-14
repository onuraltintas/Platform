using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedReading.ProfileService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentReadingLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Goals = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LearningStyle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessibilityNeeds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Profiles");
        }
    }
}
