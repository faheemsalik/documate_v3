using System;
using System.Collections.Generic;
using System.IO;
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
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IAccountService accountService;

        public AccountController(
            ILogger<AccountController> logger,
            IAccountService accountService)
        {
            _logger = logger;
            this.accountService = accountService;
        }

        [HttpPost]
        [Route("account/CreateAccount")]
        public async Task<ResponseModel> CreateAccount(AccountModel account)
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue =await accountService.CreateAccount(account);
            return returnValue;
        }

        [HttpPost]
        [Route("account/CreateUser")]
        public async Task<ResponseModel> CreateUser(UserModel user)
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue = await accountService.CreateUser(user);
            return returnValue;
        }

        [HttpPost]
        [Route("account/UpdateUser")]
        public ResponseModel UpdateUser(User user)
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue.IsSucessfull = true;
            return returnValue;
        }

        [HttpPost]
        [Route("account/ChangePassword")]
        public ResponseModel ChangePassword(ChangePasswordVM model)
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue.IsSucessfull = true;
            returnValue.Message = "Password has beenc hanged successfully.";
            //TODO update call.
            //returnValue = AccountService.ChangePassword(model);
            return returnValue;
        }

        [HttpPost]
        [Route("auth/login")]
        public ResponseModel login(CredentialModel credentials)
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue = accountService.Login(credentials);
            //Dictionary<string, string> token = new Dictionary<string, string>();
            //token.Add("token", "123456789");
            //token.Add("expiry_utc", DateTime.Now.ToUniversalTime().AddDays(90).ToString());
            return returnValue;
        }

        [HttpGet]
        [Route("account/GetAccountList")]
        public ResponseModel GetAccountList()
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue.Result = accountService.GetAccounts(x => x.Id > 0);
            return returnValue;
        }

        [HttpGet]
        [Route("account/refresh")]
        public IActionResult Refresh()
        {
            // Touch web.config to trigger app restart
            var webConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "web.config");
            System.IO.File.SetLastWriteTimeUtc(webConfigPath, DateTime.UtcNow);
            return Ok("Refresh triggered");
        }
    }
}
