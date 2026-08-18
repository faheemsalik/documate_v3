using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Documate.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Documate.Services;


namespace Documate.Controllers
{
    [ApiController]
    [Route("v1/[controller]/[action]")]
    public class BillingController : ControllerBase
    {
        private readonly ILogger<BillingController> _logger;
        private readonly IBillingService BillingService;

        public BillingController(ILogger<BillingController> logger, IBillingService billingService)
        {
            _logger = logger;
            BillingService = billingService;
        }

        [HttpPost]
        public ResponseModel UploadDocument(DocumentModel doc)
        {
            ResponseModel returnValue = null;
            returnValue.IsSucessfull = true;
            return returnValue;
        }
    }
}
