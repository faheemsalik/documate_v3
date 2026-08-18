using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Documate.Domain;
using Documate.Models;
using Documate.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Documate.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class KeywordController : ControllerBase
    {
        private readonly ILogger<KeywordController> _logger;
        private readonly IKeywordService keywordService;

        public KeywordController(ILogger<KeywordController> logger, IKeywordService keywordService)
        {
            _logger = logger;
            this.keywordService = keywordService;
        }

        [HttpPost]
        public ResponseModel UploadDocument(DocumentModel doc)
        {
            ResponseModel returnValue = null;
            returnValue.IsSucessfull = true;
            return returnValue;
        }

        [HttpGet]
        public ResponseModel GetMasterKeywordList()
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue.Result = keywordService.GetMasterKeywordList();
            return returnValue;
        }

        [HttpPost]
        public async Task<ResponseModel> SaveMasterKeyword(MasterKeywordSet keyword)
        {
            return await keywordService.SaveMasterKeyword(keyword);
        }

        [HttpGet]
        public ResponseModel GetSynonymList(int keywordId)
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue.Result = keywordService.GetSynonymList(keywordId);
            return returnValue;
        }

        [HttpGet]
        public ResponseModel GetTemplateKwElements(int templateId) // for debug use. not for public
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue.Result = keywordService.GetTemplateKwElements(templateId);
            return returnValue;
        }
        [HttpGet]
        public ResponseModel GetAllKeywordSynom() // for debug use. not for public
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue.Result = keywordService.GetAllKeywordSynom();
            return returnValue;
        }

        [HttpGet]
        public ResponseModel GetAllKeywords(int templateId) // for debug use. not for public
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue.Result = keywordService.GetAllKeywords(templateId);
            return returnValue;
        }

        [HttpPost]
        public async Task<ResponseModel> SaveSynonym(KeywordSynonym keyword)
        {
            return await keywordService.SaveSynonym(keyword);
        }

    }
}
