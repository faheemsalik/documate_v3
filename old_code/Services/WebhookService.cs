using Documate.Data;
using Documate.Domain;
using Documate.Extensions;
using Documate.Models;
using Documate.Services;
using Microsoft.Extensions.Logging;

using RestSharp;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;

namespace Documate.Services
{
    public interface IWebhookService
    {
        void WebhookCall(int docId);
    }
    //================================================================

    public class WebhookService : IWebhookService
    {
        private readonly DBContext dbContext;
        private readonly ILogger<AccountService> Logger;
        private readonly IQueueService queueService;

        public WebhookService(
            ILogger<AccountService> logger,
            DBContext context,
            IQueueService queueService
            )
        {
            Logger = logger;
            dbContext = context;
            this.queueService = queueService;
        }
        // Call customer webhook url to tell some files are ready
        public void WebhookCall(int docId)
        {
            try
            {
                if (docId == 0)
                    return;
                //DocumentModel doc = documentService.GetEntityById(docId);
                //QueueModel queue = queueService.GetEntityById(doc.QueueId);

                //var client = new RestClient($"{queue.WebhookURL}");
                //var request = new RestRequest(Method.POST);
                //var param = new JsonParameter("id", docId);
                //request.AddParameter(param);
                //IRestResponse response = client.Execute(request);
                //if (response.StatusCode != HttpStatusCode.OK)
                //    throw new InvalidDataException($"Couldn't call webhook for queue id: {queue.Id} at URL: {queue.WebhookURL}");

            }
            catch (Exception ex)
            {
                Logger.LogError($"Error while calling Webhook for doc id: {docId}. Error Msg: {ex.Message}");
            }

        }

        public void WebhookCallFromNano()
        {
            // do all the processing
        }
    }

}
