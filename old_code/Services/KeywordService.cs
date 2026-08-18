using Documate.Data;
using Documate.Domain;
using Documate.Models;
using Documate.Common.Models;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using Documate.Extensions;
using Documate.Services;
using System.Threading.Tasks;

namespace Documate.Services
{
    public interface IKeywordService
    {
        List<KeywordSynomModel> GetAllKeywordSynom();
        List<KeywordSynomModel> GetAllKeywords(int templateId);
        List<IdentifyingElement> GetIdentifyingElements(Expression<Func<IdentifyingElement, bool>> where = null);
        List<TemplateKwElementModel> GetTemplateKwElements(int templateKeywordId);
        List<MasterKeywordSet> GetMasterKeywordList();
        Task<ResponseModel> SaveMasterKeyword(MasterKeywordSet keyword);
        List<KeywordSynonym> GetSynonymList(int keywordId);
        Task<ResponseModel> SaveSynonym(KeywordSynonym keyword);
    }

    public class KeywordService : IKeywordService
    {
        private readonly ILogger<DocumentService> Logger;
        private readonly IDocumentRepo documentRepo;
        private readonly IMasterKeywordSetRepo masterKwRepo;
        private readonly IKeywordSynonymRepo synonymRepo;
        private readonly IIdentifyingElementRepo identifyingElementRepo;
        private readonly ITemplateKeywordRepo templateKeywordRepo;
        private readonly IKeywordElementRepo keywordElementRepo;

        public KeywordService(
            ILogger<DocumentService> logger,
            IDocumentRepo documentRepo,
            IMasterKeywordSetRepo masterKwRepo,
            IKeywordSynonymRepo synonymRepo,
            IIdentifyingElementRepo identifyingElementRepo,
            ITemplateKeywordRepo templateKeywordRepo,
            IKeywordElementRepo keywordElementRepo
            )
        {
            Logger = logger;
            this.documentRepo = documentRepo;
            this.masterKwRepo = masterKwRepo;
            this.synonymRepo = synonymRepo;
            this.identifyingElementRepo = identifyingElementRepo;
            this.templateKeywordRepo = templateKeywordRepo;
            this.keywordElementRepo = keywordElementRepo;
        }

        public List<KeywordSynomModel> GetAllKeywordSynom()
        {
            List<KeywordSynomModel> model = null;
            try
            {
                var query = from kw in masterKwRepo.Table
                            join synom in synonymRepo.Table on kw.Id equals synom.MasterKeywordId
                            where kw.FlgDeleted == false
                            select new KeywordSynomModel
                            {
                                Id = kw.Id,
                                Keyword = kw.Keyword,
                                SchemaId = kw.SchemaId,
                                AwsBlock = kw.AwsBlock,
                                Synonym = synom.Synonym,
                                weight = synom.weight,
                                MasterKeywordId = synom.MasterKeywordId
                            };
                query = query.OrderByDescending(x => x.weight);
                model = query.ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return model;
        }
        public List<KeywordSynomModel> GetAllKeywords(int templateId)
        {
            List<KeywordSynomModel> model = null;
            try
            {
                var keywords = (from kw in masterKwRepo.Table
                                join synom in synonymRepo.Table on kw.Id equals synom.MasterKeywordId
                                where kw.FlgDeleted == false
                                select new KeywordSynomModel
                                {
                                    Id = kw.Id,
                                    Keyword = synom.Synonym,
                                    SchemaId = kw.SchemaId,
                                    AwsBlock = kw.AwsBlock,
                                    weight = synom.weight,
                                    MasterKeywordId = synom.MasterKeywordId,
                                    ValuePosition = null
                                }).ToList();

                var templateKeywords = (from tkw in templateKeywordRepo.Table
                                        where tkw.FlgDeleted == false && tkw.TemplateId == templateId
                                        select new KeywordSynomModel
                                        {
                                            Id = tkw.Id,
                                            Keyword = tkw.Keyword,
                                            SchemaId = tkw.SchemaId,
                                            AwsBlock = tkw.AwsBlock,
                                            weight = (float)1.0,
                                            MasterKeywordId = 0,
                                            ValuePosition = tkw.ValuePosition
                                        }).ToList();
                model = keywords.Union(templateKeywords).OrderBy(x => x.MasterKeywordId).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return model;
        }
        public List<IdentifyingElement> GetIdentifyingElements(Expression<Func<IdentifyingElement, bool>> where = null)
        {
            return identifyingElementRepo.GetEntities(where).ToList();
        }

        public List<TemplateKwElementModel> GetTemplateKwElements(int templateId)
        {
            var query = from tkw in templateKeywordRepo.Table
                        join kwe in keywordElementRepo.Table on tkw.Id equals kwe.TemplateKeywordId
                        join element in identifyingElementRepo.Table on kwe.ElementId equals element.Id
                        where tkw.TemplateId == templateId

                        select new TemplateKwElementModel
                        {
                            ElementId = element.Id,
                            kwElementId = kwe.Id,
                            ElementName = element.ElementName,
                            ElementKey = element.Elementkey,
                            Keyword = tkw.Keyword,
                            TemplateKeywordId = tkw.Id,
                            ValueNum = kwe.ValueNum,
                            ValueStr = kwe.ValueStr,
                            SchemaId = tkw.SchemaId,
                            AwsBlock = tkw.AwsBlock,
                            ComparisonType = kwe.ComparisonType
                        };
            return query.ToList();
        }

        public List<MasterKeywordSet> GetMasterKeywordList()
        {
            return masterKwRepo.GetEntities().ToList();
        }

        public async Task<ResponseModel> SaveMasterKeyword(MasterKeywordSet keyword)
        {
            ResponseModel returnValue = new ResponseModel();
            RepoResult repoResult = new RepoResult();
            try
            {
                MasterKeywordSet entity = await masterKwRepo.GetEntityById(keyword.Id);
                if (entity == null) entity = new MasterKeywordSet();
                entity.CopyPropertyValues(keyword);
                await masterKwRepo.InsertOrUpdate(entity, false);
                if (repoResult.success == true)
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Keyword saved/updated successfully.";
                }
                else
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "Keyword could not be saved/updated";
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = true;
                returnValue.Message = "Keyword could not be saved/updated.";
                throw ex;
            }
            return returnValue;
        }

        public List<KeywordSynonym> GetSynonymList(int keywordId)
        {
            return synonymRepo.GetEntities(x => x.MasterKeywordId == keywordId).ToList();
        }

        public async Task<ResponseModel> SaveSynonym(KeywordSynonym synonym)
        {
            ResponseModel returnValue = new ResponseModel();
            RepoResult repoResult = new RepoResult();
            try
            {
                KeywordSynonym entity = await synonymRepo.GetEntityById(synonym.Id);
                if (entity == null) entity = new KeywordSynonym();
                entity.CopyPropertyValues(synonym);
                await synonymRepo.InsertOrUpdate(entity, false);
                if (repoResult.success == true)
                {
                    returnValue.IsSucessfull = true;
                    returnValue.Message = "Synonym saved/updated successfully.";
                }
                else
                {
                    returnValue.IsSucessfull = false;
                    returnValue.Message = "Synonym could not be saved/updated";
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSucessfull = true;
                returnValue.Message = "Synonym could not be saved/updated.";
                throw ex;
            }
            return returnValue;
        }

    }


}
