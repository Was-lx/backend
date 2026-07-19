using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoEscalationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAtUtc",
                table: "Escalations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedToId",
                table: "Escalations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAtUtc",
                table: "Escalations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedByUserId",
                table: "Escalations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CreatedBySystem",
                table: "Escalations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EscalationReason",
                table: "Escalations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MessageClassificationId",
                table: "Escalations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MessageId",
                table: "Escalations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeAtDecision",
                table: "Escalations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NotifiedAtUtc",
                table: "Escalations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideReason",
                table: "Escalations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Escalations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sentiment",
                table: "Escalations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EscalatedAtUtc",
                table: "conversations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EscalationReason",
                table: "conversations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEscalated",
                table: "conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TenantEscalationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantEscalationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantEscalationSettings_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "tenant_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantEscalationSettings_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDTuFTA1lRxRgJi6hB+jVj0TaAIxsCnEz4cDCer1AkdslCM03V22jfRnk9XWuK8HlA==");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_AssignedToId",
                table: "Escalations",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_ConfirmedByUserId",
                table: "Escalations",
                column: "ConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_MessageClassificationId",
                table: "Escalations",
                column: "MessageClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_MessageId",
                table: "Escalations",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantEscalationSettings_TenantId",
                table: "TenantEscalationSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantEscalationSettings_UpdatedByUserId",
                table: "TenantEscalationSettings",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Escalations_MessageClassifications_MessageClassificationId",
                table: "Escalations",
                column: "MessageClassificationId",
                principalTable: "MessageClassifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Escalations_messages_MessageId",
                table: "Escalations",
                column: "MessageId",
                principalTable: "messages",
                principalColumn: "message_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Escalations_users_AssignedToId",
                table: "Escalations",
                column: "AssignedToId",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Escalations_users_ConfirmedByUserId",
                table: "Escalations",
                column: "ConfirmedByUserId",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Escalations_MessageClassifications_MessageClassificationId",
                table: "Escalations");

            migrationBuilder.DropForeignKey(
                name: "FK_Escalations_messages_MessageId",
                table: "Escalations");

            migrationBuilder.DropForeignKey(
                name: "FK_Escalations_users_AssignedToId",
                table: "Escalations");

            migrationBuilder.DropForeignKey(
                name: "FK_Escalations_users_ConfirmedByUserId",
                table: "Escalations");

            migrationBuilder.DropTable(
                name: "TenantEscalationSettings");

            migrationBuilder.DropIndex(
                name: "IX_Escalations_AssignedToId",
                table: "Escalations");

            migrationBuilder.DropIndex(
                name: "IX_Escalations_ConfirmedByUserId",
                table: "Escalations");

            migrationBuilder.DropIndex(
                name: "IX_Escalations_MessageClassificationId",
                table: "Escalations");

            migrationBuilder.DropIndex(
                name: "IX_Escalations_MessageId",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "AssignedAtUtc",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "CreatedBySystem",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "EscalationReason",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "MessageClassificationId",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "ModeAtDecision",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "NotifiedAtUtc",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "OverrideReason",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "Sentiment",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "EscalatedAtUtc",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "EscalationReason",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "IsEscalated",
                table: "conversations");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEENBCESqDwDGMkHjv1b9z2532OdHOJGKNCfW5NEA8ndd9S9mAoh8SalvK91ftIpMhQ==");
        }
    }
}
