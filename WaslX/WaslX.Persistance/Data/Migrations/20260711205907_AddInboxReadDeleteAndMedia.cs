using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxReadDeleteAndMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaFileName",
                table: "messages",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaMimeType",
                table: "messages",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "messages",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEOY3ssyXaUar2EpfeJLfvdpg8JG/o2/gwoyyTt+mhje7zjWpEgH5Jgq5mzwFB50MQQ==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaFileName",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "MediaMimeType",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "messages");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEN8LDUKKAWZXcYwrpOYlrpGatyPk50D2egGAGo5LEl3Te1VKIebaW/qDJKbAdkAfrQ==");
        }
    }
}
