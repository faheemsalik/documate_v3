namespace Documate.Api.Infrastructure.Pipeline;

using Documate.Api.Infrastructure.Extract;
using Documate.Api.Infrastructure.Intelligence;
using Documate.Api.Infrastructure.Notifications;
using Documate.Api.Infrastructure.Ocr;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Pipeline.Stages;
using Documate.Api.Infrastructure.PostProcess;
using Documate.Api.Infrastructure.Storage;
using Documate.Api.Infrastructure.Webhooks;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;

public static class PipelineServiceCollectionExtensions
{
    public static IServiceCollection AddDocumatePipeline(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PipelineOptions>(configuration.GetSection(PipelineOptions.SectionName));
        services.Configure<OcrOptions>(configuration.GetSection(OcrOptions.SectionName));
        services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));
        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Documate")
            ?? throw new InvalidOperationException("Connection string 'Documate' is missing for Hangfire.");

        var pipeline = configuration.GetSection(PipelineOptions.SectionName).Get<PipelineOptions>()
            ?? new PipelineOptions();
        var maxFileWorkers = Math.Max(1, pipeline.MaxConcurrentFiles);
        var maxWebhookWorkers = Math.Max(1, pipeline.MaxConcurrentWebhooks);

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

        // File OCR/pipeline — do not share workers with webhooks (webhooks used to steal OCR slots).
        services.AddHangfireServer(options =>
        {
            options.ServerName = $"documate-files:{Environment.MachineName}";
            options.WorkerCount = maxFileWorkers;
            options.Queues = ["priority", "default"];
        });

        services.AddHangfireServer(options =>
        {
            options.ServerName = $"documate-webhooks:{Environment.MachineName}";
            options.WorkerCount = maxWebhookWorkers;
            options.Queues = ["webhooks"];
        });

        services.AddScoped<IFilePipelineStub, FilePipelineStub>();
        services.AddSingleton<IOcrEngine, TextractOcrEngine>();
        services.AddSingleton<IOcrEngine, GoogleDocumentAiOcrEngine>();
        services.AddScoped<IOcrNormalizeAdapter, Mode1OcrNormalizeAdapter>();
        services.AddScoped<IPageIntelligenceService, PageIntelligenceService>();
        services.AddScoped<IFileSplitStage, FileSplitStage>();
        services.AddScoped<IFileClassifyStage, FileClassifyStage>();
        services.AddScoped<IDocumentRouteStage, DocumentRouteStage>();
        services.AddScoped<IDocumentExtractStage, DocumentExtractStage>();
        services.AddScoped<IDocumentExtractAdapter, LiveLlmDocumentExtractAdapter>();
        services.AddScoped<IDocumentPdfMaterializer, DocumentPdfMaterializer>();
        services.AddScoped<ISignedDownloadUrlService, SignedDownloadUrlService>();
        services.AddSingleton<IPlatformMcpTool, NormalizeDateTool>();
        services.AddSingleton<IPlatformMcpTool, NormalizeCurrencyTool>();
        services.AddSingleton<IInternalMcpHost, InternalMcpHost>();
        services.AddScoped<IAgentPostProcessRunner, AgentPostProcessRunner>();
        services.AddScoped<IDefaultWorkflowBootstrap, DefaultWorkflowBootstrap>();
        services.AddSingleton<IOpsAlertSender, OpsAlertSender>();
        services.AddHttpClient("documate-llm", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(90);
        });
        services.AddSingleton<IWebhookSecretProtector, WebhookSecretProtector>();
        services.AddScoped<IDocumentWebhookScheduler, DocumentWebhookScheduler>();
        services.AddHttpClient("documate-webhooks", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<DocumentWebhookDelivery>();
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
