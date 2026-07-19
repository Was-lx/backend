using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessageClassifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Sentiment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Escalate = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClassifierVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageClassifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageClassifications_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "conversations",
                        principalColumn: "conversation_id");
                    table.ForeignKey(
                        name: "FK_MessageClassifications_messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "messages",
                        principalColumn: "message_id");
                    table.ForeignKey(
                        name: "FK_MessageClassifications_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "tenant_id");
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEEd/2ScFj79pv1kOzHVKaTT9laardh4wJOjCKn9I3E9fBEnukNACx0/bctS+JBKH6Q==");

            migrationBuilder.CreateIndex(
                name: "IX_MessageClassifications_ConversationId",
                table: "MessageClassifications",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageClassifications_Escalate",
                table: "MessageClassifications",
                column: "Escalate");

            migrationBuilder.CreateIndex(
                name: "IX_MessageClassifications_MessageId",
                table: "MessageClassifications",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageClassifications_Priority",
                table: "MessageClassifications",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_MessageClassifications_Sentiment",
                table: "MessageClassifications",
                column: "Sentiment");

            migrationBuilder.CreateIndex(
                name: "IX_MessageClassifications_TenantId",
                table: "MessageClassifications",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageClassifications");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGL5dfFDcJBvnpT9ZUQUtfkj3cYcm5b93u5zcvvcvWAT6KKNUcNT+bwIuLrFYt9+tw==");
        }
    }
}
