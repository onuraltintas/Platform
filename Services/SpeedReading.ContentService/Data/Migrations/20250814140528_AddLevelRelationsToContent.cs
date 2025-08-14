using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedReading.ContentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLevelRelationsToContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LevelId",
                table: "Texts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TargetComprehension",
                table: "ReadingLevels",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LevelId",
                table: "Questions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LevelId",
                table: "Exercises",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Texts_LevelId",
                table: "Texts",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_LevelId",
                table: "Questions",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_LevelId",
                table: "Exercises",
                column: "LevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_ReadingLevels_LevelId",
                table: "Exercises",
                column: "LevelId",
                principalTable: "ReadingLevels",
                principalColumn: "LevelId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ReadingLevels_LevelId",
                table: "Questions",
                column: "LevelId",
                principalTable: "ReadingLevels",
                principalColumn: "LevelId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Texts_ReadingLevels_LevelId",
                table: "Texts",
                column: "LevelId",
                principalTable: "ReadingLevels",
                principalColumn: "LevelId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_ReadingLevels_LevelId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ReadingLevels_LevelId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Texts_ReadingLevels_LevelId",
                table: "Texts");

            migrationBuilder.DropIndex(
                name: "IX_Texts_LevelId",
                table: "Texts");

            migrationBuilder.DropIndex(
                name: "IX_Questions_LevelId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_LevelId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "LevelId",
                table: "Texts");

            migrationBuilder.DropColumn(
                name: "LevelId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "LevelId",
                table: "Exercises");

            migrationBuilder.AlterColumn<decimal>(
                name: "TargetComprehension",
                table: "ReadingLevels",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
