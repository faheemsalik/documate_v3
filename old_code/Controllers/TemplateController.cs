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
    public class TemplateController : ControllerBase
    {
        private readonly ILogger<TemplateController> _logger;
        private readonly ITemplateService templateService;

        public TemplateController(ILogger<TemplateController> logger, ITemplateService templateService)
        {
            _logger = logger;
            this.templateService = templateService;
        }

        [HttpPost]
        public async Task<ResponseModel> SaveTemplate(Template template)
        {
            return await templateService.SaveTemplate(template);
        }

        [HttpPost]
        public async Task<ResponseModel> SaveTemplateKeyword(TemplateKeyword templateKeyword)
        {
            return await templateService.SaveTemplateKeyword(templateKeyword);
        }
        [HttpPost]
        public async Task<ResponseModel> SaveKeywordElement(KeywordElement keywordElement)
        {
            return await templateService.SaveKeywordElement(keywordElement);
        }

        [HttpGet]
        public ResponseModel GetTemplateKeywordList(int templateId)
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue.Result = templateService.GetTemplateKeywords(templateId);
            return returnValue;
        }
        [HttpGet]
        public ResponseModel GetTemplateList()
        {
            _logger.LogInformation("Getting Templates");
            ResponseModel returnValue = new ResponseModel();
            returnValue.Result = templateService.GetTemplates();
            return returnValue;
        }

        [HttpGet]
        public ResponseModel GetKeywordElementList(int templateKeywordId)
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue.Result = templateService.GetKeywordElements(templateKeywordId);
            return returnValue;
        }
        [HttpGet]
        public ResponseModel GetIdentifyingElementList()
        {
            ResponseModel returnValue = new ResponseModel();
            returnValue.Result = templateService.GetIdentifyingElements();
            return returnValue;
        }
    }
}
