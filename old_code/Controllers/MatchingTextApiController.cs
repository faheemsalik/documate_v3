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
using System.Collections.Generic;
using Documate.Services;

namespace Documate.Controllers
{
    [ApiController]
    //[Route("[controller]/[action]")]
    public class MatchingTextApiController : ControllerBase
    {
        private readonly ILogger<DocumentController> _logger;
        private readonly IMatchingTextApiService matchingTextApiService;

        public MatchingTextApiController(
            ILogger<DocumentController> logger,
            IMatchingTextApiService matchingTextApiService
            )
        {
            _logger = logger;
            this.matchingTextApiService = matchingTextApiService;
        }

        [HttpPost]
        [Route("matchingtextapi/SymanticMachData")]
        public ResponseModel SymanticMachData(SymanticMatchDataInput data)
        {
            var returnValue = matchingTextApiService.SymanticMachData(data);
            return returnValue;
        }

    }

}
