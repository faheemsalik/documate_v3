using Amazon.Runtime.Internal.Util;
using Documate.Data;
using Documate.Domain;
using Documate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Documate.Services
{
    public interface IBillingService
    {
        ResponseModel CreateInvoice(Account account);
    }
    //================================================================

    public class BillingService : IBillingService
    {
        private readonly ILogger<AccountService> Logger;

        public BillingService(ILogger<AccountService> logger)
        {
            Logger = logger;
        }

        public ResponseModel CreateInvoice(Account account)
        {
            ResponseModel returnValue = null;
            returnValue.IsSucessfull = false;
            string logMessage = "Error creating context";
            try
            {
                //returnValue.TheObject = SaveAccount(account);
                if (returnValue.Result != null)
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Account has beens aved successfully";
                }
                else
                {
                    logMessage = "Saves changes return 0";
                    returnValue.Message = "Account could not be created";
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + " - " + logMessage);
            }
            return returnValue;
        }
    }
}
