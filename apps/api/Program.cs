using System.Reflection;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Configuration;
using Documate.Api.Infrastructure.EmailIntake;
using Documate.Api.Infrastructure.Health;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Persistence.Seeding;
using Documate.Api.Infrastructure.Pipeline;
using Documate.Api.Infrastructure.Queues;
using Documate.Api.Infrastructure.Storage;
using Documate.Api.Infrastructure.Work;
using Documate.Api.Modules.External.Features.EmailIntake;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Plan 17: optional AWS Secrets Manager JSON blob → IConfiguration (once per process).
// Local default: disabled (user-secrets/env). Staging/prod: Enabled=true + SecretId.
using (var startupLoggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information)))
{
    var secretsLogger = startupLoggerFactory.CreateLogger("Documate.SecretsManager");
    builder.Configuration.AddDocumateSecretsManager(builder.Environment, secretsLogger);
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Customer + admin Angular apps call API cross-origin until a same-origin proxy is used.
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
                "https://127.0.0.1:4200",
                "http://localhost:4203",
                "https://localhost:4203",
                "http://127.0.0.1:4203",
                "https://127.0.0.1:4203",
                "http://app.documate.ai",
                "https://app.documate.ai")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.Configure<ProviderCredentialsOptions>(builder.Configuration.GetSection(ProviderCredentialsOptions.SectionName));
builder.Services.Configure<AwsOptions>(builder.Configuration.GetSection(AwsOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<EmailIntakeOptions>(builder.Configuration.GetSection(EmailIntakeOptions.SectionName));
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.AddSingleton<Documate.Api.Infrastructure.Settings.SystemSettingsOptionsChangeSource>();
builder.Services.AddSingleton<Microsoft.Extensions.Options.IOptionsChangeTokenSource<Documate.Api.Infrastructure.Options.StorageOptions>>(
    sp => sp.GetRequiredService<Documate.Api.Infrastructure.Settings.SystemSettingsOptionsChangeSource>());
builder.Services.AddSingleton<Microsoft.Extensions.Options.IOptionsChangeTokenSource<Documate.Api.Infrastructure.Options.OcrOptions>>(
    sp => sp.GetRequiredService<Documate.Api.Infrastructure.Settings.SystemSettingsOptionsChangeSource>());
builder.Services.AddSingleton<Microsoft.Extensions.Options.IOptionsChangeTokenSource<Documate.Api.Infrastructure.Options.PipelineOptions>>(
    sp => sp.GetRequiredService<Documate.Api.Infrastructure.Settings.SystemSettingsOptionsChangeSource>());
builder.Services.AddSingleton<Microsoft.Extensions.Options.IOptionsChangeTokenSource<Documate.Api.Infrastructure.Options.NotificationOptions>>(
    sp => sp.GetRequiredService<Documate.Api.Infrastructure.Settings.SystemSettingsOptionsChangeSource>());
builder.Services.AddSingleton<Microsoft.Extensions.Options.IOptionsChangeTokenSource<Documate.Api.Infrastructure.Options.AdminOptions>>(
    sp => sp.GetRequiredService<Documate.Api.Infrastructure.Settings.SystemSettingsOptionsChangeSource>());
builder.Services.AddSingleton<Documate.Api.Infrastructure.Settings.ISystemSettings, Documate.Api.Infrastructure.Settings.MemoryCachedSystemSettings>();
builder.Services.AddSingleton<Documate.Api.Infrastructure.Settings.IEmailIntakeSettings, Documate.Api.Infrastructure.Settings.EmailIntakeSettings>();
builder.Services.AddSingleton<Documate.Api.Infrastructure.Settings.IPipelineSyncSettings, Documate.Api.Infrastructure.Settings.PipelineSyncSettings>();
builder.Services.AddSingleton<Documate.Api.Infrastructure.Settings.IPipelineModelSettings, Documate.Api.Infrastructure.Settings.PipelineModelSettings>();
builder.Services.AddSingleton<Documate.Api.Infrastructure.Settings.DbBackedOptionsPostConfigure>();
builder.Services.AddSingleton<Microsoft.Extensions.Options.IPostConfigureOptions<Documate.Api.Infrastructure.Options.StorageOptions>>(
    sp => sp.GetRequiredService<Documate.Api.Infrastructure.Settings.DbBackedOptionsPostConfigure>());
builder.Services.AddSingleton<Microsoft.Extensions.Options.IPostConfigureOptions<Documate.Api.Infrastructure.Options.OcrOptions>>(
    sp => sp.GetRequiredService<Documate.Api.Infrastructure.Settings.DbBackedOptionsPostConfigure>());
builder.Services.AddSingleton<Microsoft.Extensions.Options.IPostConfigureOptions<Documate.Api.Infrastructure.Options.PipelineOptions>>(
    sp => sp.GetRequiredService<Documate.Api.Infrastructure.Settings.DbBackedOptionsPostConfigure>());
builder.Services.AddSingleton<Microsoft.Extensions.Options.IPostConfigureOptions<Documate.Api.Infrastructure.Options.NotificationOptions>>(
    sp => sp.GetRequiredService<Documate.Api.Infrastructure.Settings.DbBackedOptionsPostConfigure>());
builder.Services.AddSingleton<Microsoft.Extensions.Options.IPostConfigureOptions<Documate.Api.Infrastructure.Options.AdminOptions>>(
    sp => sp.GetRequiredService<Documate.Api.Infrastructure.Settings.DbBackedOptionsPostConfigure>());
LlmStartupGate.EnsureConfigured(builder.Configuration, builder.Environment);
builder.Services.AddDocumateObjectStorage(builder.Configuration);
builder.Services.AddDocumatePipeline(builder.Configuration);
builder.Services.AddScoped<IWorkRecordService, WorkRecordService>();
builder.Services.AddSingleton<IUploadIntakeMetrics, UploadIntakeMetrics>();
builder.Services.AddScoped<ICancelWorkService, CancelWorkService>();
builder.Services.AddScoped<IReprocessWorkService, ReprocessWorkService>();
builder.Services.AddScoped<IResetWorkService, ResetWorkService>();

builder.Services.AddDbContext<DocumateDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Documate")
        ?? throw new InvalidOperationException("Connection string 'Documate' is missing.");
    options.UseSqlServer(cs);
});

