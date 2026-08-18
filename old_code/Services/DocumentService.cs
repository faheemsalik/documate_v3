using System;
using System.IO;
using System.Text;
using System.Net;

using Amazon.Textract;
using Amazon.Textract.Model;

using Documate.Data;
using Documate.Domain;
using Documate.Extensions;
using Documate.Models;
using Documate.Common.Models;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ServiceStack;
using System.Text.RegularExpressions;
using RestSharp;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;
using Microsoft.EntityFrameworkCore;
using Universal.Common;
using System.Net.Http;
using Google.Api;

namespace Documate.Services
{
    public interface IDocumentService
    {
        Task<UploadDocReponse> CreateDocAsync(DocumentModel docModel);
        Task<UploadDocReponse> CreateDoc(DocumentModel docModel);
        Task<Domain.Document> GetEntityById(int id);
        Task<DocumentModel> GetModelById(int id);
        DocumateDocsListResponse GetDocList(int? page, Expression<Func<DocumateDocListInfo, bool>> where = null);
        Task<DocumateDocsListResponse> GetDocList(int? page, string Ids);

        public string DebugRawDataJSON { get; set; }
        public bool GetMetaDataFromServer { get; set; }
        public int DebugQueueId { get; set; }
        
        Task<ResponseModel> UpdateRawJsonAsync(string jobId, EnumAiServiceSource aiSource);
        Task<ResponseModel> UpdateNanoJSON(int docId);
        Task<ResponseModel> UpdateNanoBulkJsonAsync();

        Task<string> GetOriginalFileURL(int id);
        Task<ResponseModel> UpdateAnnotation(int docId, string annotationData);

        Task<ResponseModel> ReProcessDocument(int docId);
        Task<ResponseModel> ReUploadDocument(int docId);

        Task ScheduleProcessAllDocs();
        Task<ResponseModel> ProcessNanoDataV2(int docId);

        Task WebhookNano(string streamText);
        Task<ResponseModel> UpdateNanoJsonMultiPage(int docId);
        void WebhookCallToClient(Domain.Document doc);

        Task RemoveOldThreads();
    }

    public class DocumentService : IDocumentService
    {
        public string DebugRawDataJSON { get; set; }
        public string DebugComprehendOutputJSON { get; set; }
        public int DebugQueueId { get; set; }
        public bool GetMetaDataFromServer { get; set; }
        private readonly ILogger<DocumentService> _logger;
        private readonly IDocumentRepo documentRepo;
        private readonly IS3Service s3Service;
        private readonly IQueueService queueService;
        private readonly ITextractService textractService;
        private readonly IAccountRepo accountRepo;
        private readonly INanoModelRepo nanoModelRepo;

        private readonly ISysDocStatusRepo sysDocStatusRepo;
        private readonly IOpenAiService _openAiService;
        private readonly IDocumentAiService _documentAiService;

