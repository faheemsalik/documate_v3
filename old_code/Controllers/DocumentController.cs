using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Amazon.Runtime.Internal.Util;

using Documate.Extensions;
using Documate.Models;
using Documate.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;

using Newtonsoft.Json;
using Universal.Common;
using Documate.Services;
using System.Collections;

namespace Documate.Controllers
{
    [ApiController]
    //[Route("[controller]/[action]")]
    public class DocumentController : ControllerBase
    {
        private readonly ILogger<DocumentController> _logger;
        private readonly IDocumentService docService;
        private readonly IAccountService accountService;
        private readonly IQueueService queueService;
        private readonly IServerDataService serverDataService;
        private readonly IWebhookService webhookService;

        public DocumentController(
            ILogger<DocumentController> logger,
            IDocumentService documentService,
            IAccountService accountService,
            IQueueService queueService,
            IServerDataService serverDataService,
            IWebhookService webhookService)
        {
            _logger = logger;
            docService = documentService;
            this.accountService = accountService;
            this.queueService = queueService;
            this.serverDataService = serverDataService;
            this.webhookService = webhookService;
        }

        [HttpPost]
        [Route("documents/UploadDocumentAsync")]
        public async Task<UploadDocReponse> UploadDocumentAsync()
        {
            UploadDocReponse returnValue = new UploadDocReponse();
            // Auth validity
            //TODO Shift to Attribute
            string authToken = string.Empty;
            //int userId = 0;
            var authHeader = Request.Headers.Where(x => x.Key == "token");
            if (authHeader != null)
            {
                authToken = authHeader.FirstOrDefault().Value.ToString();
                LoginReturnModel loginObj = accountService.AuthValidility(authToken);
                if (loginObj.IsSuccfull == false)
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "User is not logged in or token is invalid";
                    return returnValue;
                }
            }
            //---------
            DocumentModel docModel = new DocumentModel();
            string streamText;
            // call me
            string logMessage = "Data not received properly";
            string externalMessage = "Data not received properly";
            try
            {
                using (StreamReader reader = new StreamReader(Request.Body))
                    streamText = await reader.ReadToEndAsync();
                docModel = JsonConvert.DeserializeObject<DocumentModel>(streamText) as DocumentModel;

                //ToDo Apply validation checks.
                //Todo: Check Tenant credit balance.
                UserModel user = accountService.GetUserFromAuthToken(authToken);
                UserQueueModel userQueue = queueService.GetUserQueue(user.Id, docModel.QueueId);
                if (userQueue == null)
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = $"Either Queue does not exist or user {user.UserName} doesn't have permission to access the queue!";
                    return returnValue;
                }
                //string[] supportedTypes = { ".pdf", ".jpg", ".png" };
                //docModel.ContentType = Path.GetExtension(docModel.FileName);
                //if (supportedTypes.Where(x => x.Contains(docModel.ContentType.ToLower())).FirstOrDefault()==null)
                //{
                //    returnValue.IsSucessfull = false;
                //    returnValue.Message = "File type not supported. Supported types are: PDF, PNG, JPG";
                //    return returnValue;
                //}

                logMessage = "Error while processing memory stream";
                if (!docModel.FileBase64.IsNullOrEmpty())
                    docModel.FileBytes =  Convert.FromBase64String(docModel.FileBase64);
                docModel.MemStream = new MemoryStream(docModel.FileBytes);
                logMessage = "Error while calling service method";
                externalMessage = "Problem in document saving";
                returnValue = await docService.CreateDocAsync(docModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(logMessage + "-" + ex.Message);
                returnValue.IsSucessfull = false;
                returnValue.Message = externalMessage;
            }
            return returnValue;
        }

        [HttpGet]
        [Route("documents/ProcessNanoDataV2")]
        public async Task<ResponseModel> ProcessNanoDataV2(int id, bool getMetaDataFromServer)
        {
            ResponseModel returnValue = new ResponseModel();
            Extensions.Helper.isEnabledScheduler = false;
            docService.GetMetaDataFromServer = getMetaDataFromServer;
            returnValue = await docService.ProcessNanoDataV2(id);
            Extensions.Helper.isEnabledScheduler = true;
            return returnValue;
        }

