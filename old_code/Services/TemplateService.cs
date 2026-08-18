using Amazon.Runtime.Internal.Util;
using Documate.Data;
using Documate.Domain;
using Documate.Extensions;
using Documate.Models;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Documate.Services
{
    public interface ITemplateService
    {
        List<Template> GetTemplates();
        List<TemplateKeyword> GetTemplateKeywords(int templateId);
        Task<ResponseModel> SaveTemplate(Template template);
        Task<ResponseModel> SaveTemplateKeyword(TemplateKeyword templateKeyword);
        Task<ResponseModel> SaveKeywordElement(KeywordElement keywordElement);
        List<KeywordElement> GetKeywordElements(int templateKeywordId);
        List<IdentifyingElement> GetIdentifyingElements();
        Task<Template> GetTemplateById(int id);
        Template GetTemplateByName(string templateName);
    }

    public class TemplateService : ITemplateService
    {
        private readonly ILogger<QueueService> Logger;
        private readonly IQueueRepo queueRepo;
        private readonly ITemplateRepo templateRepo;
        private readonly ITemplateQueueRepo templateQueueRepo;
        private readonly ITemplateKeywordRepo templateKeywordRepo;
        private readonly IKeywordElementRepo keywordElementRepo;
        private readonly IIdentifyingElementRepo identifyingElementRepo;


        public TemplateService(
            ILogger<QueueService> logger,
            IQueueRepo queueRepo,
            IUserQueueRepo userQueueRepo,
            ITemplateRepo templateRepo,
            ITemplateQueueRepo templateQueueRepo,
            ITemplateKeywordRepo templateKeywordRepo,
            IKeywordElementRepo keywordElementRepo,
            IIdentifyingElementRepo identifyingElementRepo
            )
        {
            Logger = logger;
            this.queueRepo = queueRepo;
            this.templateRepo = templateRepo;
            this.templateQueueRepo = templateQueueRepo;
            this.templateKeywordRepo = templateKeywordRepo;
            this.keywordElementRepo = keywordElementRepo;
            this.identifyingElementRepo = identifyingElementRepo;

        }

        public async Task<ResponseModel> SaveTemplate(Template template)
        {
            ResponseModel returnValue = new ResponseModel();
            RepoResult repoResult = new RepoResult();
            try
            {
                Template entity = new Template();
                if (template.Id > 0)
                    entity = await templateRepo.GetEntityById(template.Id);
                else
                    entity = GetTemplateByName(template.TemplateName);
                if (entity == null) entity = new Template();
                entity.CopyPropertyValues(template);
                await templateRepo.InsertOrUpdate(entity, false);
                if (repoResult.success == true)
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Template saved/updated successfully.";
                    returnValue.Result = repoResult.keyColId;
                }
                else
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "Template could not be saved/updated";
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = true;
                returnValue.Message = "Template could not be saved/updated.";
                throw ex;
            }
            return returnValue;
        }

        public async Task<ResponseModel> SaveTemplateKeyword(TemplateKeyword templateKeyword)
        {
            ResponseModel returnValue = new ResponseModel();
            RepoResult repoResult = new RepoResult();
            try
            {
                TemplateKeyword entity = await templateKeywordRepo.GetEntityById(templateKeyword.Id);
                if (entity == null) entity = new TemplateKeyword();
                entity.CopyPropertyValues(templateKeyword);
                await templateKeywordRepo.InsertOrUpdate(entity, false);
                if (repoResult.success == true)
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Template Keyword saved/updated successfully.";
                }
                else
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "Template Keyword could not be saved/updated";
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = true;
                returnValue.Message = "Template Keyword could not be saved/updated.";
                throw ex;
            }
            return returnValue;
        }

        public async Task<ResponseModel> SaveKeywordElement(KeywordElement keywordElement)
        {
            ResponseModel returnValue = new ResponseModel();
            RepoResult repoResult = new RepoResult();
            try
            {
                KeywordElement entity = await keywordElementRepo.GetEntityById(keywordElement.Id);
                if (entity == null) entity = new KeywordElement();
                entity.CopyPropertyValues(keywordElement);
                await keywordElementRepo.InsertOrUpdate(entity, false);
                if (repoResult.success == true)
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Keyword Element saved/updated successfully.";
                }
                else
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "Keyword Element could not be saved/updated";
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = true;
                returnValue.Message = " Keyword Element could not be saved/updated.";
                throw ex;
            }
            return returnValue;
        }

        public TemplateQueueModel GetTemplateQueue(int templateId, int queueId)
        {
            var qry = from t in templateRepo.Table
                      join tq in templateQueueRepo.Table on t.Id equals tq.TemplateId
                      where tq.TemplateId == templateId && tq.QueueId == queueId
                      select new TemplateQueueModel
                      {
                          TemplateId = t.Id,
                          TemplateName = t.TemplateName,
                          DocIdentifier = t.DocIdentifier,
                          TemplateQueueId = tq.Id,
                          QueueId = tq.QueueId
                      };
            return qry.ToList().FirstOrDefault();
        }
        public List<Template> GetTemplates()
        {
            return templateRepo.GetEntities().ToList();
        }
        public async Task<Template> GetTemplateById(int id)
        {
            return await templateRepo.GetEntityById(id);
        }
        public Template GetTemplateByName(string templateName)
        {
            Template template = templateRepo.GetEntities().FirstOrDefault(x => x.TemplateName == templateName);
            return template;
        }
        public List<TemplateKeyword> GetTemplateKeywords(int templateId)
        {
            return templateKeywordRepo.GetEntities(x => x.TemplateId == templateId).ToList();
        }

        public List<KeywordElement> GetKeywordElements(int templateKeywordId)
        {
            return keywordElementRepo.GetEntities(x => x.TemplateKeywordId == templateKeywordId).ToList();
        }

        public List<IdentifyingElement> GetIdentifyingElements()
        {
            return identifyingElementRepo.GetEntities().ToList();
        }
    }
}
