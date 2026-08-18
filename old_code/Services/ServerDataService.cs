using Documate.Data;
using Documate.Domain;
using Documate.Extensions;
using Documate.Models;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Documate.Services
{
    public interface IServerDataService
    {
        Task<ResponseModel> GetServerData(string methodName, string id);
    }
    //================================================================

    public class ServerDataService : IServerDataService
    {
        private readonly ILogger<ServerDataService> Logger;
        private HttpClient client = new HttpClient();
        private string ApiEndpoint;
        private string status = string.Empty;

        public ServerDataService(ILogger<ServerDataService> logger)
        {
            Logger = logger;
        }

        public async Task<ResponseModel> GetServerData(string methodName, string id)
        {
            ResponseModel returnValue = new ResponseModel();
            try
            {
                await CreateHttpClientAsync();
                if (client == null)
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "Http client failed";
                    return returnValue;
                }
                string uri = string.Empty;
                if (methodName == "GetAllTemplates")
                    methodName = $"template/GetTemplateList";
                else if (methodName == "GetMasterKeywordList")
                    methodName = $"keyword/GetMasterKeywordList";
                else if (methodName == "GetTemplateKeywordList")
                    methodName = $"template/GetTemplateKeywordList?templateId={id}";
                else if (methodName == "GetKeywordElementList")
                    methodName = $"template/GetKeywordElementList?templateKeywordId={id}";
                else if (methodName == "GetTemplateKwElements")
                    methodName = $"keyword/GetTemplateKwElements?templateId={id}";
                else if (methodName == "GetAllKeywordSynom")
                    methodName = $"keyword/GetAllKeywordSynom";
                else if (methodName == "GetAllKeywords")
                    methodName = $"keyword/GetAllKeywords?templateId={id}";
                else if (methodName == "GetDocDebugData")
                    methodName = $"documents/GetDocDebugData?docId={id}";

                uri = $"{ApiEndpoint}{methodName}";
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string streamResponse = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(streamResponse))
                        {
                            returnValue = JsonConvert.DeserializeObject<ResponseModel>(streamResponse);
                        }
                        else
                            Logger.LogError($"Aws client returned null. Make sure user name and password are correct");
                    }
                    else
                        Logger.LogError($"HTTP request failed.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error returned from server:  {ex.Message}");
            }
            return returnValue;
        }

        private async Task CreateHttpClientAsync()
        {
            try
            {
                string userId = string.Empty;
                string password = string.Empty;
                ApiEndpoint = "http://3.11.192.72:8081/";
                string token = string.Empty;
                string status = string.Empty;

                Credentials cr = new Credentials() { username = userId, password = password };
                var credentials = JsonConvert.SerializeObject(cr);
                var stringContent = new StringContent(credentials, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Add("ContentType", "application/json");
                using (HttpResponseMessage result = await client.PostAsync($"{ApiEndpoint}auth/login", stringContent))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        string streamResponse = await result.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(streamResponse))
                        {
                            Dictionary<string, string> ret = JsonConvert.DeserializeObject<Dictionary<string, string>>(streamResponse);
                            if (ret.FirstOrDefault().Key == null)
                                status = "Aws client returned null. Make sure user name and password are correct";
                            else
                            {
                                token = ret.FirstOrDefault().Value;
                                if (client.DefaultRequestHeaders.Where(x => x.Key == "token").FirstOrDefault().Key == null)
                                {
                                    client.DefaultRequestHeaders.Add("ContentType", "application/json");
                                    client.DefaultRequestHeaders.Add("token", token);
                                    status = "";
                                }

                            }
                        }
                        else status = "Aws client returned null. Make sure user name and password are correct";
                    }
                    else status = "HTTP request failed.";
                }
                //HttpResponseMessage response = await client.PostAsync($"{ApiEndpoint}auth/login", stringContent);            
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + "-" + "Coud not create HTTP client for Aws");
            }
            return;
        }
        internal class Credentials
        {
            public string username { get; set; }
            public string password { get; set; }
        }
    }

}
