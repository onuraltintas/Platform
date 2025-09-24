using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToSecurityPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "public",
                table: "Services",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "public",
                table: "Services",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<double>(
                name: "ConfidenceScore",
                schema: "public",
                table: "SecurityAlerts",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 100.0,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldDefaultValue: 100.0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "public",
                table: "RolePermissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWildcard",
                schema: "public",
                table: "RolePermissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PermissionPattern",
                schema: "public",
                table: "RolePermissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "public",
                table: "Permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "public",
                table: "Permissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "public",
                table: "Permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                schema: "public",
                table: "Permissions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserSessions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DeviceInfo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActivity = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastLoginAttempt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrustScore = table.Column<double>(type: "double precision", nullable: false),
                    TrustFactors = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_IsActive",
                schema: "public",
                table: "UserSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_LastActivity",
                schema: "public",
                table: "UserSessions",
                column: "LastActivity");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionId",
                schema: "public",
                table: "UserSessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_StartTime",
                schema: "public",
                table: "UserSessions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_User_Active",
                schema: "public",
                table: "UserSessions",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                schema: "public",
                table: "UserSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSessions",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "public",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "public",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "public",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "IsWildcard",
                schema: "public",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "PermissionPattern",
                schema: "public",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "public",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "public",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "public",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                schema: "public",
                table: "Permissions");

            migrationBuilder.AlterColumn<decimal>(
                name: "ConfidenceScore",
                schema: "public",
                table: "SecurityAlerts",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 100.0m,
                oldClrType: typeof(double),
                oldType: "numeric(5,2)",
                oldDefaultValue: 100.0);
        }
    }
}
