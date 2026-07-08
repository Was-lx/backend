using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppWebhookLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "whatsapp_accounts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiresAt",
                table: "whatsapp_accounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "whatsapp_webhook_logs",
                columns: table => new
                {
                    wa_webhook_log_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PhoneNumberId = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessingError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_webhook_logs", x => x.wa_webhook_log_id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKaD1c3EcvCjrvY5Jk+qTVC56NcN8xJ+z8qAf2JhFYS9s7a7ReH8cOZ63yTsbp25Kw==");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_webhook_logs_PhoneNumberId",
                table: "whatsapp_webhook_logs",
                column: "PhoneNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_webhook_logs_Processed",
                table: "whatsapp_webhook_logs",
                column: "Processed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "whatsapp_webhook_logs");

            migrationBuilder.DropColumn(
                name: "TokenExpiresAt",
                table: "whatsapp_accounts");

            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "whatsapp_accounts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMKBy2OROd4fzMqlOH27aii26eDbC8Qr1qPnxdnqLwVTcAJpPk4a2ZxdcMJwX7b9NQ==");
        }
    }
}
