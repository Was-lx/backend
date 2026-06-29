using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthRolesAndTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => new { x.UserId, x.Id });
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "IsDefault", "IsDeleted", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "1d4f0b3a-7b6e-4c9a-9d21-0a1b2c3d4e5f", "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d", false, false, "SuperAdmin", "SUPERADMIN" },
                    { "2e5a1c4b-8c7f-4d0b-ae32-1b2c3d4e5f60", "b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e", false, false, "Admin", "ADMIN" },
                    { "3f6b2d5c-9d80-4e1c-bf43-2c3d4e5f6071", "c3d4e5f6-a7b8-4c9d-ae1f-2a3b4c5d6e7f", false, false, "Manager", "MANAGER" },
                    { "4a7c3e6d-ae91-4f2d-c054-3d4e5f607182", "d4e5f6a7-b8c9-4d0e-bf2a-3b4c5d6e7f80", true, false, "Agent", "AGENT" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "FullName", "IsDisabled", "IsForgetPasswordOtpConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TenantId", "TwoFactorEnabled", "UserName" },
                values: new object[] { "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b", 0, "e5f6a7b8-c9d0-4e1f-a2b3-4c5d6e7f8091", "superadmin@waslx.com", true, "Platform Owner", false, false, false, null, "SUPERADMIN@WASLX.COM", "SUPERADMIN@WASLX.COM", "AQAAAAIAAYagAAAAEB3XdvSm05juJA7XtdnPvc9q3qOFwxYwxE6TfivyoRo7eoenY+Ai+gN/4RTrNtDcsA==", null, false, "f6a7b8c9-d0e1-4f2a-b3c4-5d6e7f809102", null, false, "superadmin@waslx.com" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "1d4f0b3a-7b6e-4c9a-9d21-0a1b2c3d4e5f", "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2e5a1c4b-8c7f-4d0b-ae32-1b2c3d4e5f60");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3f6b2d5c-9d80-4e1c-bf43-2c3d4e5f6071");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4a7c3e6d-ae91-4f2d-c054-3d4e5f607182");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "1d4f0b3a-7b6e-4c9a-9d21-0a1b2c3d4e5f", "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b" });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1d4f0b3a-7b6e-4c9a-9d21-0a1b2c3d4e5f");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => new { x.ApplicationUserId, x.Id });
                    table.ForeignKey(
                        name: "FK_RefreshToken_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
