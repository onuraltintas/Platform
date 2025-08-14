using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedReading.ContentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTextCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Texts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Texts",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