        [HttpGet]
        [Route("documents/GetDocsList")]
        public async Task<DocumateDocsListResponse> GetDocsList(string id, DocumateDocStatus? status, int? page)
        {
            DocumateDocListInfo theObject = new DocumateDocListInfo();
            Expression<Func<DocumateDocListInfo, bool>> exp = null;
            DocumateDocsListResponse returnValue = new DocumateDocsListResponse();
            //string[] ids;
            try { 
                //if (!id.IsNullOrEmpty())
                //{
                //    id = id.TrimEnd(',');
                //    if (id.IndexOf("-") > 1)
                //    {
                //        ids = id.Split("-");
                //        exp = x => (status == null || x.Status == status) && x.Id >= ids[0].ToInt32() && x.Id <= ids[1].ToInt32();
                //    }
                //    else if (id.IndexOf(",") > 1)
                //    {
                //            //ids = id.Split(",").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                //        var idList = id.Split(",")
                //       .Where(s => !string.IsNullOrWhiteSpace(s))
                //       .Select(s => int.Parse(s.Trim()))
                //       .ToList();
                //            exp = x => (status == null || x.Status == status) && idList.Contains(x.Id);
                //    }
                //    else
                //        exp = x => (status == null || x.Status == status) && x.Id == id.ToInt32();
                //}
                //else
                //    exp = x => status == null || x.Status == status;

                //returnValue = docService.GetDocList(page, exp);
                returnValue = await docService.GetDocList(page, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                returnValue.IsSucessfull = false;
                returnValue.Message = "Invalid document IDs string format";
            }
            return returnValue;
        }

        [HttpGet]
        [Route("documents/GetDocAsync")]
        public async Task<DocumateDocumentResponse>  GetDocAsync(int id)
        {
            //ToDO Apply Token security
            DocumateDocumentResponse returnValue = new DocumateDocumentResponse();
            DocumentModel docModel = await docService.GetModelById(id);
            if (docModel == null)
            {
                returnValue.IsSucessfull = false;
                returnValue.Message = "Document not found";
                return returnValue;
            }

            DocumateDocument externalDoc = new DocumateDocument();
            if (docModel != null)
            {
                externalDoc.CopyPropertyValues(docModel);
                externalDoc.ProcessedDataJSON = docModel.ProcessedDataJSON;
                returnValue.IsSucessfull = true;
                returnValue.Message = "Get document content in TheObject node";
                returnValue.Result = externalDoc;
            }
            return returnValue;
        }

        [HttpGet]
        [Route("documents/ReProcessDocument")]
        public async Task<ResponseModel> ReProcessDocument(int id)
        {
            //ToDO Apply Token security
            return await docService.ReProcessDocument(id); ;
        }
        [HttpGet]
        [Route("documents/ReUploadDocument")]
        public async Task<ResponseModel> ReUploadDocument(int id)
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue = await docService.ReUploadDocument(id);
            return returnValue;
        }

        [HttpGet]
        [Route("documents/GetRawDataJSON")]
        public async Task<ResponseModel> GetRawDataJSON(int id)
        {
            _logger.LogError("In Controller GetRawDataJSON");
            ResponseModel returnValue = new ResponseModel();
            var ret = await docService.GetModelById(id);
            if (ret != null)
            {
                returnValue.Result = ret.RawDataJSON;
                returnValue.IsSucessfull = true;
            }
            return returnValue;
        }

        [HttpGet]
        [Route("documents/GetProcessedDataJSON")]
        public async Task<ResponseModel> GetProcessedDataJSON(int id)
        {
            ResponseModel returnValue = new ResponseModel();
            var ret = await docService.GetModelById(id);
            if (ret != null)
            {
                returnValue.Result = ret.ProcessedDataJSON;
                returnValue.IsSucessfull = true;
            }
            return returnValue;
        }

        [HttpPost]
        [Route("documents/DebugNanoDoc")]
        public async Task<ResponseModel> DebugNanoDoc(debugNanoDoc data)
        {
            ResponseModel returnValue = new ResponseModel();
            if (data == null || data.debugData == null)
                return returnValue;
            //docService.DebugRawDataJSON = data.debugData;
            //docService.DebugQueueId = data.localQueueId;
            returnValue = await docService.ProcessNanoDataV2(29);
            return returnValue;
        }

        [HttpGet]
        [Route("documents/ProcessAllJobs")]
        public async Task ProcessAllJobs()
        {
            await docService.ScheduleProcessAllDocs();
        }

        [HttpGet]
        [Route("documents/GetOriginalFileURL")]
        public async Task<string> GetOriginalFileURL(int id)
        {
            var ret = await docService.GetOriginalFileURL(id);
            return ret;
        }

        [HttpGet]
        [Route("documents/GetDocDebugData")]
        public async Task<ResponseModel> GetDocDebugData(int id)
        {
            ResponseModel returnValue = new ResponseModel();
            var ret = await docService.GetEntityById(id);
            DocDebugDataVM debugData = new DocDebugDataVM();
            debugData.RawDataJSON = ret.RawDataJSON;
            debugData.ProcessedDataJSON = ret.ProcessedDataJSON;
            debugData.AwsJobId = ret.AwsJobId;
            debugData.DocId = id;
            returnValue.Result = debugData;
            return returnValue;
        }

        [HttpGet]
        [Route("documents/UpdateBulkJsonMultiPageAsync")]
        public async Task<ResponseModel> UpdateBulkJsonMultiPageAsync()
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue = await docService.UpdateNanoBulkJsonAsync();
            return returnValue;
        }

        [HttpGet]
        [Route("documents/UpdateNanoJsonMultiPage")]
        public async Task<ResponseModel> UpdateNanoJsonMultiPage(int id)
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue = await docService.UpdateNanoJsonMultiPage(id);
            return returnValue;
        }

        //------------------------ Webhhok for Nano
        [HttpPost]
        [Route("documents/webhook_nano")]
        public async Task WebhookForNano()
        {
            //if (Documate.Extensions.Helper.isRunningScheduler) return;
            string streamText;
            _logger.LogInformation("In Webhook controller call - WebhookForNano");
            string logMessage = "Data not received properly";
            try
            {
                logMessage = "Error while reading stream";
                using (StreamReader reader = new StreamReader(Request.Body))
                {
                    streamText = await reader.ReadToEndAsync();
                    if (string.IsNullOrEmpty(streamText))
                    {
                        _logger.LogInformation($"Webhook-Nano: Process failed: StreamText was null");
                        return;
                    }
                    await docService.WebhookNano(streamText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(logMessage + "-" + ex.Message);
            }
        }

        [HttpPost]
        [Route("documents/WebhookServiceCall")]
        public async Task WebhookServiceCall(int docId)
        {
            Domain.Document doc = await docService.GetEntityById(docId);
            docService.WebhookCallToClient(doc);
        }
    }
    public class debugNanoDoc
    {
        public string debugData { get; set; }
        public int localQueueId { get; set; }
    }
}