        public DocumentService(

            ILogger<DocumentService> logger,
            IDocumentRepo documentRepo,
            IQueueService queueService,
            IS3Service s3Service,
            ITextractService textractService,
            ISysDocStatusRepo sysDocStatusRepo,
            IAccountRepo accountRepo,
            IServerDataService serverDataService,
            INanoModelRepo nanoModelRepo,
            IOpenAiService openAiService,
            IDocumentAiService documentAiService
            )
        {
            _logger = logger;
            this.documentRepo = documentRepo;
            this.queueService = queueService;
            this.s3Service = s3Service;
            this.textractService = textractService;
            this.sysDocStatusRepo = sysDocStatusRepo;
            this.accountRepo = accountRepo;
            this.nanoModelRepo = nanoModelRepo;
            this._openAiService = openAiService;
            this._documentAiService = documentAiService;
        }
        public async Task ScheduleProcessAllDocs()
        {
            _logger.LogDebug($"Scheduled Process Called.");
            List<Domain.Document> docList = new List<Domain.Document>();

            try
            {
                docList = await documentRepo.GetEntities(x =>
                x.FlgDeleted != true
                && x.FlgFailed != true
                && x.NoOfRetries <= 5
                && (x.RawDataJSON == null || x.ProcessedDataJSON == null)).ToListAsync();
                //docList.Where(x => (DateTime.UtcNow - x.CreatedOnUtc).TotalMinutes > ProjectSettings.SchdularTimeMinutes - 1.00).ToList();
                docList.OrderBy(x => x.CreatedOnUtc).ToList();
                //_logger.LogInformation($"Files List to process:  {string.Join(",", docList.Select(x=>x.Id))}");
                docList.Take(30);
                _logger.LogInformation($"No of documents to process:{docList.Count()}");
                foreach (Domain.Document doc in docList)
                {
                    try
                    {
                        QueueModel qm = queueService.GetEntityById(doc.QueueId);
                        if (qm == null) continue;
                        if (doc.RawDataJSON == null)
                        {
                            _logger.LogInformation($"Updating Raw JSON - id={doc.Id}- Queue: {doc.QueueId}");
                            if (qm.AiServiceSource == (int)EnumAiServiceSource.NANO)
                            {
                                _logger.LogInformation($"calling :UpdateNanoJsonMultiPage - Docid:{doc.Id}");
                                await UpdateNanoJsonMultiPage(doc.Id);
                            }
                            else if (qm.AiServiceSource == (int)EnumAiServiceSource.OPENAI)
                            {
                                _logger.LogInformation($"calling :updateRawTextJson - Docid:{doc.Id}");
                                await updateRawTextJson(doc.Id);
                            }
                        }
                        if (doc.ProcessedDataJSON == null && doc.RawDataJSON != null && qm.AiServiceSource == (int)EnumAiServiceSource.NANO)
                        {
                            //_logger.LogDebug($"Processing JSON Documate- id={doc.Id}- Queue: {doc.QueueId}");
                            _logger.LogInformation($"calling :ProcessNanoDataV2 - Docid:{doc.Id}");
                            await ProcessNanoDataV2(doc.Id);
                        }
                        else if (doc.ProcessedDataJSON == null && doc.RawDataJSON != null && qm.AiServiceSource == (int)EnumAiServiceSource.OPENAI)
                        {
                            await Task.Delay(1000);
                            _logger.LogInformation($"calling :ProcessOpenAiDocExtraction - Docid:{doc.Id}");
                            await ProcessOpenAiDocExtraction(doc.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        //Sentry.SentrySdk.CaptureException(ex);
                        _logger.LogError($"Error in processing document id: {doc.Id}. Error: {ex.Message}");
                        //throw;
                    }
                    //await Simplicity_keep_alive_call();
                }
            }
            catch (Exception ex)
            {
                //Sentry.SentrySdk.CaptureException(ex);
                _logger.LogDebug($"Error in ScheduleProcessAllDocs. Error: {ex.Message}");
            }
        }


        public async Task<UploadDocReponse> CreateDocAsync(DocumentModel docModel)
        {

            UploadDocReponse returnValue = new UploadDocReponse();

            RepoResult repoResult = new RepoResult();
            _logger.LogInformation($"CreateDocAsync - {docModel.Id}");

            string logMessage = $"Error in saving file. Doc id:{docModel.Id}, Q id:{docModel.QueueId}";
            //if (!Directory.Exists(ProjectSettings.TempDocsFolder))
            //    Directory.CreateDirectory(ProjectSettings.TempDocsFolder);
            try
            {
                QueueModel queueModel = queueService.GetEntityById(docModel.QueueId);
                Account accEntity = accountRepo.GetEntities(x => x.Id == queueModel.AccountId).FirstOrDefault();
                docModel.OriginalFileName = docModel.FileName;
                docModel.FileName = GetUniqueFileName(docModel.FileName);
                docModel.PageCount = UpdatePageCount(docModel.MemStream, docModel.FileName);
                string filePath = Path.Combine(ProjectSettings.TempDocsFolder, docModel.FileName);
                // saving local file
                //using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                //{
                //    byte[] bytes = Convert.FromBase64String(docModel.FileBase64);
                //    file.Write(bytes, 0, bytes.Length);
                //}

                // saving on S3
                S3FileModel uploadModel = new S3FileModel();
                uploadModel.BucketName = queueModel.S3BucketName;
                uploadModel.FileName = docModel.FileName; // $@"QId-{queueModel.Id}-{DateTime.UtcNow.Year}-{DateTime.UtcNow.Month}/{
                if (docModel.MemStream == null)
                    throw new InvalidDataException("Can't process document. Memory stream is empty.");

                uploadModel.FileMemoryStream = docModel.MemStream;
                logMessage = $"Uploading to S3- QueueId:{queueModel.Id} - FileName:{docModel.FileName}";
                await s3Service.UploadAsync(uploadModel);
                docModel.BucketName = queueModel.S3BucketName;

                if (queueModel.AiServiceSource == (int)EnumAiServiceSource.OPENAI) // 4
                {
                    ExtractRawTextModel textExtractModel = new ExtractRawTextModel {S3FileModel = uploadModel};
                    //docModel.AwsJobId = await textractService.StartDocumentTextExtractionAsync(docModel);
                    //docModel.RawDataJSON = await textractService.ExtractRawTextFromDocumentSync(uploadModel);
                    //docModel.ProcessedDataJSON = await _openAiService.GetAssistantOutputAsyncSDK(docModel.RawDataJSON,"");
                    if (queueModel.TextExtractionService == (int)EnumRawTextService.AWS) 
                    {
                        textExtractModel.Service = EnumRawTextService.AWS;
                        docModel.RawDataJSON = await _documentAiService.ExtractTextFromPdfAsync(textExtractModel);
                        // in case of error in text detection, call google service
                        if (string.IsNullOrEmpty(docModel.RawDataJSON))
                        {
                            textExtractModel.Service = EnumRawTextService.GOOGLE;
                            textExtractModel.FileBytes = docModel.FileBytes;
                            docModel.RawDataJSON = await _documentAiService.ExtractTextFromPdfAsync(textExtractModel);
                        }
                    }
                    else if (queueModel.TextExtractionService == (int)EnumRawTextService.GOOGLE)
                    {
                        textExtractModel.Service = EnumRawTextService.GOOGLE;
                        textExtractModel.FileBytes = docModel.FileBytes;
                        docModel.RawDataJSON = await _documentAiService.ExtractTextFromPdfAsync(textExtractModel);
                    }
                    else
                        throw new InvalidDataException("Text extraction service not found");

                }
                else if (queueModel.AiServiceSource == (int)EnumAiServiceSource.NANO)  // 3
                {

                    //var client = new RestClient();
                    //var request = new RestRequest($"{ProjectSettings.NanoApiEndPoint}OCR/Model/{queueModel.NanoModelId}/LabelFile/?async=true", Method.Post);
                    ////request.AddHeader("authorization", ProjectSettings.NanoApiKey);
                    ////string apikey = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(ProjectSettings.NanoApiKey));
                    //string apikey = "Basic MjA4NWU1OGUtYTU3YS0xMWVlLThjMzQtNDY1NDJlYzkyZTAyOg==";
                    //request.AddHeader("authorization", apikey);
                    //request.AddHeader("accept", "Multipart/form-data");
                    //request.AddFile("file", docModel.FileBytes, docModel.FileName);
                    //RestResponse response = client.Execute(request);
                    //if (response.StatusCode == HttpStatusCode.OK)
                    //{
                    //    NanoGenericResponse nanoResponse = JsonConvert.DeserializeObject<NanoGenericResponse>(response.Content);
                    //    if (nanoResponse.message.ToLower() == "success" && nanoResponse.result.Count > 0)
                    //    {
                    //        docModel.AwsJobId = "";
                    //        docModel.NanoUploadResponse = response.Content;
                    //        docModel.NanoRequestFileId = nanoResponse.result.FirstOrDefault().request_file_id;
                    //        //docModel.CdnThumbnail = nanoResponse.result.FirstOrDefault().signed_urls
                    //        if (docModel.NanoUploadResponse == null)
                    //        {
                    //            _logger.LogDebug(response.ToString());
                    //            throw new InvalidDataException("Nano response null");
                    //        }
                    //        //JsonObjectAttribute a = new JsonObjectAttribute(nanoResponse.result.FirstOrDefault().signed_urls);
                    //        _logger.LogDebug($"File uploaded to nano server - {docModel.FileName}");
                    //    }
                    //    else
                    //    {
                    //        docModel.ProcessingRemarks = "Nano server returned error. File upload didn't work properly";
                    //    }
                    //    logMessage = $"Processing - FileName:{uploadModel.FileName} - NanoFileId: {docModel.AwsJobId} - Process completed";
                    //}
                    //else
                    //{
                    //    logMessage = $"Processing - FileName:{uploadModel.FileName} - Nano upload Process Failed";
                    //}
                }
                // Saving in DB
                Domain.Document docEntity = new Domain.Document
                {
                    FileName = docModel.FileName,
                    StatusId = await GetdocStatus(DocumateDocStatus.IMPORTING),
                    QueueId = docModel.QueueId,
                    AwsJobId = docModel.AwsJobId,
                    NanoUploadResponse = docModel.NanoUploadResponse,
                    ContentType = docModel.ContentType,
                    OriginalFileName = docModel.OriginalFileName,
                    NanoRequestFileId = docModel.NanoRequestFileId,
                    PageCount = docModel.PageCount,
                    ModelId = queueModel.ModelId,
                    //StorageId = queueModel.StorageId,
                    UserMetaData = docModel.UserMetaData,
                    RawDataJSON = docModel.RawDataJSON
                };
                docEntity = await documentRepo.InsertOrUpdate(docEntity, false);
                if (docEntity!=null)
                {
                    //returnValue.Result = repoResult.data;
                    returnValue.IsSucessfull = true;
                    returnValue.Result.Id = docEntity.Id;
                    returnValue.Result.QueueId = docModel.QueueId;
                    returnValue.Message = "Document uploaded successfully.";
                }
                else
                {
                    logMessage = "Error in saving Doc Entity";
                    returnValue.Message = "Document could not be created";
                    returnValue.IsSucessfull = false;
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = false;
                returnValue.Message = "Document could not be created";
                _logger.LogError($"{ex.Message} - {logMessage}");
            }
            return returnValue;
        }

        public async Task<UploadDocReponse> CreateDoc(DocumentModel docModel)
        {

            UploadDocReponse returnValue = new UploadDocReponse();

            RepoResult repoResult = new RepoResult();
            string logMessage = $"Error in saving file. Doc id:{docModel.Id}, Q id:{docModel.QueueId}";
            string errMsgToSave = "";
            try
            {
                QueueModel qm = queueService.GetEntityById(docModel.QueueId);
                Account accEntity = accountRepo.GetEntities(x => x.Id == qm.AccountId).FirstOrDefault();
                docModel.OriginalFileName = docModel.FileName;
                docModel.FileName = GetUniqueFileName(docModel.FileName);
                docModel.PageCount = UpdatePageCount(docModel.MemStream, docModel.FileName);
                string filePath = Path.Combine(ProjectSettings.TempDocsFolder, docModel.FileName);
                // saving on S3
                S3FileModel uploadModel = new S3FileModel();
                uploadModel.BucketName = qm.S3BucketName;
                uploadModel.FileName = docModel.FileName; // $@"QId-{queueModel.Id}-{DateTime.UtcNow.Year}-{DateTime.UtcNow.Month}/{
                if (docModel.MemStream == null)
                    throw new InvalidDataException("Can't process document. Memory stream is empty.");

                uploadModel.FileMemoryStream = docModel.MemStream;
                logMessage = $"Uploading to S3- QueueId:{qm.Id} - FileName:{docModel.FileName}";
                await s3Service.UploadAsync(uploadModel);
                docModel.BucketName = qm.S3BucketName;

                if (qm.AiServiceSource == (int)EnumAiServiceSource.NANO)
                {

                    var client = new RestClient();
                    var request = new RestRequest($"{ProjectSettings.NanoApiEndPoint}OCR/Model/{qm.NanoModelId}/LabelFile/?async=true", Method.Post);
                    //request.AddHeader("authorization", "Basic " + ProjectSettings.NanoApiKey);
                    request.AddHeader("authorization", "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(ProjectSettings.NanoApiKey)));
                    request.AddHeader("accept", "Multipart/form-data");
                    request.AddFile("file", docModel.FileBytes, docModel.FileName);
                    RestResponse response = client.Execute(request);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        NanoGenericResponse nanoResponse = JsonConvert.DeserializeObject<NanoGenericResponse>(response.Content);
                        if (nanoResponse.message.ToLower() == "success" && nanoResponse.result.Count > 0)
                        {
                            docModel.AwsJobId = "";
                            docModel.NanoUploadResponse = response.Content;
                            docModel.NanoRequestFileId = nanoResponse.result.FirstOrDefault().request_file_id;
                            //docModel.CdnThumbnail = nanoResponse.result.FirstOrDefault().signed_urls
                            if (docModel.NanoUploadResponse == null)
                            {
                                _logger.LogDebug(response.ToString());
                                throw new InvalidDataException("Nano response null");
                            }
                            //JsonObjectAttribute a = new JsonObjectAttribute(nanoResponse.result.FirstOrDefault().signed_urls);
                        }
                        else
                        {
                            docModel.ProcessingRemarks = "Nano server returned error. File upload didn't work properly";
                        }
                        logMessage = $"Processing - FileName:{uploadModel.FileName} - NanoFileId: {docModel.AwsJobId} - Process completed";
                    }
                    else
                    {
                        logMessage = $"Processing - FileName:{uploadModel.FileName} - Nano upload Process Failed";
                    }
                }
                // Saving in DB
                Domain.Document docEntity = new Domain.Document
                {
                    FileName = docModel.FileName,
                    StatusId = await GetdocStatus(DocumateDocStatus.IMPORTING),
                    QueueId = docModel.QueueId,
                    AwsJobId = docModel.AwsJobId,
                    NanoUploadResponse = docModel.NanoUploadResponse,
                    ContentType = docModel.ContentType,
                    OriginalFileName = docModel.OriginalFileName,
                    NanoRequestFileId = docModel.NanoRequestFileId,
                    PageCount = docModel.PageCount,
                    ModelId = qm.ModelId,
                    //StorageId = qm.StorageId,
                    UserMetaData = docModel.UserMetaData
                };
                await documentRepo.InsertOrUpdate(docEntity, false);
                if (repoResult.success == true)
                {
                    //returnValue.Result = repoResult.data;
                    returnValue.IsSucessfull = true;
                    returnValue.Result.Id = repoResult.keyColId;
                    returnValue.Result.QueueId = docModel.QueueId;
                    returnValue.Message = "Document uploaded successfully.";
                }
                else
                {
                    logMessage = "Saves changes return 0";
                    returnValue.Message = "Document could not be created";
                    returnValue.IsSucessfull = false;
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = false;
                returnValue.Message = "Document could not be created";
                _logger.LogError($"{ex.Message} - {logMessage}");
            }
            return returnValue;
        }

        public async Task<ResponseModel> UpdateRawJsonAsync(string jobId, EnumAiServiceSource aiService)
        {
            ResponseModel returnValue = new ResponseModel();
            string logMessage = string.Empty;
            try
            {
                Domain.Document docEntity = documentRepo.GetEntities(x => x.AwsJobId == jobId && x.FlgFailed != true && x.FlgDeleted != true).ToList().FirstOrDefault();
                if (docEntity == null)
                    throw new KeyNotFoundException($"Document data could not be updated. Document does not exist. job id: {jobId}");
                if (docEntity.RawDataJSON != null) // already processed
                    return returnValue;
                GetDocumentAnalysisResponse response = await textractService.GetJobResultAsync(jobId);

                //var serviceScope = serviceScopeFactory.CreateScope();
                //var repo = serviceScope.ServiceProvider.GetService<IDocumentRepo>();              
                if (response.JobStatus == null)
                {
                    _logger.LogDebug($"Job status returned null- Doc id: {docEntity.Id}");
                    return returnValue;
                }
                if (response.JobStatus.Equals(AwsJobStatus.IN_PROGRESS.ToString()))
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "Job is still in progress";
                    return returnValue;
                }
                else if (response.JobStatus.Equals(AwsJobStatus.FAILED.ToString()))
                {
                    docEntity = await documentRepo.GetEntityById(docEntity.Id);
                    docEntity.ProcessingRemarks = response.StatusMessage;
                    docEntity.StatusId = await GetdocStatus(DocumateDocStatus.FAILED_EXPORT);
                    docEntity.FlgFailed = true;
                }
                else if (response.JobStatus.Equals(AwsJobStatus.SUCCEEDED.ToString()))
                {
                    docEntity = await documentRepo.GetEntityById(docEntity.Id);
                    docEntity.StatusId = await GetdocStatus(DocumateDocStatus.EXPORTING);
                    docEntity.EndProccessingDateTimeUTC = DateTime.Now.ToUniversalTime();
                    if (aiService == EnumAiServiceSource.OPENAI)
                    {
                        var lineItems = textractService.GetLines(response);
                        docEntity.RawDataJSON = string.Join(" ", lineItems);
                    }
                    else if(aiService == EnumAiServiceSource.AWS){
                        docEntity.RawDataJSON = JsonConvert.SerializeObject(response);
                        var lineItems = textractService.GetLines(response);
                        docEntity.RawDataJSON = string.Join(" ", lineItems);

                    }
                }
                else
                {
                    _logger.LogDebug($"Job status returned from AWS: {response.JobStatus}");
                }
                RepoResult repoResult = new RepoResult();
                await documentRepo.InsertOrUpdate(docEntity, false);
                if (repoResult.success == true)
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Document processed successfully.";
                }
                else
                {
                    logMessage = "Saves changes return 0";
                    returnValue.Message = "Document could not be updated";
                }
            }
            catch (Exception ex)
            {
                returnValue.Message = "Catch Exception document could not be updated";
                returnValue.IsSucessfull = false;
                _logger.LogError(ex.Message + " - " + logMessage);
            }
            return returnValue;
        }

        public async Task<ResponseModel> UpdateNanoJSON(int docId)  // This moethod should be removed when multipage works well.
        {
            //================================ CHANGE  FROM FILE BY FILE TO BATCH FILES ========================================
            //"https://app.nanonets.com/api/v2/Inferences/Model/{{model_id}}/ImageLevelInferences?start_day_interval={start_day}&current_batch_day={end_day}");
            ResponseModel returnValue = new ResponseModel();
            string logMessage = string.Empty;
            NanoGenericResponse nanoJobResponse = null;
            bool isDirty = false;
            try
            {
                Domain.Document docEntity = documentRepo.GetEntities(x => x.Id == docId && x.FlgFailed != true && x.FlgDeleted != true).ToList().FirstOrDefault();
                if (docEntity == null)
                    throw new KeyNotFoundException($"Document data could not be updated. Document does not exist. id: {docId}");
                if (docEntity.RawDataJSON != null) // already processed
                    return returnValue;

                var client = new RestClient($"{ProjectSettings.NanoApiEndPoint}Inferences/Model/{ProjectSettings.NanoModelId}/ImageLevelInferences/{docEntity.AwsJobId}");
                var request = new RestRequest();
                request.AddHeader("authorization", "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(ProjectSettings.NanoApiKey)));
                RestResponse response = client.Execute(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    returnValue.Message = $"Get Pridiction failed from nano server. StatusCode: {response.StatusCode.ToString()} . Doc: {docEntity.Id} - {docEntity.FileName} - {docEntity.NanoRequestFileId}";
                    throw new InvalidDataException(returnValue.Message);
                }
                nanoJobResponse = JsonConvert.DeserializeObject<NanoGenericResponse>(response.Content);
                logMessage = $"nano json data fetch success";
                if (nanoJobResponse == null)
                {
                    _logger.LogDebug($"Nano Job status returned null- Doc id: {docEntity.Id}");
                    return returnValue;
                }
                else if (nanoJobResponse.message.ToLower() == NanoFileStatus.SUCCESS.ToString().ToLower() && nanoJobResponse.result.Count > 0)
                {
                    docEntity = await documentRepo.GetEntityById(docEntity.Id);
                    if (docEntity.RawDataJSON == null) //&& nanoJobResponse.result.FirstOrDefault().is_moderated
                    {
                        docEntity.RawDataJSON = JsonConvert.SerializeObject(nanoJobResponse);
                        docEntity.StatusId = await GetdocStatus(DocumateDocStatus.EXPORTING);
                        docEntity.EndProccessingDateTimeUTC = DateTime.Now.ToUniversalTime();
                        isDirty = true;
                    }
                    //else if(nanoJobResponse.result.FirstOrDefault().is_moderated==false && docEntity.RawDataJSON == null 
                    //    && docEntity.StatusId != GetdocStatus(InnovoiceDocStatus.TO_REVIEW))
                    //{
                    //    docEntity.StatusId = GetdocStatus(InnovoiceDocStatus.TO_REVIEW);
                    //    isDirty = true;
                    //}
                }
                else if (nanoJobResponse.message.ToLower() == NanoFileStatus.SUCCESS.ToString().ToLower() && nanoJobResponse.result.Count == 0)
                {
                    docEntity = await documentRepo.GetEntityById(docEntity.Id);
                    docEntity.ProcessingRemarks = "File process ok but returned empty json";
                    docEntity.StatusId = await GetdocStatus(DocumateDocStatus.FAILED_EXPORT);
                    docEntity.FlgFailed = true;
                    isDirty = true;
                }
                else if (nanoJobResponse.message.ToLower() == NanoFileStatus.PENDING.ToString().ToLower() && nanoJobResponse.result.Count == 0)
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "Job is still in progress";
                    return returnValue;
                }
                else if (nanoJobResponse.message.ToLower() == NanoFileStatus.FAILURE.ToString().ToLower() && nanoJobResponse.result.Count == 0)
                {
                    docEntity = await documentRepo.GetEntityById(docEntity.Id);
                    docEntity.ProcessingRemarks = "Nanonet server responded with failure message";
                    docEntity.StatusId = await GetdocStatus(DocumateDocStatus.FAILED_EXPORT);
                    docEntity.FlgFailed = true;
                    isDirty = true;
                }
                if (isDirty)
                {
                    RepoResult repoResult = new RepoResult();
                    await documentRepo.InsertOrUpdate(docEntity, false);
                    if (repoResult.success == true)
                    {
                        returnValue.IsSucessfull = true;
                        returnValue.Message = "Document processed successfully.";
                    }
                    else
                    {
                        logMessage = "Saves changes return 0";
                        returnValue.Message = "Document could not be updated";
                    }
                }
            }
            catch (Exception ex)
            {
                //returnValue.Message = "Catch Exception document could not be updated";
                returnValue.IsSucessfull = false;
                _logger.LogError(ex.Message + " - " + logMessage);
            }
            return returnValue;
        }

        private ResponseModel GetNanoPageData(string pageId, string nanoModelId, string message)
        {
            ResponseModel returnValue = new ResponseModel();
            try
            {
                var client = new RestClient($"{ProjectSettings.NanoApiEndPoint}Inferences/Model/{nanoModelId}/ImageLevelInferences/{pageId}");
                var request = new RestRequest();
                //request.AddHeader("authorization", "Basic " + ProjectSettings.NanoApiKey);
                //string apikey = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(ProjectSettings.NanoApiKey));
                string apikey = "Basic MjA4NWU1OGUtYTU3YS0xMWVlLThjMzQtNDY1NDJlYzkyZTAyOg==";
                request.AddHeader("authorization", apikey);
                RestResponse response = client.Execute(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new InvalidDataException(message);
                }
                returnValue.IsSucessfull = true;
                returnValue.Result = response;
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = false;
                returnValue.Message = ex.Message;
                _logger.LogError(ex.Message);
            }
            return returnValue;
        }

        public async Task<ResponseModel> UpdateNanoJsonMultiPage(int docId)  // This moethod should be removed when multipage works well.
        {
            ResponseModel returnValue = new ResponseModel();
            string logMessage = string.Empty;
            NanoGenericResponse nanoPageContent = null;
            bool isDirty = false;
            try
            {
                Domain.Document docEntity = documentRepo.GetEntities(x => x.Id == docId && x.FlgFailed != true && x.FlgDeleted != true).ToList().FirstOrDefault();
                NanoModel nanoModel = await nanoModelRepo.GetEntityById(docEntity.ModelId);
                if (docEntity == null)
                    throw new KeyNotFoundException($"Document data could not be updated. Document does not exist. id: {docId}");
                if (docEntity.RawDataJSON != null) // already processed
                    return returnValue;

                //QueueModel queueModel = queueService.GetEntityById(docEntity.QueueId);               
                string message = $"Get Pridiction failed from nano server. Doc: {docEntity.Id} - {docEntity.FileName} - {docEntity.NanoRequestFileId}";

                // Preparing same object for page id stored in AwsJobId column
                NanoGenericResponse nanoPages = new NanoGenericResponse();
                if (string.IsNullOrEmpty(docEntity.AwsJobId) == false) //-------------- Case AwsJobId
                {
                    nanoPages.result = new List<NanoGenericResponseDetail>();
                    NanoGenericResponseDetail AwsPageIdObj = new NanoGenericResponseDetail() { message = "success", page = 0, id = docEntity.AwsJobId };
                    nanoPages.result.Add(AwsPageIdObj);

                }
                else
                {
                    nanoPages = JsonConvert.DeserializeObject<NanoGenericResponse>(docEntity.NanoUploadResponse);
                }
                NanoGenericResponse accumulativePageResult = null;
                int pageCount = 0;
                foreach (NanoGenericResponseDetail nanoPage in nanoPages.result)
                {
                    ResponseModel nanoPageData = GetNanoPageData(nanoPage.id, nanoModel.NanoModelId, message);
                    RestResponse prestResponseObj = (RestResponse)nanoPageData.Result;
                    if (nanoPageData.IsSucessfull == false || prestResponseObj.StatusCode != HttpStatusCode.OK)
                        throw new InvalidDataException(message + " - " + returnValue.Message);

                    logMessage = $"Error in deserialization of Nano Response Content";
                    nanoPageContent = JsonConvert.DeserializeObject<NanoGenericResponse>(prestResponseObj.Content);
                    if (nanoPageContent == null)
                    {
                        message = $"Nano Job status returned null. Doc: {docEntity.Id} - {docEntity.FileName} - {docEntity.NanoRequestFileId}";
                        throw new InvalidDataException(message);
                    }
                    NanoGenericResponseDetail pageResult = nanoPageContent.result[0];
                    if (nanoPage.page == 0)
                        accumulativePageResult = nanoPageContent;
                    else
                        accumulativePageResult.result[0].prediction.AddRange(pageResult.prediction);
                    pageCount++;
                    logMessage = $"nano json data fetch success";
                    if (nanoPageContent == null)
                    {
                        _logger.LogDebug($"Nano Job status returned null- Doc id: {docEntity.Id}");
                        return returnValue;
                    }
                }
                //------------ 
                if (nanoPageContent.message.ToLower() == NanoFileStatus.SUCCESS.ToString().ToLower() && nanoPageContent.result.Count > 0)
                {
                    docEntity = await documentRepo.GetEntityById(docEntity.Id);
                    if (docEntity.RawDataJSON == null) //&& nanoJobResponse.result.FirstOrDefault().is_moderated
                    {
                        docEntity.RawDataJSON = JsonConvert.SerializeObject(accumulativePageResult);
                        docEntity.StatusId = await GetdocStatus(DocumateDocStatus.EXPORTING);
                        docEntity.EndProccessingDateTimeUTC = DateTime.Now.ToUniversalTime();
                        isDirty = true;
                    }
                }
                else if (nanoPageContent.message.ToLower() == NanoFileStatus.SUCCESS.ToString().ToLower() && nanoPageContent.result.Count == 0)
                {
                    docEntity = await documentRepo.GetEntityById(docEntity.Id);
                    docEntity.ProcessingRemarks = "File process ok but returned empty json";
                    docEntity.StatusId = await GetdocStatus(DocumateDocStatus.FAILED_EXPORT);
                    docEntity.FlgFailed = true;
                    isDirty = true;
                }
                else if (nanoPageContent.message.ToLower() == NanoFileStatus.PENDING.ToString().ToLower() && nanoPageContent.result.Count == 0)
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "Job is still in progress";
                    return returnValue;
                }
                else if (nanoPageContent.message.ToLower() == NanoFileStatus.FAILURE.ToString().ToLower() && nanoPageContent.result.Count == 0)
                {
                    docEntity =await documentRepo.GetEntityById(docEntity.Id);
                    docEntity.ProcessingRemarks = "Nanonet server responded with failure message";
                    docEntity.StatusId = await GetdocStatus(DocumateDocStatus.FAILED_EXPORT);
                    docEntity.FlgFailed = true;
                    isDirty = true;
                }

                //------------  SAVING DOCUMENT
                if (isDirty)
                {
                    RepoResult repoResult = new RepoResult();
                    await documentRepo.InsertOrUpdate(docEntity, false);
                    if (repoResult.success == true)
                    {
                        returnValue.IsSucessfull = true;
                        returnValue.Message = "Document processed successfully.";
                    }
                    else
                    {
                        logMessage = "Saves changes return 0";
                        returnValue.Message = "Document could not be updated";
                    }
                }
            }
            catch (Exception ex)
            {
                //returnValue.Message = "Catch Exception document could not be updated";
                returnValue.IsSucessfull = false;
                _logger.LogError(ex.Message + " - " + logMessage);
            }
            return returnValue;
        }

        public async Task<ResponseModel> UpdateNanoBulkJsonAsync()
        {
            ResponseModel returnValue = new ResponseModel();
            string logMessage = string.Empty;
            try
            {
                List<Domain.Document> docsToProcess = documentRepo.GetEntities(x => x.RawDataJSON == null && x.FlgFailed != true && x.FlgDeleted != true && x.ModelId != null).OrderBy(x => x.CreatedOnUtc).ToList();
                if (docsToProcess == null || docsToProcess.Count < 1)
                {
                    logMessage = "No docs found to proces in method: UpdateBulkJsonMultiPageAsync";
                    throw new InvalidDataException(logMessage);
                }
                List<Domain.Document> modelsList = docsToProcess.GroupBy(x => x.ModelId).Select(x => x.FirstOrDefault()).ToList();
                foreach (Domain.Document docModel in modelsList) // Iterate the whole process as number of time as number of different models found in the doc list
                {
                    var nanoModelEntity = await nanoModelRepo.GetEntityById(docModel.ModelId);
                    string nanoModelId = nanoModelEntity.NanoModelId;
                    int startDay = 0, endDay = 0;
                    TimeSpan span = docsToProcess.FirstOrDefault().CreatedOnUtc.Subtract(DateTime.Parse("01-Jan-1970"));
                    startDay = span.Days;
                    span = docsToProcess.LastOrDefault().CreatedOnUtc.AddHours(1.0).Subtract(DateTime.Parse("01-Jan-1970"));
                    endDay = span.Days;
                    logMessage = "Error in HTTP call to Nano server";
                    var client = new RestClient($"{ProjectSettings.NanoApiEndPoint}Inferences/Model/{nanoModelId}/ImageLevelInferences?start_day_interval={startDay}&current_batch_day={endDay}");
                    var request = new RestRequest();
                    request.AddHeader("authorization", "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(ProjectSettings.NanoApiKey)));
                    RestResponse response = client.Execute(request);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        returnValue.Message = $"Get Pridiction failed from nano server. StatusCode: {response.StatusCode.ToString()} - Days Range: {startDay}-{endDay}";
                        throw new InvalidDataException(returnValue.Message);
                    }
                    //---
                    bool isDirty = false;
                    logMessage = "Error while deserializing nano response";
                    NanoGenericResponse preditionsData = JsonConvert.DeserializeObject<NanoGenericResponse>(response.Content);
                    if (preditionsData == null || preditionsData.message.ToLower() != NanoFileStatus.SUCCESS.ToString() || preditionsData.result.Count < 1)
                    {
                        returnValue.Message = $"Pridiction data is either empty or not successfull. response message: {preditionsData.message} - Result Count: {preditionsData.result.Count}";
                        throw new InvalidDataException(returnValue.Message);
                    }
                    // Replace docSave with doc if its safe to be used.
                    foreach (Domain.Document doc in docsToProcess)
                    {
                        if (doc.AwsJobId.Length < 36 || doc.AwsJobId.Contains("{") == false) // its either Aws Job id or its nanoResponse.
                        {
                            _logger.LogInformation($"Not a NANO document  Doc: {doc.Id}- {doc.FileName}");
                            continue;
                        }
                        if (doc.RawDataJSON == null)
                        {
                            _logger.LogInformation($"Document was already processed. continuing to next doc. . Doc: {doc.Id}- {doc.FileName}- Method: UpdateBulkJsonMultiPageAsync");
                            continue;
                        }
                        Domain.Document docToSave = doc;
                        logMessage = $"Error in deserialization of AwsJobId";
                        NanoGenericResponse oldUploadResponse = JsonConvert.DeserializeObject<NanoGenericResponse>(doc.AwsJobId);
                        NanoGenericResponseDetail accumulativePageResult = null;
                        bool isPredictionComplete = true;
                        int pageCount = 0;
                        foreach (NanoGenericResponseDetail page in oldUploadResponse.result)
                        {
                            // Find the id of the page in the preditionsData
                            logMessage = $"Error in finding page id in received prediction data";
                            NanoGenericResponseDetail pageResult = preditionsData.result.Find(x => x.id == page.id);
                            if (pageResult == null)
                            {
                                _logger.LogInformation($"Page result not received yet.  Doc: {doc.Id}- {doc.FileName}");
                                isPredictionComplete = false;
                                break;
                            }
                            if (pageResult.message.ToLower() == NanoFileStatus.SUCCESS.ToString().ToLower() && pageResult.prediction.Count > 0)
                            {
                                pageCount++;
                                if (pageResult.page == 0)
                                    accumulativePageResult = pageResult;
                                else
                                    accumulativePageResult.prediction.AddRange(pageResult.prediction);
                                if (accumulativePageResult != null) //&& nanoJobResponse.result.FirstOrDefault().is_moderated
                                {
                                    docToSave.RawDataJSON = JsonConvert.SerializeObject(accumulativePageResult);
                                    docToSave.StatusId = await GetdocStatus(DocumateDocStatus.EXPORTING);
                                    docToSave.EndProccessingDateTimeUTC = DateTime.Now.ToUniversalTime();
                                    docToSave.IsModerated = accumulativePageResult.is_moderated;
                                    docToSave.PageCount = pageCount;
                                    isDirty = true;
                                }
                            }
                            else if (pageResult.message.ToLower() == NanoFileStatus.SUCCESS.ToString().ToLower() && pageResult.prediction.Count == 0)
                            {
                                docToSave.ProcessingRemarks = "Prediction data is empty.";
                                docToSave.StatusId = await GetdocStatus(DocumateDocStatus.FAILED_EXPORT);
                                docToSave.FlgFailed = true;
                                isDirty = true;
                            }
                            else if (pageResult.message.ToLower() == NanoFileStatus.PENDING.ToString().ToLower())
                            {
                                returnValue.IsSucessfull = false;
                                returnValue.Message = "Job is still in progress";
                                return returnValue;
                            }
                            else if (pageResult.message.ToLower() == NanoFileStatus.FAILURE.ToString().ToLower())
                            {
                                docToSave.ProcessingRemarks = "Nanonet server responded with failure message";
                                docToSave.StatusId = await GetdocStatus(DocumateDocStatus.FAILED_EXPORT);
                                docToSave.FlgFailed = true;
                                isDirty = true;
                            }
                        }// ForEach pageResult
                        if (isDirty && isPredictionComplete && accumulativePageResult != null)
                        {
                            RepoResult repoResult = new RepoResult();
                            Domain.Document docEntity = documentRepo.GetEntities(x => x.Id == doc.Id).ToList().FirstOrDefault();
                            docEntity.CopyPropertyValues(docToSave);
                            await documentRepo.InsertOrUpdate(docEntity, false);
                            if (repoResult.success == true)
                            {
                                returnValue.IsSucessfull = true;
                                returnValue.Message = "Document processed successfully.";
                            }
                            else
                            {
                                logMessage = "Saves changes return 0";
                                returnValue.Message = "Document could not be updated";
                            }
                        }
                    }// foreach docsToProcess
                }
            }
            catch (Exception ex)
            {
                returnValue.Message = ex.Message;
                returnValue.IsSucessfull = false;
                _logger.LogError(ex.Message + " - " + logMessage);
            }
            return returnValue;
        }

        public async Task<ResponseModel> UpdateAnnotation(int docId, string annotationData)
        {
            ResponseModel returnValue = new ResponseModel();
            string logMessage = string.Empty;
            try
            {
                Domain.Document docEntity = await documentRepo.GetEntityById(docId);
                if (docEntity == null)
                    throw new KeyNotFoundException($"Document data could not be updated. Document does not exist. docId={docId}");
                docEntity.ProcessedDataJSON = annotationData;
                docEntity.UserAnnotation = annotationData;
                docEntity.StatusId = await GetdocStatus(DocumateDocStatus.EXPORTED);
                RepoResult repoResult = new RepoResult();
                await documentRepo.InsertOrUpdate(docEntity, false);
                if (repoResult.success == true)
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Annotation updated successfully.";
                }
                else
                    returnValue.Message = "Annotation could not be updated";
            }
            catch (Exception ex)
            {
                returnValue.Message = "Catch Exception: Annotation could not be updated";
                returnValue.IsSucessfull = false;
                _logger.LogError(ex.Message + " - " + logMessage);
            }
            return returnValue;
        }

