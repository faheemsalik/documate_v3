using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Documate.Domain
{
    public class Queue : BaseEntity
    {
        [MaxLength(30)]
        public string QueueName { get; set; }
        [MaxLength(200)]
        public string Description { get; set; }
        public bool FlgActive{ get; set; }
        public string SchemaJSON { get; set; }

        //********************************************************************************************************************
        //                                                        Ai Fields 
        //********************************************************************************************************************
        /// <summary>
        /// Ai Service Enum: Queuemodel.EnumAiServiceSource
        /// AWS=0, ROSSUM=1, GOOGLE=2, NANO=3, OPENAI=4, CLAUDE=5, GEMINI=6
        /// </summary>
        public int AiServiceSource { get; set; } // 

        /// <summary>
        /// Raw Text extraction service: QueueModel.RawTextService
        ///  GOOGLE=0,  AWS=1
        /// </summary>
        /// 
        [DefaultValue(0)]
        public int TextExtractionService { get; set; } //Raw Text extraction service: QueueModel.RawTextService

        [MaxLength(64)]
        public string OpenAiAssistantId { get; set; } // to be used for Open Ai
        [MaxLength(128)]
        public string CustomModelName { get; set; } // to be used for Open Ai custom models.
        [MaxLength(128)]
        public string FineTuneDataFileName { get; set; } // The file name used for fine tuning the model.

        //*************************************************** End Ai Fields *****************************************************************

        [MaxLength(128)]
        public string WebhookURL { get; set; }
        public int AutomationLevel { get; set; } // 0=Never, 1= Confidence_Level, 3= Always
        public float ConfidenceScoreThresold { get; set; }
        //public bool FlgImportModeratedOnly { get; set; } // Import docs only if they have been moderated.

        //[ForeignKey(nameof(sysDocTypeObj))]
        //public int DocTypeId { get; set; }
        //public virtual SysDocType sysDocTypeObj { get; set; }

        [ForeignKey(nameof(AccObj))]
        public int AccountId { get; set; }
        public virtual Account AccObj { get; set; }

        [ForeignKey(nameof(DocStorageObj))]
        public int? StorageId { get; set; }
        public virtual DocStorage DocStorageObj { get; set; }

        [ForeignKey(nameof(NanoModelObj))]
        public int? ModelId { get; set; }
        public virtual NanoModel NanoModelObj { get; set; }

        // 🔹 Navigation to UserQueue (child collection)
        public virtual ICollection<UserQueue> UserQueues { get; set; }
        public virtual ICollection<Document> Documents { get; set; }
    }

    public class UserQueue : BaseEntity
    {
        [ForeignKey(nameof(QueueObj))]
        public int QueueId { get; set; }
        public virtual Queue QueueObj { get; set; }

        [ForeignKey(nameof(UserObj))]
        public int UserId { get; set; }
        public virtual User UserObj { get; set; }
    }

}
