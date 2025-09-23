using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZeroTrustAndAuditSystemsFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Groups_GroupId",
                schema: "public",
                table: "Roles");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "public",
                table: "Roles",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsSystemRole",
                schema: "public",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "public",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "public",
                table: "Roles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "public",
                table: "Roles",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "public",
                table: "Roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId1",
                schema: "public",
                table: "Roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HierarchyLevel",
                schema: "public",
                table: "Roles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HierarchyPath",
                schema: "public",
                table: "Roles",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "InheritPermissions",
                schema: "public",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentRoleId",
                schema: "public",
                table: "Roles",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                schema: "public",
                table: "Roles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AlertRules",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Conditions = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    Actions = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Medium"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    NotificationChannels = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    CooldownPeriod = table.Column<TimeSpan>(type: "interval", nullable: true, defaultValue: new TimeSpan(0, 0, 5, 0, 0)),
                    MaxAlertsPerHour = table.Column<int>(type: "integer", nullable: true, defaultValue: 10),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertRules_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "public",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DeviceTrusts",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OperatingSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsTrusted = table.Column<bool>(type: "boolean", nullable: false),
                    IsManaged = table.Column<bool>(type: "boolean", nullable: false),
                    IsCompliant = table.Column<bool>(type: "boolean", nullable: false),
                    IsJailbroken = table.Column<bool>(type: "boolean", nullable: false),
                    CertificateFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TrustScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 50.0m),
                    CompliancePolicies = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    SecurityFeatures = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    AdditionalInfo = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    FirstSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastComplianceCheck = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTrusts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceTrusts_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PermissionAuditLogs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId1 = table.Column<string>(type: "text", nullable: true),
                    TargetUserId = table.Column<string>(type: "text", nullable: true),
                    TargetRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetRoleId1 = table.Column<string>(type: "text", nullable: true),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeleteAfter = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionAuditLogs_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "public",
                        principalTable: "Groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionAuditLogs_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "public",
                        principalTable: "Permissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionAuditLogs_Roles_RoleId1",
                        column: x => x.RoleId1,
                        principalSchema: "public",
                        principalTable: "Roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionAuditLogs_Roles_TargetRoleId1",
                        column: x => x.TargetRoleId1,
                        principalSchema: "public",
                        principalTable: "Roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionAuditLogs_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SecurityPolicies",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PolicyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rules = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    Conditions = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    MinimumTrustScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 50.0m),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsEnforced = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityPolicies_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "public",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TrustScores",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TrustLevel = table.Column<int>(type: "integer", nullable: false),
                    DeviceScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    NetworkScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    BehaviorScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AuthenticationScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    LocationScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Factors = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    Risks = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    Recommendations = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrustScores_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PermissionPattern = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsWildcard = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    GrantedBy = table.Column<string>(type: "text", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    Conditions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "public",
                        principalTable: "Groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "public",
                        principalTable: "Permissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAlerts",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Medium"),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "New"),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AlertData = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    TriggerConditions = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    RuleId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Resolution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAutoGenerated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 100.0m),
                    AlertRuleId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityAlerts_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalSchema: "public",
                        principalTable: "AlertRules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SecurityAlerts_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "public",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecurityAlerts_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DeviceActivities",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceTrustId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActivityData = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ErrorMessage = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceActivities_DeviceTrusts_DeviceTrustId",
                        column: x => x.DeviceTrustId,
                        principalSchema: "public",
                        principalTable: "DeviceTrusts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PolicyViolations",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ViolationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ViolationData = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Open"),
                    Resolution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyViolations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyViolations_SecurityPolicies_SecurityPolicyId",
                        column: x => x.SecurityPolicyId,
                        principalSchema: "public",
                        principalTable: "SecurityPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PolicyViolations_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrustScoreHistory",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrustScoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    NewScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventData = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustScoreHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrustScoreHistory_TrustScores_TrustScoreId",
                        column: x => x.TrustScoreId,
                        principalSchema: "public",
                        principalTable: "TrustScores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    OldValues = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    NewValues = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    Severity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Category = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsSecurityEvent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Duration = table.Column<long>(type: "bigint", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SecurityAlertId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEvents_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "public",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AuditEvents_SecurityAlerts_SecurityAlertId",
                        column: x => x.SecurityAlertId,
                        principalSchema: "public",
                        principalTable: "SecurityAlerts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditEvents_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAlertActions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityAlertId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ActionDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActionData = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAlertActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityAlertActions_SecurityAlerts_SecurityAlertId",
                        column: x => x.SecurityAlertId,
                        principalSchema: "public",
                        principalTable: "SecurityAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SecurityAlertActions_Users_PerformedBy",
                        column: x => x.PerformedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditEventAttachments",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEventAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEventAttachments_AuditEvents_AuditEventId",
                        column: x => x.AuditEventId,
                        principalSchema: "public",
                        principalTable: "AuditEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CreatedAt",
                schema: "public",
                table: "Roles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Group_Name",
                schema: "public",
                table: "Roles",
                columns: new[] { "GroupId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_GroupId1",
                schema: "public",
                table: "Roles",
                column: "GroupId1");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_HierarchyLevel",
                schema: "public",
                table: "Roles",
                column: "HierarchyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_IsActive",
                schema: "public",
                table: "Roles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_ParentRoleId",
                schema: "public",
                table: "Roles",
                column: "ParentRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Priority",
                schema: "public",
                table: "Roles",
                column: "Priority");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Roles_NoSelfReference",
                schema: "public",
                table: "Roles",
                sql: "\"Id\" != \"ParentRoleId\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Roles_ValidHierarchyLevel",
                schema: "public",
                table: "Roles",
                sql: "\"HierarchyLevel\" >= 0 AND \"HierarchyLevel\" <= 10");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_Category",
                schema: "public",
                table: "AlertRules",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_GroupId",
                schema: "public",
                table: "AlertRules",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_IsActive",
                schema: "public",
                table: "AlertRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_Name",
                schema: "public",
                table: "AlertRules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEventAttachments_AuditEventId",
                schema: "public",
                table: "AuditEventAttachments",
                column: "AuditEventId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEventAttachments_CreatedAt",
                schema: "public",
                table: "AuditEventAttachments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CorrelationId",
                schema: "public",
                table: "AuditEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Entity_Composite",
                schema: "public",
                table: "AuditEvents",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EntityId",
                schema: "public",
                table: "AuditEvents",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EntityType",
                schema: "public",
                table: "AuditEvents",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EventType",
                schema: "public",
                table: "AuditEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_GroupId",
                schema: "public",
                table: "AuditEvents",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_IsSecurityEvent",
                schema: "public",
                table: "AuditEvents",
                column: "IsSecurityEvent");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_SecurityAlertId",
                schema: "public",
                table: "AuditEvents",
                column: "SecurityAlertId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Severity",
                schema: "public",
                table: "AuditEvents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Timestamp",
                schema: "public",
                table: "AuditEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_User_Time",
                schema: "public",
                table: "AuditEvents",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_UserId",
                schema: "public",
                table: "AuditEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceActivities_ActivityType",
                schema: "public",
                table: "DeviceActivities",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceActivities_DeviceTrustId",
                schema: "public",
                table: "DeviceActivities",
                column: "DeviceTrustId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceActivities_IpAddress",
                schema: "public",
                table: "DeviceActivities",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceActivities_OccurredAt",
                schema: "public",
                table: "DeviceActivities",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTrusts_DeviceId",
                schema: "public",
                table: "DeviceTrusts",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTrusts_IsTrusted",
                schema: "public",
                table: "DeviceTrusts",
                column: "IsTrusted");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTrusts_LastSeen",
                schema: "public",
                table: "DeviceTrusts",
                column: "LastSeen");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTrusts_User_Device",
                schema: "public",
                table: "DeviceTrusts",
                columns: new[] { "UserId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTrusts_UserId",
                schema: "public",
                table: "DeviceTrusts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuditLogs_GroupId",
                schema: "public",
                table: "PermissionAuditLogs",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuditLogs_PermissionId",
                schema: "public",
                table: "PermissionAuditLogs",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuditLogs_RoleId1",
                schema: "public",
                table: "PermissionAuditLogs",
                column: "RoleId1");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuditLogs_TargetRoleId1",
                schema: "public",
                table: "PermissionAuditLogs",
                column: "TargetRoleId1");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuditLogs_TargetUserId",
                schema: "public",
                table: "PermissionAuditLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuditLogs_UserId",
                schema: "public",
                table: "PermissionAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyViolations_DetectedAt",
                schema: "public",
                table: "PolicyViolations",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyViolations_SecurityPolicyId",
                schema: "public",
                table: "PolicyViolations",
                column: "SecurityPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyViolations_Severity",
                schema: "public",
                table: "PolicyViolations",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyViolations_Status",
                schema: "public",
                table: "PolicyViolations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyViolations_User_Time",
                schema: "public",
                table: "PolicyViolations",
                columns: new[] { "UserId", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyViolations_UserId",
                schema: "public",
                table: "PolicyViolations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyViolations_ViolationType",
                schema: "public",
                table: "PolicyViolations",
                column: "ViolationType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlertActions_ActionType",
                schema: "public",
                table: "SecurityAlertActions",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlertActions_PerformedAt",
                schema: "public",
                table: "SecurityAlertActions",
                column: "PerformedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlertActions_PerformedBy",
                schema: "public",
                table: "SecurityAlertActions",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlertActions_SecurityAlertId",
                schema: "public",
                table: "SecurityAlertActions",
                column: "SecurityAlertId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_AlertRuleId",
                schema: "public",
                table: "SecurityAlerts",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_AlertType",
                schema: "public",
                table: "SecurityAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_CorrelationId",
                schema: "public",
                table: "SecurityAlerts",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_CreatedAt",
                schema: "public",
                table: "SecurityAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_GroupId",
                schema: "public",
                table: "SecurityAlerts",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Severity",
                schema: "public",
                table: "SecurityAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Status",
                schema: "public",
                table: "SecurityAlerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_UserId",
                schema: "public",
                table: "SecurityAlerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityPolicies_Category",
                schema: "public",
                table: "SecurityPolicies",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityPolicies_CreatedAt",
                schema: "public",
                table: "SecurityPolicies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityPolicies_GroupId",
                schema: "public",
                table: "SecurityPolicies",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityPolicies_IsActive",
                schema: "public",
                table: "SecurityPolicies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityPolicies_Name",
                schema: "public",
                table: "SecurityPolicies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityPolicies_PolicyType",
                schema: "public",
                table: "SecurityPolicies",
                column: "PolicyType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityPolicies_Priority",
                schema: "public",
                table: "SecurityPolicies",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_TrustScoreHistory_ChangedAt",
                schema: "public",
                table: "TrustScoreHistory",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrustScoreHistory_TrustScoreId",
                schema: "public",
                table: "TrustScoreHistory",
                column: "TrustScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_TrustScores_CalculatedAt",
                schema: "public",
                table: "TrustScores",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrustScores_Composite",
                schema: "public",
                table: "TrustScores",
                columns: new[] { "UserId", "DeviceId", "IpAddress" });

            migrationBuilder.CreateIndex(
                name: "IX_TrustScores_DeviceId",
                schema: "public",
                table: "TrustScores",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_TrustScores_IpAddress",
                schema: "public",
                table: "TrustScores",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_TrustScores_UserId",
                schema: "public",
                table: "TrustScores",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_GroupId",
                schema: "public",
                table: "UserPermissions",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                schema: "public",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId",
                schema: "public",
                table: "UserPermissions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Groups_GroupId",
                schema: "public",
                table: "Roles",
                column: "GroupId",
                principalSchema: "public",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Groups_GroupId1",
                schema: "public",
                table: "Roles",
                column: "GroupId1",
                principalSchema: "public",
                principalTable: "Groups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Roles_ParentRoleId",
                schema: "public",
                table: "Roles",
                column: "ParentRoleId",
                principalSchema: "public",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Groups_GroupId",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Groups_GroupId1",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Roles_ParentRoleId",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropTable(
                name: "AuditEventAttachments",
                schema: "public");

            migrationBuilder.DropTable(
                name: "DeviceActivities",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PermissionAuditLogs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PolicyViolations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SecurityAlertActions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "TrustScoreHistory",
                schema: "public");

            migrationBuilder.DropTable(
                name: "UserPermissions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AuditEvents",
                schema: "public");

            migrationBuilder.DropTable(
                name: "DeviceTrusts",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SecurityPolicies",
                schema: "public");

            migrationBuilder.DropTable(
                name: "TrustScores",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SecurityAlerts",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AlertRules",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Roles_CreatedAt",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_Group_Name",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_GroupId1",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_HierarchyLevel",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_IsActive",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_ParentRoleId",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_Priority",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Roles_NoSelfReference",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Roles_ValidHierarchyLevel",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "GroupId1",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "HierarchyLevel",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "HierarchyPath",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "InheritPermissions",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "ParentRoleId",
                schema: "public",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "public",
                table: "Roles");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "public",
                table: "Roles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsSystemRole",
                schema: "public",
                table: "Roles",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "public",
                table: "Roles",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "public",
                table: "Roles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "public",
                table: "Roles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "public",
                table: "Roles",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Groups_GroupId",
                schema: "public",
                table: "Roles",
                column: "GroupId",
                principalSchema: "public",
                principalTable: "Groups",
                principalColumn: "Id");
        }
    }
}
