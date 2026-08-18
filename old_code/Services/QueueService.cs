using Amazon.Runtime.Internal.Util;
using Documate.Data;
using Documate.Domain;
using Documate.Extensions;
using Documate.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Documate.Services
{
    public interface IQueueService
    {
        Task<ResponseModel> CreateQueue(QueueAppVM queue);
        Task<ResponseModel> UpdateQueue(QueueModel model);
        UserQueueModel GetUserQueue(int userId, int queueId);
        QueueModel GetEntityById(int id);
        Task<List<QueueAppVM>> GetAccountQueuesList(int userId);
        List<QueueAppVM> GetUserQueuesList(int userId);
        List<QueueModel> GetAllQueuesList();
    }

    public class QueueService : IQueueService
    {
        private readonly ILogger<QueueService> Logger;
        private readonly IQueueRepo queueRepo;
        private readonly IUserQueueRepo userQueueRepo;
        private readonly IAccountRepo accountRepo;
        private readonly IUserRepo userRepo;
        private readonly INanoModelRepo nanoModelRepo;
        private readonly IDocStorageRepo docStorageRepo;

        public QueueService(
            ILogger<QueueService> logger,
            IQueueRepo queueRepo,
            IUserQueueRepo userQueueRepo,
            IAccountRepo accountRepo,
            IUserRepo userRepo,
            INanoModelRepo nanoModelRepo,
            IDocStorageRepo docStorageRepo
            )
        {
            Logger = logger;
            this.queueRepo = queueRepo;
            this.userQueueRepo = userQueueRepo;
            this.accountRepo = accountRepo;
            this.userRepo = userRepo;
            this.nanoModelRepo = nanoModelRepo;
            this.docStorageRepo = docStorageRepo;
        }

        public async Task<ResponseModel> CreateQueue(QueueAppVM queue)
        {
            ResponseModel returnValue = null;
            RepoResult repoResult = new RepoResult();
            string logMessage = "Error on Loading";
            try
            {
                Queue QueueEntity = new Queue();
                QueueEntity.CopyPropertyValues(queue);

                await queueRepo.InsertOrUpdate(QueueEntity, false);
                if (repoResult.success == true)
                {
                    returnValue.Result = repoResult.data;
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Queue has beens created successfully.";
                }
                else
                {
                    logMessage = "Saves changes return 0";
                    returnValue.Message = "Queue could not be created";
                }
            }
            catch (Exception ex)
            {
                returnValue.Message = ex.Message + " - " + logMessage;
                Logger.LogError(ex.Message + " - " + logMessage);
            }
            return returnValue;
        }
        public async Task<ResponseModel> UpdateQueue(QueueModel model)
        {
            ResponseModel returnValue = new ResponseModel();
            RepoResult repoResult = new RepoResult();
            string logMessage = "Error on Loading";
            try
            {
                Queue entity = await queueRepo.GetEntityById(model.Id);

                if (entity == null)
                    throw new KeyNotFoundException("Queue data could not be updated. Queue does not exist.");
                entity.CopyPropertyValues(model);
                await queueRepo.InsertOrUpdate(entity, false);
                //TODO Update Queue Keywords according to updated schema
                // Get all datapoint type schema_ids and update/create keywords tables accordingly.
                if (repoResult.success == true)
                {
                    returnValue.Result = repoResult.data;
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Queue has beens updated successfully.";
                }
                else
                {
                    logMessage = "Saves changes return 0";
                    returnValue.Message = "Queue could not be updated";
                }
            }
            catch (Exception ex)
            {
                returnValue.Message = ex.Message + " - " + logMessage;
                Logger.LogError(ex.Message + " - " + logMessage);
            }
            return returnValue;
        }

        public UserQueueModel GetUserQueue(int userId, int queueId)
        {
            UserQueueModel returnValue = new UserQueueModel();
            var query = from userQueue in userQueueRepo.Table
                        join queue in queueRepo.Table on userQueue.QueueId equals queue.Id
                        where
                            userQueue.UserId == userId
                            && userQueue.QueueId == queueId
                            && queue.FlgActive == true
                            && queue.FlgDeleted == false
                        select new UserQueueModel
                        {
                            Id = userQueue.Id,
                            UserId = userId,
                            QueueId = userQueue.QueueId
                        };
            returnValue = query.ToList().FirstOrDefault();
            return returnValue;
        }

        public QueueModel GetEntityById(int id)
        {
            QueueModel returnValue = new QueueModel();
            var qry = from queue in queueRepo.Table
                      join nanoModel in nanoModelRepo.Table on queue.ModelId equals nanoModel.Id
                      join docStorage in docStorageRepo.Table on queue.StorageId equals docStorage.Id
                      where queue.Id == id
                      select new QueueModel
                      {
                          Id = queue.Id,
                          QueueName = queue.QueueName,
                          Description = queue.Description,
                          FlgActive = queue.FlgActive,
                          WebhookURL = queue.WebhookURL,
                          AutomationLevel = queue.AutomationLevel,
                          //ConfidenceScoreThresold = queue.ConfidenceScoreThresold,
                          //DocTypeId = queue.DocTypeId,
                          AccountId = queue.AccountId,
                          S3BucketName = docStorage.BucketName,
                          NanoModelId = nanoModel.NanoModelId,
                          ModelId = queue.ModelId,
                          StorageId = queue.StorageId,
                          AiServiceSource = queue.AiServiceSource,
                          SchemaJSON = queue.SchemaJSON,
                          OpenAiAssistantId = queue.OpenAiAssistantId,
                          TextExtractionService = queue.TextExtractionService
                      };
            returnValue = qry.ToList().FirstOrDefault();
            return returnValue;
        }
        public List<QueueModel> GetAllQueuesList()
        {
            var qry = from queue in queueRepo.Table
                      where queue.FlgActive == true && queue.FlgDeleted == false
                      select new QueueModel
                      {
                          Id = queue.Id,
                          QueueName = queue.QueueName,
                          Description = queue.Description,
                          FlgActive = queue.FlgActive,
                          WebhookURL = queue.WebhookURL,
                          AutomationLevel = queue.AutomationLevel,
                          //ConfidenceScoreThresold = queue.ConfidenceScoreThresold,
                          //DocTypeId = queue.DocTypeId,
                          AccountId = queue.AccountId
                      };
            List<QueueModel> returnValue = qry.ToList();
            return returnValue;
        }

        public async Task<List<QueueAppVM>> GetAccountQueuesList(int userId)
        {
            var user = await userRepo.GetEntityById(userId);
            var account = await accountRepo.GetEntityById(user.AccountId);
            int accountId = account.Id;
            var qry = from queue in queueRepo.Table
                      where queue.AccountId == accountId && queue.FlgActive == true && queue.FlgDeleted == false
                      select new QueueAppVM
                      {
                          Id = queue.Id,
                          QueueName = queue.QueueName,
                          Description = queue.Description,
                          FlgActive = queue.FlgActive,
                          WebhookURL = queue.WebhookURL,
                          AutomationLevel = queue.AutomationLevel,
                          //ConfidenceScoreThresold = queue.ConfidenceScoreThresold,
                          //DocTypeId = queue.DocTypeId,
                          AccountId = queue.AccountId
                      };
            List<QueueAppVM> returnValue = qry.ToList();
            return returnValue;
        }
        public List<QueueAppVM> GetUserQueuesList(int userId)
        {
            var user = userRepo.GetEntityById(userId);
            var qry = from queue in queueRepo.Table
                      join userQueue in userQueueRepo.Table on queue.Id equals userQueue.QueueId
                      where userQueue.UserId == userId && queue.FlgActive == true && queue.FlgDeleted == false
                      select new QueueAppVM
                      {
                          Id = queue.Id,
                          QueueName = queue.QueueName,
                          Description = queue.Description,
                          FlgActive = queue.FlgActive,
                          WebhookURL = queue.WebhookURL,
                          AutomationLevel = queue.AutomationLevel,
                          //ConfidenceScoreThresold = queue.ConfidenceScoreThresold,
                          //DocTypeId = queue.DocTypeId,
                          AccountId = queue.AccountId
                      };
            List<QueueAppVM> returnValue = qry.ToList();
            return returnValue;
        }

    }
}
