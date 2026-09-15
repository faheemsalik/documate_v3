namespace Documate.Api.Modules.PlatformAdmin.Features.Monitoring;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/monitoring")]
public sealed class AdminMonitoringController(
    HealthCheckService healthChecks,
    IOptionsMonitor<AdminOptions> adminOptions) : ControllerBase
{
    [HttpGet("snapshot")]
    public async Task<ActionResult<AdminMonitoringSnapshotDto>> Snapshot(CancellationToken cancellationToken)
    {
        var report = await healthChecks.CheckHealthAsync(cancellationToken);
        var checks = report.Entries
            .Select(e => new AdminHealthCheckDto(
                e.Key,
                e.Value.Status.ToString(),
                e.Value.Description))
            .OrderBy(c => c.Name)
            .ToList();

        var opts = adminOptions.CurrentValue;
        return Ok(new AdminMonitoringSnapshotDto(
            report.Status.ToString(),
            checks,
            opts.HangfireDashboardUrl,
            opts.DatadogDashboardUrl));
    }
}

public sealed record AdminHealthCheckDto(string Name, string Status, string? Description);

public sealed record AdminMonitoringSnapshotDto(
    string HealthStatus,
    IReadOnlyList<AdminHealthCheckDto> Checks,
    string? HangfireDashboardUrl,
    string? DatadogDashboardUrl);
