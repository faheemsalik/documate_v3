namespace Documate.Api.Infrastructure.Iden;

public sealed class IdenOptions
{
    public const string SectionName = "Iden";

    /// <summary>e.g. https://iden.manticapps.com/api/v1 or http://localhost:5277/api/v1</summary>
    public string BaseUrl { get; set; } = "";

    public string SoftwareKey { get; set; } = "documate";

    public IdenDocumateClientOptions Documate { get; set; } = new();

    public IdenJwtOptions Jwt { get; set; } = new();
}

public sealed class IdenDocumateClientOptions
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}

public sealed class IdenJwtOptions
{
    /// <summary>Authority / issuer for JWT validation (optional when Mode=DevBypass).</summary>
    public string Authority { get; set; } = "";

    public string Audience { get; set; } = "";

    /// <summary>JWKS URL; if empty and Authority set, derived as {Authority}/.well-known/openid-configuration path may vary — set explicitly.</summary>
    public string MetadataAddress { get; set; } = "";

    public string ValidIssuer { get; set; } = "";
}
