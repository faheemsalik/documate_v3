using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Documate.Services;

namespace Documate.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class OpenAiController : ControllerBase
    {
        private readonly ILogger<DocumentController> _logger;
        private readonly IOpenAiService _openAiService;
        private readonly IMatchingTextApiService matchingTextApiService;

        public OpenAiController(IOpenAiService openAiService, ILogger<DocumentController> logger)
        {
            _logger = logger;
            _openAiService = openAiService;
        }

        [HttpPost]
        public async Task<IActionResult> GetResponse(chatMessage msg)
        {
            var _assistantId = "asst_pDKccrNMWg3XRbsoQBC7sxmc";
            //var response = await _openAiService.GetAssistantOutputAsync(msg.Message,"");
            var response = await _openAiService.GetAssistantOutputAsync(msg.Message, msg.AssistantId?? _assistantId);
            return Ok(response);
        }
        public class chatMessage
        {
            public string Message { get; set; }
            public string AssistantId { get; set; }
        }

    }
}
