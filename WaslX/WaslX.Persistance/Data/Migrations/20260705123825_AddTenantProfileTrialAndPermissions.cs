using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantProfileTrialAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PlatformUserId",
                table: "tenants",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentPeriodEnd",
                table: "tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerType",
                table: "tenants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "tenants",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnboardingCompleted",
                table: "tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OnboardingStep",
                table: "tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "tenants",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "tenants",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "subscription_plans",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Features",
                table: "subscription_plans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "subscription_plans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustom",
                table: "subscription_plans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "subscription_plans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceYearly",
                table: "subscription_plans",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "subscription_plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tagline",
                table: "subscription_plans",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrialDays",
                table: "subscription_plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "permissions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsScope",
                table: "permissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ScopeOptions",
                table: "permissions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "permissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "permissions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    payment_method_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Last4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    ExpMonth = table.Column<int>(type: "int", nullable: false),
                    ExpYear = table.Column<int>(type: "int", nullable: false),
                    HolderName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_methods", x => x.payment_method_id);
                    table.ForeignKey(
                        name: "FK_payment_methods_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "tenant_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_role_permissions",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    ScopeValue = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_role_permissions", x => new { x.TenantId, x.Role, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_tenant_role_permissions_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "permissions",
                        principalColumn: "permission_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tenant_role_permissions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "tenant_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMi/eIFqhse8FG5xMteh3v8pSNOgNwY0pwYMEuonyEmBtZn2lc/7CqoVweSGs+0JGA==");

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "permission_id", "Category", "Code", "CreatedAt", "Description", "IsScope", "ScopeOptions", "SortOrder", "Tier", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Conversations", "conversation.view_scope", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Which conversations they can see", true, "assigned,team,all", 10, "Configurable", null },
                    { 2, "Conversations", "conversation.reply", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Send WhatsApp replies", false, null, 11, "Configurable", null },
                    { 3, "Conversations", "conversation.note", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Add internal notes", false, null, 12, "Configurable", null },
                    { 4, "Conversations", "conversation.status", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Change conversation status", false, null, 13, "Configurable", null },
                    { 5, "Conversations", "conversation.priority", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Set conversation priority", false, null, 14, "Configurable", null },
                    { 6, "Conversations", "conversation.assign", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Assign / take conversations", false, null, 15, "Configurable", null },
                    { 7, "Conversations", "conversation.stage_handoff", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Move a conversation across stages", false, null, 16, "Configurable", null },
                    { 8, "Conversations", "conversation.delete", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Delete conversations", false, null, 17, "AdminOnly", null },
                    { 9, "Contacts", "contact.view_scope", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Which contacts they can see", true, "team,all", 20, "Configurable", null },
                    { 10, "Contacts", "contact.edit", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Edit contact details", false, null, 21, "Configurable", null },
                    { 11, "Contacts", "contact.export", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Export contacts", false, null, 22, "ManagerPlus", null },
                    { 12, "Contacts", "contact.delete", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Delete contacts", false, null, 23, "AdminOnly", null },
                    { 13, "Tags", "tag.apply", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Apply / remove tags", false, null, 30, "Configurable", null },
                    { 14, "Tags", "tag.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Create / edit / delete tags", false, null, 31, "Configurable", null },
                    { 15, "Routing & Teams", "routing.configure", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Configure routing / round-robin", false, null, 40, "ManagerPlus", null },
                    { 16, "Routing & Teams", "group.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manage groups, teams & stages", false, null, 41, "ManagerPlus", null },
                    { 17, "Routing & Teams", "assignment.reassign_others", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Reassign other people's conversations", false, null, 42, "ManagerPlus", null },
                    { 18, "AI", "ai.use_suggestions", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "See AI reply suggestions", false, null, 50, "Configurable", null },
                    { 19, "AI", "ai.configure", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Configure AI (RAG / routing / models)", false, null, 51, "AdminOnly", null },
                    { 20, "Campaigns", "campaign.view", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "View campaigns", false, null, 60, "Configurable", null },
                    { 21, "Campaigns", "campaign.create_send", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Create & send campaigns", false, null, 61, "ManagerPlus", null },
                    { 22, "Reports", "report.view_own", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "View own performance", false, null, 70, "Configurable", null },
                    { 23, "Reports", "report.view_team", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "View team reports", false, null, 71, "ManagerPlus", null },
                    { 24, "Reports", "report.export", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Export reports", false, null, 72, "ManagerPlus", null },
                    { 25, "WhatsApp", "channel.connect", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Connect / disconnect WhatsApp", false, null, 80, "AdminOnly", null },
                    { 26, "WhatsApp", "template.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manage message templates", false, null, 81, "ManagerPlus", null },
                    { 27, "Team & Access", "user.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Invite / manage users & assign roles", false, null, 90, "AdminOnly", null },
                    { 28, "Team & Access", "role.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Edit roles & permissions", false, null, 91, "AdminOnly", null },
                    { 29, "Workspace", "billing.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manage plan, subscription & invoices", false, null, 100, "AdminOnly", null },
                    { 30, "Workspace", "settings.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manage workspace settings", false, null, 101, "AdminOnly", null },
                    { 31, "Workspace", "audit.view", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "View audit logs", false, null, 102, "AdminOnly", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_Code",
                table: "subscription_plans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_methods_TenantId",
                table: "payment_methods",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_role_permissions_PermissionId",
                table: "tenant_role_permissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_role_permissions_TenantId_Role",
                table: "tenant_role_permissions",
                columns: new[] { "TenantId", "Role" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "tenant_role_permissions");

            migrationBuilder.DropIndex(
                name: "IX_subscription_plans_Code",
                table: "subscription_plans");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "permission_id",
                keyValue: 31);

            migrationBuilder.DropColumn(
                name: "CurrentPeriodEnd",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "CustomerType",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "OnboardingCompleted",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "OnboardingStep",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "Features",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "IsCustom",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "PriceYearly",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "Tagline",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "TrialDays",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "IsScope",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "ScopeOptions",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "permissions");

            migrationBuilder.AlterColumn<int>(
                name: "PlatformUserId",
                table: "tenants",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEB3XdvSm05juJA7XtdnPvc9q3qOFwxYwxE6TfivyoRo7eoenY+Ai+gN/4RTrNtDcsA==");
        }
    }
}
