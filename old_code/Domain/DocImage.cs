using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Documate.Domain
{

    public class DocImage : BaseEntity
    {
        [MaxLength(128)]
        public string FileName { get; set; }
        public int PageNbr { get; set; }
        [MaxLength(128)]
        public string S3ObjectRef { get; set; }

        [ForeignKey(nameof(DocObj))]
        public int DocId { get; set; }
        public virtual Document DocObj { get; set; }
    }
}
