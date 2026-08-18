using Google.Cloud.DocumentAI.V1;

using Amazon.Runtime;
using Amazon.S3;
using Amazon.Textract;
using FluentScheduler;


using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Documate.Helper;
using Amazon.Comprehend;
using Documate.Services;
using Documate.Data;
using Documate.Extensions;
using Documate.GraphQL;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Playground;
using HotChocolate.Data;
using Sentry;


namespace Documate
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnv)
        {
            hostingEnv.ConfigureLog4Net("log4net.xml");
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddDbContext<DBContext>(options =>
                   options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddCors();

            //services.AddMvc(option => option.EnableEndpointRouting = false)
            //    .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
            //    .AddJsonOptions(options =>
            //    {
            //        options.JsonSerializerOptions.IgnoreNullValues = true;
            //        options.JsonSerializerOptions.PropertyNamingPolicy = null;
            //    });

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });

            services.AddLogging(builder => builder
                 .AddConsole()
                 .AddDebug()
                 .AddLog4Net("log4net.xml")
                 );  // Rule for all providers

            //----- AWS Profile and credentials loading
            // TODO: Load credntials from a secure location
            var awsOption = Configuration.GetAWSOptions();
            awsOption.Credentials = new BasicAWSCredentials(Configuration["AWS:AccessKey"], Configuration["AWS:SecretKey"]);
            //awsOption.Region = Amazon.RegionEndpoint.USEast1;
            awsOption.Profile = "Documate";
            services.AddDefaultAWSOptions(awsOption);
            services.AddAWSService<IAmazonS3>();
            services.AddAWSService<IAmazonTextract>();
            services.AddAWSService<IAmazonComprehend>();

            //-----Open Ai
            string apiKey = Configuration["OpenAI:ServiceAccountKey"];
            services.AddScoped<IOpenAiService>(sp => new OpenAiService(sp.GetRequiredService<ILogger<OpenAiService>>(), apiKey));

            //-----Google
            string googleKeyJSON = Configuration["Google:ServiceAccountKey"];
            services.AddScoped<IGoogleService>(sp => new GoogleService(sp.GetRequiredService<ILogger<GoogleService>>(), googleKeyJSON));
            //-----ADD SCOPE
            services.AddScoped<IS3Service, S3Service>();
            services.AddScoped<ITextractService, TextractService>();
            //services.AddScoped<ITextractTextDetectionService, TextractTextDetectionService>();
            services.AddScoped<IComprehendService, ComprehendService>();
            services.AddScoped<IDocumentAiService, DocumentAiService>();

            //Tenant Repos
            services.AddScoped<IAccountRepo, AccountRepo>();
            services.AddScoped<IUserRepo, UserRepo>();
            services.AddScoped<IAuthTokenRepo, AuthTokenRepo>();
            //Document Repos
            services.AddScoped<INanoModelRepo, NanoModelRepo>();
            services.AddScoped<IDocStorageRepo, DocStorageRepo>();
            services.AddScoped<IDocumentRepo, DocumentRepo>();
            services.AddScoped<IDocImageRepo, DocImageRepo>();
            services.AddScoped<IStatusHistoryRepo, StatusHistoryRepo>();
            //Queue Repos
            services.AddScoped<IQueueRepo, QueueRepo>();
            services.AddScoped<IUserQueueRepo, UserQueueRepo>();
            //System Repos
            //services.AddScoped<ISysDocTypeRepo, SysDocTypeRepo>();
            services.AddScoped<ISysDocStatusRepo, SysDocStatusRepo>();
            //Template Repos
            services.AddScoped<ITemplateRepo, TemplateRepo>();
            services.AddScoped<ITemplateQueueRepo, TemplateQueueRepo>();
            services.AddScoped<ITemplateKeywordRepo, TemplateKeywordRepo>();
            services.AddScoped<IMasterKeywordSetRepo, MasterKeywordSetRepo>();
            services.AddScoped<IKeywordSynonymRepo, KeywordSynonymRepo>();
            services.AddScoped<IIdentifyingElementRepo, IdentifyingElementRepo>();
            services.AddScoped<IKeywordElementRepo, KeywordElementRepo>();
            services.AddScoped<InnovoiceScheduler, InnovoiceScheduler>();
            services.AddScoped<IJob, DocumentUpdatesJob>();

            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IQueueService, QueueService>();
            services.AddScoped<IKeywordService, KeywordService>();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<IQueueService, QueueService>();
            services.AddScoped<IBillingService, BillingService>();
            services.AddScoped<IServerDataService, ServerDataService>();
            services.AddScoped<IMailkitService, MailkitService>();
            services.AddScoped<IAnnotationService, AnnotationService>();
            services.AddScoped<IWebhookService, WebhookService>();
            services.AddScoped<INanoModelService, NanoModelService>();

            //services.AddScoped<IQuery, Query>();

            // Add GraphQL Playground for testing queries
            services.AddGraphQLServer()
                .AddQueryType<Query>().ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .AddProjections()
                .AddFiltering()
                .AddSorting();

            //SentrySdk.Init(options =>
            //{
            //    // A Sentry Data Source Name (DSN) is required.
            //    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            //    // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
            //    options.Dsn = "https://0720db20fbab4f209bbd14606233cd8b@o4504762126368768.ingest.us.sentry.io/4504762131087360";

            //    // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
            //    // This might be helpful, or might interfere with the normal operation of your application.
            //    // We enable it here for demonstration purposes when first trying Sentry.
            //    // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
            //    options.Debug = false;

            //    // This option is recommended. It enables Sentry's "Release Health" feature.
            //    options.AutoSessionTracking = true;
            //    options.TracesSampleRate = 0.0;
            //});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            ILogger<InnovoiceScheduler> logger, InnovoiceScheduler scheduler
            )
        {
            //IServiceScopeFactory serviceScopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            //JobManager.Initialize(new InnovoiceScheduler(serviceScopeFactory));
            JobManager.Initialize(scheduler);

            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGraphQL();
            });


            // Add GraphQL Playground for testing queries
            app.UsePlayground(new PlaygroundOptions
            {
                Path = "/playground"
            });

            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<DBContext>();
                if (!context.Database.EnsureCreated())
                    context.Database.Migrate();
            }

            logger.LogInformation($"Application started");

        }
    }
}
