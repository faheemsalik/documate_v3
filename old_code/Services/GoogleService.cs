using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.DocumentAI.V1;
using Google.Apis.Auth.OAuth2;
using System.IO;
using System;


namespace Documate.Services
{
    public interface IGoogleService
    {
        Task<string> ExtractTextFromPdfAsync(byte[] fileBytes);
    }

    public class GoogleService : IGoogleService
    {
        private readonly DocumentProcessorServiceClient _client;
        private const string ProjectId = "864919711616";
        private const string Location = "us"; // or other regions like "eu"
        private const string ProcessorId = "554f58a7326953bc";
        private readonly ILogger<GoogleService> _logger;

        public GoogleService(ILogger<GoogleService> logger, string jsonKey)
        {
            var credential = GoogleCredential.FromJson(jsonKey);
            _client = new DocumentProcessorServiceClientBuilder
            {
                Credential = credential
            }.Build();
            _logger = logger;
        }

        public async Task<string> ExtractTextFromPdfAsync(byte[] fileBytes)
        {
            ProcessResponse response = null;
            try
            {
                var request = new ProcessRequest
                {
                    Name = $"projects/{ProjectId}/locations/{Location}/processors/{ProcessorId}",
                    RawDocument = new RawDocument
                    {
                        Content = Google.Protobuf.ByteString.CopyFrom(fileBytes),
                        MimeType = "application/pdf"
                    }
                };

                response = await _client.ProcessDocumentAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF" + ex.Message);
                return null;
            }
            string ret = response.Document.Text; // Extracted plain text
            return ret;
        }
    }
}
