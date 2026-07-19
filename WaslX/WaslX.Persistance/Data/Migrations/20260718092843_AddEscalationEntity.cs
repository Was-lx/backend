using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Escalations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    AssignedUserId = table.Column<int>(type: "int", nullable: true),
                    SuggestedAssigneeId = table.Column<int>(type: "int", nullable: true),
                    SuggestedReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escalations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Escalations_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "conversations",
                        principalColumn: "conversation_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Escalations_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "tenant_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Escalations_users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Escalations_users_SuggestedAssigneeId",
                        column: x => x.SuggestedAssigneeId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEENBCESqDwDGMkHjv1b9z2532OdHOJGKNCfW5NEA8ndd9S9mAoh8SalvK91ftIpMhQ==");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_AssignedUserId",
                table: "Escalations",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_ConversationId",
                table: "Escalations",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_SuggestedAssigneeId",
                table: "Escalations",
                column: "SuggestedAssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_TenantId",
                table: "Escalations",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Escalations");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEEd/2ScFj79pv1kOzHVKaTT9laardh4wJOjCKn9I3E9fBEnukNACx0/bctS+JBKH6Q==");
        }
    }
}
