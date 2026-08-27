using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Documate.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OpsQueueWebhookSecretProtected : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WebhookSecretProtected",
                table: "OpsQueues",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WebhookSecretProtected",
                table: "OpsQueues");
        }
    }
}
