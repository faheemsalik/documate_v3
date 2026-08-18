namespace Documate.Api.Infrastructure.Pipeline;

using Documate.Api.Infrastructure.Extract;
using Documate.Api.Infrastructure.Ocr;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Pipeline.Stages;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;

public static class PipelineServiceCollectionExtensions
{
    public static IServiceCollection AddDocumatePipeline(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PipelineOptions>(configuration.GetSection(PipelineOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Documate")
            ?? throw new InvalidOperationException("Connection string 'Documate' is missing for Hangfire.");

        var pipeline = configuration.GetSection(PipelineOptions.SectionName).Get<PipelineOptions>()
            ?? new PipelineOptions();
        var maxWorkers = Math.Max(1, pipeline.MaxConcurrentFiles);

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero, // near-realtime dequeue
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                PrepareSchemaIfNecessary = true,
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = maxWorkers;
            options.Queues = ["default", "webhooks"];
        });

        services.AddScoped<IFilePipelineStub, FilePipelineStub>();
        services.AddScoped<IOcrNormalizeAdapter, Mode1OcrNormalizeAdapter>();
        services.AddScoped<IFileSplitStage, FileSplitStage>();
        services.AddScoped<IFileClassifyStage, FileClassifyStage>();
        services.AddScoped<IDocumentRouteStage, DocumentRouteStage>();
        services.AddScoped<IDocumentExtractStage, DocumentExtractStage>();
        services.AddScoped<IDocumentExtractAdapter, Mode1DocumateMetaExtractAdapter>();
        services.AddScoped<FilePipelineJobs>();
        services.AddScoped<WebhookJobs>();
        services.AddSingleton<IWorkDispatcher, HangfireWorkDispatcher>();
        services.AddSingleton<IWebhookDispatcher, HangfireWebhookDispatcher>();
        return services;
    }

    public static WebApplication UseDocumateHangfireDashboard(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new HangfireDevDashboardAuthFilter()],
            });
        }

        return app;
    }
}

/// <summary>Dev-only dashboard access (local / Development). Tighten before any shared env.</summary>
file sealed class HangfireDevDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
