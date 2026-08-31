using System.Reflection;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Health;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Persistence.Seeding;
using Documate.Api.Infrastructure.Pipeline;
using Documate.Api.Infrastructure.Queues;
using Documate.Api.Infrastructure.Storage;
using Documate.Api.Infrastructure.Work;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Customer Angular app (ng serve) calls /api/app cross-origin until a same-origin proxy is used.
builder.Services.AddCors(options =>
{
    options.AddPolicy("CustomerWebDev", policy =>
        policy.WithOrigins(
                "http://localhost:4202",
                "https://localhost:4202",
                "http://127.0.0.1:4202",
                "https://127.0.0.1:4202",
                "http://localhost:4200",
                "https://localhost:4200",
                "http://127.0.0.1:4200",
                "https://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.Configure<ProviderCredentialsOptions>(builder.Configuration.GetSection(ProviderCredentialsOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<EmailIntakeOptions>(builder.Configuration.GetSection(EmailIntakeOptions.SectionName));
LlmStartupGate.EnsureConfigured(builder.Configuration, builder.Environment);
builder.Services.AddDocumateObjectStorage(builder.Configuration);
builder.Services.AddDocumatePipeline(builder.Configuration);
builder.Services.AddScoped<IWorkRecordService, WorkRecordService>();
builder.Services.AddScoped<ICancelWorkService, CancelWorkService>();
builder.Services.AddScoped<IReprocessWorkService, ReprocessWorkService>();

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
builder.Services.AddScoped<IDefaultQueueBootstrap, DefaultQueueBootstrap>();
builder.Services.AddScoped<IAgentQueueRouteAutoMapper, AgentQueueRouteAutoMapper>();
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

if (app.Environment.IsDevelopment())
{
    app.UseCors("CustomerWebDev");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantBusinessProvisioningMiddleware>();
app.UseDocumateHangfireDashboard();
app.MapControllers();
app.MapHealthChecks("/health");

// Liveness for IIS deploy tooling — anonymous, no dependency checks.
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));

app.Run();

public partial class Program;
