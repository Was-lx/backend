using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiAgentSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiAgentNumberSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WhatsAppAccountId = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    AutoReplyEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AllowOutsideWindow = table.Column<bool>(type: "bit", nullable: false),
                    MaxConversationMessages = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAgentNumberSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiAgentNumberSettings_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AiAgentNumberSettings_whatsapp_accounts_WhatsAppAccountId",
                        column: x => x.WhatsAppAccountId,
                        principalTable: "whatsapp_accounts",
                        principalColumn: "wa_account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantAiAgentSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    PersonaName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToneInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    HandoffThreshold = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAiAgentSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantAiAgentSettings_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "tenant_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantAiAgentSettings_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEOSo+5gJ8U17JxdOEa3UC/CoY9h777eGOSwhyoXbogdR5BjNk3d3T5QMSU2HCRFh+g==");

            migrationBuilder.CreateIndex(
                name: "IX_AiAgentNumberSettings_UpdatedByUserId",
                table: "AiAgentNumberSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAgentNumberSettings_WhatsAppAccountId",
                table: "AiAgentNumberSettings",
                column: "WhatsAppAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantAiAgentSettings_TenantId",
                table: "TenantAiAgentSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantAiAgentSettings_UpdatedByUserId",
                table: "TenantAiAgentSettings",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAgentNumberSettings");

            migrationBuilder.DropTable(
                name: "TenantAiAgentSettings");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEO1bbLWdMPBLJ2Wy5jcEXOuvfu26HCB3j5DP+JyYPLnLrImsKh0ltwGFhu5N/pNpbA==");
        }
    }
}
