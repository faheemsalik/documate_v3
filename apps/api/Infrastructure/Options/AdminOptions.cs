namespace Documate.Api.Infrastructure.Options;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string HangfireDashboardUrl { get; set; } = "/hangfire";
    public string? DatadogDashboardUrl { get; set; }
}
