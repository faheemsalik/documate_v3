using System.IO;
using System.Threading.Tasks;

using Amazon.Textract.Model;

using Documate.Extensions;
using Documate.Models;
using Documate.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace Documate.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class AwsController : ControllerBase
    {
        private readonly ILogger<DocumentController> _logger;
        private readonly IDocumentService documentService;
        private readonly ITextractService textractService;
        private readonly IQueueService _queueService;

        public AwsController(
            ILogger<DocumentController> logger,
            IDocumentService documentService,
            ITextractService textractService,
            IQueueService queueService)
            
        {
            _logger = logger;
            this.documentService = documentService;
            this.textractService = textractService;
            _queueService = queueService;
        }


        [HttpPost]
        public void SNSNotification()// called by SNS
        {
            string streamText = string.Empty;
            using (StreamReader reader = new StreamReader(Request.Body))
                streamText = reader.ReadToEndAsync().Result;
        }

        [HttpPost]
        public async Task ProcessAllJobs()
        {
            await documentService.ScheduleProcessAllDocs();
            string s = string.Empty;
        }

        //[HttpPost]
        //public ResponseModel UpdateOutputSchema(int id)
        //{
        //    //Domain.Document docEntity = documentService.GetEntityById(id); 
        //    //ResponseModel a = await documentService.GetJobResultAsync(docEntity.AwsJobId);
        //    //ResponseModel response =  documentService.ProcessOcrDataV2(id);
        //    return response;
        //}

        [HttpPost]
        public async Task<ResponseModel> UpdateRawJSON(int id)
        {
            ResponseModel returnValue = new ResponseModel();
            //Domain.Document docEntity = documentService.GetEntityById(id); 
            //returnValue = documentService.UpdateNanoJsonAsync(id);
            returnValue = await documentService.UpdateNanoJsonMultiPage(id);
            string s = string.Empty;
            return returnValue;
        }

        [HttpPost]
        public void DebugJobResult()
        {
            string streamText = string.Empty;
            ResponseModel returnValue = new ResponseModel();
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                streamText = reader.ReadToEndAsync().Result;
            }
            GetDocumentAnalysisResponse response = JsonConvert.DeserializeObject<GetDocumentAnalysisResponse>(streamText);
            TextractDocument tDoc = new TextractDocument(response);

            string s = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> ExtractRawTextFromDocumentSync(int id)
        {
            Domain.Document docEntity = await documentService.GetEntityById(id);
            QueueModel qm = _queueService.GetEntityById(docEntity.QueueId);
            //DocumentModel docModel = new DocumentModel();
            //docModel.CopyPropertyValues(docEntity);
            S3FileModel s3FileModel = new S3FileModel();
            s3FileModel.BucketName = qm.S3BucketName;
            s3FileModel.FileName = docEntity.FileName;
            var response = await textractService.ExtractRawTextFromDocumentSync(s3FileModel);
            return Ok(response);
        }


    }
}
