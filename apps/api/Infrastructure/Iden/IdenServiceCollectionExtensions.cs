namespace Documate.Api.Infrastructure.Iden;

using Documate.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

public static class IdenServiceCollectionExtensions
{
    public const string IdenJwtScheme = "IdenJwt";

    public static IServiceCollection AddDocumateIden(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IdenOptions>(configuration.GetSection(IdenOptions.SectionName));
        services.AddHttpClient("iden");
        services.AddSingleton<IIdenClient, IdenClient>();
        services.AddScoped<IFeatureEnforce, FeatureEnforce>();
        services.AddScoped<TenancyReconcileJobs>();
        return services;
    }

    public static AuthenticationBuilder AddDocumateIdenJwt(
        this AuthenticationBuilder builder,
        IConfiguration configuration)
    {
        var authMode = configuration.GetSection(AuthOptions.SectionName)["Mode"] ?? "DevBypass";
        var iden = configuration.GetSection(IdenOptions.SectionName).Get<IdenOptions>() ?? new IdenOptions();

        var enableJwt = string.Equals(authMode, "Iden", StringComparison.OrdinalIgnoreCase)
            && (!string.IsNullOrWhiteSpace(iden.Jwt.Authority)
                || !string.IsNullOrWhiteSpace(iden.Jwt.MetadataAddress)
                || !string.IsNullOrWhiteSpace(iden.Jwt.ValidIssuer));

        // Always register scheme so policy forward target exists; validation may be permissive until Authority set.
        builder.AddJwtBearer(IdenJwtScheme, options =>
        {
            if (!enableJwt)
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    SignatureValidator = (token, _) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token),
                };
                return;
            }

            if (!string.IsNullOrWhiteSpace(iden.Jwt.MetadataAddress))
            {
                options.MetadataAddress = iden.Jwt.MetadataAddress;
            }
            else if (!string.IsNullOrWhiteSpace(iden.Jwt.Authority))
            {
                options.Authority = iden.Jwt.Authority;
            }

            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = !string.IsNullOrWhiteSpace(iden.Jwt.Audience),
                ValidAudience = string.IsNullOrWhiteSpace(iden.Jwt.Audience) ? null : iden.Jwt.Audience,
                ValidateIssuer = !string.IsNullOrWhiteSpace(iden.Jwt.ValidIssuer) || !string.IsNullOrWhiteSpace(iden.Jwt.Authority),
                ValidIssuer = string.IsNullOrWhiteSpace(iden.Jwt.ValidIssuer) ? iden.Jwt.Authority : iden.Jwt.ValidIssuer,
                NameClaimType = "sub",
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = ctx =>
                {
                    var identity = ctx.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                    if (identity is null)
                    {
                        return Task.CompletedTask;
                    }

                    MapClaim(identity, "sub", AuthClaimTypes.UserId);
                    MapClaim(identity, "tenant_id", AuthClaimTypes.TenantId);
                    MapClaim(identity, "tenant_business_id", AuthClaimTypes.BusinessId);
                    MapClaim(identity, "bu_context_id", AuthClaimTypes.BuContextId);
                    MapClaim(identity, "context_id", AuthClaimTypes.BuContextId);
                    MapClaim(identity, "identity_class", AuthClaimTypes.IdentityClass);
                    MapClaim(identity, "tenant_name", AuthClaimTypes.TenantName);
                    MapClaim(identity, "business_name", AuthClaimTypes.BusinessName);
                    return Task.CompletedTask;
                },
            };
        });

        return builder;
    }

    private static void MapClaim(System.Security.Claims.ClaimsIdentity identity, string sourceType, string targetType)
    {
        var value = identity.FindFirst(sourceType)?.Value;
        if (string.IsNullOrWhiteSpace(value) || identity.FindFirst(targetType) is not null)
        {
            return;
        }

        identity.AddClaim(new System.Security.Claims.Claim(targetType, value));
    }
}