        public async Task<ResponseModel> UpdateTemplate(int docId, int templateId)
        {
            ResponseModel returnValue = new ResponseModel();
            string logMessage = string.Empty;
            try
            {
                Domain.Document docEntity = await documentRepo.GetEntityById(docId);
                if (docEntity == null)
                    throw new KeyNotFoundException($"Document data could not be updated. Document does not exist. docId={docId}");
                docEntity.TemplateId = templateId;
                RepoResult repoResult = new RepoResult();
                await documentRepo.InsertOrUpdate(docEntity, false);
                if (repoResult.success == true)
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Template id updated successfully.";
                }
                else
                    returnValue.Message = "Template id could not be updated";
            }
            catch (Exception ex)
            {
                returnValue.Message = "Catch Exception: Annotation could not be updated";
                returnValue.IsSucessfull = false;
                _logger.LogError(ex.Message + " - " + logMessage);
            }
            return returnValue;
        }

        public async Task<ResponseModel> ProcessNanoDataV2(int docId)
        {
            _logger.LogDebug("In ProcessNanoDataV2");
            ResponseModel returnValue = new ResponseModel();
            DocSchema_In schemaIn = new DocSchema_In();
            DocSchema_Out schemaOut = new DocSchema_Out();
            string RawDataJSON = string.Empty;
            bool isReadyToExport = true;
            Domain.Document docEntity = null;
            docEntity = await documentRepo.GetEntityById(docId);
            if (DebugRawDataJSON != null)
                RawDataJSON = DebugRawDataJSON;
            else
            {
                RawDataJSON = docEntity.RawDataJSON;
                schemaOut.doc_id = docId;
            }
            QueueModel queueModel = queueService.GetEntityById(DebugRawDataJSON == null ? docEntity.QueueId : DebugQueueId);
            try
            {
                NanoPredictionResponse nanoResponse = JsonConvert.DeserializeObject<NanoPredictionResponse>(RawDataJSON);
                schemaIn = JsonConvert.DeserializeObject<DocSchema_In>(queueModel.SchemaJSON);
                var headerSections = schemaIn.content.Where(x => x.category == "section" && x.id != "line_items_section").ToList();
                foreach (DocSchemaSection_In section in headerSections)
                {
                    DocSchemaSection_Out newSection = new DocSchemaSection_Out();
                    newSection.children = new List<ChildNode_Out>();
                    newSection.schema_id = section.id;
                    newSection.category = section.category;
                    foreach (DocSchemaDataPoint_In dataPoint in section.children)// Data point at this level are available online non Line Item Sections
                    {
                        ChildNode_Out newDataPoint = new ChildNode_Out();
                        newDataPoint.schema_id = dataPoint.id;
                        newDataPoint.category = dataPoint.category;
                        newDataPoint.type = dataPoint.type;
                        newDataPoint.content = new DataPointContent();

                        // finding data point in header prediction items
                        NanoPrediction nanoHeaderDPs = nanoResponse.result[0].moderated_boxes.Where(x => x.label == dataPoint.nano_label).FirstOrDefault();
                        if (nanoHeaderDPs == null)
                            nanoHeaderDPs = nanoResponse.result[0].prediction.Where(x => x.label == dataPoint.nano_label).FirstOrDefault();
                        // If there are multiple values of the same data label then find take the one with highest score.
                        //TODO getting hiehgts scored label
                        if (nanoHeaderDPs != null) //found nano label
                        {
                            newDataPoint.content.value = nanoHeaderDPs.ocr_text;
                            newDataPoint.content.confidence = nanoHeaderDPs.score;
                            if (dataPoint.score_threshold > 0 && nanoHeaderDPs.score < dataPoint.score_threshold)
                            {
                                //isReadyToExport = false;
                                docEntity.Description = $"{dataPoint.nano_label} Score is less than Threshold";
                            }
                        }
                        if (newDataPoint.content.value == null && dataPoint.constraints.required == true ||
                            newDataPoint.content.value != null && newDataPoint.content.value.Trim() == string.Empty && dataPoint.constraints.required == true)
                        {
                            if (queueModel.AutomationLevel != (int)AutomationLevel.ALWAYS) // if automation level = Always then let it go
                            {
                                //isReadyToExport = false;
                                docEntity.Description = $"{dataPoint.nano_label}: Required field not found in nano RawJSON";
                            }
                            // If datapoint has contraint Rqueired but there is no value to store then store default values.
                            if (dataPoint.constraints.required == true && string.IsNullOrEmpty(newDataPoint.content.value))
                            {
                                if (dataPoint.type == "date")
                                    newDataPoint.content.value = DateTime.Now.ToString(dataPoint.format);
                                else if (dataPoint.type == "number")
                                {
                                    Random rnd = new Random();
                                    newDataPoint.content.value = rnd.Next(1000, 9000).ToString();
                                }
                                else if (dataPoint.type == "string")
                                    newDataPoint.content.value = "Tmp" + Guid.NewGuid().ToString().Substring(0, 6);
                            }
                            newDataPoint.content.value = DpConstraint(dataPoint, ref newDataPoint);
                            newSection.children.Add(newDataPoint);
                            continue;
                        }
                        newSection.children.Add(newDataPoint);
                    }
                    schemaOut.content.Add(newSection);
                }
                // Table data processing start here
                DocSchemaSection_In lineItemsSection = schemaIn.content.Where(x => x.id == "line_items_section" && x.category == "section").FirstOrDefault();
                List<NanoPrediction> tablesList = nanoResponse.result[0].prediction.Where(x => x.type == "table").ToList();
                if (tablesList == null)
                    tablesList = nanoResponse.result[0].moderated_boxes.Where(x => x.type == "table").ToList();
                if (tablesList != null && tablesList.Count > 0)
                {
                    NanoTable accumoulativeTable = null;
                    int tableCount = 0;
                    foreach (NanoPrediction table in tablesList)
                    {
                        NanoTable nanoTable = new NanoTable(table);
                        if (tableCount == 0)
                            accumoulativeTable = nanoTable;
                        else
                        {
                            if (accumoulativeTable.Rows[0].Cells.Count == nanoTable.Rows[0].Cells.Count)
                                accumoulativeTable.Rows.AddRange(nanoTable.Rows);
                        }
                        tableCount++;
                        if ((accumoulativeTable == null || accumoulativeTable.Rows == null || accumoulativeTable.Rows.Count == 0) && lineItemsSection != null)
                            docEntity.Description = "Table not found in Nano return";
                    }
                    if (lineItemsSection != null && accumoulativeTable != null && accumoulativeTable.Rows != null && accumoulativeTable.Rows.Count > 0) // tables section exists in the schema_in.
                    {
                        ChildNode_In tuple = lineItemsSection.children.Find(x => x.category == "tuple");
                        // Adding Table section 
                        DocSchemaSection_Out newTableSection = new DocSchemaSection_Out();
                        newTableSection.children = new List<ChildNode_Out>();
                        newTableSection.schema_id = lineItemsSection.id;
                        newTableSection.category = lineItemsSection.category;
                        // Adding Table Rows
                        foreach (NanoRow nRow in accumoulativeTable.Rows)
                        {
                            //TODO- Row should not be null. temp solution applied
                            if (nRow == null)
                                continue;
                            ChildNode_Out newTuple = new ChildNode_Out(); // create new Tuple on every new row.
                            newTuple.children = new List<ChildNode_Out>();
                            foreach (ChildNode_In sourceDataPoint in tuple.children)
                            {
                                ChildNode_Out newTupalDataPoint = new ChildNode_Out()
                                {
                                    schema_id = sourceDataPoint.id,
                                    category = sourceDataPoint.category,
                                    type = sourceDataPoint.type,
                                };
                                NanoCell nCell = nRow.Cells.Find(x => x.label == sourceDataPoint.nano_label);
                                newTupalDataPoint.content = new DataPointContent { value = null, confidence = null };
                                if (nCell != null)
                                {
                                    newTupalDataPoint.content.value = nCell.text;
                                    newTupalDataPoint.content.value = DpConstraint(sourceDataPoint, ref newTupalDataPoint);
                                    newTupalDataPoint.content.confidence = nCell.score;
                                }
                                newTuple.children.Add(newTupalDataPoint);
                            }
                            newTableSection.children.Add(newTuple);
                        }
                        schemaOut.content.Add(newTableSection);
                    }
                }
                else
                {
                    if (lineItemsSection != null) docEntity.Description = "Table not found in Nano return";
                }
                if (DebugRawDataJSON == null)
                {

                    if (isReadyToExport)
                    {
                        docEntity.StatusId = await GetdocStatus(DocumateDocStatus.EXPORTED);
                        docEntity.ProcessedDataJSON = JsonConvert.SerializeObject(schemaOut);
                        docEntity.Description = null;
                    }
                    else
                        docEntity.StatusId = await GetdocStatus(DocumateDocStatus.TO_REVIEW);
                    RepoResult repoResult = new RepoResult();
                    var checkDoc = documentRepo.GetEntities(x => x.Id == docEntity.Id).FirstOrDefault();
                    if (checkDoc.FlgWebbookCalled == true)
                    {
                        returnValue.IsSucessfull = false;
                        returnValue.Message = $"Process Document stopped because webhook already called: {docId}";
                        _logger.LogInformation(returnValue.Message);
                    }
                    await documentRepo.InsertOrUpdate(docEntity, false);
                    if (repoResult.success == true)
                    {
                        returnValue.IsSucessfull = true;
                        _logger.LogDebug($"Process Document successful for Doc id: {docId}");
                        WebhookCallToClient(docEntity);
                    }
                    else
                    {
                        returnValue.Message = $"Process Document failed for Doc id: {docId}";
                        _logger.LogError(returnValue.Message);
                    }
                }
                returnValue.Result = schemaOut;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Processing Document failed for Doc id: {docId} --  {ex.Message}");
                if (DebugRawDataJSON == null)
                {
                    docEntity.NoOfRetries++;
                    if (docEntity.NoOfRetries >= 5)
                    {

                        docEntity.ProcessingRemarks = $"Doc id: {docId}: System could not extract data from the document.";
                        docEntity.FailedException = $" Try no:{docEntity.NoOfRetries} - Error Msg: {ex.Message}";
                        docEntity.FlgFailed = true;
                    }
                    RepoResult repoResult = new RepoResult();
                    await documentRepo.InsertOrUpdate(docEntity, false);
                }
            }
            return returnValue;
        } // 2nd algorithm

