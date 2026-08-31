using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Documate.Api.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(DocumateDbContext))]
    [Migration("20260828010000_OpsQueueIsDefault")]
    public class OpsQueueIsDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "OpsQueues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Promote oldest non-deleted queue per Business to default when none marked yet.
            migrationBuilder.Sql(
                """
                ;WITH ranked AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY BusinessId ORDER BY SequenceId) AS rn
                    FROM OpsQueues
                    WHERE IsDeleted = 0
                      AND BusinessId NOT IN (
                          SELECT BusinessId FROM OpsQueues WHERE IsDefault = 1 AND IsDeleted = 0
                      )
                )
                UPDATE q
                SET IsDefault = 1
                FROM OpsQueues q
                INNER JOIN ranked r ON r.Id = q.Id AND r.rn = 1;
                """);

            // Create default queues for Businesses that have CorTenantBusiness but no Queue.
            // Provisioner also ensures this at runtime; SQL covers offline backfill.
            migrationBuilder.Sql(
                """
                DECLARE @openId bigint = (
                    SELECT TOP 1 e.Id
                    FROM CorEnums e
                    INNER JOIN CorEnumTypes t ON t.Id = e.TypeId
                    WHERE t.EnumTypeKey = N'allowlist_mode' AND e.EnumKey = N'open' AND e.IsDeleted = 0 AND t.IsDeleted = 0
                );
                DECLARE @inheritId bigint = (
                    SELECT TOP 1 e.Id
                    FROM CorEnums e
                    INNER JOIN CorEnumTypes t ON t.Id = e.TypeId
                    WHERE t.EnumTypeKey = N'workflow_mode' AND e.EnumKey = N'inherit_agent_default' AND e.IsDeleted = 0 AND t.IsDeleted = 0
                );

                IF @openId IS NOT NULL AND @inheritId IS NOT NULL
                BEGIN
                    INSERT INTO OpsQueues (
                        Id, BusinessId, Name, Description, IsDefault,
                        RoutingLocked, WebhookEnabled, EmailIntakeEnabled, EmailAddressVersion,
                        AllowlistModeEnumId, WorkflowModeEnumId, IsActive,
                        CreatedAt, UpdatedAt, IsDeleted
                    )
                    SELECT
                        NEWID(),
                        b.IdenBusinessId,
                        N'Default channel',
                        N'System default intake channel',
                        1,
                        0, 0, 0, 0,
                        @openId, @inheritId, 1,
                        SYSUTCDATETIME(), SYSUTCDATETIME(), 0
                    FROM CorTenantBusinesses b
                    WHERE b.IsDeleted = 0
                      AND NOT EXISTS (
                          SELECT 1 FROM OpsQueues q
                          WHERE q.BusinessId = b.IdenBusinessId AND q.IsDeleted = 0
                      );
                END
                """);

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueues_BusinessId_IsDefault",
                table: "OpsQueues",
                columns: new[] { "BusinessId", "IsDefault" },
                unique: true,
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OpsQueues_BusinessId_IsDefault",
                table: "OpsQueues");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "OpsQueues");
        }
    }
}
