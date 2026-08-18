using FluentScheduler;

using Documate.Data;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using Documate.Services;
//using Sentry;
using System.Threading;
using System.Threading.Tasks;


namespace Documate.Extensions
{

    public class InnovoiceScheduler : Registry
    {
        protected readonly IServiceScopeFactory serviceScopeFactory;

        public InnovoiceScheduler(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;

            NonReentrantAsDefault();
            Schedule(() => new DocumentUpdatesJob(serviceScopeFactory)).ToRunNow().AndEvery(Documate.Data.ProjectSettings.SchdularTimeMinutes).Minutes();
            //Schedule(() => new RemoveThread(serviceScopeFactory)).ToRunEvery(1).Days().At(7, 0);
        }
    }

    public class DocumentUpdatesJob : IJob
    {
        private static bool isRunning = false;
        protected readonly IServiceScopeFactory serviceScopeFactory;

        public DocumentUpdatesJob(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public void Execute()
        {
            if (isRunning)
                return;
            isRunning = true;
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            IServiceScope serviceScope = null;
            try
            {
                if (Helper.isEnabledScheduler)
                {
                    serviceScope = serviceScopeFactory.CreateScope();
                    IDocumentService documentService = serviceScope.ServiceProvider.GetService<IDocumentService>();
                    // Call the method to process all documents
                    //var task = documentService.ScheduleProcessAllDocs().GetAwaiter().GetResult();
                    var task = documentService.ScheduleProcessAllDocs();
                    task.Wait(cts.Token); // Will throw if timeout
                    //SentrySdk.CaptureMessage("Something went wrong");
                }
            }
            catch (OperationCanceledException ex2)
            {
                // Timeout occurred, reset isRunning
                //SentrySdk.CaptureException(ex2);
                isRunning = false;
            }
            catch (Exception ex)
            {
                // Log the exception using Sentry
                //SentrySdk.CaptureException(ex);
                // Optionally, log to console or other logging service
                Console.WriteLine($"Error in DocumentUpdatesJob: {ex.Message}");
                isRunning = false;
            }
            finally
            {
                isRunning = false;
            }
        }
    }

    public class RemoveThread : IJob
    {
        protected readonly IServiceScopeFactory serviceScopeFactory;

        public RemoveThread(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public void Execute()
        {
            IServiceScope serviceScope = null;
            try
            {
                if (Helper.isEnabledScheduler)
                {
                    serviceScope = serviceScopeFactory.CreateScope();
                    IDocumentService documentService = serviceScope.ServiceProvider.GetService<IDocumentService>();
                    documentService.RemoveOldThreads().Wait();
                }
            }
            finally
            { }
        }
    }

}