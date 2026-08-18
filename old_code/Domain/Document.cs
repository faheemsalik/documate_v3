using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Documate.Domain
{
    [DebuggerDisplay("{Id}- {FileName}- Status Id>{StatusId}")]
    public class Document : BaseEntity
    {
        [MaxLength(256), Required]
        public string FileName { get; set; }
        [MaxLength(256)]
        public string Description { get; set; }
        [MaxLength(256)]
        public string ProcessingRemarks { get; set; }
        [MaxLength(256)]
        public string FailedException { get; set; }
        public string RawDataJSON { get; set; }
        public string ProcessedDataJSON { get; set; }
        public string UserAnnotation { get; set; }

        public DateTime? StartProccessingDateTimeUTC { get; set; }
        public DateTime? EndProccessingDateTimeUTC { get; set; }
        [MaxLength(128)]
        public string AwsJobId { get; set; } // use this column only for Aws job id not for Nano response
        public int NoOfRetries { get; set; }
        public bool FlgFailed { get; set; }
        [MaxLength(200)]
        public string OriginalFileName { get; set; }
        [MaxLength(100)]
        public string UserMetaData { get; set; }
        [MaxLength(20)]
        public string ContentType { get; set; }
        public string ElementsMissing { get; set; }
        public int TemplateId { get; set; }
        [MaxLength(64)]
        public string NanoRequestFileId { get; set; }
        public bool FlgWebbookCalled { get; set; }
        public int PageCount { get; set; }
        public bool IsModerated { get; set; }
        public bool IsArchive { get; set; } // when we archive a document, it will participate in any process
        public string NanoUploadResponse { get; set; } // required to store nano response data for multi page document
        [MaxLength(512)]
        public string AdditionalPrompt { get; set; }

        [MaxLength(64)]
        public string ThreadId { get; set; } // required to store nano response data for multi page document

        [ForeignKey(nameof(SysDocStatusObj))]
        public int StatusId { get; set; }
        public virtual SysDocStatus SysDocStatusObj { get; set; }

        [ForeignKey(nameof(QueueObj))]
        public int QueueId { get; set; }
        public virtual Queue QueueObj { get; set; } // Final decision. Document should have QueueID instead of userQueue. UserQueue is only to check access of the user.

        [ForeignKey(nameof(NanoModelObj))]
        public int? ModelId { get; set; }
        public virtual NanoModel NanoModelObj { get; set; }
    }

    public enum AutomationLevel
    { 
        NEVER=0,
        CONFIDENCE_LEVEL=1,
        ALWAYS=2
    }

}
