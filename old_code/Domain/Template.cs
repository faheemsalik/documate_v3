using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Documate.Domain
{
    [DebuggerDisplay("{Id}- {TemplateName}- {SenderName}")]
    public class Template : BaseEntity
    {
        [MaxLength(50)]
        public string TemplateName { get; set; }
        [MaxLength(200)]
        public string DocIdentifier { get; set; }
        [MaxLength(200)]
        public string SenderName { get; set; }
        [MaxLength(128)]
        public string PictureFileName { get; set; }
        public string Rule { get; set; }
    }

    public class TemplateQueue : BaseEntity
    {
        [ForeignKey(nameof(QueueObj))]
        public int QueueId { get; set; }
        public virtual Queue QueueObj { get; set; }

        [ForeignKey(nameof(TemplateObj))]
        public int TemplateId { get; set; }
        public virtual Template TemplateObj { get; set; }

    }

}
