using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationWindowAndTemplateReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_customer_message_at",
                table: "conversations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "service_window_expires_at",
                table: "conversations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "template_reviews",
                columns: table => new
                {
                    template_review_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    meta_template_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    message_template_name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    language = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    reason_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    reason_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    meta_notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    submitted_category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    allow_category_change = table.Column<bool>(type: "bit", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_reviews", x => x.template_review_id);
                    table.ForeignKey(
                        name: "FK_template_reviews_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "tenant_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMRa6mgIoBxJi+5hNBblDSwU/8a9Kc8xiRU8bdA4F1lOtVauWziqTH7YoVHEFsZl+g==");

            migrationBuilder.CreateIndex(
                name: "IX_template_reviews_TenantId_meta_template_id",
                table: "template_reviews",
                columns: new[] { "TenantId", "meta_template_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "template_reviews");

            migrationBuilder.DropColumn(
                name: "last_customer_message_at",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "service_window_expires_at",
                table: "conversations");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEOY3ssyXaUar2EpfeJLfvdpg8JG/o2/gwoyyTt+mhje7zjWpEgH5Jgq5mzwFB50MQQ==");
        }
    }
}
