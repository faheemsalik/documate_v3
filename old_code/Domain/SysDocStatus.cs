using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Documate.Domain
{
    public class SysDocStatus : BaseEntity
    {
        [MaxLength(20)]
        public string Status { get; set; }
        [MaxLength(20)]
        public string StatusKey { get; set; }
        public int Order { get; set; }

        public virtual ICollection<Document> Documents { get; set; }
    }
}
