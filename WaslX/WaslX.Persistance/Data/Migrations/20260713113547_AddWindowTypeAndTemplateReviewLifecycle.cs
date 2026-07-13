using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWindowTypeAndTemplateReviewLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "template_reviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisableTimestamp",
                table: "template_reviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalCategory",
                table: "template_reviews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaStatusRaw",
                table: "template_reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PauseInfo",
                table: "template_reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WindowType",
                table: "conversations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TemplateReviewHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateReviewId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReasonText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FinalCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PauseInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaStatusRaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateReviewHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateReviewHistories_template_reviews_TemplateReviewId",
                        column: x => x.TemplateReviewId,
                        principalTable: "template_reviews",
                        principalColumn: "template_review_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateReviewHistories_tenants_TenantId",
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
                value: "AQAAAAIAAYagAAAAEGqOjkSy/GRu5650ybMzOyd+dvFIH9qBp9dAXijs3OoGJ+Gh2l8E36oq2oCtmPZlVw==");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateReviewHistories_TemplateReviewId",
                table: "TemplateReviewHistories",
                column: "TemplateReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateReviewHistories_TenantId",
                table: "TemplateReviewHistories",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateReviewHistories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "template_reviews");

            migrationBuilder.DropColumn(
                name: "DisableTimestamp",
                table: "template_reviews");

            migrationBuilder.DropColumn(
                name: "FinalCategory",
                table: "template_reviews");

            migrationBuilder.DropColumn(
                name: "MetaStatusRaw",
                table: "template_reviews");

            migrationBuilder.DropColumn(
                name: "PauseInfo",
                table: "template_reviews");

            migrationBuilder.DropColumn(
                name: "WindowType",
                table: "conversations");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEEluK9WwOu/F8Mwr1DskulBCVKpsaQGwO+3GzPL9nU6rZqm8RSXY/2v7fmzSmpF6OQ==");
        }
    }
}
