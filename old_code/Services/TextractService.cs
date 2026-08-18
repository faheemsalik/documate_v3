using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Textract;
using Amazon.Textract.Model;
using Documate.Domain;
using Documate.Models;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Documate.Services
{
    public interface ITextractService
    {
        Task<string> StartDocumentAnalysis(DocumentModel doc);
        bool IsJobComplete(string jobId);
        void WaitForJobCompletion(string jobId, int delay);
        Task<GetDocumentAnalysisResponse> GetJobResultAsync(string jobId);
        List<string> GetLines(DetectDocumentTextResponse result);
        List<string> GetLines(GetDocumentAnalysisResponse result);
        Task<string> StartDocumentTextExtractionAsync(DocumentModel doc);
        Task<string> ExtractRawTextFromDocumentSync(S3FileModel s3FileModel);
    }

    public class TextractService : ITextractService
    {
        private readonly ILogger<TextractService> Logger;
        private readonly IAmazonTextract _textract;

        public TextractService(
            ILogger<TextractService> logger,
            IAmazonTextract textract)
        {
            Logger = logger;
            this._textract = textract;
        }

        public async Task<string> StartDocumentAnalysis(DocumentModel doc)
        {
            var request = new StartDocumentAnalysisRequest();
            var s3Object = new S3Object
            {
                Bucket = doc.BucketName,
                Name = doc.FileName
            };
            request.DocumentLocation = new DocumentLocation { S3Object = s3Object };
            request.FeatureTypes = new List<string> { FeatureType.TABLES, FeatureType.FORMS };

            StartDocumentAnalysisResponse docResponse = await _textract.StartDocumentAnalysisAsync(request);
            return docResponse.JobId;
        }

        public async Task<string> StartDocumentTextExtractionAsync(DocumentModel doc)
        {
                var request = new StartDocumentTextDetectionRequest
                {
                    DocumentLocation = new DocumentLocation
                    {
                        S3Object = new S3Object
                        {
                            Bucket = doc.BucketName,
                            Name = doc.FileName
                        }
                    }
                };
            StartDocumentTextDetectionResponse response = null;
            try
            {
                response = await _textract.StartDocumentTextDetectionAsync(request);
            }
            catch (Exception ex)
            {
                Logger.LogError($"{ex.Message} - Error in StartDocumentTextDetectionAsync process");
            }
            return response.JobId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s3FileModel"></param>
        /// <returns></returns>
        public async Task<string> ExtractRawTextFromDocumentSync(S3FileModel s3FileModel)
        {
            var request = new DetectDocumentTextRequest
            {
                Document = new Amazon.Textract.Model.Document
                {
                    S3Object = new S3Object
                    {
                        Bucket = s3FileModel.BucketName,
                        Name = s3FileModel.FileName
                    }
                }
            };
            var extractedText = new StringBuilder();
            DetectDocumentTextResponse response=null;
            try
            {
                response = await _textract.DetectDocumentTextAsync(request);
                //UnsupportedDocumentException
                foreach (var block in response.Blocks)
                {
                    if (block.BlockType == BlockType.LINE)
                    {
                        extractedText.AppendLine(block.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                //Sentry.SentrySdk.CaptureException(ex);
                Logger.LogError($"{ex.Message} - Error in ExtractRawTextFromDocumentSync process");                
            }   
            return extractedText.ToString();
        }

        public async Task<GetDocumentAnalysisResponse> GetJobResultAsync(string jobId)
        {
            GetDocumentAnalysisResponse returnValue = new GetDocumentAnalysisResponse();
            GetDocumentAnalysisResponse response = new GetDocumentAnalysisResponse();
            string nextToken = string.Empty;
            try
            {
                if (IsJobComplete(jobId))
                {
                    GetDocumentAnalysisRequest request = new GetDocumentAnalysisRequest() { JobId = jobId };
                    do
                    {
                        //if (!string.IsNullOrEmpty(nextToken))
                        //	request.NextToken = nextToken;
                        response = await _textract.GetDocumentAnalysisAsync(request);
                        if (string.IsNullOrEmpty(nextToken)) // if nextToken is empty means this is first iteration
                            returnValue = response;
                        else
                            returnValue.Blocks.AddRange(response.Blocks);
                        if (response.NextToken != null && !string.IsNullOrEmpty(returnValue.NextToken))
                        {
                            request.NextToken = response.NextToken;
                            nextToken = response.NextToken;
                        }
                        else
                            nextToken = string.Empty;
                    } while (!string.IsNullOrEmpty(nextToken));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"{ex.Message} - Error in GetDocumentAnalysisAsync process");
            }
            return returnValue;
        }

        public bool IsJobComplete(string jobId)
        {
            var response = _textract.GetDocumentAnalysisAsync(new GetDocumentAnalysisRequest
            {
                JobId = jobId
            });
            response.Wait();
            return !response.Result.JobStatus.Equals("IN_PROGRESS");
        }

        public void WaitForJobCompletion(string jobId, int delay = 5000)
        {
            while (!IsJobComplete(jobId))
            {
                Wait(delay);
            }
        }

        private void Wait(int delay = 3000)
        {
            Task.Delay(delay).Wait();
            Console.Write(".");
        }

        public void PrintDebug(GetDocumentAnalysisResponse response)
        {
            response.Blocks.ForEach(y =>
            {
                Console.WriteLine("<block>");
                Console.WriteLine(y.Id + ":" + y.BlockType + ":" + y.Text);
                if (y.BlockType == "KEY_VALUE_SET")
                {
                    Console.WriteLine(" <KEY_VALUE_SET>");
                    PrintBlock(y);
                    Console.WriteLine(" </KEY_VALUE_SET>");
                }
                else if (y.BlockType == "TABLE")
                {
                    Console.WriteLine(" <TABLE>");
                    PrintBlock(y);
                    Console.WriteLine(" </TABLE>");
                }
                else if (y.BlockType == "CELL")
                {
                    Console.WriteLine(" <CELL>");
                    PrintBlock(y);
                    Console.WriteLine(" </CELL>");
                }
                Console.WriteLine("</block>");
            });
        }
        private void PrintBlock(Block block)
        {
            Console.WriteLine("  <entity>");
            block.EntityTypes.ForEach(z => Console.WriteLine("   " + z));
            Console.WriteLine("  </entity>");
            block.Relationships.ForEach(z =>
            {
                Console.WriteLine("  <relation>");
                Console.WriteLine("   " + z.Type);
                Console.WriteLine("   <id>");
                z.Ids.ForEach(a => Console.WriteLine("    " + a));
                Console.WriteLine("   </id>");
                Console.WriteLine("  </relation>");
            });
        }

        public List<string> GetLines(DetectDocumentTextResponse result)
        {
            var lines = new List<string>();
            result.Blocks.FindAll(block => block.BlockType == "LINE").ForEach(block => lines.Add(block.Text));
            return lines;
        }
        public List<string> GetLines(GetDocumentAnalysisResponse result)
        {
            var lines = new List<string>();
            result.Blocks.FindAll(block => block.BlockType == "LINE").ForEach(block => lines.Add(block.Text));
            return lines;
        }
    }
}
