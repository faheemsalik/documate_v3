using System;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Documate.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DocumateDbContext))]
    [Migration("20260920040000_Band19ExtractPrompts")]
    public partial class Band19ExtractPrompts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string systemPrompt =
                "You extract structured data from documents. Return ONLY a JSON object matching the schema. No markdown.";

            migrationBuilder.AddColumn<string>(
                name: "DefaultPostProcessPrompt",
                table: "CorAgentTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SystemPrompt",
                table: "CorAgentTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: systemPrompt);

            migrationBuilder.AddColumn<string>(
                name: "PostProcessPrompt",
                table: "OpsAgents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SystemPrompt",
                table: "OpsAgents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: systemPrompt);

            migrationBuilder.CreateTable(
                name: "OpsDocumentExtractPrompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemPromptText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserPromptText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SequenceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpsDocumentExtractPrompts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsDocumentExtractPrompts_OpsDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "OpsDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsDocumentExtractPrompts_OpsFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "OpsFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocumentExtractPrompts_BusinessId",
                table: "OpsDocumentExtractPrompts",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocumentExtractPrompts_CreatedAt",
                table: "OpsDocumentExtractPrompts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocumentExtractPrompts_DocumentId",
                table: "OpsDocumentExtractPrompts",
                column: "DocumentId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocumentExtractPrompts_FileId",
                table: "OpsDocumentExtractPrompts",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocumentExtractPrompts_SequenceId",
                table: "OpsDocumentExtractPrompts",
                column: "SequenceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpsDocumentExtractPrompts");

            migrationBuilder.DropColumn(
                name: "DefaultPostProcessPrompt",
                table: "CorAgentTemplates");

            migrationBuilder.DropColumn(
                name: "SystemPrompt",
                table: "CorAgentTemplates");

            migrationBuilder.DropColumn(
                name: "PostProcessPrompt",
                table: "OpsAgents");

            migrationBuilder.DropColumn(
                name: "SystemPrompt",
                table: "OpsAgents");
        }
    }
}
