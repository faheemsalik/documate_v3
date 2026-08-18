using Documate.Models;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using OpenAI.Threads;

using Sentry.Protocol;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using static Documate.Services.OpenAiService;

namespace Documate.Services
{
    public interface IOpenAiService
    {
        Task<bool> RemoveOldThreadsAsync(string threadId);
        Task<AssistantResponseModel> GetAssistantOutputAsync(string ocrText, string systemInstruction, string customInstructions = null);
    }

    public class OpenAiService : IOpenAiService
    {
        private readonly ILogger<OpenAiService> _logger;
        private readonly OpenAIClient _openAiClient;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;


        public OpenAiService(ILogger<OpenAiService> logger, string apiKey)
        {
            _logger = logger;
            _openAiClient = new OpenAIClient(apiKey);
            _httpClient = new HttpClient();
            _apiKey = apiKey;
        }
        public async Task<AssistantResponseModel> GetAssistantOutputAsync(string ocrText, string systemInstruction, string customInstructions=null)
        {
            _logger.LogInformation("Entered GetAssistantOutputAsync (Responses API)");

            var returnModel = new AssistantResponseModel();
            var apiUrl = "https://api.openai.com/v1/responses";
            if (!string.IsNullOrWhiteSpace(customInstructions))
                systemInstruction = systemInstruction + ". - Additionally," + customInstructions;
            try
            {
                // Build the payload
                var payload = new
                {
                    model = "gpt-5.1", //"gpt-4.1",
                    input = new[]
                    {
                        new
                        {
                            role = "system",
                            content = new[]
                            {
                                new { type = "input_text", text = systemInstruction }
                            }
                        },
                        new
                        {
                            role = "user",
                            content = new[]
                            {
                                new { type = "input_text", text = ocrText }
                            }
                        }
                    },
                    text = new
                    {
                        format = new
                        {
                            type = "json_object"  // ✅ must be object, not plain string
                        }
                    }
                };
                string jsonPayload = JsonConvert.SerializeObject(payload);
                _logger.LogInformation("Sending request to OpenAI Responses API...");

                var responseString = await SendPostRequestAsync(apiUrl, jsonPayload);

                dynamic responseJson = JsonConvert.DeserializeObject(responseString);
                string output = responseJson?.output?[0]?.content?[0]?.text ?? "";

                if (string.IsNullOrWhiteSpace(output))
                {
                    returnModel.ErrorMessage = "{\"error\": \"No response from AI Server\"}";
                }
                else
                {
                    returnModel.ExtractedJSON = output;
                    returnModel.ErrorMessage = "";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting data using Responses API: {ex.Message}");
                returnModel.ErrorMessage = "{\"error\": \"Failed to process data\"}";
            }

            return returnModel;
        }


        private async Task<string> SendPostRequestAsync(string url, string payload)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Add("OpenAI-Beta", "responses=v1");
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            //_logger.LogError($"OpenAI raw response: {body}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"OpenAI API Error: {response.StatusCode} - {body}");
            }

            return body;
        }


        private async Task<string> SendGetRequestAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Headers.Add("OpenAI-Beta", "responses=v1"); // ✅ updated for Responses API

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // Method to fine-tune a model with a training file
        public async Task FineTuneModelAsync(FinTuneModel fineTuneModel)
        {
            // Endpoint for fine-tuning models
            string endpoint = $"{Data.ProjectSettings.OpenAiEndPoint}fine-tunes";

            var fineTuneRequest = new
            {
                model = fineTuneModel, // You can change this to any model you want to fine-tune
                training_file = fineTuneModel.training_file,
                n_epochs = fineTuneModel.n_epochs  // You can adjust the number of epochs
            };

            var requestContent = new StringContent(
                JsonConvert.SerializeObject(fineTuneRequest),
                Encoding.UTF8,
                "application/json"
            );

            // Setting the authorization header with the OpenAI API key
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            // Send POST request for fine-tuning
            var response = await _httpClient.PostAsync(endpoint, requestContent);
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Fine-tuning started successfully! - Reponse: {responseContent}");
                Console.WriteLine("Fine-tuning started successfully!");
                Console.WriteLine(responseContent);
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Error starting fine-tuning:: {errorContent}");
                Console.WriteLine("Error starting fine-tuning:");
                Console.WriteLine(errorContent);
            }
        }

        // Method to upload a file to OpenAI for fine-tuning
        public async Task<string> UploadTrainingFileAsync(string filePath)
        {
            string endpoint = $"{Data.ProjectSettings.OpenAiEndPoint}files";

            var formContent = new MultipartFormDataContent();
            formContent.Add(new StringContent("fine-tune"), "purpose");  // specify the purpose
            formContent.Add(new StreamContent(System.IO.File.OpenRead(filePath)), "file", "file.jsonl");

            // Set the authorization header with the OpenAI API key
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            // Send the file upload request
            var response = await _httpClient.PostAsync(endpoint, formContent);
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<dynamic>(responseContent);
                string fileId = responseObject.id;
                Console.WriteLine("File uploaded successfully. File ID: " + fileId);
                return fileId;
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Error uploading file:");
                Console.WriteLine(errorContent);
                return null;
            }
        }


        public async Task DeleteTrainingFileAsync(string fileId)
        {
            string endpoint = $"{Data.ProjectSettings.OpenAiEndPoint}files/{fileId}";
            try
            {
                // Set the authorization header with the OpenAI API key
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                // Send DELETE request to delete the file
                var response = await _httpClient.DeleteAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("File deleted successfully.");
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Error deleting file:");
                    Console.WriteLine(errorContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting file:");
                Console.WriteLine(ex.Message);
            }
        }
        public async Task<bool> RemoveOldThreadsAsync(string threadId)
        {
            try
            {
                var result = await _openAiClient.ThreadsEndpoint.DeleteThreadAsync(threadId);
                _logger.LogInformation($"Deleted OpenAI thread: {threadId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while removing old OpenAI threads");
            }
            return true;
        }

        //=======================================================================================================
        // Models
        //=======================================================================================================
        public class FinTuneModel
        {
            public string model { get; set; } // the basse model to fine tune
            public string newModel { get; set; } // the name of the custom model
            public string training_file { get; set; } // the name of the training file.
            public int n_epochs { get; set; }
        }

        private class UserMessageModel
        {
            public string role { get; set; }
            public string content { get; set; }
        }
    }
}
