using System.Reflection;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Health;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Persistence.Seeding;
using Documate.Api.Infrastructure.Pipeline;
using Documate.Api.Infrastructure.Storage;
using Documate.Api.Infrastructure.Work;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.Configure<ProviderCredentialsOptions>(builder.Configuration.GetSection(ProviderCredentialsOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<EmailIntakeOptions>(builder.Configuration.GetSection(EmailIntakeOptions.SectionName));
builder.Services.AddDocumateObjectStorage(builder.Configuration);
builder.Services.AddDocumatePipeline(builder.Configuration);
builder.Services.AddScoped<IWorkRecordService, WorkRecordService>();

builder.Services.AddDbContext<DocumateDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Documate")
        ?? throw new InvalidOperationException("Connection string 'Documate' is missing.");
    options.UseSqlServer(cs);
});

builder.Services.AddSingleton<CorEnumIdResolver>();
builder.Services.AddSingleton<ICorEnumIdResolver>(sp => sp.GetRequiredService<CorEnumIdResolver>());
builder.Services.AddHostedService<CorEnumSeedHostedService>();

builder.Services.AddScoped<IBusinessContext, BusinessContextAccessor>();
builder.Services.AddScoped<ITenantBusinessProvisioner, TenantBusinessProvisioner>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = DocumateAuthDefaults.Scheme;
        options.DefaultChallengeScheme = DocumateAuthDefaults.Scheme;
    })
    .AddPolicyScheme(DocumateAuthDefaults.Scheme, "Documate auth router", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            if (context.Request.Headers.ContainsKey(ApiKeyService.KeyHeaderName)
                || context.Request.Headers.Authorization.ToString()
                    .StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.StartsWithSegments("/api/v1"))
            {
                return ApiKeyAuthDefaults.Scheme;
            }

            return DevBypassAuthDefaults.Scheme;
        };
    })
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthDefaults.Scheme,
        _ => { })
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevBypassAuthenticationHandler>(
        DevBypassAuthDefaults.Scheme,
        _ => { });

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantBusinessProvisioningMiddleware>();
app.UseDocumateHangfireDashboard();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
