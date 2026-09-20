using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Documate.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DocumateDbContext))]
    [Migration("20260920163000_Band20AdditionalDocumentInstructions")]
    public partial class Band20AdditionalDocumentInstructions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultAdditionalDocumentInstructions",
                table: "CorAgentTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalDocumentInstructions",
                table: "OpsAgents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultAdditionalDocumentInstructions",
                table: "CorAgentTemplates");

            migrationBuilder.DropColumn(
                name: "AdditionalDocumentInstructions",
                table: "OpsAgents");
        }
    }
}
