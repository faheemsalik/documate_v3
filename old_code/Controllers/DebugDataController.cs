using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Documate.Domain;
using Documate.Models;
using Documate.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Documate.Controllers
{
    [ApiController]
    //    [Route("[controller]/[action]")]
    public class DebugDataController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IAccountService AccountService;

        public DebugDataController(
            ILogger<AccountController> logger,
            IAccountService accountService)
        {
            _logger = logger;
            AccountService = accountService;
        }

        [HttpPost]
        [Route("debug/GetTemplates")]
        public async Task<ResponseModel> GetTemplates(AccountModel account)
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue = await AccountService.CreateAccount(account);
            return returnValue;
        }



    }
}
