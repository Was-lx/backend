using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaslX.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeDocumentsAndChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_knowledge_vectors_TenantId",
                table: "knowledge_vectors");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "knowledge_vectors");

            migrationBuilder.AddColumn<int>(
                name: "ChunkIndex",
                table: "knowledge_vectors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "knowledge_vectors",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DocumentId",
                table: "knowledge_vectors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                table: "knowledge_vectors",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "QdrantPointId",
                table: "knowledge_vectors",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "knowledge_vectors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TokenCount",
                table: "knowledge_vectors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "knowledge_vectors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "knowledge_documents",
                columns: table => new
                {
                    document_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceRefId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MimeType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChunkCount = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_documents", x => x.document_id);
                    table.ForeignKey(
                        name: "FK_knowledge_documents_tenants_TenantId",
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
                value: "AQAAAAIAAYagAAAAEO1bbLWdMPBLJ2Wy5jcEXOuvfu26HCB3j5DP+JyYPLnLrImsKh0ltwGFhu5N/pNpbA==");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_vectors_DocumentId_ChunkIndex",
                table: "knowledge_vectors",
                columns: new[] { "DocumentId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_vectors_QdrantPointId",
                table: "knowledge_vectors",
                column: "QdrantPointId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_vectors_TenantId_DocumentId",
                table: "knowledge_vectors",
                columns: new[] { "TenantId", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_documents_TenantId_SourceType",
                table: "knowledge_documents",
                columns: new[] { "TenantId", "SourceType" });

            migrationBuilder.AddForeignKey(
                name: "FK_knowledge_vectors_knowledge_documents_DocumentId",
                table: "knowledge_vectors",
                column: "DocumentId",
                principalTable: "knowledge_documents",
                principalColumn: "document_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_knowledge_vectors_knowledge_documents_DocumentId",
                table: "knowledge_vectors");

            migrationBuilder.DropTable(
                name: "knowledge_documents");

            migrationBuilder.DropIndex(
                name: "IX_knowledge_vectors_DocumentId_ChunkIndex",
                table: "knowledge_vectors");

            migrationBuilder.DropIndex(
                name: "IX_knowledge_vectors_QdrantPointId",
                table: "knowledge_vectors");

            migrationBuilder.DropIndex(
                name: "IX_knowledge_vectors_TenantId_DocumentId",
                table: "knowledge_vectors");

            migrationBuilder.DropColumn(
                name: "ChunkIndex",
                table: "knowledge_vectors");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "knowledge_vectors");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "knowledge_vectors");

            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                table: "knowledge_vectors");

            migrationBuilder.DropColumn(
                name: "QdrantPointId",
                table: "knowledge_vectors");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "knowledge_vectors");

            migrationBuilder.DropColumn(
                name: "TokenCount",
                table: "knowledge_vectors");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "knowledge_vectors");

            migrationBuilder.AddColumn<string>(
                name: "Embedding",
                table: "knowledge_vectors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJrzSuMWghbdNn3ZH3ksTtlmvAe7qL7jAofHkmIocpOkrcr3Qq329hkfxLXhyFoNBQ==");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_vectors_TenantId",
                table: "knowledge_vectors",
                column: "TenantId");
        }
    }
}
