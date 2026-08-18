using Documate.Data;
using Documate.Domain;
using Documate.Extensions;
using Documate.Models;
using Microsoft.Extensions.Logging;

using MimeKit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Documate.Services
{
    public interface IMailkitService
    {
        ResponseModel ProcessMsg();
    }
    //================================================================

    public class MailkitService : IMailkitService
    {
        private readonly DBContext dbContext;
        private readonly ILogger<AccountService> Logger;

        public MailkitService(
            ILogger<AccountService> logger,
            DBContext context, IUserRepo userRepo
            )
        {
            Logger = logger;
            dbContext = context;
        }

        public ResponseModel ProcessMsg()
        {
            ResponseModel returnValue = new ResponseModel();
            string logMessage = "Error creating context";
            try
            {
                MimeMessage msg = new MimeMessage();
                //var message = MimeMessage.Load(stream);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + " - " + logMessage);
            }
            return returnValue;
        }

    }

}
