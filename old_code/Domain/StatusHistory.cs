using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Documate.Domain
{
    public class StatusHistory : BaseEntity
    {
        [ForeignKey(nameof(SysDocStatusObj))]
        public int StatusId { get; set; }
        public virtual SysDocStatus SysDocStatusObj { get; set; }

        [ForeignKey(nameof(DocObj))]
        public int DocId { get; set; }
        public virtual Document DocObj { get; set; }

    }
}
