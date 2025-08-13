using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgitimPlatform.Services.IdentityService.Migrations
{
    public partial class AddAuthorizationPolicies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthorizationPolicies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MatchRegex = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RequiredRolesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredPermissionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationPolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationPolicies_MatchRegex_TenantId",
                table: "AuthorizationPolicies",
                columns: new[] { "MatchRegex", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorizationPolicies");
        }
    }
}

