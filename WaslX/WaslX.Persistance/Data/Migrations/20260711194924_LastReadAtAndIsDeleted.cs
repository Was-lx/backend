using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class LastReadAtAndIsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEN8LDUKKAWZXcYwrpOYlrpGatyPk50D2egGAGo5LEl3Te1VKIebaW/qDJKbAdkAfrQ==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "conversations");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEE1QUh2+7HIEu9hcU7he5bPrhw1/o+dWlGI/01lzfWvpfWCStyEpUFxOTY5kdUEFQg==");
        }
    }
}
