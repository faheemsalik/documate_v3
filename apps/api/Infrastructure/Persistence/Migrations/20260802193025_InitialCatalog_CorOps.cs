using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Documate.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog_CorOps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorDocumentTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentTypeKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_CorDocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorEnumTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnumTypeKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_CorEnumTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorTenantApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    KeyPrefix = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_CorTenantApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorWorkflowDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkflowKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_CorWorkflowDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorEnums",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EnumKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Narration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayStyle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_CorEnums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorEnums_CorEnumTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "CorEnumTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CorProviders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CategoryEnumId = table.Column<long>(type: "bigint", nullable: false),
                    VendorHint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsPlatformManaged = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_CorProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorProviders_CorEnums_CategoryEnumId",
                        column: x => x.CategoryEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CorTenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdenTenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProviderModeEnumId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_CorTenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorTenants_CorEnums_ProviderModeEnumId",
                        column: x => x.ProviderModeEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpsQueues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoutingLocked = table.Column<bool>(type: "bit", nullable: false),
                    RoutingLockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    WebhookUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    WebhookSecretHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    WebhookEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailIntakeEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailLocalPart = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EmailDomain = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailAddressVersion = table.Column<int>(type: "int", nullable: false),
                    AllowlistModeEnumId = table.Column<long>(type: "bigint", nullable: false),
                    WorkflowModeEnumId = table.Column<long>(type: "bigint", nullable: false),
                    WorkflowId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_OpsQueues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsQueues_CorEnums_AllowlistModeEnumId",
                        column: x => x.AllowlistModeEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsQueues_CorEnums_WorkflowModeEnumId",
                        column: x => x.WorkflowModeEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsQueues_CorWorkflowDefinitions_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "CorWorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CorAgentTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentTemplateKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    DefaultSchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultInstructions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultProviderId = table.Column<long>(type: "bigint", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CorAgentTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorAgentTemplates_CorDocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "CorDocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorAgentTemplates_CorProviders_DefaultProviderId",
                        column: x => x.DefaultProviderId,
                        principalTable: "CorProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpsWorkEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubjectTypeEnumId = table.Column<long>(type: "bigint", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventTypeEnumId = table.Column<long>(type: "bigint", nullable: false),
                    ProviderId = table.Column<long>(type: "bigint", nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_OpsWorkEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsWorkEvents_CorEnums_EventTypeEnumId",
                        column: x => x.EventTypeEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsWorkEvents_CorEnums_SubjectTypeEnumId",
                        column: x => x.SubjectTypeEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsWorkEvents_CorProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "CorProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CorTenantBusinesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdenBusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TenantName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_CorTenantBusinesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorTenantBusinesses_CorTenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "CorTenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpsBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEnumId = table.Column<long>(type: "bigint", nullable: false),
                    EmailMessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    FileCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_OpsBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsBatches_CorEnums_SourceEnumId",
                        column: x => x.SourceEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsBatches_OpsQueues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "OpsQueues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpsIntakeRejections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEnumId = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EmailMessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EmailFrom = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EmailSubject = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
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
                    table.PrimaryKey("PK_OpsIntakeRejections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsIntakeRejections_CorEnums_SourceEnumId",
                        column: x => x.SourceEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsIntakeRejections_OpsQueues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "OpsQueues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpsQueueEmailAllowlistEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_OpsQueueEmailAllowlistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsQueueEmailAllowlistEntries_CorEnums_MatchTypeEnumId",
                        column: x => x.MatchTypeEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsQueueEmailAllowlistEntries_OpsQueues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "OpsQueues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpsAgents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    OutputSchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceTemplateId = table.Column<long>(type: "bigint", nullable: true),
                    DefaultWorkflowId = table.Column<long>(type: "bigint", nullable: true),
                    DefaultProviderId = table.Column<long>(type: "bigint", nullable: true),
                    ProviderStrategyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_OpsAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsAgents_CorAgentTemplates_SourceTemplateId",
                        column: x => x.SourceTemplateId,
                        principalTable: "CorAgentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsAgents_CorDocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "CorDocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsAgents_CorProviders_DefaultProviderId",
                        column: x => x.DefaultProviderId,
                        principalTable: "CorProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsAgents_CorWorkflowDefinitions_DefaultWorkflowId",
                        column: x => x.DefaultWorkflowId,
                        principalTable: "CorWorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpsFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceEnumId = table.Column<long>(type: "bigint", nullable: false),
                    PublicStatusEnumId = table.Column<long>(type: "bigint", nullable: false),
                    InternalStageEnumId = table.Column<long>(type: "bigint", nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    StorageBucket = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EmailMessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EmailFrom = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EmailSubject = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ReprocessOfFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_OpsFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsFiles_CorEnums_InternalStageEnumId",
                        column: x => x.InternalStageEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsFiles_CorEnums_PublicStatusEnumId",
                        column: x => x.PublicStatusEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsFiles_CorEnums_SourceEnumId",
                        column: x => x.SourceEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsFiles_OpsBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "OpsBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsFiles_OpsFiles_ReprocessOfFileId",
                        column: x => x.ReprocessOfFileId,
                        principalTable: "OpsFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsFiles_OpsQueues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "OpsQueues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpsQueueRoutes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_OpsQueueRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsQueueRoutes_CorDocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "CorDocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsQueueRoutes_OpsAgents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "OpsAgents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsQueueRoutes_OpsQueues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "OpsQueues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpsDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentTypeId = table.Column<long>(type: "bigint", nullable: true),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderId = table.Column<long>(type: "bigint", nullable: true),
                    SchemaVersion = table.Column<int>(type: "int", nullable: true),
                    PublicStatusEnumId = table.Column<long>(type: "bigint", nullable: false),
                    InternalStageEnumId = table.Column<long>(type: "bigint", nullable: true),
                    PageStart = table.Column<int>(type: "int", nullable: true),
                    PageEnd = table.Column<int>(type: "int", nullable: true),
                    SliceRefJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FailedStage = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    WebhookStatusEnumId = table.Column<long>(type: "bigint", nullable: true),
                    WebhookAttempts = table.Column<int>(type: "int", nullable: false),
                    WebhookLastAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    WebhookLastHttpStatus = table.Column<int>(type: "int", nullable: true),
                    WebhookLastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_OpsDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsDocuments_CorDocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "CorDocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsDocuments_CorEnums_InternalStageEnumId",
                        column: x => x.InternalStageEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsDocuments_CorEnums_PublicStatusEnumId",
                        column: x => x.PublicStatusEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsDocuments_CorEnums_WebhookStatusEnumId",
                        column: x => x.WebhookStatusEnumId,
                        principalTable: "CorEnums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsDocuments_CorProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "CorProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsDocuments_OpsAgents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "OpsAgents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsDocuments_OpsBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "OpsBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsDocuments_OpsFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "OpsFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpsDocuments_OpsQueues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "OpsQueues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorAgentTemplates_AgentTemplateKey",
                table: "CorAgentTemplates",
                column: "AgentTemplateKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorAgentTemplates_DefaultProviderId",
                table: "CorAgentTemplates",
                column: "DefaultProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_CorAgentTemplates_DocumentTypeId",
                table: "CorAgentTemplates",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CorDocumentTypes_DocumentTypeKey",
                table: "CorDocumentTypes",
                column: "DocumentTypeKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorEnums_TypeId_EnumKey_BusinessId",
                table: "CorEnums",
                columns: new[] { "TypeId", "EnumKey", "BusinessId" },
                unique: true,
                filter: "[BusinessId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CorEnumTypes_EnumTypeKey",
                table: "CorEnumTypes",
                column: "EnumTypeKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorProviders_CategoryEnumId",
                table: "CorProviders",
                column: "CategoryEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_CorProviders_ProviderKey",
                table: "CorProviders",
                column: "ProviderKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorTenantApiKeys_BusinessId",
                table: "CorTenantApiKeys",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_CorTenantApiKeys_KeyPrefix",
                table: "CorTenantApiKeys",
                column: "KeyPrefix");

            migrationBuilder.CreateIndex(
                name: "IX_CorTenantApiKeys_SequenceId",
                table: "CorTenantApiKeys",
                column: "SequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorTenantBusinesses_IdenBusinessId",
                table: "CorTenantBusinesses",
                column: "IdenBusinessId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorTenantBusinesses_SequenceId",
                table: "CorTenantBusinesses",
                column: "SequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorTenantBusinesses_TenantId",
                table: "CorTenantBusinesses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CorTenants_IdenTenantId",
                table: "CorTenants",
                column: "IdenTenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorTenants_ProviderModeEnumId",
                table: "CorTenants",
                column: "ProviderModeEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_CorTenants_SequenceId",
                table: "CorTenants",
                column: "SequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorWorkflowDefinitions_BusinessId",
                table: "CorWorkflowDefinitions",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_CorWorkflowDefinitions_BusinessId_WorkflowKey",
                table: "CorWorkflowDefinitions",
                columns: new[] { "BusinessId", "WorkflowKey" });

            migrationBuilder.CreateIndex(
                name: "IX_OpsAgents_BusinessId",
                table: "OpsAgents",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsAgents_DefaultProviderId",
                table: "OpsAgents",
                column: "DefaultProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsAgents_DefaultWorkflowId",
                table: "OpsAgents",
                column: "DefaultWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsAgents_DocumentTypeId",
                table: "OpsAgents",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsAgents_SequenceId",
                table: "OpsAgents",
                column: "SequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpsAgents_SourceTemplateId",
                table: "OpsAgents",
                column: "SourceTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsBatches_BusinessId",
                table: "OpsBatches",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsBatches_QueueId",
                table: "OpsBatches",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsBatches_SequenceId",
                table: "OpsBatches",
                column: "SequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpsBatches_SourceEnumId",
                table: "OpsBatches",
                column: "SourceEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocuments_AgentId",
                table: "OpsDocuments",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocuments_BatchId",
                table: "OpsDocuments",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocuments_BusinessId",
                table: "OpsDocuments",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocuments_DocumentTypeId",
                table: "OpsDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocuments_FileId",
                table: "OpsDocuments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocuments_InternalStageEnumId",
                table: "OpsDocuments",
                column: "InternalStageEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocuments_ProviderId",
                table: "OpsDocuments",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocuments_PublicStatusEnumId",
                table: "OpsDocuments",
                column: "PublicStatusEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocuments_QueueId",
                table: "OpsDocuments",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocuments_SequenceId",
                table: "OpsDocuments",
                column: "SequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpsDocuments_WebhookStatusEnumId",
                table: "OpsDocuments",
                column: "WebhookStatusEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsFiles_BatchId",
                table: "OpsFiles",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsFiles_BusinessId",
                table: "OpsFiles",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsFiles_InternalStageEnumId",
                table: "OpsFiles",
                column: "InternalStageEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsFiles_PublicStatusEnumId",
                table: "OpsFiles",
                column: "PublicStatusEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsFiles_QueueId",
                table: "OpsFiles",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsFiles_ReprocessOfFileId",
                table: "OpsFiles",
                column: "ReprocessOfFileId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsFiles_SequenceId",
                table: "OpsFiles",
                column: "SequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpsFiles_SourceEnumId",
                table: "OpsFiles",
                column: "SourceEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeRejections_BusinessId",
                table: "OpsIntakeRejections",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeRejections_QueueId",
                table: "OpsIntakeRejections",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeRejections_SequenceId",
                table: "OpsIntakeRejections",
                column: "SequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpsIntakeRejections_SourceEnumId",
                table: "OpsIntakeRejections",
                column: "SourceEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueueEmailAllowlistEntries_BusinessId",
                table: "OpsQueueEmailAllowlistEntries",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueueEmailAllowlistEntries_MatchTypeEnumId",
                table: "OpsQueueEmailAllowlistEntries",
                column: "MatchTypeEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueueEmailAllowlistEntries_QueueId",
                table: "OpsQueueEmailAllowlistEntries",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueueRoutes_AgentId",
                table: "OpsQueueRoutes",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueueRoutes_BusinessId",
                table: "OpsQueueRoutes",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueueRoutes_DocumentTypeId",
                table: "OpsQueueRoutes",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueueRoutes_QueueId_DocumentTypeId",
                table: "OpsQueueRoutes",
                columns: new[] { "QueueId", "DocumentTypeId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueues_AllowlistModeEnumId",
                table: "OpsQueues",
                column: "AllowlistModeEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueues_BusinessId",
                table: "OpsQueues",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueues_SequenceId",
                table: "OpsQueues",
                column: "SequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueues_WorkflowId",
                table: "OpsQueues",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsQueues_WorkflowModeEnumId",
                table: "OpsQueues",
                column: "WorkflowModeEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsWorkEvents_BusinessId",
                table: "OpsWorkEvents",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsWorkEvents_BusinessId_SubjectTypeEnumId_SubjectId",
                table: "OpsWorkEvents",
                columns: new[] { "BusinessId", "SubjectTypeEnumId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpsWorkEvents_EventTypeEnumId",
                table: "OpsWorkEvents",
                column: "EventTypeEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsWorkEvents_ProviderId",
                table: "OpsWorkEvents",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_OpsWorkEvents_SubjectTypeEnumId",
                table: "OpsWorkEvents",
                column: "SubjectTypeEnumId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorTenantApiKeys");

            migrationBuilder.DropTable(
                name: "CorTenantBusinesses");

            migrationBuilder.DropTable(
                name: "OpsDocuments");

            migrationBuilder.DropTable(
                name: "OpsIntakeRejections");

            migrationBuilder.DropTable(
                name: "OpsQueueEmailAllowlistEntries");

            migrationBuilder.DropTable(
                name: "OpsQueueRoutes");

            migrationBuilder.DropTable(
                name: "OpsWorkEvents");

            migrationBuilder.DropTable(
                name: "CorTenants");

            migrationBuilder.DropTable(
                name: "OpsFiles");

            migrationBuilder.DropTable(
                name: "OpsAgents");

            migrationBuilder.DropTable(
                name: "OpsBatches");

            migrationBuilder.DropTable(
                name: "CorAgentTemplates");

            migrationBuilder.DropTable(
                name: "OpsQueues");

            migrationBuilder.DropTable(
                name: "CorDocumentTypes");

            migrationBuilder.DropTable(
                name: "CorProviders");

            migrationBuilder.DropTable(
                name: "CorWorkflowDefinitions");

            migrationBuilder.DropTable(
                name: "CorEnums");

            migrationBuilder.DropTable(
                name: "CorEnumTypes");
        }
    }
}
