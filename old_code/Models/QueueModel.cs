using Documate.Domain;

using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Documate.Models
{
    [NotMapped]
    [DebuggerDisplay("Queue:{QueueName}-Acc:{AccountName}- QueueId:{QueueId}")]
    public class QueueModel:Queue
    {
        public string SchemaName { get; set; }
        public string AccountName { get; set; }
        public string NanoModelId { get; set; }
        public string S3BucketName { get; set; }
        
    }

    [NotMapped]
    public class UserQueueModel : UserQueue
    {
    }
    [DebuggerDisplay("{TemplateId}-{DocIdentifier}- QueueId:{QueueId}")]
    public class TemplateQueueModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string DocIdentifier { get; set; }

        public int TemplateQueueId { get; set; }
        public int QueueId { get; set; }
    }

    public enum EnumAiServiceSource
    {
        AWS = 0,
        ROSSUM = 1,
        GOOGLE = 2,
        NANO = 3,
        OPENAI = 4,
        CLAUDE = 5,
        GEMINI = 6,
    }
    public enum EnumRawTextService
    {
        GOOGLE=0,
        AWS=1
    }
}
