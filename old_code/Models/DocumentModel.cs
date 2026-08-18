using Documate.Domain;

using Documate.Common.Models;

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Documate.Services;

namespace Documate.Models
{
    [NotMapped]
    public class DocumentModel : Domain.Document
    {
        //private int queueId;
        public byte[] FileBytes { get; set; }
        public string FileBase64 { get; set; }
        public MemoryStream MemStream { get; set; }
        public string BucketName { get; set; }

        //public int QueueId { 
        //    get { return UserQueueId; }
        //    set { QueueId = value; } 
    } // For external user only 

    //public class AwsJobStatus
    //{
    //    private AwsJobStatus(string value) { Value = value; }

    //    public string Value { get; set; }

    //    public static AwsJobStatus Trace { get { return new AwsJobStatus("IN_PROGRESS"); } }
    //    public static AwsJobStatus Debug { get { return new AwsJobStatus("SUCCEEDED"); } }
    //    public static AwsJobStatus Info { get { return new AwsJobStatus("FAILED"); } }
    //    public static AwsJobStatus Warning { get { return new AwsJobStatus("PARTIAL_SUCCESS"); } }
    //}
    public enum AwsJobStatus
    {
        IN_PROGRESS,
        SUCCEEDED,
        FAILED,
        PARTIAL_SUCCESS
    }

    [NotMapped]
    public class ResponseDocModel
    {
        public int id { get; set; }
        public string original_file_name { get; set; }
        public string original_file_content { get; set; } // url to download the original file
        public List<ResponseDocMessages> messages { get; set; }
        public DocumateDocStatus status { get; set; }
        public int queue_id { get; set; } //Id with full api endpoint

        public DateTime? arrived_at { get; set; }
        public DateTime? exported_at { get; set; }
        public string content { get; set; } // url to get content of the exported data
        //public List<string> pages { get; set; }
        //public DateTime? assigned_at { get; set; }
        //public DocStatus? previous_status { get; set; }
        //public DateTime? modified_at { get; set; }
        //public DateTime? confirmed_at { get; set; }
        //public string schema { get; set; } //Id with full api endpoint
        //public string queue { get; set; } //Id with full api endpoint
    }

    [NotMapped]
    public class ResponseDocMessages
    {
        public int id { get; set; }
        public string message { get; set; }
        public string type { get; set; }
    }
    //public enum DocStatus
    //{
    //    NOT_SET = 0,
    //    IMPORTING = 1, //Document is being processed by the AI Core Engine for data extraction; initial state of the document.
    //    FAILED_IMPORT = 2,  //Import failed e.g.due to a malformed document file.
    //    TO_REVIEW = 3, //Initial extraction step is done and the document is waiting for user validation.
    //    REVIEWING = 4, //Document is undergoing validation in the user interface.
    //    EXPORTING = 5, //Document is validated and is now awaiting the completion of connector save call.See connector extension for more information on this status.
    //    EXPORTED = 6, //Document is validated and successfully passed all hooks; this is the typical terminal state of a document.
    //    FAILED_EXPORT = 7, // When the connector returned an error.
    //    POSTPONED = 8, // Operator has chosen to postpone the document instead of exporting it.
    //    DELETED = 9, //When the document was deleted by the user.
    //    PURGED = 10 //Only metadata was preserved after a deletion.
    //}

    [NotMapped]
    public class DocDebugDataVM
    {
        public int DocId { get; set; }
        public string RawDataJSON { get; set; }
        public string ProcessedDataJSON { get; set; }
        public string AwsJobId { get; set; }
    }
    //-------------------------------- Nanonets
    public class NanoBulkPredictionResponse
    {
        public int moderated_images_count { get; set; }
        public int unmoderated_images_count { get; set; }
        public string message { get; set; }
        public List<NanoGenericResponseDetail> moderated_images { get; set; }
        public dynamic signed_urls { get; set; }
    }

    public class NanoPredictionResponse
    {
        public string message { get; set; }
        public List<NanoGenericResponseDetail> result { get; set; }
        public dynamic signed_urls { get; set; }
    }

    public class NanoBulkPreditionResponseDetail : NanoGenericResponseDetail
    {
        public string model_id { get; set; }
        public string assigned_member { get; set; }
        public bool is_deleted { get; set; }
        public string source { get; set; }
        public int no_of_fields { get; set; }
        public double cost { get; set; }
        public double payable_cost { get; set; }
        public string status { get; set; }
        public int rotation { get; set; }
    }

    public class NanoWebhookResult
    {
        public List<NanoGenericResponse> result { get; set; }
    }
    public class NanoGenericResponse
    { 
        public string message { get; set; }
        public List<NanoGenericResponseDetail> result { get; set; }
        public dynamic signed_urls { get; set; }        
    }
    public class NanoGenericResponseDetail
    {        
        public string message { get; set; }
        public string input { get; set; }
        public int page { get; set; }
        public string request_file_id { get; set; }
        public string filepath { get; set; }
        public string id { get; set; }
        public List<NanoPrediction> prediction { get; set; }
        public List<NanoPrediction> predicted_boxes { get; set; }
        public List<NanoPrediction> moderated_boxes { get; set; }
        public string custom_response { get; set; }
        public int day_since_epoch { get; set; }
        public int hour_of_day { get; set; }
        public bool is_moderated { get; set; }
        public dynamic signed_urls { get; set; }
    }
    public class NanoFileURLs
    { 
		public string original { get; set; }
        public string original_compressed { get; set; }
        public string thumbnail { get; set; }
        public string acw_rotate_90 { get; set; }
        public string acw_rotate_180 { get; set; }
        public string acw_rotate_270 { get; set; }
        public string original_with_long_expiry { get; set; }
	}

    public class NanoPrediction
    {
        public string id { get; set; }
        public string label { get; set; }
        public int xmin { get; set; }
        public int ymin { get; set; }
        public int xmax { get; set; }
        public int ymax { get; set; }
        public double score { get; set; }
        public string ocr_text { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public List<NanoCell> cells { get; set; }
    }
    //public class NanoPageIds
    //{ 
    //    public string id { get; set; }
    //    public int pageNo { get; set; }
    //}


    public enum NanoFileStatus
    { 
        SUCCESS,
        PENDING,
        FAILURE
    }

    public class ExtractRawTextModel
    {
        public byte[] FileBytes { get; set; }
        public int? DocId { get; set; }
        public S3FileModel S3FileModel { get; set; }
        public EnumRawTextService Service { get; set; }
    }
}
