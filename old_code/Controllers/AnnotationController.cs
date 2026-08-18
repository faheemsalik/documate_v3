using System.IO;
using System.Threading.Tasks;
using Documate.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Amazon.Textract.Model;
using System;
using Documate.Modelss;
using Documate.Domain;
using Documate.Services;


namespace Documate.Controllers
{
    [ApiController]
    //[Route("[controller]/[action]")]
    public class AnnotationController : ControllerBase
    {
        private readonly ILogger<DocumentController> _logger;
        private readonly IAnnotationService annotationService;
        private readonly IDocumentService documentService;

        public AnnotationController(
            ILogger<DocumentController> logger,
            IAnnotationService annotationService,
            IDocumentService documentService
            )
        {
            _logger = logger;
            this.annotationService = annotationService;
            this.documentService = documentService;
        }

        //[HttpGet]
        //[Route("annotation/GetAnnotation")]
        //public async Task<AnnotationModel> GetAnnotation(int id)
        //{
        //    var returnValue = await annotationService.GetAnnotation(id);
        //    return returnValue;
        //}

        //[HttpGet]
        //[Route("annotation/GetAnnotationURL")]
        //public async Task<ResponseModel> GetAnnotationURL(int docId, string redirect_url)
        //{
        //    ResponseModel returnValue = await annotationService.GetAnnotationURL(docId, redirect_url);
        //    return returnValue;
        //}
        //[HttpGet]
        //[Route("annotation/GetAnnotationPageId")]
        //public async Task<ResponseModel> GetAnnotationPageId(int docId, string redirect_url)
        //{
        //    ResponseModel returnValue = await annotationService.GetAnnotationPageId(docId);
        //    return returnValue;
        //}
        //[HttpGet]
        //[Route("annotation/GetBlocks")]
        //public async Task <GetDocumentAnalysisResponse> GetBlocks(int id)
        //{
        //    var doc = await documentService.GetEntityById(id);
        //    GetDocumentAnalysisResponse AwsBlocks = JsonConvert.DeserializeObject<GetDocumentAnalysisResponse>(doc.RawDataJSON);
        //    return AwsBlocks;
        //}

        //[HttpGet]
        //[Route("annotation/GetOriginalFileURL")]
        //public async Task<string> GetOriginalFileURL(int id)
        //{
        //    //AnnotationFileURLModel returnValue = new AnnotationFileURLModel();
        //    string returnValue = await documentService.GetOriginalFileURL(id);
        //    return returnValue;
        //}

        //[HttpPost]
        //[Route("annotation/SaveAnnotation")]
        //public async Task<ResponseModel> SaveAnnotation(string prmModel)
        //{
        //    ResponseModel returnValue = new ResponseModel();
        //    try
        //    {
        //        string streamText;
        //        using (StreamReader reader = new StreamReader(Request.Body))
        //            streamText = await reader.ReadToEndAsync();
        //        //var userAnnotation = JsonConvert.DeserializeObject<AnnotationObj>(streamText);
        //        returnValue = await annotationService.SaveAnnotation(streamText);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error in Save Annotation - {ex.Message}");
        //    }
        //    return returnValue;
        //}
        //[HttpPost]
        //[Route("annotation/SaveTemplate")]
        //public async Task<ResponseModel> SaveTemplate(AnnotationTemplate annoTemplate)
        //{
        //    ResponseModel returnValue = new ResponseModel();
        //    returnValue = await annotationService.SaveTemplate(int.Parse(annoTemplate.docId), annoTemplate.AnnoTemplate);
        //    return returnValue;
        //}
    }
    public class AnnotationTemplate
    {
        public string docId { get; set; }
        public Template AnnoTemplate { get; set; }
    }
}