builder.Services.AddSingleton<CorEnumIdResolver>();
builder.Services.AddSingleton<ICorEnumIdResolver>(sp => sp.GetRequiredService<CorEnumIdResolver>());
builder.Services.AddHostedService<CorEnumSeedHostedService>();
builder.Services.AddHostedService<Documate.Api.Infrastructure.PublicEvents.ActionBindingMigrateHostedService>();

builder.Services.AddScoped<BusinessContextAccessor>();
builder.Services.AddScoped<IBusinessContext>(sp => sp.GetRequiredService<BusinessContextAccessor>());
builder.Services.AddScoped<IBusinessContextSetter>(sp => sp.GetRequiredService<BusinessContextAccessor>());
builder.Services.AddSingleton<TenantBusinessEnsureCache>();
builder.Services.AddScoped<ITenantBusinessProvisioner, TenantBusinessProvisioner>();
builder.Services.AddScoped<IDefaultQueueBootstrap, DefaultQueueBootstrap>();
builder.Services.AddScoped<IAgentQueueRouteAutoMapper, AgentQueueRouteAutoMapper>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IEmailIntakeDecisionAgent, HeuristicEmailIntakeDecisionAgent>();
builder.Services.AddScoped<IEmailIntakeProcessor, EmailIntakeProcessor>();
builder.Services.AddSingleton<IEmailIntakeRateLimiter, MemoryEmailIntakeRateLimiter>();
builder.Services.AddSingleton<IEmailIntakeMetrics, EmailIntakeMetrics>();
builder.Services.AddSingleton<IInboundMimeParser, MimeKitInboundMimeParser>();
builder.Services.AddScoped<ISesInboundEmailHandler, SesInboundEmailHandler>();
builder.Services.AddScoped<EmailIntakeJobs>();
builder.Services.AddSingleton<IEmailIntakeMimeRetention, EmailIntakeMimeRetentionService>();
builder.Services.AddHttpClient("documate-sns-confirm", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

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
            if (context.Request.Path.StartsWithSegments("/api/admin"))
            {
                return AdminGateAuthDefaults.Scheme;
            }

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
        _ => { })
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, AdminGateAuthenticationHandler>(
        AdminGateAuthDefaults.Scheme,
        _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PlatformAdminAuth.PolicyName, policy =>
        policy
            .AddAuthenticationSchemes(AdminGateAuthDefaults.Scheme)
            .RequireAuthenticatedUser()
            .RequireClaim(AuthClaimTypes.PlatformAdmin, "true"));
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

// Must be first so API clients never see EF / framework stacks (incl. Development).
app.UseMiddleware<Documate.Api.Infrastructure.Http.ApiExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Customer web may be a different origin in staging/production (see apps/web public/env.json).
app.UseCors("CustomerWebDev");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<BusinessContextOverrideMiddleware>();
app.UseAuthorization();
app.UseMiddleware<TenantBusinessProvisioningMiddleware>();
app.UseDocumateHangfireDashboard();
app.MapControllers();
app.MapHealthChecks("/health");

// Liveness for IIS deploy tooling — anonymous, no dependency checks.
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));

// DI manager — static RecurringJob uses JobStorage.Current, which is unset until Hangfire is fully wired (IIS deploy crash).
app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<EmailIntakeJobs>(
    "email-intake-mime-retention",
    j => j.PurgeExpiredMimeAsync(),
    Cron.Daily);

app.Run();

public partial class Program;