        public DocumateDocsListResponse GetDocList(int? page, Expression<Func<DocumateDocListInfo, bool>> where = null)
        {
            DocumateDocsListResponse returnValue = new DocumateDocsListResponse();
            int pageSize = 100;
            page ??= 1;
            int skip = pageSize * ((int)page - 1);

            var query = from doc in documentRepo.Table
                        join docStatus in sysDocStatusRepo.Table on doc.StatusId equals docStatus.Id
                        where doc.FlgDeleted == false
                        select new DocumateDocListInfo
                        {
                            Id = doc.Id,
                            FileName = doc.FileName,
                            Status = (DocumateDocStatus)docStatus.Order,
                            QueueId = doc.QueueId
                        };
            if (where != null)
                query = query.Where(where);

            query = query.OrderBy(x => x.Id).Skip(skip).Take(pageSize);
            List<DocumateDocListInfo> docModel = query.ToList();

            if (docModel != null && docModel.Count > 0)
            {
                returnValue.IsSucessfull = true;
                returnValue.Results = docModel.ToArray();
            }
            else
            {
                returnValue.IsSucessfull = false;
                returnValue.Results = Array.Empty<DocumateDocListInfo>();
            }

            return returnValue;
        }

        public async Task<DocumateDocsListResponse> GetDocList(int? page, string ids)
        {
            DocumateDocsListResponse returnValue = new DocumateDocsListResponse();
            int pageSize = 100;
            page ??= 1;
            try
            {
                int skip = pageSize * ((int)page - 1);

                var query = from doc in documentRepo.Table
                            join docStatus in sysDocStatusRepo.Table on doc.StatusId equals docStatus.Id
                            where doc.FlgDeleted == false && doc.CreatedOnUtc >= DateTime.Now.AddDays(-30)
                            select new DocumateDocListInfo
                            {
                                Id = doc.Id,
                                FileName = doc.FileName,
                                Status = (DocumateDocStatus)docStatus.Order,
                                QueueId = doc.QueueId
                            };
                //query = query.OrderBy(x => x.Id).Skip(skip).Take(pageSize);

                List<DocumateDocListInfo> docModel = await query.ToListAsync();
                List<int> idList = new List<int>();

                if (!string.IsNullOrEmpty(ids))
                {
                    ids = ids.TrimEnd(',');                   
                    if (ids.IndexOf(",") > 1)
                    {
                        idList = ids.Split(",")
                       .Where(s => !string.IsNullOrWhiteSpace(s))
                       .Select(s => int.Parse(s.Trim()))
                       .ToList();
                        docModel = docModel.Where(x => idList.Any(y => y == x.Id)).ToList();
                        docModel.OrderBy(x => x.Id).Skip(skip).Take(pageSize);
                    }
                    else
                        docModel = docModel.Where(x => x.Id == ids.ToInt32()).ToList();
                }
                returnValue.Results = docModel.ToArray();
                returnValue.IsSucessfull = true;
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = false;
                returnValue.Results = Array.Empty<DocumateDocListInfo>();
                _logger.LogError($"Error in GetDocList service method - ex.msg: {ex.Message}");
            }

            return returnValue;
        }

