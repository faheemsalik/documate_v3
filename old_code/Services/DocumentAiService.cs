using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.DocumentAI.V1;
using Google.Apis.Auth.OAuth2;
using System.IO;
using System;
using Documate.Models;


namespace Documate.Services
{
    public interface IDocumentAiService
    {
        Task<string> ExtractTextFromPdfAsync(ExtractRawTextModel model);
    }

    public class DocumentAiService : IDocumentAiService
    {
        private readonly ILogger<DocumentAiService> _logger;
        private readonly IGoogleService _googleService;
        private readonly ITextractService _textractService;

        public DocumentAiService(
            ILogger<DocumentAiService> logger,
            IGoogleService googleService,
            ITextractService textractService
            )
        {
            _logger = logger;
            _googleService = googleService;
            _textractService = textractService;
        }

        public async Task<string> ExtractTextFromPdfAsync(ExtractRawTextModel model)
        {
            switch (model.Service)
            {
                case EnumRawTextService.GOOGLE:
                    return await _googleService.ExtractTextFromPdfAsync(model.FileBytes);
                case EnumRawTextService.AWS:
                    return await _textractService.ExtractRawTextFromDocumentSync(model.S3FileModel);
                default:
                    return null;
            }
            //ProcessResponse response = null;
            //try
            //{
            //    var request = new ProcessRequest
            //    {
            //        Name = $"projects/{ProjectId}/locations/{Location}/processors/{ProcessorId}",
            //        RawDocument = new RawDocument
            //        {
            //            Content = Google.Protobuf.ByteString.CopyFrom(fileBytes),
            //            MimeType = "application/pdf"
            //        }
            //    };

            //    response = await _client.ProcessDocumentAsync(request);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error extracting text from PDF" + ex.Message);
            //    return null;
            //}
            //string ret = response.Document.Text; // Extracted plain text
            //return ret;
        }
    }
}
