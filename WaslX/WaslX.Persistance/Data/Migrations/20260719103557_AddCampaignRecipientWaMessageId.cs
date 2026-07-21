using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignRecipientWaMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WhatsAppMessageId",
                table: "campaign_recipients",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGfvI8HntycYU9K4v4evmzK66i2JSUrYL4Ybzul2B3kDV21C5i+hsIoFEw7fram5Sg==");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_recipients_WhatsAppMessageId",
                table: "campaign_recipients",
                column: "WhatsAppMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_campaign_recipients_WhatsAppMessageId",
                table: "campaign_recipients");

            migrationBuilder.DropColumn(
                name: "WhatsAppMessageId",
                table: "campaign_recipients");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHjP6HFSF4uhUDPnITLID/mjxR9PnH+y1206YvZKM8BN9ZGMSoGyn4thcp4YGIiFEg==");
        }
    }
}
