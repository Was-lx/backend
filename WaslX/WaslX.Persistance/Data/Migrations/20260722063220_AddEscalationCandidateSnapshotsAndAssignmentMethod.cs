using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalationCandidateSnapshotsAndAssignmentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "Escalations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "SuggestedScore",
                table: "Escalations",
                type: "decimal(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecommendationGeneratedAtUtc",
                table: "Escalations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EscalationCandidateSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EscalationId = table.Column<int>(type: "int", nullable: false),
                    AgentId = table.Column<int>(type: "int", nullable: false),
                    AgentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    PerformanceScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    ResponseSpeedScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    WorkloadScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    ActiveChats = table.Column<int>(type: "int", nullable: false),
                    RankingOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationCandidateSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalationCandidateSnapshots_Escalations_EscalationId",
                        column: x => x.EscalationId,
                        principalTable: "Escalations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EscalationCandidateSnapshots_EscalationId",
                table: "EscalationCandidateSnapshots",
                column: "EscalationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EscalationCandidateSnapshots");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "SuggestedScore",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "RecommendationGeneratedAtUtc",
                table: "Escalations");
        }
    }
}
