using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class Lasttest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_agent_performances_UserId",
                table: "agent_performances");

            migrationBuilder.AddColumn<int>(
                name: "ResolvedChats",
                table: "agent_performances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "agent_performances",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEOC01q3jTAsCFs1e8zyIlqQloM4Xa7/r2OzLsgpQKPIdeYJjqe/CAQbM0tCkyPN0Zw==");

            migrationBuilder.CreateIndex(
                name: "IX_agent_performances_UserId",
                table: "agent_performances",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_agent_performances_UserId",
                table: "agent_performances");

            migrationBuilder.DropColumn(
                name: "ResolvedChats",
                table: "agent_performances");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "agent_performances");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDTuFTA1lRxRgJi6hB+jVj0TaAIxsCnEz4cDCer1AkdslCM03V22jfRnk9XWuK8HlA==");

            migrationBuilder.CreateIndex(
                name: "IX_agent_performances_UserId",
                table: "agent_performances",
                column: "UserId");
        }
    }
}
