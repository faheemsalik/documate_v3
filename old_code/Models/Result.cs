using Microsoft.AspNetCore.Mvc.ViewFeatures;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Documate.Models
{
    public class ActionResultTechnical
    {
        public string stackTrace { get; set; } = "";
        public string controller { get; set; }
        public string action { get; set; }
        public string view { get; set; }

        public ViewDataDictionary ViewData { get; set; }
        public TempDataDictionary TempDate { get; set; }
    }

    public interface IRepoResult
    {
        bool success { get; set; }
        string successMsg { get; set; }
        int keyColId { get; set; }
        //ToDo find replacement of exception class
        //ExceptionMsg exception { get; set; }
        List<string> errorList { get; set; }
        Dictionary<string, string> modelStateErrors { get; set; }

        //string redirectTo { get; set; }
        //string reloadUrl { get; set; }

        ActionResultTechnical technical { get; set; }
        List<string> log { get; set; }
        int statusCode { get; set; }

        dynamic data { get; set; }
    }

    public class RepoResult : IRepoResult
    {
        public RepoResult()
        {
            log = new List<string>();
            //exception = new ExceptionMsg();
            errorList = new List<string>();
            modelStateErrors = new Dictionary<string, string>();
            technical = new ActionResultTechnical();
        }

        public bool isCustomResponse = true;
        public int keyColId { get; set; } = 0;
        public bool success { get; set; } = false;
        public string successMsg { get; set; }

        //public ExceptionMsg exception { get; set; }
        public List<string> errorList { get; set; }
        public Dictionary<string, string> modelStateErrors { get; set; }

        public string redirectTo { get; set; }
        public string reloadUrl { get; set; }

        public ActionResultTechnical technical { get; set; }
        public List<string> log { get; set; }
        public int statusCode { get; set; }

        public dynamic data { get; set; }
    }
}
