using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3_ChannelsDistributionWorkingHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DistributeToOffline",
                table: "whatsapp_accounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DistributionMode",
                table: "whatsapp_accounts",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlatformName",
                table: "whatsapp_accounts",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ReassignOnOffline",
                table: "whatsapp_accounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StartingGroupId",
                table: "whatsapp_accounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnBreak",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOwner",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoResolveEnabled",
                table: "tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AutoResolveHours",
                table: "tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "StickyReturningCustomer",
                table: "tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "tenants",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAssignableByDistribution",
                table: "groups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "groups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "agent_whatsapp_distribution",
                columns: table => new
                {
                    agent_whatsapp_distribution_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    WhatsAppAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_whatsapp_distribution", x => x.agent_whatsapp_distribution_id);
                    table.ForeignKey(
                        name: "FK_agent_whatsapp_distribution_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agent_whatsapp_distribution_whatsapp_accounts_WhatsAppAccountId",
                        column: x => x.WhatsAppAccountId,
                        principalTable: "whatsapp_accounts",
                        principalColumn: "wa_account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "channels",
                columns: table => new
                {
                    channel_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channels", x => x.channel_id);
                    table.ForeignKey(
                        name: "FK_channels_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "tenant_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_working_days",
                columns: table => new
                {
                    company_working_day_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "bit", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_working_days", x => x.company_working_day_id);
                    table.ForeignKey(
                        name: "FK_company_working_days_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "tenant_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shifts",
                columns: table => new
                {
                    shift_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts", x => x.shift_id);
                    table.ForeignKey(
                        name: "FK_shifts_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "tenant_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agent_channel_access",
                columns: table => new
                {
                    agent_channel_access_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_channel_access", x => x.agent_channel_access_id);
                    table.ForeignKey(
                        name: "FK_agent_channel_access_channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "channels",
                        principalColumn: "channel_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agent_channel_access_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "channel_whatsapp_accounts",
                columns: table => new
                {
                    channel_whatsapp_account_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    WhatsAppAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel_whatsapp_accounts", x => x.channel_whatsapp_account_id);
                    table.ForeignKey(
                        name: "FK_channel_whatsapp_accounts_channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "channels",
                        principalColumn: "channel_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_channel_whatsapp_accounts_whatsapp_accounts_WhatsAppAccountId",
                        column: x => x.WhatsAppAccountId,
                        principalTable: "whatsapp_accounts",
                        principalColumn: "wa_account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agent_shifts",
                columns: table => new
                {
                    agent_shift_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_shifts", x => x.agent_shift_id);
                    table.ForeignKey(
                        name: "FK_agent_shifts_shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "shifts",
                        principalColumn: "shift_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agent_shifts_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_days",
                columns: table => new
                {
                    shift_day_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_days", x => x.shift_day_id);
                    table.ForeignKey(
                        name: "FK_shift_days_shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "shifts",
                        principalColumn: "shift_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJrzSuMWghbdNn3ZH3ksTtlmvAe7qL7jAofHkmIocpOkrcr3Qq329hkfxLXhyFoNBQ==");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_accounts_StartingGroupId",
                table: "whatsapp_accounts",
                column: "StartingGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_channel_access_ChannelId",
                table: "agent_channel_access",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_channel_access_UserId_ChannelId",
                table: "agent_channel_access",
                columns: new[] { "UserId", "ChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agent_shifts_ShiftId",
                table: "agent_shifts",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_shifts_UserId_ShiftId",
                table: "agent_shifts",
                columns: new[] { "UserId", "ShiftId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agent_whatsapp_distribution_UserId_WhatsAppAccountId",
                table: "agent_whatsapp_distribution",
                columns: new[] { "UserId", "WhatsAppAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agent_whatsapp_distribution_WhatsAppAccountId",
                table: "agent_whatsapp_distribution",
                column: "WhatsAppAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_channel_whatsapp_accounts_ChannelId_WhatsAppAccountId",
                table: "channel_whatsapp_accounts",
                columns: new[] { "ChannelId", "WhatsAppAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_channel_whatsapp_accounts_WhatsAppAccountId",
                table: "channel_whatsapp_accounts",
                column: "WhatsAppAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_channels_TenantId",
                table: "channels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_company_working_days_TenantId_DayOfWeek",
                table: "company_working_days",
                columns: new[] { "TenantId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shift_days_ShiftId_DayOfWeek",
                table: "shift_days",
                columns: new[] { "ShiftId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shifts_TenantId",
                table: "shifts",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_whatsapp_accounts_groups_StartingGroupId",
                table: "whatsapp_accounts",
                column: "StartingGroupId",
                principalTable: "groups",
                principalColumn: "group_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_whatsapp_accounts_groups_StartingGroupId",
                table: "whatsapp_accounts");

            migrationBuilder.DropTable(
                name: "agent_channel_access");

            migrationBuilder.DropTable(
                name: "agent_shifts");

            migrationBuilder.DropTable(
                name: "agent_whatsapp_distribution");

            migrationBuilder.DropTable(
                name: "channel_whatsapp_accounts");

            migrationBuilder.DropTable(
                name: "company_working_days");

            migrationBuilder.DropTable(
                name: "shift_days");

            migrationBuilder.DropTable(
                name: "channels");

            migrationBuilder.DropTable(
                name: "shifts");

            migrationBuilder.DropIndex(
                name: "IX_whatsapp_accounts_StartingGroupId",
                table: "whatsapp_accounts");

            migrationBuilder.DropColumn(
                name: "DistributeToOffline",
                table: "whatsapp_accounts");

            migrationBuilder.DropColumn(
                name: "DistributionMode",
                table: "whatsapp_accounts");

            migrationBuilder.DropColumn(
                name: "PlatformName",
                table: "whatsapp_accounts");

            migrationBuilder.DropColumn(
                name: "ReassignOnOffline",
                table: "whatsapp_accounts");

            migrationBuilder.DropColumn(
                name: "StartingGroupId",
                table: "whatsapp_accounts");

            migrationBuilder.DropColumn(
                name: "IsOnBreak",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsOwner",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AutoResolveEnabled",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "AutoResolveHours",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "StickyReturningCustomer",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "IsAssignableByDistribution",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "groups");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGqOjkSy/GRu5650ybMzOyd+dvFIH9qBp9dAXijs3OoGJ+Gh2l8E36oq2oCtmPZlVw==");
        }
    }
}
