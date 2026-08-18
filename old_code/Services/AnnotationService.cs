using Amazon.Textract.Model;
using Documate.Common.Models;
using Documate.Data;
using Documate.Domain;
using Documate.Extensions;
using Documate.Models;
using Documate.Services;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using RestSharp;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Documate.Services
{
    public interface IAnnotationService
    {
        Task<AnnotationModel> GetAnnotation(int id);
        Task<ResponseModel> SaveAnnotation(string data);
        Task<ResponseModel> SaveTemplate(int docId, Template template);
        Task<ResponseModel> GetAnnotationURL(int docId, string redirect_url);
        Task<ResponseModel> GetAnnotationPageId(int docId);
    }
    //================================================================

    public class AnnotationService : IAnnotationService
    {
        private readonly ILogger<ServerDataService> Logger;
        private readonly IDocumentService documentService;
        private readonly IQueueService queueService;
        private readonly ITemplateService TemplateService;
        private readonly INanoModelService nanoModelService;

        public AnnotationService(ILogger<ServerDataService> logger,
            IDocumentService documentService,
            IQueueService queueService,
            ITemplateService templateService, INanoModelService nanoModelService)
        {
            Logger = logger;
            this.documentService = documentService;
            this.queueService = queueService;
            TemplateService = templateService;
            this.nanoModelService = nanoModelService;
        }

        public async Task<AnnotationModel> GetAnnotation(int id)
        {
            AnnotationModel returnValue = new AnnotationModel();
            var doc = await documentService.GetEntityById(id);
            returnValue.Blocks = JsonConvert.DeserializeObject<GetDocumentAnalysisResponse>(doc.RawDataJSON);
            returnValue.annotationData = JsonConvert.DeserializeObject<DocSchema_Out>(doc.ProcessedDataJSON);
            var queue = queueService.GetEntityById(doc.QueueId);
            returnValue.Schema = JsonConvert.DeserializeObject<DocSchema_In>(queue.SchemaJSON);
            returnValue.DocUrl = await documentService.GetOriginalFileURL(id);
            returnValue.template = await TemplateService.GetTemplateById(doc.TemplateId);
            return returnValue;

        }
        public async Task<ResponseModel> GetAnnotationURL(int docId, string redirect_url)
        {
            // TODO: Apply security
            ResponseModel returnValue = new ResponseModel();
            string errMsg = "Document ID is incorrect or not found in the system";

            try
            {
                if (docId < 1)
                    throw new InvalidDataException(errMsg);
                DocumentModel docModel = await documentService.GetModelById(docId);
                NanoModel nanoModel =await  nanoModelService.GetEntityById((int)docModel.ModelId);
                QueueModel queue = queueService.GetEntityById(docModel.QueueId);
                long expiry = DateTimeOffset.Now.ToUnixTimeSeconds() + 86400;
                string getURL = $"https://preview.invoicedata.info/Inferences/Model/{nanoModel.NanoModelId}/ValidationUrl/{docModel.NanoRequestFileId}?redirect={redirect_url}&expires={expiry.ToString()}&callback=your.url.com";
                var client = new RestClient(getURL);
                var request = new RestRequest();
                request.AddHeader("authorization", "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(ProjectSettings.NanoApiKey)));
                RestResponse response = client.Execute(request);
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new InvalidDataException("Error while creating annotation URL");
                //NanoGenericResponse nanoJobResponse = JsonConvert.DeserializeObject<NanoGenericResponse>(response.Content);
                if (response.Content == null)
                {
                    Logger.LogDebug($"Nano server responded will null in GetAnnotationURL- Doc id: {docModel.Id}");
                    throw new InvalidDataException("Error while creating annotation URL");
                }
                returnValue.Result = response.Content;
                returnValue.IsSucessfull = true;
                returnValue.Message = $"Success";
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = false;
                returnValue.Message = ex.Message;
                Logger.LogError($"Error while getting annotation URL for Doc id: {docId} --  {ex.Message}");
            }
            return returnValue;

        }
        public async Task<ResponseModel> GetAnnotationPageId(int docId)
        {
            // TODO: Apply security
            ResponseModel returnValue = new ResponseModel();
            string errMsg = "Document ID is incorrect or not found in the system";

            try
            {
                if (docId < 1)
                    throw new InvalidDataException(errMsg);
                DocumentModel docModel = await documentService.GetModelById(docId);
                NanoGenericResponse nanoResponseModel = JsonConvert.DeserializeObject<NanoGenericResponse>(docModel.NanoUploadResponse);
                returnValue.Result = nanoResponseModel.result.FirstOrDefault().id.ToString();
                NanoModel nanoModel = await nanoModelService.GetEntityById((int)docModel.ModelId);
                returnValue.IsSucessfull = true;
                returnValue.Message = $"Success";
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = false;
                returnValue.Message = ex.Message;
                Logger.LogError($"Error while Getting NanoPage Id Doc id: {docId} --  {ex.Message}");
            }
            return returnValue;

        }
        public async Task<ResponseModel> SaveAnnotation(string data)
        {
            ResponseModel returnValue = new ResponseModel();
            int docId = 0;
            try
            {
                var annoObj = JsonConvert.DeserializeObject<AnnotationObj>(data);
                docId = annoObj.docId;
                DataPointContent content;
                Domain.Document docEntity = await documentService.GetEntityById(annoObj.docId);
                DocSchema_Out userAnnotation = JsonConvert.DeserializeObject<DocSchema_Out>(annoObj.content);
                //DocSchema_Out annotation = JsonConvert.DeserializeObject<DocSchema_Out>(docEntity.ProcessedDataJSON);
                //Matching number of tuples

                //foreach (dynamic item in userAnnotation)
                //{
                //    var filename = item.Name;
                //    var val = item.Value;
                //    int rowNo = 0;
                //    if (filename == "LineItems")
                //    {
                //        foreach (dynamic allRows in item)
                //        {
                //            foreach (dynamic row in allRows)
                //            {
                //                foreach (dynamic prop in row)
                //                {
                //                    content = new DataPointContent { confidence = 1.00, value = prop.Value, validation_source = "human" };
                //                    var tpl = annotation.content.Find(sec => sec.schema_id == "line_items_section").children.ElementAt(rowNo);
                //                    var dp = tpl.children.Find(x => x.schema_id == prop.Name);
                //                    dp.content = content;
                //                    //annotation.content.Find(sec => sec.schema_id == "line_items_section")
                //                    //    .children.Find(tpl => tpl.children.Find(dp => dp.schema_id == prop.Name) != null)
                //                    //.children.Find(x => x.schema_id == prop.Name).content = content;
                //                }
                //                rowNo++;
                //            }
                //        }
                //        continue;
                //    }
                //    if (filename == "doc_identifier" || filename== "__RequestVerificationToken") 
                //        continue;
                //    content = new DataPointContent { confidence = 1.00, value = val, validation_source = "human" };
                //    //annotation.content.Find(x=> x.children.Find(y=> y.schema_id==filename)!=null)
                //    //    .children.Find(x => x.schema_id == filename)
                //    //    .content=content;
                //}
                returnValue = await documentService.UpdateAnnotation(docId, JsonConvert.SerializeObject(userAnnotation));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Annotation processing failed for Doc id: {docId} --  {ex.Message}");
            }
            return returnValue;
        }
        public async Task<ResponseModel> SaveTemplate(int docId, Template template)
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue =await TemplateService.SaveTemplate(template);
            //if (returnValue.IsSucessfull)
            //    returnValue = documentService.UpdateTemplate(docId, int.Parse(returnValue.Result.ToString()));

            return returnValue;
        }
    }
    public class AnnotationObj
    {
        public string content { get; set; }
        public int docId { get; set; }
    }
}
