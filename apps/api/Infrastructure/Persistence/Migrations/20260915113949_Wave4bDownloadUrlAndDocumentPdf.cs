using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Documate.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Wave4bDownloadUrlAndDocumentPdf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DownloadUrl",
                table: "OpsFiles",
                type: "nvarchar(max)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DownloadUrlExpiresAt",
                table: "OpsFiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DownloadUrl",
                table: "OpsDocuments",
                type: "nvarchar(max)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DownloadUrlExpiresAt",
                table: "OpsDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfStorageBucket",
                table: "OpsDocuments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfStorageKey",
                table: "OpsDocuments",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadUrl",
                table: "OpsFiles");

            migrationBuilder.DropColumn(
                name: "DownloadUrlExpiresAt",
                table: "OpsFiles");

            migrationBuilder.DropColumn(
                name: "DownloadUrl",
                table: "OpsDocuments");

            migrationBuilder.DropColumn(
                name: "DownloadUrlExpiresAt",
                table: "OpsDocuments");

            migrationBuilder.DropColumn(
                name: "PdfStorageBucket",
                table: "OpsDocuments");

            migrationBuilder.DropColumn(
                name: "PdfStorageKey",
                table: "OpsDocuments");
        }
    }
}
