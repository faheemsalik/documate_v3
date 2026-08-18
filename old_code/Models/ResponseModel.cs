using System;
using System.Collections.Generic;

namespace Documate.Models
{
    public class ResponseModel
    {
        public bool IsSucessfull { get; set; } = false;
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public object Result { get; set; }
    }

    public class AssistantResponseModel
    {
        public string ExtractedJSON { get; set; }
        public string ThreadId { get; set; }
        public string ErrorMessage { get; set; }
    }

}
