namespace Documate.Api.Infrastructure.Options;

public sealed class EmailIntakeOptions
{
    public const string SectionName = "EmailIntake";

    /// <summary>Domain used when minting queue inbound addresses (e.g. intake.documate.local).</summary>
    public string DefaultDomain { get; set; } = "intake.documate.local";
}