        public async Task<string> GetOriginalFileURL(int id)
        {
            string returnValue = string.Empty;
            Domain.Document docEntity = await GetEntityById(id);
            QueueModel qm = queueService.GetEntityById(docEntity.QueueId);

            S3FileModel s3Fileodel = new S3FileModel();
            s3Fileodel.FileName = docEntity.FileName;
            s3Fileodel.BucketName = qm.S3BucketName;
            s3Fileodel.FilePath = docEntity.FileName; //TODO: Replace path property
            returnValue = await s3Service.GetSignedUrl(s3Fileodel);

            return returnValue;
        }

        public async Task<Domain.Document> GetEntityById(int id)
        {
            Domain.Document docEntity = await documentRepo.GetEntityById(id);
            return docEntity;
        }

        public async Task<DocumentModel> GetModelById(int id)
        {
            DocumentModel docModel = new DocumentModel();
            Domain.Document docEntity = await documentRepo.GetEntities(x=> x.Id==id).FirstOrDefaultAsync();
            docModel.CopyPropertyValues(docEntity);
            Domain.Queue qm  = queueService.GetEntityById(docModel.QueueId);

            return docModel;
        }

        public async Task<ResponseModel> ReProcessDocument(int id)
        {
            ResponseModel returnValue = new ResponseModel();
            try
            {
                Domain.Document docEntity = await documentRepo.GetEntityById(id);
                if (docEntity == null)
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "Invalid Document Id";
                    return returnValue;
                }
                if (docEntity.UserAnnotation == null)
                {
                    var random = new Random();
                    int zCount = random.Next(1, 10); // Generate a random number between 1 and 10
                    string zString = new string('z', zCount);
                    docEntity.RawDataJSON = (docEntity.RawDataJSON ?? string.Empty) + "  " + zString;
                    docEntity.ProcessedDataJSON = null;
                    docEntity.StartProccessingDateTimeUTC = null;
                    docEntity.EndProccessingDateTimeUTC = null;
                    docEntity.FailedException = null;
                    docEntity.FlgFailed = false;
                    docEntity.ProcessingRemarks = null;
                    docEntity.Description = null;
                    docEntity.NoOfRetries = 0;
                    docEntity.StatusId = (int)DocumateDocStatus.IMPORTING;
                    docEntity.FlgWebbookCalled = false;
                }
                else
                {
                    docEntity.ProcessedDataJSON = docEntity.UserAnnotation;
                    docEntity.FailedException = null;
                    docEntity.FlgFailed = false;
                    docEntity.NoOfRetries = 0;
                }
                RepoResult repoResult = new RepoResult();
                await documentRepo.InsertOrUpdate(docEntity, false);
                if (repoResult.success)
                    returnValue.IsSucessfull = true;
                else
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Message = repoResult.successMsg;
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = false;
                returnValue.Message = $"Document coud not be re-processed- Error:{ex.Message}";
                _logger.LogError(returnValue.Message + "-" + id);
            }
            //await ScheduleProcessAllDocs();
            return returnValue;
        }

        public async Task<ResponseModel> ReUploadDocument(int id)
        {
            ResponseModel returnValue = new ResponseModel();
            DocumentModel docModel = new DocumentModel();
            string logMessage = "";
            try
            {
                docModel = await GetModelById(id);
                QueueModel qm = queueService.GetEntityById(docModel.QueueId);
                Account accEntity = accountRepo.GetEntities(x => x.Id == qm.AccountId).FirstOrDefault();
                // downlaoding from S3
                S3FileModel fileModel = new S3FileModel();
                fileModel.BucketName = qm.S3BucketName;
                fileModel.FileName = docModel.FileName;

                fileModel.FileMemoryStream = docModel.MemStream;
                logMessage = $"Downloading from S3- QueueId:{qm.Id} - FileName:{docModel.FileName}";
                docModel.MemStream = await s3Service.DownloadFile(fileModel);
                // writing file to local file system for test
                //const string fileName = "test.pdf";
                //using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                //{
                //    // Write the data to the file, byte by byte.
                //    for (int i = 0; i < docModel.FileBytes.Length; i++)
                //    {
                //        fileStream.WriteByte(docModel.FileBytes[i]);
                //    }
                //    // Set the stream position to the beginning of the file.
                //    fileStream.Seek(0, SeekOrigin.Begin);
                //}
                //return null;
                // PDF regenration
                PdfDocument pdf = PdfReader.Open(docModel.MemStream, PdfDocumentOpenMode.Modify);
                //pdf.Save("test-rotated.pdf");
                pdf.Save(docModel.MemStream, true);
                docModel.FileBytes = docModel.MemStream.ToArray();

                // ---------------------- Uploading to Nano server
                var client = new RestClient();
                var request = new RestRequest($"{ProjectSettings.NanoApiEndPoint}OCR/Model/{qm.NanoModelId}/LabelFile/?async=true", Method.Post);
                //request.AddHeader("authorization", "Basic " + ProjectSettings.NanoApiKey);
                //string apikey = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(ProjectSettings.NanoApiKey));
                string apikey = "Basic MjA4NWU1OGUtYTU3YS0xMWVlLThjMzQtNDY1NDJlYzkyZTAyOg==";
                request.AddHeader("authorization", apikey);
                request.AddHeader("accept", "Multipart/form-data");
                request.AddFile("file", docModel.FileBytes, docModel.FileName);
                RestResponse response = client.Execute(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    NanoGenericResponse nanoResponse = JsonConvert.DeserializeObject<NanoGenericResponse>(response.Content);
                    if (nanoResponse.message.ToLower() == "success" && nanoResponse.result.Count > 0)
                    {
                        docModel.AwsJobId = "";
                        docModel.NanoUploadResponse = response.Content;
                        docModel.NanoRequestFileId = nanoResponse.result.FirstOrDefault().request_file_id;
                        //docModel.CdnThumbnail = nanoResponse.result.FirstOrDefault().signed_urls
                        if (docModel.NanoUploadResponse == null)
                        {
                            _logger.LogDebug(response.ToString());
                            throw new InvalidDataException("Nano response null");
                        }
                    }
                    else
                    {
                        _logger.LogError("Nano server returned error. File upload didn't work properly" + docModel.Id);
                        returnValue.Message = "Ai Server returned error while re-uploading the file.";
                        returnValue.IsSucessfull = false;
                        return returnValue;
                    }
                    logMessage = $"Processing - FileName:{fileModel.FileName} - NanoFileId: {docModel.AwsJobId} - Process completed";
                }
                // Saving in DB
                Domain.Document docEntity = await documentRepo.GetEntityById(id);
                if (docEntity == null)
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "Invalid Document Id";
                    return returnValue;
                }
                docEntity.AwsJobId = docModel.AwsJobId;
                docEntity.NanoUploadResponse = docModel.NanoUploadResponse;
                docEntity.NanoRequestFileId = docModel.NanoRequestFileId;

                RepoResult repoResult = new RepoResult();
                await documentRepo.InsertOrUpdate(docEntity, false);
                if (repoResult.success)
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Re-upload process successsful. document is being re-processed now.";
                }
                else
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "The document couldn't be processed properly.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Document re-upload process failed" + docModel.Id);
            }
            return returnValue;
        }

        private void ApplyPageRules(ref TextractDocument document, TemplateRulesModel ruleModel)
        {
            var pageRule = ruleModel.rules.Where(x => x.category == "page").FirstOrDefault();
            if (pageRule.page_actions.remove_pages != null && pageRule.page_actions.remove_pages.Count() > 0)
            {
                foreach (string page in pageRule.page_actions.remove_pages)
                {
                    int pageNo = -1;
                    if (page == "last")
                        pageNo = document.Pages.Count - 1;
                    else if (page == "first")
                        pageNo = 0;
                    else if (page.ToInt() > 0)
                        pageNo = page.ToInt();
                    if (pageNo > 0)
                        document.Pages.RemoveAt(pageNo); //Todo: Remove all the respective blocks from Forms/Tables/BlockMap etc.
                }
            }

            //Todo: Fix this Rule type.
            if (!string.IsNullOrEmpty(pageRule.page_actions.cut_off_block_value))
            {
                int LineIndex = 0;
                int pageIndex = 0;
                //int wordIndex = 0;
                string wordId = string.Empty;
                foreach (var pg in document.Pages)
                {
                    //blockIndex== the page index where line block found.
                    LineIndex = pg.Blocks.FindIndex(x => x.BlockType == BlockType.LINE && x.Text == pageRule.page_actions.cut_off_block_value);
                    if (LineIndex > -1)
                    {
                        pageIndex = pg.Blocks[LineIndex].Page - 1;
                        wordId = pg.Blocks[LineIndex].Relationships[0].Ids[0];
                        //wordIndex = pg.Blocks.FindIndex(x => x.BlockType == BlockType.WORD && x.Id == wordId);
                        if (LineIndex > -1)
                            break;
                    }
                }
                // Removing pages ahead of the page where block was found
                for (int i = pageIndex + 1; i <= document.Pages.Count - 1; i++)
                {
                    document.Pages.RemoveAt(i); // remove all the pages exist after the page where block was found.
                }
                // Removing Lin Block
                bool isFound = false;
                foreach (var block in document.Pages[pageIndex].Blocks)
                {
                    if (block.BlockType == BlockType.LINE && block.Text == pageRule.page_actions.cut_off_block_value)
                        isFound = true;
                    if (block.BlockType == BlockType.LINE && isFound)
                        document.Pages[pageIndex].Blocks.Remove(block);
                }
                // Removing Word Blocks
                isFound = false;
                foreach (var block in document.Pages[pageIndex].Blocks)
                {
                    if (block.BlockType == BlockType.WORD && block.Id == wordId)
                        isFound = true;
                    if (block.BlockType == BlockType.WORD && isFound)
                        document.Pages[pageIndex].Blocks.Remove(block);
                }
            }
        }

        private string DpConstraint(DocSchemaDataPoint_In dataPoint, ref ChildNode_Out newDataPoint)
        {
            string returnValue = "0";
            if (dataPoint.type == "number" && dataPoint.constraints.length != null && dataPoint.constraints.length.max != null)
            {
                var cleanNumber = ParseNumber(newDataPoint.content.value, typeof(double));
                double dblValue = 0.0;
                if (string.IsNullOrEmpty(cleanNumber))
                    returnValue = "0";
                else if (double.TryParse(cleanNumber, out dblValue) && dblValue > dataPoint.constraints.length.max)
                    returnValue = "0";
                else
                    returnValue = cleanNumber;
            }
            else
                returnValue = newDataPoint.content.value;
            return returnValue;
        }

        private Table DescRowMerging(Table finalTable, List<KeywordSynomModel> allKeywords, int HeadingRowNo)
        {
            Table mergedTable = finalTable;
            return mergedTable;
            // using temporary fix column item_description
            //TO Do: Use some column definition which column has to be unified
            int finalTableColCount = finalTable.Rows[HeadingRowNo].Cells.Count;
            int descColNo = -1;
            List<KeywordSynomModel> desc = allKeywords.Where(x => x.AwsBlock == "product_table_cell" && x.SchemaId == "item_description").ToList();
            descColNo = finalTable.Rows[HeadingRowNo].Cells.Find(x =>
            {
                var foundDesc = desc.Find(y => y.Keyword.Trim().ToLower() == x.Text.Trim().ToLower());
                if (foundDesc != null)
                    return true;
                else
                    return false;
            }).ColumnIndex;
            //for (int i = 0; i < finalTableColCount; i++)
            //{
            //    bool foundDesc = desc.Any(x => x.Keyword.Trim().ToLower() ==finalTable.Rows[HeadingRowNo].Cells[i].Text.Trim().ToLower());
            //    if (foundDesc)
            //    {
            //        descColNo = i;
            //        break;
            //    }
            //}
            if (descColNo > -1) // description column found
            {
                int rowNoToMergewith = 0;
                Row prevRow = null;
                bool firstRow = true;
                for (int r = HeadingRowNo + 1; r < finalTable.Rows.Count; r++)
                {
                    Row currentRow = finalTable.Rows[r];
                    int nonEmptyCells = currentRow.Cells.Where(x => x.ColumnIndex != descColNo && x.Text != string.Empty).ToList().Count;
                    if (nonEmptyCells == 0) // // if no cell have value then merge
                    {
                        if (prevRow == null) // this is the first row
                        {
                        }
                        prevRow.Cells[descColNo].Text += " " + currentRow.Cells[descColNo].Text;
                        finalTable.Rows.RemoveAt(r);
                    }
                    else // if any cell have value then follow this
                    {
                        prevRow = finalTable.Rows[r];
                    }
                }
            }
            return mergedTable;
        }

        private ChildNode_Out CreateEmptyTuple(ChildNode_In tuple)
        {
            // creating empty value tuple
            ChildNode_Out newTuple = new ChildNode_Out(); // create new Tuple on every new row.
            newTuple.children = new List<ChildNode_Out>();
            newTuple.schema_id = tuple.id;
            newTuple.category = tuple.category;
            foreach (ChildNode_In sourceDataPoint in tuple.children)
            {
                ChildNode_Out newTupalDataPoint = new ChildNode_Out()
                {
                    schema_id = sourceDataPoint.id,
                    category = sourceDataPoint.category,
                    type = sourceDataPoint.type,
                    content = new DataPointContent { value = "0", confidence = 0.0 }
                };
                newTuple.children.Add(newTupalDataPoint);
            }
            return newTuple;
        }

        private string ParseNumber(string value, Type targetType)
        {
            string returnValue = Regex.Replace(value, "[^.0-9]", "");
            double doubleValue;
            float floatValue;
            int intValue;
            if (targetType == typeof(double))
            {
                if (double.TryParse(returnValue, out doubleValue))
                    returnValue = doubleValue.ToString();
                else
                    returnValue = "0.0";
            }
            if (targetType == typeof(float))
            {
                if (float.TryParse(returnValue, out floatValue))
                    returnValue = floatValue.ToString();
                else
                    returnValue = "0.0";
            }
            if (targetType == typeof(int))
            {
                if (int.TryParse(returnValue, out intValue))
                    returnValue = intValue.ToString();
                else
                    returnValue = "";
            }
            return returnValue;
        }

        private string GetUniqueFileName(string fileName)
        {
            fileName = Path.GetFileName(fileName);
            string fileWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            if (fileWithoutExt.Length > 88) fileWithoutExt = fileWithoutExt.Substring(0, 88);
            return fileWithoutExt + "_"
                      + Guid.NewGuid().ToString().Substring(0, 6)
                      + Path.GetExtension(fileName);
        }

        //private async Task<bool> ProcessDataFailed(ref Domain.Document docEntity)
        //{
        //    ResponseModel returnValue = new ResponseModel();
        //    docEntity.FlgFailed = true;
        //    docEntity.StatusId = await GetdocStatus(DocumateDocStatus.FAILED_EXPORT);
        //    RepoResult repoResult = new RepoResult();
        //    documentRepo.InsertOrUpdate(docEntity, ref repoResult);
        //    return repoResult.success;
        //}
        private async Task<int> GetdocStatus(DocumateDocStatus docStatus)
        {
            var response  = await sysDocStatusRepo.GetEntities(x => x.Order == (int)docStatus).FirstOrDefaultAsync();
            return response.Id;
        }

        private DocumateDocStatus GetdocStatus(int statusId)
        {
            DocumateDocStatus status = (DocumateDocStatus)statusId;
            return status;
        }

        private int UpdatePageCount(Stream memStream, string fileName)
        {
            int returnValue = 0;
            string[] supportedTypes = { ".jpeg", ".jpg", ".png" };
            try
            {
                string ext = Path.GetExtension(fileName);
                if (supportedTypes.Where(x => x.Contains(ext.ToLower())).FirstOrDefault() != null)
                    returnValue = 1;
                else
                    returnValue = PdfReader.Open(memStream, PdfDocumentOpenMode.InformationOnly).PageCount;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Error in UpdatePageCount. Error: {ex.Message}");
            }
            return returnValue;
        }

        private class TemplateResponse
        {
            public string SenderName { get; set; }
            public Documate.Domain.Template SelectedTemplate { get; set; }
        }
        //private class WebhookCallBody
        //{
        //    public int DocId { get; set; }
        //    public string UploadDocModel { get; set; }
        //    public DocumateDocStatus DocStatus { get; set; }
        //    public int QueueId { get; set; }
        //    public string Result { get; set; }
        //}
        public void WebhookCallToClient(Domain.Document doc)
        {
            if (doc == null) return;
            Domain.Queue que = queueService.GetEntityById(doc.QueueId);
            _logger.LogInformation($"Webhook client call step 1: {doc.Id}-{que.WebhookURL}.");
            if (string.IsNullOrEmpty(que.WebhookURL)) return;
            try
            {
                var client = new RestClient($"{que.WebhookURL}");
                var request = new RestRequest();

                //TODO: Fix UserText from Queue object
                //var param = new WebhookCallBody() {DocId= doc.Id, UploadDocModel="LOWRY",DocStatus= docStatus, Result = doc.ProcessedDataJSON};
                var param = new DocumateDocumentResponse();
                param.Result = new DocumateDocument();
                param.Result.CopyPropertyValues(doc);
                param.Result.Status = GetdocStatus(doc.StatusId);
                request.AddJsonBody(JsonConvert.SerializeObject(param));
                _logger.LogDebug($"Webhook client call step 2: {doc.Id}.");
                RestResponse response = client.Execute(request);
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new InvalidDataException($"Couldn't call webhook for URL: {que.WebhookURL}");
                else
                    _logger.LogInformation($"Webhook called for: {doc.Id}.");


            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while calling Webhook for doc id: {doc.Id}. Error Msg: {ex.Message}");
            }

        }

        public async Task WebhookNano(string streamText)
        {
            Domain.Document doc = null;
            NanoWebhookResult webHookData = new NanoWebhookResult();
            _logger.LogInformation($"Webhook called from Nano");

            try
            {
                webHookData = JsonConvert.DeserializeObject<NanoWebhookResult>(streamText);
                string message = webHookData.result.FirstOrDefault().result.FirstOrDefault().message;
                string lastPageMsg = webHookData.result.LastOrDefault().result.LastOrDefault().message;
                if (lastPageMsg.ToLower() != "success")
                {
                    _logger.LogError($"Webhook-Nano: Process failed: Last Page Msg: {lastPageMsg}");
                    return;
                }
                string requestFileId = webHookData.result.FirstOrDefault().result.FirstOrDefault().request_file_id;
                doc = documentRepo.GetEntities(x => x.NanoRequestFileId == requestFileId).FirstOrDefault();
                Domain.Document docEntity = await documentRepo.GetEntityById(doc.Id);
                if (doc == null)
                {
                    _logger.LogError($"Webhook-Nano: Document not found Request File id: {requestFileId}");
                    return;
                }
                ResponseModel response = AccumulatedNanoRawJson(streamText);
                docEntity.RawDataJSON = JsonConvert.SerializeObject(response.Result);
                RepoResult repoResult = new RepoResult();
                await documentRepo.InsertOrUpdate(docEntity, false);
                if (!repoResult.success)
                {
                    throw new InvalidDataException($"Document couldn't be saved from Webhook. Doc id: {doc.Id}");
                }
                _logger.LogInformation($"Webhook process successfull: {doc.Id}");
                QueueModel qm = queueService.GetEntityById(doc.QueueId);
                Account accEntity = await accountRepo.GetEntityById(qm.AccountId);
                if (doc.ProcessedDataJSON == null && docEntity.RawDataJSON != null && accEntity.AiServiceSource == (int)EnumAiServiceSource.NANO)
                {
                    await ProcessNanoDataV2(doc.Id);
                    //WebhookCallToClient(docEntity);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public ResponseModel AccumulatedNanoRawJson(string streamText)
        {
            ResponseModel returnValue = new ResponseModel();
            string logMessage = string.Empty;
            try
            {
                //Domain.Document docEntity = documentRepo.GetEntities(x => x.Id == docId && x.FlgFailed != true && x.FlgDeleted != true).ToList().FirstOrDefault();
                //NanoModel nanoModel = nanoModelRepo.GetEntityById(docEntity.ModelId);
                //if (docEntity == null)
                //    throw new KeyNotFoundException($"Document data could not be updated. Document does not exist. ");
                //if (docEntity.RawDataJSON != null) // already processed
                //    return returnValue;

                string message = $"AccumulatedNanoRawJson: Deserialize object failed.";
                NanoGenericResponse nanoPages = new NanoGenericResponse();
                NanoWebhookResult webhookResult = new NanoWebhookResult();
                webhookResult = JsonConvert.DeserializeObject<NanoWebhookResult>(streamText);
                NanoGenericResponse accumulativePageResult = null;
                int pageCount = 0;
                foreach (NanoGenericResponse pageResult in webhookResult.result)
                {
                    logMessage = $"Error in deserialization of Nano Response Content";
                    if (pageResult == null || pageResult.result == null || pageResult.result[0] == null || pageResult.result[0].prediction == null)
                    {
                        message = $"Webhook data returned null values.";
                        throw new InvalidDataException(message);
                    }
                    if (pageResult.result[0].page == 0)
                        accumulativePageResult = pageResult;
                    else
                        accumulativePageResult.result[0].prediction.AddRange(pageResult.result[0].prediction);
                    pageCount++;
                }
                if (accumulativePageResult != null)
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Result = accumulativePageResult;
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = false;
                _logger.LogError(ex.Message + " - " + logMessage);
            }
            return returnValue;
        }

        private async Task<ResponseModel> updateRawTextJson(int docId)
        {
            _logger.LogDebug("In Text extraction from PDF file");
            ResponseModel returnValue = new ResponseModel();
            Domain.Document docEntity = await documentRepo.GetEntityById(docId);
            try
            {
                DocumentModel docModel = new DocumentModel();
                ExtractRawTextModel textExtractModel = new ExtractRawTextModel();
                docModel.CopyPropertyValues(docEntity);
                //var docModel = GetDocModelById(docId);
                QueueModel qm = queueService.GetEntityById(docModel.QueueId);

                // downlaoding from S3
                S3FileModel fileModel = new S3FileModel();
                fileModel.BucketName = qm.S3BucketName;
                fileModel.FileName = docModel.FileName;
                
                textExtractModel.S3FileModel = fileModel;
                if (qm.TextExtractionService == (int)EnumRawTextService.AWS)
                {
                    textExtractModel.Service = EnumRawTextService.AWS;
                    docEntity.RawDataJSON = await _documentAiService.ExtractTextFromPdfAsync(textExtractModel);
                }
                else if (qm.TextExtractionService == (int)EnumRawTextService.GOOGLE)
                {
                    //fileModel.FileMemoryStream = docModel.MemStream;
                    fileModel.FileMemoryStream = await s3Service.DownloadFile(fileModel);
                    textExtractModel.FileBytes = fileModel.FileMemoryStream.ToArray();
                    textExtractModel.Service = EnumRawTextService.GOOGLE;
                    docEntity.RawDataJSON = await _documentAiService.ExtractTextFromPdfAsync(textExtractModel);
                }
                else
                    throw new InvalidDataException("Text extraction service not found");

                docEntity.StatusId = await GetdocStatus(DocumateDocStatus.IMPORTING);

                RepoResult repoResult = new RepoResult();
                await documentRepo.InsertOrUpdate(docEntity, false);
                if (repoResult.success == true)
                {
                    returnValue.IsSucessfull = true;
                    _logger.LogDebug($"Text extraction successful for Doc id: {docId}");
                }
                else
                    throw new Exception($"Text extraction failed for Doc id: {docId}");

                returnValue.Result = JsonConvert.SerializeObject(docEntity.ProcessedDataJSON);
            }
            catch (Exception ex)
            {
                //Sentry.SentrySdk.CaptureException(ex);
                _logger.LogError($"Text extraction failed for Doc id: {docId} --  {ex.Message}");
                docEntity.NoOfRetries++;
                if (docEntity.NoOfRetries >= 5)
                {

                    docEntity.ProcessingRemarks = $"Doc id: {docId}: System could not extract text from the document.";
                    docEntity.FailedException = $" Try no:{docEntity.NoOfRetries} - Error Msg: {ex.Message}";
                    docEntity.FlgFailed = true;
                }
                RepoResult repoResult = new RepoResult();
                await documentRepo.InsertOrUpdate(docEntity, false);
            }
            return returnValue;
        }
        private async Task<ResponseModel> ProcessOpenAiDocExtraction(int docId)
        {
            _logger.LogInformation("In ProcessGenAiDocExtract for Open Ai");
            ResponseModel returnValue = new ResponseModel();
            Domain.Document docModel = await GetModelById(docId);
            QueueModel queueModel = queueService.GetEntityById(docModel.QueueId);
            AssistantResponseModel assistantOutput = new AssistantResponseModel();
            try
            {
                //docEntity.RawDataJSON = await textractService.ExtractRawTextFromDocumentSync(docModel);
                _logger.LogInformation("Calling GetAssistantOutputAsyncSDK");
                //assistantOutput = await _openAiService.GetAssistantOutputAsyncSDK(docModel.RawDataJSON, queueModel.OpenAiAssistantId);
                assistantOutput = await _openAiService.GetAssistantOutputAsync(docModel.RawDataJSON, queueModel.SchemaJSON, docModel.AdditionalPrompt);
                _logger.LogInformation("Returned from GetAssistantOutputAsyncSDK");
                DocumateMetaData docModelOut = new DocumateMetaData
                {
                    doc_id = docId,
                    user_meta_data = docModel.UserMetaData,
                    version = "2025-02-01"
                };
                //Console.WriteLine("================================== \n\r");
                //Console.WriteLine(assistantOutput.ExtractedJSON);
                //Console.WriteLine("================================== \n\r");
                dynamic outContent = JsonConvert.DeserializeObject<dynamic>(assistantOutput.ExtractedJSON);
                docModelOut.content = outContent;
                _logger.LogInformation("docService.ProcessOpenAiDocExtraction step 3");
                Domain.Document docEntity = await documentRepo.GetEntityById(docId);
                docEntity.ProcessedDataJSON = JsonConvert.SerializeObject(docModelOut);
                docEntity.StatusId = await GetdocStatus(DocumateDocStatus.EXPORTED);
                docEntity.Description = null;

                RepoResult repoResult = new RepoResult();
                _logger.LogInformation("docService.ProcessOpenAiDocExtraction Calling documentRepo.InsertOrUpdate");

                var result = await documentRepo.InsertOrUpdate(docEntity,false);
                if (result != null)
                {
                    returnValue.IsSucessfull = true;
                    _logger.LogDebug($"Process Document successful for Doc id: {docId}");
                    repoResult.errorList = null;
                    //WebhookCallToClient(docEntity);
                }
                //if (repoResult.success == true)
                //{
                //    returnValue.IsSucessfull = true;
                //    _logger.LogDebug($"Process Document successful for Doc id: {docId}");
                //    repoResult.errorList = null;
                //    //WebhookCallToClient(docEntity);
                //}
                else
                {
                    returnValue.Message = $"Process Document failed for Doc id: {docId}";
                    _logger.LogError(returnValue.Message);
                }
                returnValue.Result = JsonConvert.SerializeObject(docEntity.ProcessedDataJSON);
            }
            catch (Exception ex)
            {
                //Sentry.SentrySdk.CaptureException(ex);
                _logger.LogError($"Catch: Processing Document failed for Doc id: {docId} --  {ex.Message}  -- {assistantOutput.ErrorMessage}");
                Domain.Document docEntity = documentRepo.GetEntities(x => x.Id == docId).FirstOrDefault();
                docEntity.NoOfRetries++;
                if (docEntity.NoOfRetries >= 5)
                {

                    docEntity.ProcessingRemarks = $"Doc id: {docId}: System could not extract data from the document.";
                    docEntity.FailedException = $" Try no:{docEntity.NoOfRetries} - Error Msg: {ex.Message}";
                    docEntity.FlgFailed = true;
                }
                RepoResult repoResult = new RepoResult();
                _logger.LogError($"Catch step 2");
                await documentRepo.InsertOrUpdate(docEntity, false);
            }
            return returnValue;
        }
        private async Task Simplicity_keep_alive_call()
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetAsync("https://simplicity-cloud.com:7014/api");
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("External service response: {ResponseBody}", responseBody);
                }
                catch (HttpRequestException e)
                {
                    _logger.LogError(e, "Error calling external service");
                }
            }
        }

        public async Task<ResponseModel> UpdateTrainingData(TrainingDataModel trainingData)
        {
            try
            {
                // Retrieve the document using the DocId
                var document = await documentRepo.GetEntityById(trainingData.DocId);
                if (document == null)
                {
                    return new ResponseModel { IsSucessfull = false, Message = "Document not found." };
                }

                // Save the training data in the UserAnnotation column
                document.UserAnnotation = trainingData.Data;
                await documentRepo.InsertOrUpdate(document, false);

                // Prepare the CSV content with 2 rows:
                // 1st row: RawJSONData field
                // 2nd row: trainingData.Data
                var csvContent = new StringBuilder();
                csvContent.AppendLine(document.RawDataJSON ?? string.Empty);
                csvContent.AppendLine(trainingData.Data);

                // Define the CSV file path based on the document id
                var filePath = $"training_data_{trainingData.DocId}.csv";

                // Check if the file exists, then append new content or create a new file
                if (File.Exists(filePath))
                {
                    await File.AppendAllTextAsync(filePath, csvContent.ToString());
                }
                else
                {
                    await File.WriteAllTextAsync(filePath, csvContent.ToString());
                }

                // Use OpenAI's fine-tuning API to upload the file and create a fine-tuning job
                //var fineTuneRequest = new OpenAI_API.FineTuning.FineTuneRequest
                //{
                //    TrainingFile = filePath, // The CSV file with training data
                //    Model = "gpt-4o",       // The base model to fine-tune
                //    Epochs = 4               // Number of training epochs
                //};

                //var fineTuneJob = await _openAiService.FineTuneModel(fineTuneRequest);

                return new ResponseModel
                {
                    IsSucessfull = true,
                    Message = "Training data updated successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating training data.");
                return new ResponseModel
                {
                    IsSucessfull = false,
                    Message = "Failed to update training data."
                };
            }
        }
        public async Task RemoveOldThreads()
        {
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            var documentsWithThread = await documentRepo.GetEntities(d => d.ThreadId != null).ToListAsync();
            if (documentsWithThread== null || documentsWithThread.Count == 0)
            {
                _logger.LogInformation("No documents with threads found.");
                return;
            }
            foreach (var doc in documentsWithThread)
            {
                try
                {
                    // Remove thread from OpenAI
                    var removed = await _openAiService.RemoveOldThreadsAsync(doc.ThreadId);
                    if (removed)
                    {
                        doc.ThreadId = null;
                        await documentRepo.InsertOrUpdate(doc, false);
                        _logger.LogInformation($"Removed OpenAI thread {doc.ThreadId} for document {doc.Id}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to remove OpenAI thread for document {doc.Id}");
                }
            }
        }


        public class TrainingDataModel
        {
            public string Data { get; set; }
            public int DocId { get; set; }
        }
    }
}

