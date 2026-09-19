using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Documate.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Band18PublicEventsActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PublicActionsInherit",
                table: "OpsQueues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "OpsActionBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionTypeKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventKeysJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_OpsActionBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsActionBindings_OpsQueues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "OpsQueues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpsInAppNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EventId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_OpsInAppNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpsOutboundDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActionBindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionTypeKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EventId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResourceTypeKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusEnumId = table.Column<long>(type: "bigint", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastHttpStatus = table.Column<int>(type: "int", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_OpsOutboundDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsOutboundDeliveries_CorEnums_StatusEnumId",
                        column: x => x.StatusEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsOutboundDeliveries_OpsActionBindings_ActionBindingId",
                        column: x => x.ActionBindingId,
                        principalTable: "OpsActionBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpsActionBindings_BusinessId",
                table: "OpsActionBindings",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsActionBindings_BusinessId_QueueId_ActionTypeKey",
                table: "OpsActionBindings",
                columns: new[] { "BusinessId", "QueueId", "ActionTypeKey" });

            migrationBuilder.CreateIndex(
                name: "IX_OpsActionBindings_QueueId",
                table: "OpsActionBindings",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsActionBindings_SequenceId",
                table: "OpsActionBindings",
                column: "SequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpsInAppNotifications_BusinessId",
                table: "OpsInAppNotifications",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsInAppNotifications_BusinessId_CreatedAt",
                table: "OpsInAppNotifications",
                columns: new[] { "BusinessId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OpsInAppNotifications_BusinessId_EventId",
                table: "OpsInAppNotifications",
                columns: new[] { "BusinessId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpsInAppNotifications_SequenceId",
                table: "OpsInAppNotifications",
                column: "SequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpsOutboundDeliveries_ActionBindingId",
                table: "OpsOutboundDeliveries",
                column: "ActionBindingId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsOutboundDeliveries_BusinessId",
                table: "OpsOutboundDeliveries",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsOutboundDeliveries_BusinessId_EventId_ActionBindingId",
                table: "OpsOutboundDeliveries",
                columns: new[] { "BusinessId", "EventId", "ActionBindingId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpsOutboundDeliveries_BusinessId_ResourceTypeKey_ResourceId",
                table: "OpsOutboundDeliveries",
                columns: new[] { "BusinessId", "ResourceTypeKey", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpsOutboundDeliveries_SequenceId",
                table: "OpsOutboundDeliveries",
                column: "SequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpsOutboundDeliveries_StatusEnumId",
                table: "OpsOutboundDeliveries",
                column: "StatusEnumId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpsInAppNotifications");

            migrationBuilder.DropTable(
                name: "OpsOutboundDeliveries");

            migrationBuilder.DropTable(
                name: "OpsActionBindings");

            migrationBuilder.DropColumn(
                name: "PublicActionsInherit",
                table: "OpsQueues");
        }
    }
}
