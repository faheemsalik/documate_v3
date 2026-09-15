using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Documate.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OpsIntakeMailbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntakeEmailSlug",
                table: "CorTenantBusinesses",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OpsIntakeMailboxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KindEnumId = table.Column<long>(type: "bigint", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailLocalPart = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmailDomain = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EmailAddressVersion = table.Column<int>(type: "int", nullable: false),
                    AllowlistModeEnumId = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_OpsIntakeMailboxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsIntakeMailboxes_CorEnums_AllowlistModeEnumId",
                        column: x => x.AllowlistModeEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsIntakeMailboxes_CorEnums_KindEnumId",
                        column: x => x.KindEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsIntakeMailboxes_OpsAgents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "OpsAgents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsIntakeMailboxes_OpsQueues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "OpsQueues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpsIntakeMailboxAllowlistEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MailboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchTypeEnumId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpsIntakeMailboxAllowlistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsIntakeMailboxAllowlistEntries_CorEnums_MatchTypeEnumId",
                        column: x => x.MatchTypeEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsIntakeMailboxAllowlistEntries_OpsIntakeMailboxes_MailboxId",
                        column: x => x.MailboxId,
                        principalTable: "OpsIntakeMailboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorTenantBusinesses_IntakeEmailSlug",
                table: "CorTenantBusinesses",
                column: "IntakeEmailSlug",
                unique: true,
                filter: "[IntakeEmailSlug] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeMailboxAllowlistEntries_BusinessId",
                table: "OpsIntakeMailboxAllowlistEntries",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeMailboxAllowlistEntries_MailboxId",
                table: "OpsIntakeMailboxAllowlistEntries",
                column: "MailboxId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeMailboxAllowlistEntries_MatchTypeEnumId",
                table: "OpsIntakeMailboxAllowlistEntries",
                column: "MatchTypeEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeMailboxes_AgentId_Typed",
                table: "OpsIntakeMailboxes",
                column: "AgentId",
                unique: true,
                filter: "[AgentId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeMailboxes_AllowlistModeEnumId",
                table: "OpsIntakeMailboxes",
                column: "AllowlistModeEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeMailboxes_BusinessId",
                table: "OpsIntakeMailboxes",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeMailboxes_Domain_LocalPart",
                table: "OpsIntakeMailboxes",
                columns: new[] { "EmailDomain", "EmailLocalPart" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeMailboxes_KindEnumId",
                table: "OpsIntakeMailboxes",
                column: "KindEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeMailboxes_QueueId",
                table: "OpsIntakeMailboxes",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeMailboxes_SequenceId",
                table: "OpsIntakeMailboxes",
                column: "SequenceId",
                unique: true);

            // Legacy Queue mint → multi_type mailbox when enum already seeded; otherwise skip (seeder runs after migrate).
            migrationBuilder.Sql(
                """
                DECLARE @multiKind bigint = (
                    SELECT TOP 1 e.Id FROM CorEnums e
                    INNER JOIN CorEnumTypes t ON t.Id = e.TypeId
                    WHERE t.EnumTypeKey = N'intake_mailbox_kind' AND e.EnumKey = N'multi_type'
                      AND e.IsDeleted = 0 AND t.IsDeleted = 0
                );
                IF @multiKind IS NOT NULL
                BEGIN
                    INSERT INTO OpsIntakeMailboxes (
                        Id, BusinessId, QueueId, KindEnumId, AgentId, Enabled,
                        EmailLocalPart, EmailDomain, EmailAddressVersion, AllowlistModeEnumId,
                        CreatedAt, UpdatedAt, IsDeleted
                    )
                    SELECT
                        NEWID(),
                        q.BusinessId,
                        q.Id,
                        @multiKind,
                        NULL,
                        q.EmailIntakeEnabled,
                        q.EmailLocalPart,
                        q.EmailDomain,
                        CASE WHEN q.EmailAddressVersion < 1 THEN 1 ELSE q.EmailAddressVersion END,
                        q.AllowlistModeEnumId,
                        SYSUTCDATETIME(), SYSUTCDATETIME(), 0
                    FROM OpsQueues q
                    WHERE q.IsDeleted = 0
                      AND q.EmailLocalPart IS NOT NULL AND LTRIM(RTRIM(q.EmailLocalPart)) <> N''
                      AND q.EmailDomain IS NOT NULL AND LTRIM(RTRIM(q.EmailDomain)) <> N''
                      AND NOT EXISTS (
                          SELECT 1 FROM OpsIntakeMailboxes m
                          WHERE m.EmailDomain = q.EmailDomain AND m.EmailLocalPart = q.EmailLocalPart AND m.IsDeleted = 0
                      );
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpsIntakeMailboxAllowlistEntries");

            migrationBuilder.DropTable(
                name: "OpsIntakeMailboxes");

            migrationBuilder.DropIndex(
                name: "IX_CorTenantBusinesses_IntakeEmailSlug",
                table: "CorTenantBusinesses");

            migrationBuilder.DropColumn(
                name: "IntakeEmailSlug",
                table: "CorTenantBusinesses");
        }
    }
}
