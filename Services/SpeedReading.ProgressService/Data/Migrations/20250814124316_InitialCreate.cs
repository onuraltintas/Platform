using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedReading.ProgressService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attempts",
                columns: table => new
                {
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    WPM = table.Column<int>(type: "int", nullable: true),
                    EyeTrackingMetricsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attempts", x => x.AttemptId);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TextId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    WPM = table.Column<int>(type: "int", nullable: true),
                    ComprehensionScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    EyeTrackingMetricsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "Responses",
                columns: table => new
                {
                    ResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GivenAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Responses", x => x.ResponseId);
                    table.ForeignKey(
                        name: "FK_Responses_Attempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "Attempts",
                        principalColumn: "AttemptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Responses_AttemptId",
                table: "Responses",
                column: "AttemptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Responses");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Attempts");
        }
    }
}
