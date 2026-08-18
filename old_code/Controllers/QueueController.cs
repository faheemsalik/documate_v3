using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Documate.Data;
using Documate.Extensions;
using Documate.Models;
using Documate.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace Documate.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class QueueController : ControllerBase
    {
        private readonly ILogger<QueueController> _logger;
        private readonly IQueueService queueService;
        private readonly IAccountService accountService;

        public QueueController(
            ILogger<QueueController> logger,
            IQueueService queueService,
            IAccountService accountService)
        {
            _logger = logger;
            this.queueService = queueService;
            this.accountService = accountService;
        }

        [HttpPost]
        public async Task<ResponseModel> UpdateQueue(QueueUpdatePublic extModel)
        {
            ResponseModel returnValue = new ResponseModel();
            string logMessage = "Error while checking Auth";
            // Auth validity
            //TODO Shift to Attribute
            string authToken = string.Empty;
            int userId = 0;
            var authHeader = Request.Headers.Where(x => x.Key == "token");
            if (authHeader != null)
            {
                authToken = authHeader.FirstOrDefault().Value.ToString();
                LoginReturnModel loginObj = accountService.AuthValidility(authToken);
                if (loginObj.IsSuccfull == false)
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "User is not logged in or token is invalid";
                    return returnValue;
                }
            }
            try
            {
                //ToDo Apply validation checks.
                logMessage = "Error while copying values";
                QueueModel queueModel = queueService.GetEntityById(extModel.id);
                queueModel.Id = extModel.id;
                queueModel.QueueName = extModel.name;
                queueModel.Description = extModel.description;
                queueModel.SchemaJSON = JsonConvert.SerializeObject(extModel);
                logMessage = "Error while calling service method";
                returnValue = await queueService.UpdateQueue(queueModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(logMessage + "-" + ex.Message);
                returnValue.IsSucessfull = false;
                returnValue.Message = "Could not update queue.";
            }
            return returnValue;
        }

        [HttpGet]
        [Route("queue/GetQueue")]
        public ResponseModel GetQueue(int id)
        {
            //ToDO Apply Token security
            ResponseModel returnValue = new ResponseModel();
            QueueModel model = queueService.GetEntityById(id);
            if (model != null && model.Id > 0)
            {
                returnValue.IsSucessfull = true;
                returnValue.Result = model;
            }
            return returnValue;
        }

        [HttpGet]
        //[Route("queue/GetAccountQueuesList")]
        public async Task<ResponseModel> GetAccountQueuesList()
        {
            ResponseModel returnValue = new ResponseModel();
            // Auth validity
            //TODO Shift to Attribute
            string authToken = string.Empty;
            string loginId = string.Empty;
            var authHeader = Request.Headers.Where(x => x.Key == "token");
            if (authHeader != null)
            {
                authToken = authHeader.FirstOrDefault().Value.ToString();
                LoginReturnModel loginObj = accountService.AuthValidility(authToken);
                if (loginObj.IsSuccfull == false)
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "User is not logged in or token is invalid";
                    return returnValue;
                }

                loginId = loginObj.LoginId;
            }
            UserModel user = accountService.GetUsers(x => x.LoginId == loginId).FirstOrDefault();
            List<QueueAppVM> model = await queueService.GetAccountQueuesList(user.Id);
            if (model != null && model.Count > 0)
            {
                returnValue.IsSucessfull = true;
                returnValue.Result = model;
            }
            return returnValue;
        }

        [HttpGet]
        //[Route("queue/GetUserQueuesList")]
        public ResponseModel GetUserQueuesList()
        {
            ResponseModel returnValue = new ResponseModel();
            // Auth validity
            //TODO Shift to Attribute
            string authToken = string.Empty;
            string loginId = string.Empty;
            var authHeader = Request.Headers.Where(x => x.Key == "token");
            if (authHeader != null)
            {
                authToken = authHeader.FirstOrDefault().Value.ToString();
                LoginReturnModel loginObj = accountService.AuthValidility(authToken);
                if (loginObj.IsSuccfull == false)
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "User is not logged in or token is invalid";
                    return returnValue;
                }

                loginId = loginObj.LoginId;
            }
            UserModel user = accountService.GetUsers(x => x.LoginId == loginId).FirstOrDefault();
            List<QueueAppVM> model = queueService.GetUserQueuesList(user.Id);
            if (model != null && model.Count > 0)
            {
                returnValue.IsSucessfull = true;
                returnValue.Result = model;
            }
            return returnValue;
        }

    }
}
