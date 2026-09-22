using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Documate.Api.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(DocumateDbContext))]
[Migration("20260922170000_Band20IdenTenancySync")]
public partial class Band20IdenTenancySync : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SyncStatus",
            table: "CorTenants",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "ok");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LastSyncedAtUtc",
            table: "CorTenants",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SyncStatus",
            table: "CorTenantBusinesses",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "ok");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LastSyncedAtUtc",
            table: "CorTenantBusinesses",
            type: "datetimeoffset",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SyncStatus", table: "CorTenants");
        migrationBuilder.DropColumn(name: "LastSyncedAtUtc", table: "CorTenants");
        migrationBuilder.DropColumn(name: "SyncStatus", table: "CorTenantBusinesses");
        migrationBuilder.DropColumn(name: "LastSyncedAtUtc", table: "CorTenantBusinesses");
    }
}
