using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMetaFieldsToWhatsAppAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumberId",
                table: "whatsapp_accounts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "whatsAppBusinessAccountId",
                table: "whatsapp_accounts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMKBy2OROd4fzMqlOH27aii26eDbC8Qr1qPnxdnqLwVTcAJpPk4a2ZxdcMJwX7b9NQ==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumberId",
                table: "whatsapp_accounts");

            migrationBuilder.DropColumn(
                name: "whatsAppBusinessAccountId",
                table: "whatsapp_accounts");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEA6r/FP7XqhGDyDGpbH5v6AwD1dsPhxGGSfq+2geEyCIvNhhdzYc34QRS7q9NS/jXg==");
        }
    }
}
