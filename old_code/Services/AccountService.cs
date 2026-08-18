using Documate.Data;
using Documate.Domain;
using Documate.Extensions;
using Documate.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Documate.Services
{
    public interface IAccountService
    {
        Task<ResponseModel> CreateAccount(Account account);
        Task<ResponseModel> CreateUser(User user);
        LoginReturnModel AuthValidility(string tokenStr);
        UserModel GetUserFromAuthToken(string tokenStr);
        AccountModel GetAccountByQueueId(int docId);
        List<AccountModel> GetAccounts(Expression<Func<AccountModel, bool>> where = null);
        List<UserModel> GetUsers(Expression<Func<UserModel, bool>> where = null);
        ResponseModel Login(CredentialModel credential);
        int GetUserIdFromLoginId(string loginId);
    }
    //================================================================

    public class AccountService : IAccountService
    {
        private readonly DBContext dbContext;
        private readonly ILogger<AccountService> Logger;
        private readonly IAccountRepo accountRepo;
        private readonly IUserRepo userRepo;
        private readonly IAuthTokenRepo authTokenRepo;
        private readonly IQueueRepo queueRepo;

        public AccountService(
            ILogger<AccountService> logger,
            DBContext context, IUserRepo userRepo,
            IAccountRepo accountRepo,
            IAuthTokenRepo authTokenRepo,
            IQueueRepo queueRepo
            )
        {
            Logger = logger;
            dbContext = context;
            this.userRepo = userRepo;
            this.accountRepo = accountRepo;
            this.authTokenRepo = authTokenRepo;
            this.queueRepo = queueRepo;
        }

        public async Task<ResponseModel> CreateAccount(Account account)
        {
            ResponseModel returnValue = new ResponseModel();
            RepoResult repoResult = new RepoResult();
            string logMessage = "Error creating context";
            try
            {
                Account entity = await accountRepo.GetEntityById(account.Id);
                if (entity == null) entity = new Account();
                entity.CopyPropertyValues(account);
                account.CreditBalance = 10;
                account.FlgActive = true;
                account.AiServiceSource = 3;
                await accountRepo.InsertOrUpdate(account, false);
                if (repoResult.success == true)
                {
                    returnValue.Result = repoResult.data;
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Account has beens created successfully.";
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

        public async Task<ResponseModel> CreateUser(User user)
        {
            ResponseModel returnValue = new ResponseModel();
            //---------------- Validation
            // duplication
            User existingUser = GetUsers(x => x.LoginId == user.LoginId).FirstOrDefault();
            if (existingUser != null)
            {
                returnValue.IsSucessfull = false;
                returnValue.Message = "Login Id already exists. Please use a different login name.";
                returnValue.StatusCode = 208;
                return returnValue;
            }
            // login id must be a valid email
            bool isEmail = Regex.IsMatch(user.LoginId, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase);
            if (isEmail == false)
            {
                returnValue.IsSucessfull = false;
                returnValue.Message = "Login id is not a valid email address";
                returnValue.StatusCode = 422; // unprocessable entry
                return returnValue;
            }
            // password validation
            if (!passwordValidation(user.Password))
            {
                returnValue.IsSucessfull = false;
                returnValue.Message = "Password doesn't fullfil the complexity requirement";
                returnValue.StatusCode = 422;
                return returnValue;
            }
            //-------- Validation End
            RepoResult repoResult = new RepoResult();
            string logMessage = "Error creating context";
            try
            {
                await userRepo.InsertOrUpdate(user, false);
                if (repoResult.success == true)
                {
                    returnValue.Result = repoResult.data;
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "User has beens created successfully";
                    returnValue.StatusCode = 200;
                }
                else
                {
                    logMessage = "Saves changes return 0";
                    returnValue.Message = "User could not be created. Please contact customer support.";
                    returnValue.IsSucessfull = false;
                    returnValue.StatusCode = 422; // unprocessable entry
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + " - " + logMessage);
                repoResult.success = false;
                returnValue.IsSucessfull = false;
            }
            return returnValue;
        }

        public List<UserModel> GetUsers(Expression<Func<UserModel, bool>> where = null)
        {
            List<UserModel> returnValue = new List<UserModel>();
            try
            {
                IQueryable<UserModel> query = from user in userRepo.Table
                                              join acc in accountRepo.Table on user.AccountId equals acc.Id
                                              select new UserModel
                                              {
                                                  Id = user.Id,
                                                  UserName = user.UserName,
                                                  AccountName = acc.Name,
                                                  Password = user.Password,
                                                  LoginId = user.LoginId
                                              };
                query = query.Where(where);
                returnValue = query.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in GetUsers msg:" + ex.Message);
            }
            return returnValue;
        }

        public List<AccountModel> GetAccounts(Expression<Func<AccountModel, bool>> where = null)
        {
            var query = from acc in accountRepo.Table
                        select new AccountModel
                        {
                            Id = acc.Id,
                            Name = acc.Name,
                            CreditBalance = acc.CreditBalance,
                            FlgDeleted = acc.FlgDeleted,
                            FlgActive = acc.FlgActive
                        };
            query = query.Where(where);
            return query.ToList();
        }

        public LoginReturnModel AuthValidility(string tokenStr)
        {
            //TODO Expiry check to add.
            LoginReturnModel returnObj = new LoginReturnModel();
            var query = from authToken in authTokenRepo.Table
                        join user in userRepo.Table on authToken.UserId equals user.Id
                        where authToken.Token == tokenStr //&& DateTime.Now.ToUniversalTime().AddHours(authToken.ExpiryHours) >= DateTime.Now.ToUniversalTime()
                        select new LoginReturnModel
                        {
                            LoginId = user.LoginId,
                            UserName = user.UserName,
                            Token = authToken.Token,
                            TokenExpiry = DateTime.Now.ToUniversalTime().AddHours(authToken.ExpiryHours)
                        };
            var output = query.ToList().FirstOrDefault();
            if (output == null)
            {
                returnObj.Token = "";
                returnObj.IsSuccfull = false;
                returnObj.Message = "Token is invalid or expired";
            }
            else
            {
                returnObj = output;
                returnObj.IsSuccfull = true;
            }
            return returnObj;
        }

        public UserModel GetUserFromAuthToken(string tokenStr)
        {
            var query = from authToken in authTokenRepo.Table
                        join user in userRepo.Table on authToken.UserId equals user.Id
                        where authToken.Token == tokenStr //&& DateTime.Now.ToUniversalTime().AddHours(authToken.ExpiryHours) >= DateTime.Now.ToUniversalTime()
                        select new UserModel
                        {
                            Id = authToken.UserId,
                            UserName = user.UserName,
                            AccountId = user.AccountId
                        };
            List<UserModel> tempList = query.ToList();
            if (tempList != null && tempList.Count > 0)
            {
                return tempList.FirstOrDefault();
            }
            return new UserModel();
        }
        public AccountModel GetAccountByQueueId(int queueId)
        {
            var qry = from queue in queueRepo.Table
                      join account in accountRepo.Table on queue.AccountId equals account.Id
                      where queue.Id == queueId
                      select new AccountModel
                      {
                          Id = account.Id,
                          Name = account.Name,
                          CreditBalance = account.CreditBalance
                      };
            var returnValue = qry.FirstOrDefault();
            return returnValue;
        }
        public ResponseModel Login(CredentialModel credential)
        {
            ResponseModel returnValue = new ResponseModel();
            LoginReturnModel ret = new LoginReturnModel();
            User user = GetUsers(x => x.LoginId == credential.username && x.Password == credential.password).FirstOrDefault();
            if (user != null)
            {
                ret.Token = "123456789";
                ret.TokenExpiry = DateTime.Now.ToUniversalTime().AddDays(90);
                ret.LoginId = user.LoginId;
                ret.UserName = user.UserName;
                returnValue.Result = ret;
                returnValue.IsSucessfull = true;
                returnValue.Message = "Successfully logged in. The token has been issued with 90 days expiry.";
            }
            return returnValue;
        }
        private bool passwordValidation(string password)
        {
            bool returnValue = false;
            // password length
            if (password.Trim().Length < 8) return returnValue;
            // password complexity
            var complexity = new Regex("(?=.*[0 - 9])(?=.*[a - z])(?=.*[A - Z])(?=.*[@#$%]).{8,40}");
            if (!complexity.IsMatch(password)) return returnValue;
            returnValue = true;
            return returnValue;
        }

        public int GetUserIdFromLoginId(string loginId)
        {
            int userId = 0;
            User user = GetUsers(x => x.LoginId == loginId).FirstOrDefault();
            if (user != null)
                userId = user.Id;
            return userId;
        }
    }

}
